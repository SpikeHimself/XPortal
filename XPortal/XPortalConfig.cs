using BepInEx.Configuration;
using System;

namespace XPortal
{
    public sealed class XPortalConfig
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<XPortalConfig> lazy = new Lazy<XPortalConfig>(() => new XPortalConfig());
        public static XPortalConfig Instance { get { return lazy.Value; } }
        ////////////////////////////

        private ConfigFile configFile;

        /// <summary>
        /// Container class for *all* of XPortal's grand total of ONE config setting(s) :)
        /// </summary>
        public class ConfigSettings
        {
            public bool PingMapDisabled;
        }

        /// <summary>
        /// Track local config settings
        /// </summary>
        public ConfigSettings Local { get; set; }

        /// <summary>
        /// Track Server config settings
        /// </summary>
        public ConfigSettings Server { get; set; }

        private XPortalConfig()
        {
            Local = new ConfigSettings();
            Server = new ConfigSettings();
        }

        /// <summary>
        /// Load the config file, and track the settings inside it
        /// </summary>
        /// <param name="config"></param>
        public void LoadConfigFile(ConfigFile config)
        {
            configFile = config;

            ReloadConfig();

            configFile.ConfigReloaded += ConfigFile_ConfigReloaded;
            configFile.SettingChanged += Config_SettingChanged;
        }

        /// <summary>
        /// Reload the settings inside the config file
        /// </summary>
        private void ReloadConfig()
        {
            // Add Nexus ID to config for Nexus Update Check (https://www.nexusmods.com/valheim/mods/102)
            configFile.Bind<int>("General", "NexusID", XPortal.PluginNexusId, "Nexus mod ID for updates (do not change)");

            // Add PingMapDisabled option which disables the Ping Map button
            var cfgPingMapDisabled = configFile.Bind<bool>("General", "PingMapDisabled", false, "Disable the Ping Map button completely. For players who wish to play without a map.");
            Local.PingMapDisabled = cfgPingMapDisabled.Value;
            Server.PingMapDisabled = cfgPingMapDisabled.Value;

        }

        /// <summary>
        /// The config file was reloaded on the server. Read settings from it and send them to clients.
        /// </summary>
        private void ConfigFile_ConfigReloaded(object sender, EventArgs e)
        {
            if (!XPortal.IsServer())
            {
                return;
            }

            ReloadConfig();

            Jotunn.Logger.LogDebug("[ConfigFile_ConfigReloaded] The config file was reloaded, propagating to clients..");
            RPC.SendConfigToClients(Pack());
        }

        /// <summary>
        /// One of the settings was changed on the server. Read settings from the config file and send them to clients.
        /// </summary>
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (!XPortal.IsServer())
            {
                return;
            }

            ReloadConfig();

            Jotunn.Logger.LogDebug("[Config_SettingChanged] A config value has changed, propagating to clients..");
            RPC.SendConfigToClients(Pack());
        }

        /// <summary>
        /// Wrap the config settings into a package
        /// </summary>
        /// <returns>A ZPackage containing all config settings</returns>
        public ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(Local.PingMapDisabled);
            return pkg;
        }

        /// <summary>
        /// Set our config settings based on the package we received from the server
        /// </summary>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public void ReceiveFromServer(ZPackage pkg)
        {
            Server.PingMapDisabled = pkg.ReadBool();
            Jotunn.Logger.LogDebug($"[ReceiveFromServer] PingMapDisabled {{ Local: {Local.PingMapDisabled}, Server: {Server.PingMapDisabled} }}");
        }

    }
}
