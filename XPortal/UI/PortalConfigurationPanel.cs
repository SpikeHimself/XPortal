using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using Mod;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XPortal.UI
{
    internal sealed class PortalConfigurationPanel : IDisposable
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<PortalConfigurationPanel> lazy = new Lazy<PortalConfigurationPanel>(() => new PortalConfigurationPanel());
        public static PortalConfigurationPanel Instance { get { return lazy.Value; } }
        ////////////////////////////

        internal const string GO_MAINPANEL = Info.Name + "_MainPanel";
        internal const string GO_HEADERTEXT = Info.Name + "_PanelHeader";
        internal const string GO_NAMELABEL = Info.Name + "_NameHeader";
        internal const string GO_NAMEINPUT = Info.Name + "_NameInput";
        internal const string GO_DESTINATIONLABEL = Info.Name + "_DestinationHeader";
        internal const string GO_DESTINATIONDROPDOWN = Info.Name + "_DestinationDropdown";
        internal const string GO_DESTINATIONGAMEPADHINT = Info.Name + "_DestinationGamepadHint";
        internal const string GO_NETWORKASSIGNLISTNAVHINT = Info.Name + "_NetworkAssignListNavHint";
        internal const string GO_DESTINATIONNETWORKLISTNAVHINT = Info.Name + "_DestinationNetworkListNavHint";
        internal const string GO_PINGMAPBUTTON = Info.Name + "_PingMapButton";
        internal const string GO_DEFAULTPORTALLABEL = Info.Name + "_DefaultPortalHeader";
        internal const string GO_DEFAULTPORTALCHECKBOX = Info.Name + "_DefaultPortalCheckbox";
        internal const string GO_PRIVATEPORTALLABEL = Mod.Info.Name + "_PrivatePortalHeader";
        internal const string GO_PRIVATEPORTALCHECKBOX = Mod.Info.Name + "_PrivatePortalCheckbox";
        internal const string GO_OKAYBUTTON = Info.Name + "_OkayButton";
        internal const string GO_CANCELBUTTON = Info.Name + "_CancelButton";
        internal const string GO_NETWORKASSIGNLABEL = Mod.Info.Name + "_NetworkAssignHeader";
        internal const string GO_NETWORKASSIGNDROPDOWN = Mod.Info.Name + "_NetworkAssignDropdown";
        internal const string GO_DESTINATIONNETWORKLABEL = Mod.Info.Name + "_DestinationNetworkHeader";
        internal const string GO_DESTINATIONNETWORKDROPDOWN = Mod.Info.Name + "_DestinationNetworkDropdown";

        #region Pain
        // Creating the UI was incredibly painful. I will never change the layout again. Ever.
        // ...but we can use some variables to tweak widths, heights, offsets, and such

        // Essentially it's something like this:

        //////////////////////////////////////////
        //               {header}               //
        //                                      //
        //  {label}     {i n p u t - l o n g}   //
        //  {label}     {input-short} {button}  //
        //  {label}     {chk}                   //
        //                                      //
        //                        {cancel} {ok} //
        //////////////////////////////////////////

        // So apart from the header and footer, we can define locations and offsets for "rows", "columns", "padding", ..
        static readonly float padding = 24f;
        static readonly float rowHeight = 32f;
        static readonly float labelWidth = 160f;
        static readonly float buttonWidth = 90f;
        static readonly float submitButtonWidth = 110f;
        static readonly float submitButtonHeight = 48f;
        static readonly float inputShortWidth = 460f;
        static readonly float inputLongWidth = inputShortWidth + padding + buttonWidth;
        static readonly float rowStep = rowHeight + padding;
        static readonly float networkAssignRowTop = -60f - padding;
        static readonly float nameRowTop = networkAssignRowTop - rowStep;
        static readonly float destinationNetworkRowTop = nameRowTop - rowStep;
        static readonly float destinationPortalRowTop = destinationNetworkRowTop - rowStep;
        static readonly float privatePortalRowTop = destinationPortalRowTop - rowStep;
        static readonly float defaultPortalRowTop = privatePortalRowTop - rowStep;
        static readonly float firstColumnLeft = 0f + padding;
        static readonly float secondColumnLeft = firstColumnLeft + labelWidth + padding;
        // Great. Anyway, let's move on now..
        #endregion

        private GameObject mainPanel;
        private GameObject pingMapButtonObject;
        private GameObject targetPortalDropdownObject;
        private Dropdown targetPortalDropdown;
        private readonly List<GameObject> dropdownListNavHints = new List<GameObject>();
        private InputField portalNameInputField;
        private Toggle defaultPortalToggle;
        private Toggle privatePortalToggle;
        private Button okayButton;
        private Dropdown networkAssignmentDropdown;
        private Dropdown destinationNetworkDropdown;

        // A look-up list to find the portal ZDOID by dropdown list index
        private readonly Dictionary<int, ZDOID> dropdownIndexToZDOIDMapping;
        private readonly Dictionary<int, long> destinationNetworkIndexToOwnerId = new Dictionary<int, long>();
        private readonly Dictionary<int, long> networkAssignmentIndexToOwnerId = new Dictionary<int, long>();
        private int personalNetworkAssignmentIndex;

        // The KnownPortal being configured
        private KnownPortal thisPortal;

        // The ZDOID of the target that was selected in the dropdown
        private ZDOID selectedTargetId;

        private long selectedDestinationNetworkOwnerId;
        private bool canEditNetworkAssignment;
        private bool canEditPortalFully;
        private long personalNetworkOwnerId;
        private bool readOnlyPrivatePortal;

        /// <summary>Piece creator id; personal network row uses this owner (e.g. admin editing another player's portal).</summary>
        private long pieceCreatorPlayerId;

        #region Input Button Configs
        private ButtonConfig uiDropdownScrollUpButton;
        private ButtonConfig uiDropdownScrollDownButton;
        #endregion

        // Open list, if any (managed dropdowns only).
        public Dropdown ExpandedDropdown { get; private set; }

        public bool DropdownExpanded => ExpandedDropdown != null;

        private static ScrollRect listScrollCache;

        private Dropdown lastSyncedScrollDropdown;

        private int lastSyncedListScroll = int.MinValue;

        private float listNavNextTime;

        private const float ListNavInitialDelay = 0.22f;

        private const float ListNavRepeatInterval = 0.065f;

        private const float ListWheelSensFloor = 520f;

        private const float ListBottomSlackPx = 14f;

        private const string ListBottomSpacerName = "XPortal_DropdownListBottomSpacer";

        private const float ListBottomSpacerH = 64f;

        private sealed class ListFocusState
        {
            public Dropdown Target;

            public int Row;
        }

        private static FieldInfo dropdownItemsField;

        private PortalConfigurationPanel()
        {
            dropdownIndexToZDOIDMapping = new Dictionary<int, ZDOID>();
        }

        internal static bool IsManagedDropdown(Dropdown d)
        {
            return d != null && IsManagedDropdownName(d.name);
        }

        internal static bool IsManagedDropdownName(string name)
        {
            return name == GO_DESTINATIONDROPDOWN
                || name == GO_NETWORKASSIGNDROPDOWN
                || name == GO_DESTINATIONNETWORKDROPDOWN;
        }

        internal void SetExpandedDropdown(Dropdown d)
        {
            ExpandedDropdown = d;
        }

        internal void ClearExpandedDropdownIf(Dropdown d)
        {
            if (ExpandedDropdown == d)
            {
                ExpandedDropdown = null;
            }
        }

        internal static void QueueListScroll(Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            QueuedAction.Queue(DeferredListScroll, delay: 1, state: dropdown);
            QueuedAction.Queue(DeferredListScroll, delay: 2, state: dropdown);
        }

        private static void DeferredListScroll(bool unused, object state)
        {
            ApplyListScroll(state as Dropdown);
        }

        internal static void ApplyListScroll(Dropdown dropdown, bool rebuildLayout = true)
        {
            if (dropdown?.options == null || dropdown.options.Count == 0)
            {
                return;
            }

            ScrollRect scrollRect = FindListScrollRect(dropdown);
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.scrollSensitivity = Mathf.Max(scrollRect.scrollSensitivity, ListWheelSensFloor);

            int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            RectTransform content = scrollRect.content;
            if (content == null || content.childCount == 0)
            {
                return;
            }

            if (rebuildLayout)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Canvas.ForceUpdateCanvases();
            }

            int optionCount = dropdown.options.Count;
            int lastItemRow = Mathf.Max(0, optionCount - 1);
            int scrollIndex = Mathf.Clamp(index, 0, lastItemRow);

            float normalized;
            if (lastItemRow <= 0)
            {
                normalized = 1f;
            }
            else if (scrollIndex >= lastItemRow)
            {
                normalized = 0f;
            }
            else if (scrollIndex <= 0)
            {
                normalized = 1f;
            }
            else
            {
                normalized = 1f - scrollIndex / (float)lastItemRow;
            }

            SetScrollNorm(scrollRect, normalized);

            if (scrollIndex >= content.childCount)
            {
                FocusListRow(dropdown, scrollIndex);
                QueueListFocus(dropdown, scrollIndex);
                return;
            }

            RectTransform itemRt = content.GetChild(scrollIndex) as RectTransform;
            NudgeRowIntoView(scrollRect, content, itemRt);

            FocusListRow(dropdown, scrollIndex);
            QueueListFocus(dropdown, scrollIndex);
        }

        private static Toggle ItemToggle(Dropdown dropdown, int index)
        {
            if (dropdown == null || index < 0)
            {
                return null;
            }

            if (dropdownItemsField == null)
            {
                dropdownItemsField = typeof(Dropdown).GetField("m_Items", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (dropdownItemsField == null)
            {
                return null;
            }

            IList items = dropdownItemsField.GetValue(dropdown) as IList;
            if (items == null || index >= items.Count)
            {
                return null;
            }

            object entry = items[index];
            if (entry == null)
            {
                return null;
            }

            Type entryType = entry.GetType();
            FieldInfo toggleField = entryType.GetField("toggle", BindingFlags.Instance | BindingFlags.Public);
            if (toggleField != null)
            {
                return toggleField.GetValue(entry) as Toggle;
            }

            PropertyInfo toggleProp = entryType.GetProperty("toggle", BindingFlags.Instance | BindingFlags.Public);
            return toggleProp?.GetValue(entry, null) as Toggle;
        }

        private static void FocusListRow(Dropdown dropdown, int rowIndex)
        {
            if (Instance == null || Instance.ExpandedDropdown != dropdown || dropdown == null)
            {
                return;
            }

            if (rowIndex < 0 || rowIndex >= dropdown.options.Count)
            {
                return;
            }

            Toggle toggle = ItemToggle(dropdown, rowIndex);
            if (toggle == null)
            {
                ScrollRect sr = FindListScrollRect(dropdown);
                RectTransform content = sr?.content;
                if (content != null && rowIndex < content.childCount)
                {
                    Transform row = content.GetChild(rowIndex);
                    toggle = row.GetComponent<Toggle>() ?? row.GetComponentInChildren<Toggle>(true);
                }
            }

            if (toggle == null)
            {
                return;
            }

            EventSystem es = EventSystem.current;
            if (es != null)
            {
                es.SetSelectedGameObject(toggle.gameObject);
            }
            else
            {
                toggle.Select();
            }
        }

        private static void QueueListFocus(Dropdown dropdown, int rowIndex)
        {
            if (dropdown == null)
            {
                return;
            }

            var st = new ListFocusState { Target = dropdown, Row = rowIndex };
            QueuedAction.Queue(DeferredListFocus, delay: 0, state: st);
            QueuedAction.Queue(DeferredListFocus, delay: 1, state: st);
            QueuedAction.Queue(DeferredListFocus, delay: 2, state: st);
        }

        private static void DeferredListFocus(bool unused, object state)
        {
            ListFocusState s = state as ListFocusState;
            if (s == null)
            {
                return;
            }

            FocusListRow(s.Target, s.Row);
        }

        internal static void ClearListScroll()
        {
            listScrollCache = null;
            if (Instance != null)
            {
                Instance.lastSyncedScrollDropdown = null;
                Instance.lastSyncedListScroll = int.MinValue;
                Instance.listNavNextTime = 0f;
            }
        }

        internal void SyncListScroll()
        {
            if (ExpandedDropdown == null)
            {
                return;
            }

            int v = ExpandedDropdown.value;
            if (ExpandedDropdown == lastSyncedScrollDropdown && v == lastSyncedListScroll)
            {
                return;
            }

            lastSyncedScrollDropdown = ExpandedDropdown;
            lastSyncedListScroll = v;
            ApplyListScroll(ExpandedDropdown, rebuildLayout: true);
        }

        private static MethodInfo scrollRectUpdateBounds;

        private static void UpdateScrollBounds(ScrollRect scrollRect)
        {
            if (scrollRectUpdateBounds == null)
            {
                scrollRectUpdateBounds = typeof(ScrollRect).GetMethod("UpdateBounds", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            scrollRectUpdateBounds?.Invoke(scrollRect, null);
        }

        private static void SetScrollNorm(ScrollRect scrollRect, float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            scrollRect.verticalNormalizedPosition = normalized;
            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;

            UpdateScrollBounds(scrollRect);
            Canvas.ForceUpdateCanvases();

            if (scrollRect.verticalScrollbar != null)
            {
                scrollRect.verticalScrollbar.SetValueWithoutNotify(normalized);
            }
        }

        private static Camera CanvasCamera(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        private static void ExtentsFromBounds(RectTransform item, Camera cam, Bounds lb, out float worldBottom, out float worldTop, out float screenMinY, out float screenMaxY)
        {
            Vector3 c = lb.center;
            Vector3 e = lb.extents;
            worldBottom = float.MaxValue;
            worldTop = float.MinValue;
            screenMinY = float.MaxValue;
            screenMaxY = float.MinValue;

            for (int a = 0; a < 8; a++)
            {
                Vector3 local = c + new Vector3(
                    (a & 1) != 0 ? e.x : -e.x,
                    (a & 2) != 0 ? e.y : -e.y,
                    (a & 4) != 0 ? e.z : -e.z);
                Vector3 w = item.TransformPoint(local);
                worldBottom = Mathf.Min(worldBottom, w.y);
                worldTop = Mathf.Max(worldTop, w.y);
                float sy = RectTransformUtility.WorldToScreenPoint(cam, w).y;
                screenMinY = Mathf.Min(screenMinY, sy);
                screenMaxY = Mathf.Max(screenMaxY, sy);
            }
        }

        private static float CornersMinScreenY(Vector3[] corners, Camera cam)
        {
            float m = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                m = Mathf.Min(m, RectTransformUtility.WorldToScreenPoint(cam, corners[i]).y);
            }

            return m;
        }

        private static float CornersMaxScreenY(Vector3[] corners, Camera cam)
        {
            float m = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                m = Mathf.Max(m, RectTransformUtility.WorldToScreenPoint(cam, corners[i]).y);
            }

            return m;
        }

        private static void NudgeRowIntoView(ScrollRect scrollRect, RectTransform content, RectTransform item)
        {
            RectTransform viewport = scrollRect.viewport;
            if (viewport == null || content == null || item == null)
            {
                return;
            }

            Canvas canvas = viewport.GetComponentInParent<Canvas>();
            Camera cam = CanvasCamera(canvas);

            RectTransform parentRt = content.parent as RectTransform;

            const int maxSteps = 28;
            const float epsWorld = 0.5f;

            Vector3[] viewportCorners = new Vector3[4];

            for (int step = 0; step < maxSteps; step++)
            {
                Canvas.ForceUpdateCanvases();

                Bounds itemLocalBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(item);
                ExtentsFromBounds(item, cam, itemLocalBounds, out float itemBottom, out float itemTop, out float itemBottomScreen, out float itemTopScreen);

                viewport.GetWorldCorners(viewportCorners);
                float viewBottom = Mathf.Min(viewportCorners[0].y, viewportCorners[3].y);
                float viewTop = Mathf.Max(viewportCorners[1].y, viewportCorners[2].y);

                float slackWorld = 0f;
                if (canvas != null)
                {
                    float viewPixelH = Mathf.Max(1f, viewport.rect.height * canvas.scaleFactor);
                    float viewWorldH = Mathf.Abs(viewTop - viewBottom);
                    slackWorld = ListBottomSlackPx / viewPixelH * Mathf.Max(viewWorldH, 0.0001f);
                }

                float viewBottomScreen = CornersMinScreenY(viewportCorners, cam);
                float viewTopScreen = CornersMaxScreenY(viewportCorners, cam);

                bool fitsBottom = itemBottomScreen >= viewBottomScreen - ListBottomSlackPx - 0.5f;
                bool fitsTop = itemTopScreen <= viewTopScreen + 0.5f;

                if (itemBottom >= viewBottom - epsWorld - slackWorld && itemTop <= viewTop + epsWorld && fitsBottom && fitsTop)
                {
                    scrollRect.StopMovement();
                    scrollRect.velocity = Vector2.zero;
                    return;
                }

                float shiftWorldY = 0f;
                if (itemBottom < viewBottom - epsWorld - slackWorld || !fitsBottom)
                {
                    shiftWorldY = viewBottom - itemBottom - slackWorld;
                }
                else if (itemTop > viewTop + epsWorld || !fitsTop)
                {
                    shiftWorldY = viewTop - itemTop;
                }

                if (Mathf.Abs(shiftWorldY) < 0.005f)
                {
                    break;
                }

                if (parentRt != null)
                {
                    Vector2 local = parentRt.InverseTransformVector(new Vector3(0f, shiftWorldY, 0f));
                    content.anchoredPosition += new Vector2(0f, local.y);
                }
                else
                {
                    Vector3 ls = content.InverseTransformVector(new Vector3(0f, shiftWorldY, 0f));
                    content.anchoredPosition += new Vector2(0f, ls.y);
                }

                scrollRect.StopMovement();
                scrollRect.velocity = Vector2.zero;

                UpdateScrollBounds(scrollRect);
            }

            Canvas.ForceUpdateCanvases();
        }

        private static ScrollRect FindListScrollRect(Dropdown dropdown)
        {
            if (listScrollCache != null && listScrollCache)
            {
                return listScrollCache;
            }

            Transform root = dropdown.transform.root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (t.name.IndexOf("Dropdown List", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                ScrollRect sr = t.GetComponent<ScrollRect>() ?? t.GetComponentInChildren<ScrollRect>(true);
                if (sr != null && sr.content != null)
                {
                    listScrollCache = sr;
                    return sr;
                }
            }

            FieldInfo field = typeof(Dropdown).GetField("m_Dropdown", BindingFlags.Instance | BindingFlags.NonPublic);
            object raw = field?.GetValue(dropdown);
            GameObject go = raw as GameObject ?? (raw as Component)?.gameObject;
            if (go == null || !go.activeInHierarchy)
            {
                return null;
            }

            ScrollRect found = go.GetComponent<ScrollRect>() ?? go.GetComponentInChildren<ScrollRect>(true);
            if (found != null)
            {
                listScrollCache = found;
            }

            return found;
        }

        #region Input
        internal void AddInputs()
        {
            uiDropdownScrollUpButton = AddInput("XPortal_DropdownScrollUp", "$settings_dropdown_scrollup", InputManager.GamepadButton.DPadUp, KeyCode.UpArrow);
            uiDropdownScrollDownButton = AddInput("XPortal_DropdownScrollDown", "$settings_dropdown_scrolldown", InputManager.GamepadButton.DPadDown, KeyCode.DownArrow);
        }

        private ButtonConfig AddInput(string name, string hintToken, InputManager.GamepadButton gamepadButton, KeyCode key)
        {
            var newButtonConfig = new ButtonConfig
            {
                Name = name,
                HintToken = hintToken,
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
                Key = key,
                GamepadButton = gamepadButton,
                RepeatDelay = 1000f,
                BlockOtherInputs = true,
            };
            
            InputManager.Instance.AddButton(Info.GUID, newButtonConfig);
            
            return newButtonConfig;
        }

        public void HandleInput()
        {
            bool gamepad = ZInput.IsGamepadActive();
            for (int i = 0; i < dropdownListNavHints.Count; i++)
            {
                GameObject h = dropdownListNavHints[i];
                if (!h)
                {
                    continue;
                }

                Dropdown owner = h.transform.parent != null ? h.transform.parent.GetComponent<Dropdown>() : null;
                bool show = gamepad && owner != null && ExpandedDropdown == owner;
                h.SetActive(show);
            }

            ProcessListNav();
        }

        private void ProcessListNav()
        {
            if (ExpandedDropdown == null)
            {
                listNavNextTime = 0f;
                return;
            }

            bool up = ZInput.GetButton(uiDropdownScrollUpButton.Name);
            bool down = ZInput.GetButton(uiDropdownScrollDownButton.Name);

            if (ZInput.IsGamepadActive())
            {
                up = up || ZInput.GetButton("JoyLStickUp") || ZInput.GetButton("JoyDPadUp");
                down = down || ZInput.GetButton("JoyLStickDown") || ZInput.GetButton("JoyDPadDown");
            }

            if (up && down)
            {
                listNavNextTime = 0f;
                return;
            }

            if (!up && !down)
            {
                listNavNextTime = 0f;
                return;
            }

            float now = Time.unscaledTime;
            if (listNavNextTime <= 0f)
            {
                BumpDropdown(up);
                listNavNextTime = now + ListNavInitialDelay;
                return;
            }

            if (now >= listNavNextTime)
            {
                BumpDropdown(up);
                listNavNextTime = now + ListNavRepeatInterval;
            }
        }
        #endregion

        #region Visibility
        public bool IsActive()
        {
            return mainPanel && mainPanel.activeSelf;
        }

        public void SetActive(bool active)
        {
            if (!mainPanel || !mainPanel.IsValid())
            {
                InitialiseUI();
            }

            GUIManager.BlockInput(active);
            mainPanel.SetActive(active);
            if (active)
            {
                ActivateInputField();
            }
        }

        private void ActivateInputField(bool delayed = true, object state = null)
        {
            if (delayed)
            {
                QueuedAction.Queue(ActivateInputField);
                return;
            }

            portalNameInputField.ActivateInputField();
        }

        public void Show()
        {
            SetActive(true);
        }

        public void Hide(bool delayed = true, object state = null)
        {
            if (delayed)
            {
                QueuedAction.Queue(Hide);
                return;
            }

            SetActive(false);
        }

        private void BumpDropdown(bool up)
        {
            Dropdown dd = ExpandedDropdown;
            if (dd == null)
            {
                return;
            }

            int max = dd.options.Count - 1;
            if (max < 0)
            {
                return;
            }

            int newVal = Mathf.Clamp(dd.value + (up ? -1 : 1), 0, max);
            if (newVal == dd.value)
            {
                return;
            }

            dd.value = newVal;
        }

        private void SetPingMapButtonActive(bool active)
        {
            // Never show the Ping Map button if either "nomap" is active, or the server has PingMapDisabled set
            if (ZoneSystem.instance.GetGlobalKey("nomap") || XPortalConfig.Instance.Server.PingMapDisabled)
            {
                active = false;
            }

            pingMapButtonObject.SetActive(active);

            var mainPanelRT = mainPanel.GetComponent<RectTransform>();
            var dropdownWidth = (active ? inputShortWidth : inputLongWidth) - mainPanelRT.rect.width;
            targetPortalDropdownObject.GetComponent<RectTransform>().sizeDelta = new Vector2(dropdownWidth, rowHeight);
        }
        #endregion

        #region Values
        public void ConfigurePortal(KnownPortal portal, bool canEditNetwork, bool canEditPortalFully)
        {
            InitialiseUI();

            thisPortal = portal;
            canEditNetworkAssignment = canEditNetwork;
            this.canEditPortalFully = canEditPortalFully;
            readOnlyPrivatePortal = portal.IsPrivate && !canEditPortalFully;
            pieceCreatorPlayerId = 0L;
            if (ZDOMan.instance != null)
            {
                var portalZdo = ZDOMan.instance.GetZDO(portal.Id);
                if (portalZdo != null)
                {
                    pieceCreatorPlayerId = portalZdo.GetLong(ZDOVars.s_creator);
                }
            }

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();
            personalNetworkOwnerId = pieceCreatorPlayerId != 0L ? pieceCreatorPlayerId : localPlayerId;

            portalNameInputField.text = portal.Name;
            selectedTargetId = portal.Target;

            defaultPortalToggle.isOn = thisPortal.IsDefaultPortal;
            privatePortalToggle.isOn = thisPortal.IsPrivate;
            if (defaultPortalToggle.isOn)
            {
                privatePortalToggle.SetIsOnWithoutNotify(false);
            }

            defaultPortalToggle.onValueChanged.RemoveAllListeners();
            defaultPortalToggle.onValueChanged.AddListener(OnDefaultPortalToggleChanged);

            privatePortalToggle.onValueChanged.RemoveAllListeners();
            privatePortalToggle.onValueChanged.AddListener(OnPrivatePortalToggleChanged);

            PopulateNetworkAssignmentDropdown();
            PopulateDestinationNetworkDropdown();
            PopulateDestinationPortalDropdown();

            ApplyReadOnlyState();

            Show();
        }

        private void ApplyReadOnlyState()
        {
            readOnlyPrivatePortal = thisPortal.IsPrivate && !canEditPortalFully;
            var allowEdits = !readOnlyPrivatePortal;
            portalNameInputField.interactable = allowEdits;
            targetPortalDropdown.interactable = allowEdits;
            destinationNetworkDropdown.interactable = allowEdits;
            networkAssignmentDropdown.interactable = canEditNetworkAssignment && allowEdits;
            defaultPortalToggle.interactable = allowEdits;
            privatePortalToggle.interactable = canEditPortalFully && allowEdits && !defaultPortalToggle.isOn;
            if (okayButton != null)
            {
                okayButton.interactable = allowEdits;
            }

            var pingBtn = pingMapButtonObject != null ? pingMapButtonObject.GetComponent<Button>() : null;
            if (pingBtn != null)
            {
                pingBtn.interactable = allowEdits && selectedTargetId != ZDOID.None;
            }
        }

        private void OnDefaultPortalToggleChanged(bool isOn)
        {
            if (isOn)
            {
                privatePortalToggle.SetIsOnWithoutNotify(false);
            }

            ApplyReadOnlyState();
        }

        private void OnPrivatePortalToggleChanged(bool isOn)
        {
            if (isOn)
            {
                defaultPortalToggle.SetIsOnWithoutNotify(false);
            }

            if (!canEditNetworkAssignment)
            {
                return;
            }

            if (isOn)
            {
                networkAssignmentDropdown.SetValueWithoutNotify(personalNetworkAssignmentIndex);
            }
        }

        internal void OnNetworksListChanged()
        {
            if (!IsActive() || thisPortal == null)
            {
                return;
            }

            if (!KnownPortalsManager.Instance.TryGetValue(thisPortal.Id, out var fresh))
            {
                return;
            }

            thisPortal = fresh;
            PopulateNetworkAssignmentDropdown();
            PopulateDestinationNetworkDropdown();
            PopulateDestinationPortalDropdown();
            ApplyReadOnlyState();
        }

        private int FindNetworkAssignmentDropdownIndex(long networkOwnerPlayerId)
        {
            foreach (var kvp in networkAssignmentIndexToOwnerId)
            {
                if (kvp.Value == networkOwnerPlayerId)
                {
                    return kvp.Key;
                }
            }

            return -1;
        }

        private void PopulateNetworkAssignmentDropdown()
        {
            networkAssignmentDropdown.onValueChanged.RemoveAllListeners();
            networkAssignmentDropdown.ClearOptions();
            networkAssignmentIndexToOwnerId.Clear();

            if (!canEditNetworkAssignment)
            {
                networkAssignmentDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(thisPortal.NetworkOwnerPlayerId)));
                networkAssignmentDropdown.value = 0;
                networkAssignmentDropdown.interactable = false;
                ApplyDropdownStyle(networkAssignmentDropdown);
                networkAssignmentDropdown.RefreshShownValue();
                return;
            }

            networkAssignmentDropdown.interactable = true;

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();
            var personalNetworkOwnerId = pieceCreatorPlayerId != 0L ? pieceCreatorPlayerId : localPlayerId;

            var index = -1;

            networkAssignmentDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(0L)));
            networkAssignmentIndexToOwnerId.Add(++index, 0L);

            foreach (var customId in CustomNetworks.GetSortedActiveIds())
            {
                networkAssignmentDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(customId)));
                networkAssignmentIndexToOwnerId.Add(++index, customId);
            }

            networkAssignmentDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(personalNetworkOwnerId)));
            networkAssignmentIndexToOwnerId.Add(++index, personalNetworkOwnerId);
            personalNetworkAssignmentIndex = index;

            if (thisPortal.IsPrivate)
            {
                networkAssignmentDropdown.value = personalNetworkAssignmentIndex;
            }
            else
            {
                var matchIdx = FindNetworkAssignmentDropdownIndex(thisPortal.NetworkOwnerPlayerId);
                networkAssignmentDropdown.value = matchIdx >= 0 ? matchIdx : 0;
            }

            ApplyDropdownStyle(networkAssignmentDropdown);
            networkAssignmentDropdown.RefreshShownValue();
            networkAssignmentDropdown.onValueChanged.AddListener(delegate { OnNetworkAssignmentDropdownValueChanged(networkAssignmentDropdown); });
        }

        private void OnNetworkAssignmentDropdownValueChanged(Dropdown change)
        {
            if (!networkAssignmentIndexToOwnerId.TryGetValue(change.value, out var ownerId))
            {
                return;
            }

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();
            var personalId = pieceCreatorPlayerId != 0L ? pieceCreatorPlayerId : localPlayerId;

            if (ownerId != personalId && privatePortalToggle.isOn)
            {
                privatePortalToggle.SetIsOnWithoutNotify(false);
            }
        }

        private long GetSubmittedNetworkOwnerPlayerId()
        {
            if (!canEditNetworkAssignment)
            {
                return thisPortal.NetworkOwnerPlayerId;
            }

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();
            var personalId = pieceCreatorPlayerId != 0L ? pieceCreatorPlayerId : localPlayerId;

            if (privatePortalToggle.isOn && !defaultPortalToggle.isOn)
            {
            }

            if (!networkAssignmentIndexToOwnerId.TryGetValue(networkAssignmentDropdown.value, out var ownerId))
            {
                return thisPortal.NetworkOwnerPlayerId;
            }

            return ownerId;
        }

        private void PopulateDestinationNetworkDropdown()
        {
            destinationNetworkDropdown.onValueChanged.RemoveAllListeners();
            destinationNetworkDropdown.ClearOptions();
            destinationNetworkIndexToOwnerId.Clear();

            var playerIds = new HashSet<long>();
            foreach (var p in KnownPortalsManager.Instance.GetList())
            {
                if (p.Id == thisPortal.Id)
                {
                    continue;
                }

                if (p.NetworkOwnerPlayerId != 0L)
                {
                    playerIds.Add(p.NetworkOwnerPlayerId);
                }
            }

            var sortedPlayerIds = playerIds.ToList();
            sortedPlayerIds.Sort(ComparePlayerNetworkNames);

            var index = -1;
            destinationNetworkDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(0L)));
            destinationNetworkIndexToOwnerId.Add(++index, 0L);

            destinationNetworkDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(PortalNetwork.DestinationNetworkMyPrivateBucket)));
            destinationNetworkIndexToOwnerId.Add(++index, PortalNetwork.DestinationNetworkMyPrivateBucket);

            foreach (var ownerId in sortedPlayerIds)
            {
                destinationNetworkDropdown.options.Add(new Dropdown.OptionData(PortalNetwork.FormatNetworkLabel(ownerId)));
                destinationNetworkIndexToOwnerId.Add(++index, ownerId);
            }

            selectedDestinationNetworkOwnerId = ResolveInitialDestinationNetworkOwnerId(sortedPlayerIds);
            var selectIdx = 0;
            for (var i = 0; i < destinationNetworkDropdown.options.Count; i++)
            {
                if (destinationNetworkIndexToOwnerId.TryGetValue(i, out var oid) && oid == selectedDestinationNetworkOwnerId)
                {
                    selectIdx = i;
                    break;
                }
            }

            destinationNetworkDropdown.value = selectIdx;
            if (destinationNetworkIndexToOwnerId.TryGetValue(selectIdx, out var resolved))
            {
                selectedDestinationNetworkOwnerId = resolved;
            }

            ApplyDropdownStyle(destinationNetworkDropdown);
            destinationNetworkDropdown.RefreshShownValue();
            destinationNetworkDropdown.onValueChanged.AddListener(delegate { OnDestinationNetworkDropdownValueChanged(destinationNetworkDropdown); });
        }

        private static int ComparePlayerNetworkNames(long a, long b)
        {
            var la = PortalNetwork.FormatNetworkLabel(a);
            var lb = PortalNetwork.FormatNetworkLabel(b);
            return string.Compare(la, lb, StringComparison.OrdinalIgnoreCase);
        }

        private long ResolveInitialDestinationNetworkOwnerId(List<long> sortedPlayerIds)
        {
            if (!thisPortal.HasTarget() || !KnownPortalsManager.Instance.ContainsId(thisPortal.Target))
            {
                return 0L;
            }

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();

            var targetPortal = KnownPortalsManager.Instance.GetKnownPortalById(thisPortal.Target);
            if (targetPortal.NetworkOwnerPlayerId == 0L)
            {
                return 0L;
            }

            // Destination networks are always from the local user's perspective ("My Private" = this player's privates).
            if (targetPortal.IsPrivate && targetPortal.NetworkOwnerPlayerId == localPlayerId)
            {
                return PortalNetwork.DestinationNetworkMyPrivateBucket;
            }

            if (!targetPortal.IsPrivate && sortedPlayerIds.Contains(targetPortal.NetworkOwnerPlayerId))
            {
                return targetPortal.NetworkOwnerPlayerId;
            }

            return 0L;
        }

        private void OnDestinationNetworkDropdownValueChanged(Dropdown change)
        {
            if (destinationNetworkIndexToOwnerId.Count == 0)
            {
                return;
            }

            selectedDestinationNetworkOwnerId = destinationNetworkIndexToOwnerId[change.value];
            PopulateDestinationPortalDropdown();
        }

        private void PopulateDestinationPortalDropdown()
        {
            targetPortalDropdown.onValueChanged.RemoveAllListeners();
            targetPortalDropdown.ClearOptions();
            dropdownIndexToZDOIDMapping.Clear();

            var index = -1;

            var strNone = Localization.instance.Localize("$piece_portal_target_none");
            targetPortalDropdown.options.Insert(++index, new Dropdown.OptionData(strNone));
            targetPortalDropdown.value = index;
            dropdownIndexToZDOIDMapping.Add(index, ZDOID.None);

            var localPlayerId = Player.m_localPlayer != null
                ? Player.m_localPlayer.GetPlayerID()
                : Game.instance.GetPlayerProfile().GetPlayerID();

            var portalsSorted = KnownPortalsManager.Instance.GetSortedList()
                .Where(p => p.Id != thisPortal.Id)
                .Where(p =>
                {
                    if (selectedDestinationNetworkOwnerId == 0L)
                    {
                        return p.NetworkOwnerPlayerId == 0L && !p.IsPrivate;
                    }

                    if (selectedDestinationNetworkOwnerId == PortalNetwork.DestinationNetworkMyPrivateBucket)
                    {
                        return p.NetworkOwnerPlayerId == localPlayerId && p.IsPrivate;
                    }

                    return p.NetworkOwnerPlayerId == selectedDestinationNetworkOwnerId && !p.IsPrivate;
                })
                .ToList();

            foreach (var portal in portalsSorted)
            {
                var portalName = portal.Name;
                if (string.IsNullOrEmpty(portalName))
                {
                    portalName = Localization.instance.Localize("$piece_portal_tag_none");
                }

                var distanceTag = string.Empty;
                if (!XPortalConfig.Instance.Server.HidePortalDistance)
                {
                    float distance = (int)Vector3.Distance(thisPortal.Location, portal.Location);
                    var strDistance = string.Format("{0} m", distance.ToString());
                    if (distance >= 1000)
                    {
                        strDistance = string.Format("{0:0.0} km", distance / 1000);
                    }

                    distanceTag = $"  ({strDistance})";
                }

                var colourTag = string.Empty;
                if (XPortalConfig.Instance.Local.DisplayPortalColour)
                {
                    colourTag = $"<color={portal.Colour}>>> </color>";
                }

                var option = new Dropdown.OptionData($"{colourTag}{portalName}{distanceTag}");
                targetPortalDropdown.options.Insert(++index, option);

                if (portal.Id == selectedTargetId)
                {
                    targetPortalDropdown.value = index;
                }

                dropdownIndexToZDOIDMapping.Add(index, portal.Id);
            }

            if (!readOnlyPrivatePortal && !dropdownIndexToZDOIDMapping.Values.Any(id => id == selectedTargetId))
            {
                targetPortalDropdown.value = 0;
                selectedTargetId = ZDOID.None;
            }

            targetPortalDropdown.RefreshShownValue();
            SetPingMapButtonActive(selectedTargetId != ZDOID.None);

            targetPortalDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(targetPortalDropdown); });
            ApplyReadOnlyState();
        }
        #endregion

        #region UI Events
        private void OnDropdownValueChanged(Dropdown change)
        {
            selectedTargetId = dropdownIndexToZDOIDMapping[change.value];
            SetPingMapButtonActive(selectedTargetId != ZDOID.None);
            ApplyReadOnlyState();
        }

        private void OnOkayButtonClicked()
        {
            global::XPortal.XPortal.PortalInfoSubmitted(
                thisPortal,
                portalNameInputField.text,
                selectedTargetId,
                defaultPortalToggle.isOn,
                GetSubmittedNetworkOwnerPlayerId(),
                privatePortalToggle.isOn && !defaultPortalToggle.isOn);
            Hide();
        }

        private void OnCancelButtonClicked()
        {
            Hide();
        }

        private void OnPingMapButtonClicked()
        {
            XPortal.PingMapButtonClicked(selectedTargetId);
            Hide();
        }
        #endregion

        private void InitialiseUI()
        {
            if (GUIManager.IsHeadless())
            {
                // This is a dedicated server, UI is not available
                return;
            }

            if (!mainPanel)
            {
                // Minimum width of Main Panel so that everything fits
                var mainPanelWidthMin = padding + labelWidth + padding + inputLongWidth + padding;

                var GuiHook = GameObject.Find("_GameMain/LoadingGUI/CustomGUIFront");
                
                if (!GuiHook)
                {
                    Log.Error("GuiHook not found");
                    return;
                }

                // Main "parent" panel
                mainPanel = GUIManager.Instance.CreateWoodpanel(
                        parent: GuiHook.transform,
                        anchorMin: new Vector2(0.5f, 0.5f),
                        anchorMax: new Vector2(0.5f, 0.5f),
                        position: new Vector2(0f, 0f),
                        width: mainPanelWidthMin,
                        height: 496f,
                        draggable: false);
                mainPanel.name = GO_MAINPANEL;
                mainPanel.AddComponent<CanvasGroup>();
                mainPanel.AddComponent<UIGroupHandler>();

                if (!mainPanel.GetComponentInParent<Localize>())
                {
                    mainPanel.AddComponent<Localize>();
                }

                // Header text
                var headerTextObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$hud_xportal_title"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(1f, 1f),    // anchor top right (so it stretches along with the panel)
                        position: new Vector2(20f, -15f),
                        font: GUIManager.Instance.AveriaSerifBold,
                        fontSize: 32,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: 250f,
                        height: 50f,
                        addContentSizeFitter: false);
                headerTextObject.name = GO_HEADERTEXT;
                headerTextObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                headerTextObject.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);    // pivot top middle
                headerTextObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(20f, -15f);
                headerTextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 50f);


                // Portal network assignment label
                var networkAssignLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$hud_xportal_portal_network"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, networkAssignRowTop),
                        font: GUIManager.Instance.AveriaSerif,
                        fontSize: 18,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: labelWidth,
                        height: rowHeight,
                        addContentSizeFitter: false);
                networkAssignLabelObject.name = GO_NETWORKASSIGNLABEL;
                networkAssignLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                networkAssignLabelObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                networkAssignLabelObject.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

                var networkAssignDropdownObject = GUIManager.Instance.CreateDropDown(
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),
                        anchorMax: new Vector2(1f, 1f),
                        position: new Vector2(secondColumnLeft, networkAssignRowTop),
                        fontSize: 18,
                        width: inputLongWidth,
                        height: rowHeight);
                networkAssignDropdownObject.name = GO_NETWORKASSIGNDROPDOWN;
                networkAssignmentDropdown = networkAssignDropdownObject.GetComponent<Dropdown>();
                networkAssignmentDropdown.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                ApplyDropdownStyle(networkAssignmentDropdown);

                AddGamepadHint(networkAssignDropdownObject, "JoyButtonY", KeyCode.None);
                AttachDropdownListNavHint(networkAssignDropdownObject, GO_NETWORKASSIGNLISTNAVHINT);

                // Portal name label
                var portalNameLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal_tag"), // "Name"
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, nameRowTop),
                        font: GUIManager.Instance.AveriaSerif,
                        fontSize: 18,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: labelWidth,
                        height: rowHeight,
                        addContentSizeFitter: false);
                portalNameLabelObject.name = GO_NAMELABEL;
                portalNameLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                var portalNameLabelText = portalNameLabelObject.GetComponent<Text>();
                portalNameLabelText.alignment = TextAnchor.MiddleLeft;
                portalNameLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;


                // Portal name textbox
                var portalNameInputObject = GUIManager.Instance.CreateInputField(
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),     // anchor top left
                        anchorMax: new Vector2(1f, 1f),     // anchor top right (so it stretches along with the panel)
                        position: new Vector2(secondColumnLeft, nameRowTop),
                        contentType: InputField.ContentType.Standard,
                        placeholderText: Localization.instance.Localize("$piece_portal_tag.."), // "Name.."
                        fontSize: 18,
                        width: inputLongWidth,
                        height: rowHeight);
                portalNameInputObject.name = GO_NAMEINPUT;
                portalNameInputObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left
                portalNameInputField = portalNameInputObject.GetComponent<InputField>();


                // Destination network label
                var destinationNetworkLabelObject = GUIManager.Instance.CreateText(
                    text: Localization.instance.Localize("$hud_xportal_destination_network"),
                    parent: mainPanel.transform,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(0f, 1f),
                    position: new Vector2(firstColumnLeft, destinationNetworkRowTop),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 18,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: labelWidth,
                    height: rowHeight,
                    addContentSizeFitter: false);
                destinationNetworkLabelObject.name = GO_DESTINATIONNETWORKLABEL;
                destinationNetworkLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                destinationNetworkLabelObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                destinationNetworkLabelObject.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

                var destinationNetworkDropdownObject = GUIManager.Instance.CreateDropDown(
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),
                        anchorMax: new Vector2(1f, 1f),
                        position: new Vector2(secondColumnLeft, destinationNetworkRowTop),
                        fontSize: 18,
                        width: inputLongWidth,
                        height: rowHeight);
                destinationNetworkDropdownObject.name = GO_DESTINATIONNETWORKDROPDOWN;
                destinationNetworkDropdown = destinationNetworkDropdownObject.GetComponent<Dropdown>();
                destinationNetworkDropdown.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                ApplyDropdownStyle(destinationNetworkDropdown);

                AddGamepadHint(destinationNetworkDropdownObject, "JoyLBumper", KeyCode.None);
                AttachDropdownListNavHint(destinationNetworkDropdownObject, GO_DESTINATIONNETWORKLISTNAVHINT);

                // Target portal label
                var targetPortalLabelObject = GUIManager.Instance.CreateText(
                    text: Localization.instance.Localize("$hud_xportal_destination_portal"),
                    parent: mainPanel.transform,
                    anchorMin: new Vector2(0f, 1f),    // anchor top left
                    anchorMax: new Vector2(0f, 1f),
                    position: new Vector2(firstColumnLeft, destinationPortalRowTop),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 18,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: labelWidth,
                    height: rowHeight,
                    addContentSizeFitter: false);
                targetPortalLabelObject.name = GO_DESTINATIONLABEL;
                targetPortalLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                var targetPortalLabelText = targetPortalLabelObject.GetComponent<Text>();
                targetPortalLabelText.alignment = TextAnchor.MiddleLeft;
                targetPortalLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;


                // Target portal dropdown 
                targetPortalDropdownObject = GUIManager.Instance.CreateDropDown(
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(1f, 1f),    // anchor top right (so it stretches along with the panel)
                        position: new Vector2(secondColumnLeft, destinationPortalRowTop),
                        fontSize: 18,
                        width: inputShortWidth,
                        height: rowHeight);
                targetPortalDropdownObject.name = GO_DESTINATIONDROPDOWN;
                targetPortalDropdown = targetPortalDropdownObject.GetComponent<Dropdown>();
                targetPortalDropdown.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left
                ApplyDropdownStyle(targetPortalDropdown);

                AddGamepadHint(targetPortalDropdownObject, "JoyButtonX", KeyCode.None);
                AttachDropdownListNavHint(targetPortalDropdownObject, GO_DESTINATIONGAMEPADHINT);

                // Ping on Map button
                pingMapButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$hud_ping"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 1f),    // anchor top right
                        anchorMax: new Vector2(1f, 1f),
                        position: new Vector2(0 - padding - buttonWidth, destinationPortalRowTop),
                        width: buttonWidth,
                        height: rowHeight);
                pingMapButtonObject.name = GO_PINGMAPBUTTON;
                pingMapButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                AddGamepadHint(pingMapButtonObject, "JoyRBumper", KeyCode.None);


                // Private portal label
                var privatePortalLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal_private"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, privatePortalRowTop),
                        font: GUIManager.Instance.AveriaSerif,
                        fontSize: 18,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: labelWidth,
                        height: rowHeight,
                        addContentSizeFitter: false);
                privatePortalLabelObject.name = GO_PRIVATEPORTALLABEL;
                privatePortalLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                privatePortalLabelObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                privatePortalLabelObject.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

                var privatePortalCheckboxObject = GUIManager.Instance.CreateToggle(
                    parent: mainPanel.transform,
                    width: rowHeight,
                    height: rowHeight);
                privatePortalCheckboxObject.name = GO_PRIVATEPORTALCHECKBOX;

                var privateExtraPadding = padding / 3;
                var privatePortalCheckboxRt = privatePortalCheckboxObject.GetComponent<RectTransform>();
                privatePortalCheckboxRt.pivot = new Vector2(0f, 1f);
                privatePortalCheckboxRt.anchorMin = new Vector2(0f, 1f);
                privatePortalCheckboxRt.anchorMax = new Vector2(0f, 1f);
                privatePortalCheckboxRt.anchoredPosition = new Vector2(secondColumnLeft + privateExtraPadding, privatePortalRowTop - privateExtraPadding);

                privatePortalToggle = privatePortalCheckboxObject.GetComponent<Toggle>();
                privatePortalToggle.isOn = false;

                AddGamepadHint(privatePortalCheckboxObject, "JoyRStick", KeyCode.None);


                // Default Portal label
                var defaultPortalLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal_defaultportal"), // "Default Portal"
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, defaultPortalRowTop),
                        font: GUIManager.Instance.AveriaSerif,
                        fontSize: 18,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: labelWidth,
                        height: rowHeight,
                        addContentSizeFitter: false);
                defaultPortalLabelObject.name = GO_DEFAULTPORTALLABEL;
                defaultPortalLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                var defaultPortalLabelText = defaultPortalLabelObject.GetComponent<Text>();
                defaultPortalLabelText.alignment = TextAnchor.MiddleLeft;
                defaultPortalLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;


                // Default Portal checkbox
                var defaultPortalCheckboxObject = GUIManager.Instance.CreateToggle(
                    parent: mainPanel.transform,
                    width: rowHeight,
                    height: rowHeight);
                defaultPortalCheckboxObject.name = GO_DEFAULTPORTALCHECKBOX;

                var extraPadding = padding / 3;
                var defaultPortalCheckboxRt = defaultPortalCheckboxObject.GetComponent<RectTransform>();
                defaultPortalCheckboxRt.pivot = new Vector2(0f, 1f);        // pivot top left
                defaultPortalCheckboxRt.anchorMin = new Vector2(0f, 1f);    // anchor top left
                defaultPortalCheckboxRt.anchorMax = new Vector2(0f, 1f);
                defaultPortalCheckboxRt.anchoredPosition = new Vector2(secondColumnLeft + extraPadding, defaultPortalRowTop - extraPadding);

                defaultPortalToggle = defaultPortalCheckboxObject.GetComponent<Toggle>();
                defaultPortalToggle.isOn = false;

                AddGamepadHint(defaultPortalCheckboxObject, "JoyLStick", KeyCode.None);

                // Okay button
                var okayButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$menu_ok"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 0f),    // anchor bottom right
                        anchorMax: new Vector2(1f, 0f),
                        position: new Vector2(0 - padding, padding),
                        width: submitButtonWidth,
                        height: submitButtonHeight);
                okayButtonObject.name = GO_OKAYBUTTON;
                okayButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1, 0);    // pivot bottom right
                okayButton = okayButtonObject.GetComponent<Button>();

                AddGamepadHint(okayButtonObject, "JoyButtonA", KeyCode.Return);


                // Cancel button
                var cancelButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$menu_cancel"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 0f),    // anchor bottom right
                        anchorMax: new Vector2(1f, 0f),
                        position: new Vector2(0 - padding - submitButtonWidth - padding, padding),
                        width: submitButtonWidth,
                        height: submitButtonHeight);
                cancelButtonObject.name = GO_CANCELBUTTON;
                cancelButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1, 0);    // pivot bottom right

                AddGamepadHint(cancelButtonObject, "JoyButtonB", KeyCode.Escape);


                // Add listeners to button click events
                pingMapButtonObject.GetComponent<Button>().onClick.AddListener(OnPingMapButtonClicked);
                okayButtonObject.GetComponent<Button>().onClick.AddListener(OnOkayButtonClicked);
                cancelButtonObject.GetComponent<Button>().onClick.AddListener(OnCancelButtonClicked);


                // This property name is backwards? Should select on activate? Either way: Yes.
                portalNameInputField.shouldActivateOnSelect = true;


                // Disable the Main Panel, for now
                mainPanel.SetActive(false);
            }
        }

        private void ApplyDropdownStyle(Dropdown dropdown)
        {
            // Make the expanded list larger
            dropdown.template.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 400f);

            Transform contentTransform = dropdown.template.Find("Viewport/Content");
            if (contentTransform != null)
            {
                Transform oldSpacer = contentTransform.Find(ListBottomSpacerName);
                if (oldSpacer != null)
                {
                    GameObject.Destroy(oldSpacer.gameObject);
                }

                VerticalLayoutGroup contentVlg = contentTransform.GetComponent<VerticalLayoutGroup>();
                if (contentVlg != null)
                {
                    int desiredBottom = 20 + Mathf.RoundToInt(ListBottomSpacerH);
                    contentVlg.padding.bottom = Mathf.Max(contentVlg.padding.bottom, desiredBottom);
                }
            }

            // Get the template item
            var templateItem = dropdown.template.Find("Viewport/Content/Item");

            // Highlight items when hovering over them
            var templateItemToggle = templateItem.gameObject.GetComponent<Toggle>();
            templateItemToggle.targetGraphic.enabled = true;
            templateItemToggle.colors = new ColorBlock
            {
                normalColor = new Color(0.25f, 0.25f, 0.25f, 1f),
                highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            // Fix vertical item overlap
            var itemLabel = templateItem.Find("Item Label");

            var itemLabelText = itemLabel.GetComponent<Text>();
            itemLabelText.verticalOverflow = VerticalWrapMode.Overflow;

            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.offsetMin = new Vector2(itemLabelRect.offsetMin.x, 0f);
            itemLabelRect.offsetMax = new Vector2(itemLabelRect.offsetMax.x, 0f);
        }

        private void AddGamepadHint(GameObject go, string buttonName, KeyCode keyCode)
        {
            var goGamepadHint = CreateGamepadHint(buttonName);
            goGamepadHint.transform.SetParent(go.transform, worldPositionStays: false);

            var uiGamepad = go.AddComponent<UIGamePad>();
            uiGamepad.m_hint = goGamepadHint;
            uiGamepad.m_zinputKey = buttonName;
            uiGamepad.m_keyCode = keyCode;

            var uiInputHint = go.AddComponent<UIInputHint>();
            uiInputHint.m_gamepadHint = goGamepadHint;
        }

        private void AttachDropdownListNavHint(GameObject dropdownObject, string hintObjectName)
        {
            var hint = new GameObject(hintObjectName, typeof(RectTransform), typeof(Image));
            hint.transform.SetParent(dropdownObject.transform, false);
            RectTransform rt = hint.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(36f, 36f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            var img = hint.GetComponent<Image>();
            img.sprite = GUIManager.Instance.GetSprite("dpad_updown");
            img.raycastTarget = false;
            dropdownListNavHints.Add(hint);
        }

        private GameObject CreateGamepadHint(string buttonName)
        {
            var goGamepadHint = new GameObject("gamepad_hint", typeof(RectTransform), typeof(TextMeshProUGUI));

            var textMesh = goGamepadHint.GetComponent<TextMeshProUGUI>();
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(fa => fa.name == "Valheim-AveriaSansLibre");

            textMesh.font = !font ? TMP_Settings.defaultFontAsset : font;
            textMesh.text = $"$KEY_{buttonName}";
            textMesh.fontSize = 18;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.raycastTarget = false;
            Localization.instance.textMeshStrings[textMesh] = textMesh.text;

            var rt = goGamepadHint.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f); // pivot middle centre
            rt.anchorMin = new Vector2(1f, 1f); // anchor top right
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            return goGamepadHint;
        }

        public void Dispose()
        {
            targetPortalDropdown?.onValueChanged.RemoveAllListeners();
            networkAssignmentDropdown?.onValueChanged.RemoveAllListeners();
            destinationNetworkDropdown?.onValueChanged.RemoveAllListeners();
            if (privatePortalToggle != null)
            {
                privatePortalToggle.onValueChanged.RemoveAllListeners();
            }

            if (defaultPortalToggle != null)
            {
                defaultPortalToggle.onValueChanged.RemoveAllListeners();
            }

            if (targetPortalDropdown)
                GameObject.Destroy(targetPortalDropdown);

            if (mainPanel)
            {
                GameObject.Destroy(mainPanel);
            }

            dropdownListNavHints.Clear();
        }
    }
}