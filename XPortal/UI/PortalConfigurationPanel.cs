using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        internal const string GO_MAINPANEL = Mod.Info.Name + "_MainPanel";
        internal const string GO_HEADERTEXT = Mod.Info.Name + "_PanelHeader";
        internal const string GO_NAMELABEL = Mod.Info.Name + "_NameHeader";
        internal const string GO_NAMEINPUT = Mod.Info.Name + "_NameInput";
        internal const string GO_DESTINATIONLABEL = Mod.Info.Name + "_DestinationHeader";
        internal const string GO_DESTINATIONDROPDOWN = Mod.Info.Name + "_DestinationDropdown";
        internal const string GO_DESTINATIONGAMEPADHINT = Mod.Info.Name + "_DestinationGamepadHint";
        internal const string GO_PINGMAPBUTTON = Mod.Info.Name + "_PingMapButton";
        internal const string GO_DEFAULTPORTALLABEL = Mod.Info.Name + "_DefaultPortalHeader";
        internal const string GO_DEFAULTPORTALCHECKBOX = Mod.Info.Name + "_DefaultPortalCheckbox";
        internal const string GO_OKAYBUTTON = Mod.Info.Name + "_OkayButton";
        internal const string GO_CANCELBUTTON = Mod.Info.Name + "_CancelButton";

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
        static readonly float firstRowTop = -60f - padding;
        static readonly float secondRowTop = firstRowTop - rowHeight - padding;
        static readonly float thirdRowTop = secondRowTop - rowHeight - padding;
        static readonly float firstColumnLeft = 0f + padding;
        static readonly float secondColumnLeft = firstColumnLeft + labelWidth + padding;
        // Great. Anyway, let's move on now..
        #endregion

        private GameObject mainPanel;
        private GameObject pingMapButtonObject;
        private GameObject targetPortalDropdownObject;
        private Dropdown targetPortalDropdown;
        private GameObject targetPortalDropdownUpDownKeyhint;
        private InputField portalNameInputField;
        private Toggle defaultPortalToggle;

        // A look-up list to find the portal ZDOID by dropdown list index
        private readonly Dictionary<int, ZDOID> dropdownIndexToZDOIDMapping;

        // The KnownPortal being configured
        private KnownPortal thisPortal;

        // The ZDOID of the target that was selected in the dropdown
        private ZDOID selectedTargetId;

        #region Input Button Configs
        private ButtonConfig uiDropdownScrollUpButton;
        private ButtonConfig uiDropdownScrollDownButton;
        #endregion

        public bool DropdownExpanded = false;

        private PortalConfigurationPanel()
        {
            dropdownIndexToZDOIDMapping = new Dictionary<int, ZDOID>();
        }

        #region Input
        internal void AddInputs()
        {
            uiDropdownScrollUpButton = AddInput("XPortal_DropdownScrollUp", "Dropdown scroll up", InputManager.GamepadButton.DPadUp, KeyCode.UpArrow);
            uiDropdownScrollDownButton = AddInput("XPortal_DropdownScrollDown", "Dropdown scroll down", InputManager.GamepadButton.DPadDown, KeyCode.DownArrow);
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
            InputManager.Instance.AddButton(Mod.Info.GUID, newButtonConfig);
            return newButtonConfig;
        }

        public void HandleInput()
        {


            if (targetPortalDropdownUpDownKeyhint)
            {
                targetPortalDropdownUpDownKeyhint.SetActive(ZInput.IsGamepadActive());
            }

            if (ZInput.GetButtonUp(uiDropdownScrollUpButton.Name))
            {
                ScrollDropdownItem(up: true);
                return;
            }

            if (ZInput.GetButtonUp(uiDropdownScrollDownButton.Name))
            {
                ScrollDropdownItem(up: false);
                return;
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

        private void ScrollDropdownItem(bool up)
        {
            var dropdownWasExpanded = DropdownExpanded;

            if (DropdownExpanded)
            {
                targetPortalDropdown.enabled = false;
                DropdownExpanded = false;
            }

            targetPortalDropdown.value += (up ? -1 : 1);

            if (dropdownWasExpanded)
            {
                targetPortalDropdown.enabled = true;
                targetPortalDropdown.Show();
                DropdownExpanded = true;
            }
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
            float dropdownWidth = (active ? inputShortWidth : inputLongWidth) - mainPanelRT.rect.width;
            targetPortalDropdownObject.GetComponent<RectTransform>().sizeDelta = new Vector2(dropdownWidth, rowHeight);
        }
        #endregion

        #region Values
        public void ConfigurePortal(KnownPortal portal)
        {
            InitialiseUI();
            thisPortal = portal;
            portalNameInputField.text = portal.Name;
            selectedTargetId = portal.Target;

            defaultPortalToggle.isOn = thisPortal.IsDefaultPortal;

            PopulateDropdown();

            Show();
        }

        private void PopulateDropdown()
        {
            // Remove any listeners so they don't get all upset
            targetPortalDropdown.onValueChanged.RemoveAllListeners();

            // Forget what we know
            targetPortalDropdown.ClearOptions();
            dropdownIndexToZDOIDMapping.Clear();

            int index = -1;

            // Add "None" option at index `0`
            var strNone = Localization.instance.Localize("$piece_portal_target_none"); // "(None)"
            targetPortalDropdown.options.Insert(++index, new Dropdown.OptionData(strNone));
            targetPortalDropdown.value = index;
            dropdownIndexToZDOIDMapping.Add(index, ZDOID.None);

            // Get all KnownPortals, sorted by Name
            var portalsSorted = KnownPortalsManager.Instance.GetSortedList();

            foreach (var portal in portalsSorted)
            {
                // Skip the one we're currently interacting with
                if (portal.Id == thisPortal.Id)
                {
                    continue;
                }

                // Get portal name
                string portalName = portal.Name;

                if (string.IsNullOrEmpty(portalName))
                {
                    portalName = Localization.instance.Localize("$piece_portal_tag_none"); // "(No Name)"
                }

                // Calculate portal distance
                var distanceTag = string.Empty;
                if (!XPortalConfig.Instance.Server.HidePortalDistance)
                {
                    float distance = (int)Vector3.Distance(thisPortal.Location, portal.Location);
                    string strDistance = string.Format("{0} m", distance.ToString());
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

                // Insert at the next index
                targetPortalDropdown.options.Insert(++index, option);

                // Select it in the list if this is the current target
                if (portal.Id == selectedTargetId)
                {
                    targetPortalDropdown.value = index;
                }

                dropdownIndexToZDOIDMapping.Add(index, portal.Id);
            }

            targetPortalDropdown.RefreshShownValue();
            SetPingMapButtonActive(selectedTargetId != ZDOID.None);

            targetPortalDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(targetPortalDropdown); });
        }
        #endregion

        #region UI Events
        private void OnDropdownValueChanged(Dropdown change)
        {
            selectedTargetId = dropdownIndexToZDOIDMapping[change.value];
            SetPingMapButtonActive(selectedTargetId != ZDOID.None);
        }

        private void OnOkayButtonClicked()
        {
            XPortal.PortalInfoSubmitted(thisPortal, portalNameInputField.text, selectedTargetId, defaultPortalToggle.isOn);
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
                float mainPanelWidthMin = padding + labelWidth + padding + inputLongWidth + padding;

                var pixielFixGui = GameObject.Find("_GameMain/LoadingGUI/PixelFix/IngameGui(Clone)");

                // Main "parent" panel
                mainPanel = GUIManager.Instance.CreateWoodpanel(
                        parent: pixielFixGui.transform,
                        anchorMin: new Vector2(0.5f, 0.5f),
                        anchorMax: new Vector2(0.5f, 0.5f),
                        position: new Vector2(0f, 0f),
                        width: mainPanelWidthMin,
                        height: 320f,
                        draggable: false);
                mainPanel.name = GO_MAINPANEL;
                mainPanel.AddComponent<CanvasGroup>();
                mainPanel.AddComponent<UIGroupHandler>();

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


                // Portal name label
                var portalNameLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal_tag"), // "Name"
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, firstRowTop),
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
                        position: new Vector2(secondColumnLeft, firstRowTop),
                        contentType: InputField.ContentType.Standard,
                        placeholderText: Localization.instance.Localize("$piece_portal_tag.."), // "Name.."
                        fontSize: 18,
                        width: inputLongWidth,
                        height: rowHeight);
                portalNameInputObject.name = GO_NAMEINPUT;
                portalNameInputObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left
                portalNameInputField = portalNameInputObject.GetComponent<InputField>();


                // Target portal label
                var targetPortalLabelObject = GUIManager.Instance.CreateText(
                    text: Localization.instance.Localize("$piece_portal_target"),
                    parent: mainPanel.transform,
                    anchorMin: new Vector2(0f, 1f),    // anchor top left
                    anchorMax: new Vector2(0f, 1f),
                    position: new Vector2(firstColumnLeft, secondRowTop),
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
                        position: new Vector2(secondColumnLeft, secondRowTop),
                        fontSize: 18,
                        width: inputShortWidth,
                        height: rowHeight);
                targetPortalDropdownObject.name = GO_DESTINATIONDROPDOWN;
                targetPortalDropdown = targetPortalDropdownObject.GetComponent<Dropdown>();
                targetPortalDropdown.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left
                ApplyDropdownStyle(targetPortalDropdown);

                AddGamepadHint(targetPortalDropdownObject, "JoyButtonX", KeyCode.None);


                // Target portal dropdown up/down keyhint image
                // This is shown on the *left* side of the dropdown
                targetPortalDropdownUpDownKeyhint = new GameObject(GO_DESTINATIONGAMEPADHINT, typeof(RectTransform), typeof(Image));
                targetPortalDropdownUpDownKeyhint.transform.SetParent(targetPortalDropdownObject.transform, worldPositionStays: false);

                var targetPortalDropdownUpDownKeyhintRt = targetPortalDropdownUpDownKeyhint.GetComponent<RectTransform>();
                targetPortalDropdownUpDownKeyhintRt.pivot = new Vector2(1, 0.5f); // pivot middle right
                targetPortalDropdownUpDownKeyhintRt.anchorMin = new Vector2(0, 0.5f); // anchor middle left
                targetPortalDropdownUpDownKeyhintRt.anchorMax = new Vector2(0, 0.5f);
                targetPortalDropdownUpDownKeyhintRt.sizeDelta = new Vector2(36, 36);
                targetPortalDropdownUpDownKeyhintRt.anchoredPosition = new Vector2(10, 0);

                var targetPortalDropdownUpDownKeyhintImg = targetPortalDropdownUpDownKeyhint.GetComponent<Image>();
                targetPortalDropdownUpDownKeyhintImg.sprite = GUIManager.Instance.GetSprite("dpad_updown");


                // Ping on Map button
                pingMapButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$hud_ping"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 1f),    // anchor top right
                        anchorMax: new Vector2(1f, 1f),
                        position: new Vector2(0 - padding - buttonWidth, secondRowTop),
                        width: buttonWidth,
                        height: rowHeight);
                pingMapButtonObject.name = GO_PINGMAPBUTTON;
                pingMapButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                AddGamepadHint(pingMapButtonObject, "JoyButtonY", KeyCode.None);


                // Default Portal label
                var defaultPortalLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal_defaultportal"), // "Default Portal"
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),    // anchor top left
                        anchorMax: new Vector2(0f, 1f),
                        position: new Vector2(firstColumnLeft, thirdRowTop),
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
                defaultPortalCheckboxRt.anchoredPosition = new Vector2(secondColumnLeft + extraPadding, thirdRowTop - extraPadding);

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

        private GameObject CreateGamepadHint(string buttonName)
        {
            var goGamepadHint = new GameObject("gamepad_hint", typeof(RectTransform), typeof(TextMeshProUGUI));

            var textMesh = goGamepadHint.GetComponent<TextMeshProUGUI>();
            textMesh.text = $"$KEY_{buttonName}";
            textMesh.fontSize = 18;
            textMesh.alignment = TextAlignmentOptions.Center;
            Localization.instance.textMeshStrings.Add(textMesh, textMesh.text);

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

            if (targetPortalDropdown)
                GameObject.Destroy(targetPortalDropdown);

            if (mainPanel)
                GameObject.Destroy(mainPanel);
        }
    }
}