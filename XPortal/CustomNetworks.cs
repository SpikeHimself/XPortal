using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx;
using XPortal.RPC;

namespace XPortal
{
    /// <summary>Configured portal networks (ids 1–15). Id 0 is normal Global.</summary>
    internal static class CustomNetworks
    {
        internal const int MinId = 1;
        internal const int MaxId = 15;

        internal const string ConfigFileName = "xportal_networks.json";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static readonly Dictionary<long, string> ActiveById = new Dictionary<long, string>();

        /// <summary>Extracts <c>id</c> / <c>name</c> pairs from the JSON (keys must appear as <c>"id"</c> then <c>"name"</c> per entry).</summary>
        private static readonly Regex NetworkEntryRegex = new Regex(
            @"""id""\s*:\s*(\d+)\s*,\s*""name""\s*:\s*""((?:[^""\\]|\\.)*)""",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static FileSystemWatcher _watcher;
        private static readonly object ReloadGate = new object();
        private static readonly object ReloadTimerLock = new object();
        private static SynchronizationContext _mainThreadContext;
        private static global::System.Threading.Timer _reloadCoalesceTimer;
        private const int CoalesceDelayMs = 400;

        /// <summary>Fired when the active network list changes.</summary>
        internal static event Action ListChanged;

        #region Registry (client + server)

        internal static void ResetSession()
        {
            lock (ActiveById)
            {
                ActiveById.Clear();
            }
        }

        internal static void ServerSetFromParsed(Dictionary<long, string> parsed)
        {
            lock (ActiveById)
            {
                ActiveById.Clear();
                foreach (var kv in parsed.OrderBy(k => k.Key))
                {
                    ActiveById[kv.Key] = kv.Value;
                }
            }
        }

        internal static void ApplyFromServer(ZPackage pkg)
        {
            var n = pkg.ReadInt();
            lock (ActiveById)
            {
                ActiveById.Clear();
                for (var i = 0; i < n; i++)
                {
                    var id = pkg.ReadLong();
                    var name = pkg.ReadString();
                    if (id < MinId || id > MaxId || string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    ActiveById[id] = name;
                }
            }

            ListChanged?.Invoke();
        }

        internal static ZPackage PackForServer()
        {
            List<KeyValuePair<long, string>> snapshot;
            lock (ActiveById)
            {
                snapshot = ActiveById.OrderBy(k => k.Key).ToList();
            }

            var pkg = new ZPackage();
            pkg.Write(snapshot.Count);
            foreach (var kv in snapshot)
            {
                pkg.Write(kv.Key);
                pkg.Write(kv.Value);
            }

            return pkg;
        }

        internal static bool IsActiveId(long id)
        {
            if (id < MinId || id > MaxId)
            {
                return false;
            }

            lock (ActiveById)
            {
                return ActiveById.ContainsKey(id);
            }
        }

        internal static bool TryGetDisplayName(long id, out string displayName)
        {
            lock (ActiveById)
            {
                return ActiveById.TryGetValue(id, out displayName);
            }
        }

        internal static List<long> GetSortedActiveIds()
        {
            lock (ActiveById)
            {
                return ActiveById.Keys.OrderBy(k => k).ToList();
            }
        }

        /// <summary>True if id is in the 1–15 configured range.</summary>
        internal static bool IsReservedIdRange(long id)
        {
            return id >= MinId && id <= MaxId;
        }

        internal static void MigrateInvalidNetworks()
        {
            if (!Environment.IsServer)
            {
                return;
            }

            foreach (var p in KnownPortalsManager.Instance.GetList().ToList())
            {
                if (!IsReservedIdRange(p.NetworkOwnerPlayerId))
                {
                    continue;
                }

                if (IsActiveId(p.NetworkOwnerPlayerId))
                {
                    continue;
                }

                p.NetworkOwnerPlayerId = 0L;
                p.NetworkOwnerDisplayName = string.Empty;
                KnownPortalsManager.Instance.AddOrUpdate(p);
                ZdoTools.UpdateFromKnownPortal(state: p);
                SendToClient.SyncPortal(p);
                Log.Info($"Migrated portal `{p.Id}` to Global network (network id was removed or invalid).");
            }
        }

        internal static void NotifyListChangedLocal()
        {
            ListChanged?.Invoke();
        }

        #endregion

        #region Server: file + watcher

        internal static void InitializeServer()
        {
            if (!Environment.IsServer)
            {
                return;
            }

            _mainThreadContext = SynchronizationContext.Current;

            EnsureDefaultConfigExists();
            ReloadFromDiskAndBroadcast(isInitial: true);

            var dir = Path.GetDirectoryName(GetConfigFilePath());
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            try
            {
                _watcher?.Dispose();
                _watcher = new FileSystemWatcher(dir)
                {
                    Filter = ConfigFileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                };
                _watcher.Changed += OnWatcherEvent;
                _watcher.Created += OnWatcherEvent;
                _watcher.Renamed += OnWatcherRenamed;
                _watcher.EnableRaisingEvents = true;
                Log.Debug($"Watching `{dir}` for `{ConfigFileName}` changes.");
            }
            catch (Exception ex)
            {
                Log.Error($"Could not watch custom networks config folder: {ex.Message}");
            }
        }

        internal static void ShutdownServer()
        {
            lock (ReloadTimerLock)
            {
                _reloadCoalesceTimer?.Dispose();
                _reloadCoalesceTimer = null;
            }

            _watcher?.Dispose();
            _watcher = null;
        }

        private static string GetConfigFilePath()
        {
            return Path.Combine(Paths.ConfigPath, Mod.Info.Name, ConfigFileName);
        }

        /// <summary>Default JSON baked into the assembly (see csproj EmbeddedResource).</summary>
        private static string ReadEmbeddedTemplate()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var resName in asm.GetManifestResourceNames())
                {
                    if (!resName.EndsWith(ConfigFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (var s = asm.GetManifestResourceStream(resName))
                    {
                        if (s == null)
                        {
                            continue;
                        }

                        using (var r = new StreamReader(s, Utf8NoBom))
                        {
                            return r.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to read embedded `{ConfigFileName}` from assembly: {ex.GetType().Name}: {ex.Message}");
            }

            return null;
        }

        private static void EnsureDefaultConfigExists()
        {
            var path = GetConfigFilePath();
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(path))
                {
                    return;
                }

                var templateText = ReadEmbeddedTemplate();
                if (!string.IsNullOrEmpty(templateText))
                {
                    File.WriteAllText(path, templateText, Utf8NoBom);
                    Log.Info($"Created `{path}` from embedded `{ConfigFileName}`.");
                    return;
                }

                Log.Error($"Embedded default template `{ConfigFileName}` not found; cannot create `{path}`.");
            }
            catch (Exception ex)
            {
                Log.Error($"Could not create default custom networks file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the config file from disk. Returns false if the file does not exist (not an error).
        /// Sets <paramref name="readError"/> true when the file exists but could not be read.
        /// </summary>
        private static bool TryReadConfigFile(out string text, out bool readError)
        {
            text = null;
            readError = false;
            var path = GetConfigFilePath();
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                text = File.ReadAllText(path, Utf8NoBom);
                return true;
            }
            catch (Exception ex)
            {
                readError = File.Exists(path);
                Log.Error($"Could not read custom networks file `{path}`: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static Dictionary<long, string> ParseConfigJson(string raw)
        {
            var result = new Dictionary<long, string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }

            raw = StripUtf8Bom(raw.Trim());

            var invalidId = 0;
            var emptyOrSanitizedName = 0;
            var duplicateId = 0;
            var decodeErrors = 0;

            try
            {
                foreach (Match m in NetworkEntryRegex.Matches(raw))
                {
                    if (!int.TryParse(m.Groups[1].Value, out var id) || id < MinId || id > MaxId)
                    {
                        invalidId++;
                        continue;
                    }

                    string name;
                    try
                    {
                        name = UnescapeJsonString(m.Groups[2].Value);
                    }
                    catch (Exception ex)
                    {
                        decodeErrors++;
                        Log.Warning($"Custom networks JSON: skipped entry for id {id} (invalid escape sequence in name): {ex.GetType().Name}: {ex.Message}");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        emptyOrSanitizedName++;
                        continue;
                    }

                    name = PortalNetwork.SanitizeNetworkOwnerDisplayName(name);
                    if (string.IsNullOrEmpty(name))
                    {
                        emptyOrSanitizedName++;
                        continue;
                    }

                    var idLong = (long)id;
                    if (result.ContainsKey(idLong))
                    {
                        duplicateId++;
                        continue;
                    }

                    result.Add(idLong, name);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Custom networks JSON: unexpected failure while scanning `{ConfigFileName}`: {ex.GetType().Name}: {ex.Message}");
                return result;
            }

            if (invalidId > 0)
            {
                Log.Warning($"Custom networks JSON: skipped {invalidId} {(invalidId == 1 ? "entry" : "entries")} with id outside {MinId}–{MaxId}.");
            }

            if (emptyOrSanitizedName > 0)
            {
                Log.Warning($"Custom networks JSON: skipped {emptyOrSanitizedName} {(emptyOrSanitizedName == 1 ? "entry" : "entries")} with empty or invalid name after sanitization.");
            }

            if (duplicateId > 0)
            {
                Log.Warning($"Custom networks JSON: skipped {duplicateId} duplicate id {(duplicateId == 1 ? "entry" : "entries")}.");
            }

            // Non-trivial content but nothing usable — likely malformed structure, wrong key order, or bad syntax.
            if (result.Count == 0 && raw.Length > 2)
            {
                Log.Warning(
                    $"Custom networks JSON: no valid entries found in `{ConfigFileName}`. Expected patterns like \"id\": 1, \"name\": \"...\" with ids in {MinId}–{MaxId}. Check the file format.");
            }

            return result;
        }

        private static string StripUtf8Bom(string s)
        {
            if (string.IsNullOrEmpty(s) || s[0] != '\uFEFF')
            {
                return s;
            }

            return s.Substring(1);
        }

        private static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('\\') < 0)
            {
                return s;
            }

            try
            {
                var sb = new StringBuilder(s.Length);
                for (var i = 0; i < s.Length; i++)
                {
                    if (s[i] != '\\' || i + 1 >= s.Length)
                    {
                        sb.Append(s[i]);
                        continue;
                    }

                    i++;
                    switch (s[i])
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '/':
                            sb.Append('/');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (i + 4 < s.Length
                                && uint.TryParse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out var code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                            else
                            {
                                sb.Append('u');
                            }

                            break;
                        default:
                            sb.Append(s[i]);
                            break;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unescape JSON string fragment.", ex);
            }
        }

        private static bool IsOurConfigFile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return name.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static void OnWatcherRenamed(object sender, RenamedEventArgs e)
        {
            if (IsOurConfigFile(e.Name))
            {
                QueueReload();
            }
        }

        private static void OnWatcherEvent(object sender, FileSystemEventArgs e)
        {
            if (IsOurConfigFile(e.Name))
            {
                QueueReload();
            }
        }

        private static void QueueReload()
        {
            lock (ReloadTimerLock)
            {
                _reloadCoalesceTimer?.Dispose();
                _reloadCoalesceTimer = new global::System.Threading.Timer(
                    _ => OnCoalesceTimerFired(),
                    null,
                    CoalesceDelayMs,
                    Timeout.Infinite);
            }
        }

        private static void OnCoalesceTimerFired()
        {
            try
            {
                var ctx = _mainThreadContext;
                if (ctx != null)
                {
                    ctx.Post(
                        _ =>
                        {
                            try
                            {
                                ReloadFromDiskAndBroadcast(isInitial: false);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Custom networks reload failed: {ex.Message}");
                            }
                        },
                        null);
                }
                else
                {
                    Log.Warning("No synchronization context; reloading custom networks on the watcher thread (may be unsafe).");
                    ReloadFromDiskAndBroadcast(isInitial: false);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Custom networks reload scheduling failed: {ex.Message}");
            }
        }

        private static void ReloadFromDiskAndBroadcast(bool isInitial)
        {
            lock (ReloadGate)
            {
                if (!TryReadConfigFile(out var text, out var readError))
                {
                    if (!readError)
                    {
                        EnsureDefaultConfigExists();
                    }

                    if (!TryReadConfigFile(out text, out readError) || text == null)
                    {
                        if (readError)
                        {
                            Log.Error(
                                $"Keeping the previous custom network list; fix `{GetConfigFilePath()}` and save, or restart the server after correcting the file.");
                            return;
                        }

                        text = string.Empty;
                    }
                }

                var parsed = ParseConfigJson(text ?? string.Empty);
                ServerSetFromParsed(parsed);
                MigrateInvalidNetworks();

                if (!isInitial)
                {
                    SendToClient.BroadcastCustomNetworks(PackForServer());
                }

                if (!isInitial)
                {
                    Log.Info("Custom networks file reloaded and pushed to clients.");
                }

                if (!Environment.IsHeadless)
                {
                    NotifyListChangedLocal();
                }
            }
        }

        #endregion
    }
}
