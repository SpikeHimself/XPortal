using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace XPortal
{
    public sealed class XPortalUI : IDisposable
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<XPortalUI> lazy = new Lazy<XPortalUI>(() => new XPortalUI());
        public static XPortalUI Instance { get { return lazy.Value; } }
        ////////////////////////////

        #region Events
        public delegate void PortalInfoSubmittedAction(ZDOID thisPortalZDOID, string newName, ZDOID targetZDOID);
        public event PortalInfoSubmittedAction PortalInfoSubmitted;

        public delegate void PingMapButtonClickedAction(ZDOID targetPortalZDOID);
        public event PingMapButtonClickedAction PingMapButtonClicked;
        #endregion

        #region Pain
        // Creating the UI was incredibly painful. I will never change the layout again. Ever.
        // ...but we can use some variables to tweak widths, heights, offsets, and such

        // Essentially it's something like this:

        //////////////////////////////////////////
        //               {header}               //
        //                                      //
        //  {label}     {i n p u t - l o n g}   //
        //  {label}     {input-short} {button}  //
        //                                      //
        //                        {cancel} {ok} //
        //////////////////////////////////////////

        // So apart from the header and footer, we can define locations and offsets for "rows", "columns", "padding", ..
        static readonly float padding = 20f;
        static readonly float rowHeight = 32f;
        static readonly float labelWidth = 100f;
        static readonly float buttonWidth = 90f;
        static readonly float submitButtonWidth = 110f;
        static readonly float submitButtonHeight = 48f;
        static readonly float inputShortWidth = 440f;
        static readonly float inputLongWidth = inputShortWidth + padding + buttonWidth;
        static readonly float firstRowTop = -60f - padding;
        static readonly float secondRowTop = firstRowTop - rowHeight - padding;
        static readonly float firstColumnLeft = 0f + padding;
        static readonly float secondColumnLeft = firstColumnLeft + labelWidth + padding;
        // Great. Anyway, let's move on now..
        #endregion

        private GameObject mainPanel;
        private GameObject pingMapButtonObject;
        private GameObject targetPortalDropdownObject;
        private Dropdown targetPortalDropdown;
        private InputField portalNameInputField;

        private Dictionary<int, ZDOID> dropdownIndexToZDOIDMapping;

        private ZDOID thisPortalZDOID;
        private ZDOID selectedTargetZDOID;
        private Dictionary<ZDOID, KnownPortal> knownPortals;


        private XPortalUI()
        {
            dropdownIndexToZDOIDMapping = new Dictionary<int, ZDOID>();
            InitialiseUI();
        }

        #region Visibility
        public bool IsActive()
        {
            if (mainPanel == null)
            {
                return false;
            }
            return mainPanel.activeSelf;
        }

        public void SetActive(bool active)
        {
            if (mainPanel == null || !mainPanel.IsValid())
            {
                InitialiseUI();
            }
            mainPanel.SetActive(active);
            GUIManager.BlockInput(active);
            if (active)
            {
                portalNameInputField.ActivateInputField();
                portalNameInputField.Select();
            }
        }

        public void Show()
        {
            SetActive(true);
        }

        public void Hide()
        {
            SetActive(false);
        }

        private void SetPingMapButtonActive(bool active)
        {
            pingMapButtonObject.SetActive(active);

            var mainPanelRT = mainPanel.GetComponent<RectTransform>();
            float dropdownWidth = (active ? inputShortWidth : inputLongWidth) -  mainPanelRT.sizeDelta.x;
            targetPortalDropdownObject.GetComponent<RectTransform>().sizeDelta = new Vector2(dropdownWidth, rowHeight);
        }
        #endregion

        #region Values
        public void ConfigurePortal(KnownPortal portal, ref Dictionary<ZDOID, KnownPortal> knownPortals)
        {
            InitialiseUI();

            this.thisPortalZDOID = portal.ZDOID;
            this.portalNameInputField.text = portal.Name;
            this.selectedTargetZDOID = portal.Target;
            this.knownPortals = knownPortals;
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
            var strNone = Localization.instance.Localize("$piece_portal_target_none");
            targetPortalDropdown.options.Insert(++index, new Dropdown.OptionData(strNone));
            targetPortalDropdown.value = index;
            dropdownIndexToZDOIDMapping.Add(index, ZDOID.None);

            var thisPortalZDO = Util.TryGetZDO(thisPortalZDOID);
            Vector3 thisPortalPos = thisPortalZDO.GetPosition();

            // Get the ZDOIDS of the known portals, but sort them by name
            var knowPortalsSorted = knownPortals.ToList();
            knowPortalsSorted.Sort((valueA, valueB) => valueA.Value.Name.CompareTo(valueB.Value.Name));

            foreach (var knownPortalKVP in knowPortalsSorted)
            {
                var knownPortalZDOID = knownPortalKVP.Key;
                var knownPortal = knownPortalKVP.Value;

                // Skip the one we're currently interacting with
                if (knownPortalZDOID == thisPortalZDOID)
                    continue;

                var portalZDO = Util.TryGetZDO(knownPortalZDOID);
                if (portalZDO == null)
                {
                    // This one somehow doesn't exist anymore - don't add it to the list
                    continue;
                }

                // Get portal name
                string portalName = knownPortal.Name;

                if (string.IsNullOrEmpty(portalName))
                {
                    portalName = Localization.instance.Localize("$piece_portal_tag_none");
                }

                // Get portal distance
                float distance = (int)Vector3.Distance(thisPortalPos, portalZDO.GetPosition());
                string strDistance = string.Format("{0} m", distance.ToString());
                if (distance >= 1000)
                {
                    strDistance = string.Format("{0:0.0} km", distance / 1000);
                }

                var option = new Dropdown.OptionData($"{portalName}  ({strDistance})");

                // Insert at the next index
                targetPortalDropdown.options.Insert(++index, option);

                // Select it in the list if this is the current target
                if (knownPortalZDOID == selectedTargetZDOID)
                {
                    targetPortalDropdown.value = index;
                }

                dropdownIndexToZDOIDMapping.Add(index, knownPortalZDOID);
            }

            targetPortalDropdown.RefreshShownValue();
            SetPingMapButtonActive(selectedTargetZDOID != ZDOID.None);

            targetPortalDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(targetPortalDropdown); });
        }
        #endregion

        #region UI Events
        public void Submit()
        {
            OnOkayButtonClicked();
        }

        private void OnDropdownValueChanged(Dropdown change)
        {
            selectedTargetZDOID = dropdownIndexToZDOIDMapping[change.value];
            SetPingMapButtonActive(selectedTargetZDOID != ZDOID.None);
        }

        private void OnOkayButtonClicked()
        {
            Hide();
            PortalInfoSubmitted(thisPortalZDOID, portalNameInputField.text, selectedTargetZDOID);
        }

        private void OnCancelButtonClicked()
        {
            Hide();
        }

        private void OnPingMapButtonClicked()
        {
            Hide();
            PingMapButtonClicked(selectedTargetZDOID);
        }
        #endregion

        private void InitialiseUI()
        {
            if (GUIManager.IsHeadless())
            {
                // This is a dedicated server, UI is not available
                return;
            }

            if (mainPanel == null || !mainPanel.IsValid())
            {
                // Minimum width of Main Panel so that everything fits
                float mainPanelWidthMin = padding + labelWidth + padding + inputLongWidth + padding;

                // Main "parent" panel
                mainPanel = GUIManager.Instance.CreateWoodpanel(
                        parent: GUIManager.CustomGUIFront.transform,
                        anchorMin: new Vector2(0.5f, 0.5f),
                        anchorMax: new Vector2(0.5f, 0.5f),
                        position: new Vector2(0f, 0f),
                        width: mainPanelWidthMin,
                        height: 260f,
                        draggable: false);


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
                headerTextObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                headerTextObject.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);    // pivot top middle
                headerTextObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(20f, -15f);
                headerTextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 50f);


                // Portal name label
                var portalNameLabelObject = GUIManager.Instance.CreateText(
                        text: Localization.instance.Localize("$piece_portal $piece_portal_tag"),
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
                portalNameLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                Text portalNameLabelText = portalNameLabelObject.GetComponent<Text>();
                portalNameLabelText.alignment = TextAnchor.MiddleLeft;
                portalNameLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;


                // Portal name textbox
                var portalNameInputObject = GUIManager.Instance.CreateInputField(
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(0f, 1f),     // anchor top left
                        anchorMax: new Vector2(1f, 1f),     // anchor top right (so it stretches along with the panel)
                        position: new Vector2(secondColumnLeft, firstRowTop),
                        contentType: InputField.ContentType.Standard,
                        placeholderText: Localization.instance.Localize("$piece_portal $piece_portal_tag.."),
                        fontSize: 18,
                        width: inputLongWidth,
                        height: rowHeight);
                portalNameInputObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left


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
                targetPortalLabelObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left

                Text targetPortalLabelText = targetPortalLabelObject.GetComponent<Text>();
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
                targetPortalDropdownObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left
                targetPortalDropdownObject.transform.Find("Template").GetComponent<RectTransform>().sizeDelta =new Vector2(0f, 400f); // make the expanded list larger


                // Ping on Map button
                pingMapButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$hud_ping"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 1f),    // anchor top right
                        anchorMax: new Vector2(1f, 1f),
                        position: new Vector2(0 - padding - buttonWidth, secondRowTop),
                        width: buttonWidth,
                        height: rowHeight);
                pingMapButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);    // pivot top left


                // Okay button
                var okayButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$menu_ok"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 0f),    // anchor bottom right
                        anchorMax: new Vector2(1f, 0f),
                        position: new Vector2(0 - padding, padding),
                        width: submitButtonWidth,
                        height: submitButtonHeight);
                okayButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1, 0);    // pivot bottom right


                // Cancel button
                var cancelButtonObject = GUIManager.Instance.CreateButton(
                        text: Localization.instance.Localize("$menu_cancel"),
                        parent: mainPanel.transform,
                        anchorMin: new Vector2(1f, 0f),    // anchor bottom right
                        anchorMax: new Vector2(1f, 0f),
                        position: new Vector2(0 - padding - submitButtonWidth - padding, padding),
                        width: submitButtonWidth,
                        height: submitButtonHeight);
                cancelButtonObject.GetComponent<RectTransform>().pivot = new Vector2(1, 0);    // pivot bottom right


                // Make the whole thing slightly larger 
                mainPanel.GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 1.1f);


                // Add listeners to button click events
                pingMapButtonObject.GetComponent<Button>().onClick.AddListener(OnPingMapButtonClicked);
                okayButtonObject.GetComponent<Button>().onClick.AddListener(OnOkayButtonClicked);
                cancelButtonObject.GetComponent<Button>().onClick.AddListener(OnCancelButtonClicked);


                // Keep references to the these fields
                portalNameInputField = portalNameInputObject.GetComponent<InputField>();
                targetPortalDropdown = targetPortalDropdownObject.GetComponent<Dropdown>();


                // This property name is backwards? Should select on activate? Either way: Yes.
                portalNameInputField.shouldActivateOnSelect = true;


                // Disable the Main Panel, for now
                mainPanel.SetActive(false);
            }
        }

        public void Dispose()
        {
            mainPanel?.SetActive(false);
            targetPortalDropdown?.onValueChanged.RemoveAllListeners();

            if (targetPortalDropdown != null)
                GameObject.Destroy(targetPortalDropdown);

            if (mainPanel!= null)
                GameObject.Destroy(mainPanel);
        }
    }
}