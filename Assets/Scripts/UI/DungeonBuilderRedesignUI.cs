using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public enum BuildToolTabType
    {
        Rooms = 0,
        Traps = 1,
        Monsters = 2
    }

    public sealed class DungeonBuilderRedesignUI : MonoBehaviour
    {
        private RectTransform safeAreaRoot;
        private DreamDungeonUIAssetDatabase uiAssets;
        private DungeonEditorUI editorUI;
        private RoomManager roomManager;
        private RoomPlacement roomPlacement;
        private DungeonGrid grid;
        private MonsterPlacementManager monsterPlacement;
        private TrapPlacementManager trapPlacement;
        private RoomInventoryManager roomInventory;
        private MonsterInventoryManager monsterInventory;
        private TrapInventoryManager trapInventory;

        private GameObject redesignRoot;
        private GameObject leftInfoPanel;
        private GameObject rightToolsPanel;

        // Top Bar
        private TextMeshProUGUI modeBadgeText;
        private TextMeshProUGUI topGoldText;
        private TextMeshProUGUI topCrystalText;
        private TextMeshProUGUI topSoulText;
        private TextMeshProUGUI statusBannerText;

        // Left Info Panel
        private TMP_InputField dungeonNameInput;
        private TextMeshProUGUI infoLevelText;
        private TextMeshProUGUI infoSizeText;
        private TextMeshProUGUI infoDifficultyText;
        private TextMeshProUGUI infoRewardText;
        private TextMeshProUGUI infoResourcesText;

        // Right Build Tools Panel
        private BuildToolTabType activeTab = BuildToolTabType.Rooms;
        private Image tabRoomsBg;
        private Image tabTrapsBg;
        private Image tabMonstersBg;
        private Transform itemListContainer;
        private TextMeshProUGUI selectedItemDetailText;
        private string selectedItemId = string.Empty;

        // Bottom Toolbar Buttons & Highlights
        private Image moveBtnImg;
        private Image rotateBtnImg;
        private Image removeBtnImg;
        private Image pathBtnImg;
        private Image trapBtnImg;
        private Button undoBtn;
        private Button redoBtn;
        private Button startDungeonBtn;
        private bool isBusyAction;

        public void Initialize(
            RectTransform safeRoot,
            DungeonEditorUI editor,
            RoomManager rooms,
            RoomPlacement placement,
            DungeonGrid dungeonGrid,
            MonsterPlacementManager monPlace,
            TrapPlacementManager trpPlace)
        {
            safeAreaRoot = safeRoot;
            editorUI = editor;
            roomManager = rooms;
            roomPlacement = placement;
            grid = dungeonGrid;
            monsterPlacement = monPlace;
            trapPlacement = trpPlace;

            roomInventory = FindAnyObjectByType<RoomInventoryManager>();
            monsterInventory = FindAnyObjectByType<MonsterInventoryManager>();
            trapInventory = FindAnyObjectByType<TrapInventoryManager>();

            uiAssets = DreamDungeonUIAssetDatabase.Load();
            SelectedGameModeManager.EnsureInstance().ApplyModeToBattleSession();

            HideLegacyPlainBars();
            BuildRedesignHUD();
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (roomPlacement != null)
            {
                roomPlacement.ToolChanged -= OnToolChanged;
                roomPlacement.StatusChanged -= SetStatus;
            }
            if (roomManager != null)
            {
                roomManager.LayoutChanged -= RefreshAll;
                roomManager.GoldChanged -= OnGoldChanged;
                if (roomManager.Currency != null)
                    roomManager.Currency.CurrencyChanged -= OnCurrenciesChanged;
            }
            if (SelectedGameModeManager.Instance != null)
            {
                SelectedGameModeManager.Instance.OnGameModeChanged -= OnModeChanged;
                SelectedGameModeManager.Instance.OnDifficultyChanged -= OnDifficultyChanged;
            }
        }

        private void SubscribeEvents()
        {
            if (roomPlacement != null)
            {
                roomPlacement.ToolChanged += OnToolChanged;
                roomPlacement.StatusChanged += SetStatus;
            }
            if (roomManager != null)
            {
                roomManager.LayoutChanged += RefreshAll;
                roomManager.GoldChanged += OnGoldChanged;
                if (roomManager.Currency != null)
                    roomManager.Currency.CurrencyChanged += OnCurrenciesChanged;
            }
            if (SelectedGameModeManager.Instance != null)
            {
                SelectedGameModeManager.Instance.OnGameModeChanged += OnModeChanged;
                SelectedGameModeManager.Instance.OnDifficultyChanged += OnDifficultyChanged;
            }
        }

        private void Start()
        {
            HideLegacyPlainBars();
        }

        private void HideLegacyPlainBars()
        {
            if (safeAreaRoot == null) return;
            for (int i = 0; i < safeAreaRoot.childCount; i++)
            {
                Transform child = safeAreaRoot.GetChild(i);
                if (child == null) continue;
                string n = child.name;
                if (string.Equals(n, "DungeonBuilderRedesignHUD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n, "NoticePanel", StringComparison.OrdinalIgnoreCase) ||
                    n.EndsWith("Overlay", StringComparison.OrdinalIgnoreCase) ||
                    n.EndsWith("Notification", StringComparison.OrdinalIgnoreCase) ||
                    n.EndsWith("Toast", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                child.gameObject.SetActive(false);
            }
        }

        private void BuildRedesignHUD()
        {
            if (safeAreaRoot == null || redesignRoot != null) return;

            redesignRoot = new GameObject("DungeonBuilderRedesignHUD", typeof(RectTransform));
            redesignRoot.transform.SetParent(safeAreaRoot, false);
            Stretch(redesignRoot, 0f, 0f, 0f, 0f);
            redesignRoot.transform.SetAsFirstSibling();

            BuildTopHeaderBar(redesignRoot.transform);
            BuildLeftDungeonInfoPanel(redesignRoot.transform);
            BuildRightBuildToolsPanel(redesignRoot.transform);
            BuildBottomToolbar(redesignRoot.transform);
        }

        private void BuildTopHeaderBar(Transform parent)
        {
            GameObject topBar = new GameObject("RedesignTopBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            topBar.transform.SetParent(parent, false);
            RectTransform rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 108f);

            Image bg = topBar.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.11f, 0.94f);

            HorizontalLayoutGroup hlg = topBar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 10, 10);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Info Drawer Toggle
            CreateCompactPillButton(topBar.transform, "ToggleInfoBtn", "INFO", 94f, 62f,
                new Color(0.24f, 0.16f, 0.42f, 1f), () => ToggleLeftInfoPanel());

            // Mode Badge
            GameObject badgeObj = new GameObject("ModeBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badgeObj.transform.SetParent(topBar.transform, false);
            LayoutElement bLe = badgeObj.GetComponent<LayoutElement>();
            bLe.preferredWidth = 175f;
            bLe.preferredHeight = 62f;
            badgeObj.GetComponent<Image>().color = new Color(0.14f, 0.10f, 0.26f, 1f);
            modeBadgeText = AddTmp(badgeObj.transform, "ModeBadgeText", "CAMPAIGN", 20, new Color(1f, 0.84f, 0.40f));

            // Gold Counter (real Currency_GoldCounter.png)
            topGoldText = CreateCurrencyBadge(topBar.transform, "TopGoldCounter",
                uiAssets != null ? uiAssets.currencyGoldCounter : null, new Color(1f, 0.88f, 0.40f));

            // Crystal Counter (real Currency_CrystalCounter.png)
            topCrystalText = CreateCurrencyBadge(topBar.transform, "TopCrystalCounter",
                uiAssets != null ? uiAssets.currencyCrystalCounter : null, new Color(0.84f, 0.56f, 1f));

            // Soul Counter (using sliced Gem_Blue)
            GameObject soulObj = new GameObject("TopSoulCounter", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            soulObj.transform.SetParent(topBar.transform, false);
            LayoutElement sLe = soulObj.GetComponent<LayoutElement>();
            sLe.preferredWidth = 130f;
            sLe.preferredHeight = 62f;
            soulObj.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.22f, 0.96f);
            topSoulText = AddTmp(soulObj.transform, "SoulVal", "S: 0", 19, new Color(0.55f, 0.85f, 1f));

            // Flexible spacer
            GameObject sp = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            sp.transform.SetParent(topBar.transform, false);
            sp.GetComponent<LayoutElement>().flexibleWidth = 1f;

            // Build Tools Drawer Toggle
            CreateCompactPillButton(topBar.transform, "ToggleToolsBtn", "TOOLS", 104f, 62f,
                new Color(0.32f, 0.16f, 0.52f, 1f), () => ToggleRightToolsPanel());
        }

        private void BuildLeftDungeonInfoPanel(Transform parent)
        {
            leftInfoPanel = new GameObject("LeftDungeonInfoPanel", typeof(RectTransform), typeof(Image));
            leftInfoPanel.transform.SetParent(parent, false);
            RectTransform rt = leftInfoPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.17f);
            rt.anchorMax = new Vector2(0f, 0.92f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            rt.sizeDelta = new Vector2(330f, 0f);

            Image bg = leftInfoPanel.GetComponent<Image>();
            if (uiAssets != null && uiAssets.panelEmptyFrame != null)
            {
                bg.sprite = uiAssets.panelEmptyFrame;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.07f, 0.07f, 0.14f, 0.96f);
            }

            GameObject content = new GameObject("InfoContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(leftInfoPanel.transform, false);
            Stretch(content, 24f, 24f, 24f, 44f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title + Scroll Icon
            GameObject header = new GameObject("InfoHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(content.transform, false);
            header.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup hHlg = header.GetComponent<HorizontalLayoutGroup>();
            hHlg.spacing = 8f;
            hHlg.childAlignment = TextAnchor.MiddleCenter;
            hHlg.childControlWidth = false;
            hHlg.childControlHeight = false;

            if (uiAssets != null && uiAssets.decorScroll != null)
            {
                GameObject sc = new GameObject("ScrollIcon", typeof(RectTransform), typeof(Image));
                sc.transform.SetParent(header.transform, false);
                sc.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 42f);
                Image scImg = sc.GetComponent<Image>();
                scImg.sprite = uiAssets.decorScroll;
                scImg.preserveAspect = true;
            }

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(header.transform, false);
            tObj.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 40f);
            TextMeshProUGUI tTmp = tObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "DUNGEON INFO";
            tTmp.fontSize = 24;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(1f, 0.84f, 0.42f);
            tTmp.alignment = TextAlignmentOptions.Center;

            // Dungeon Name Label + InputField
            CreateSectionLabel(content.transform, "Dungeon Name");
            dungeonNameInput = CreateNameInputField(content.transform);

            // Dungeon Level
            infoLevelText = CreateInfoRow(content.transform, "Dungeon Level: Lv. 1");

            // Dungeon Size + Expand button
            GameObject sizeRow = new GameObject("SizeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            sizeRow.transform.SetParent(content.transform, false);
            sizeRow.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup sHlg = sizeRow.GetComponent<HorizontalLayoutGroup>();
            sHlg.spacing = 6f;
            sHlg.childControlWidth = true;
            sHlg.childControlHeight = true;
            sHlg.childForceExpandWidth = false;

            GameObject sizeTxtBox = new GameObject("SizeTextBox", typeof(RectTransform), typeof(LayoutElement));
            sizeTxtBox.transform.SetParent(sizeRow.transform, false);
            sizeTxtBox.GetComponent<LayoutElement>().flexibleWidth = 1f;
            infoSizeText = AddTmp(sizeTxtBox.transform, "SizeText", "Size: 9 x 9", 18, Color.white, TextAlignmentOptions.Left);

            CreateCompactPillButton(sizeRow.transform, "ExpandSizeBtn", "EXPAND", 96f, 42f,
                new Color(0.22f, 0.36f, 0.24f, 1f), () => editorUI?.OpenUpgradePanel());

            // Difficulty Selector (< Normal >)
            CreateSectionLabel(content.transform, "Difficulty");
            GameObject diffRow = new GameObject("DifficultyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            diffRow.transform.SetParent(content.transform, false);
            diffRow.GetComponent<LayoutElement>().preferredHeight = 48f;
            HorizontalLayoutGroup dHlg = diffRow.GetComponent<HorizontalLayoutGroup>();
            dHlg.spacing = 6f;
            dHlg.childAlignment = TextAnchor.MiddleCenter;
            dHlg.childControlWidth = true;
            dHlg.childControlHeight = true;
            dHlg.childForceExpandWidth = false;

            CreateIconButton(diffRow.transform, "DiffPrevBtn", uiAssets != null ? uiAssets.iconArrowLeft : null, "<", 46f, 46f,
                () => SelectedGameModeManager.EnsureInstance().CycleDifficulty(-1));

            GameObject diffBox = new GameObject("DiffValueBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            diffBox.transform.SetParent(diffRow.transform, false);
            diffBox.GetComponent<LayoutElement>().flexibleWidth = 1f;
            diffBox.GetComponent<LayoutElement>().preferredHeight = 44f;
            diffBox.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.20f, 0.96f);
            infoDifficultyText = AddTmp(diffBox.transform, "DiffVal", "Normal", 19, new Color(1f, 0.88f, 0.48f));

            CreateIconButton(diffRow.transform, "DiffNextBtn", uiAssets != null ? uiAssets.iconArrowRight : null, ">", 46f, 46f,
                () => SelectedGameModeManager.EnsureInstance().CycleDifficulty(1));

            // Reward Multiplier
            infoRewardText = CreateInfoRow(content.transform, "Reward Bonus: +100%");

            // Live Resources Summary
            infoResourcesText = CreateInfoRow(content.transform, "Gold: 0  |  Soul: 0  |  Crystal: 0");

            // Quick Utility Buttons (Shop / Quests / Achievements)
            GameObject utilRow = new GameObject("QuickMenuRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            utilRow.transform.SetParent(content.transform, false);
            utilRow.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup uHlg = utilRow.GetComponent<HorizontalLayoutGroup>();
            uHlg.spacing = 6f;
            uHlg.childControlWidth = true;
            uHlg.childControlHeight = true;
            uHlg.childForceExpandWidth = true;

            CreateCompactPillButton(utilRow.transform, "InfoQuestsBtn", "QUEST", 85f, 42f,
                new Color(0.20f, 0.26f, 0.44f, 1f), () => editorUI?.OpenQuestPanel());
            CreateCompactPillButton(utilRow.transform, "InfoAchBtn", "ACHIEV", 85f, 42f,
                new Color(0.32f, 0.22f, 0.42f, 1f), () => editorUI?.OpenAchievementPanel());
            CreateCompactPillButton(utilRow.transform, "InfoBossBtn", "BOSS", 85f, 42f,
                new Color(0.44f, 0.16f, 0.20f, 1f), () => editorUI?.OpenBossPanel());

            // Second Quick Utility Row (Progression / World Map / Upgrades)
            GameObject utilRow2 = new GameObject("QuickMenuRow2", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            utilRow2.transform.SetParent(content.transform, false);
            utilRow2.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup uHlg2 = utilRow2.GetComponent<HorizontalLayoutGroup>();
            uHlg2.spacing = 6f;
            uHlg2.childControlWidth = true;
            uHlg2.childControlHeight = true;
            uHlg2.childForceExpandWidth = true;

            CreateCompactPillButton(utilRow2.transform, "InfoProgBtn", "LEVEL", 85f, 42f,
                new Color(0.18f, 0.30f, 0.48f, 1f), () => editorUI?.OpenProgressionPanel());
            CreateCompactPillButton(utilRow2.transform, "InfoMapBtn", "MAP", 85f, 42f,
                new Color(0.16f, 0.36f, 0.40f, 1f), () => editorUI?.OpenWorldMapPanel());
            CreateCompactPillButton(utilRow2.transform, "InfoUpgBtn", "UPGRADE", 85f, 42f,
                new Color(0.20f, 0.36f, 0.24f, 1f), () => editorUI?.OpenUpgradePanel());

            // Start collapsed on portrait mobile so center Dungeon Grid is completely unobstructed
            leftInfoPanel.SetActive(false);
        }

        private void BuildRightBuildToolsPanel(Transform parent)
        {
            rightToolsPanel = new GameObject("RightBuildToolsPanel", typeof(RectTransform), typeof(Image));
            rightToolsPanel.transform.SetParent(parent, false);
            RectTransform rt = rightToolsPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.17f);
            rt.anchorMax = new Vector2(1f, 0.92f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-10f, 0f);
            rt.sizeDelta = new Vector2(360f, 0f);

            Image bg = rightToolsPanel.GetComponent<Image>();
            if (uiAssets != null && uiAssets.panelEmptyFrame != null)
            {
                bg.sprite = uiAssets.panelEmptyFrame;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.07f, 0.07f, 0.14f, 0.96f);
            }

            GameObject content = new GameObject("ToolsContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(rightToolsPanel.transform, false);
            Stretch(content, 22f, 22f, 22f, 44f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("ToolsTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleObj.transform.SetParent(content.transform, false);
            titleObj.GetComponent<LayoutElement>().preferredHeight = 36f;
            TextMeshProUGUI tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "BUILD TOOLS";
            tTmp.fontSize = 24;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(1f, 0.84f, 0.42f);
            tTmp.alignment = TextAlignmentOptions.Center;

            // 2 Tabs: ROOMS / MONSTERS (Trap system removed)
            GameObject tabsRow = new GameObject("TabsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tabsRow.transform.SetParent(content.transform, false);
            tabsRow.GetComponent<LayoutElement>().preferredHeight = 48f;
            HorizontalLayoutGroup tHlg = tabsRow.GetComponent<HorizontalLayoutGroup>();
            tHlg.spacing = 8f;
            tHlg.childControlWidth = true;
            tHlg.childControlHeight = true;
            tHlg.childForceExpandWidth = true;

            tabRoomsBg = CreateTabButton(tabsRow.transform, "TabRooms", "ROOMS", () => SelectTab(BuildToolTabType.Rooms));
            tabTrapsBg = null;
            tabMonstersBg = CreateTabButton(tabsRow.transform, "TabMonsters", "MONSTERS", () => SelectTab(BuildToolTabType.Monsters));

            // Scrollable Item List
            GameObject scrollObj = new GameObject("ItemScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
            scrollObj.transform.SetParent(content.transform, false);
            LayoutElement scLe = scrollObj.GetComponent<LayoutElement>();
            scLe.flexibleHeight = 1f;
            scLe.minHeight = 280f;
            scrollObj.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.09f, 0.78f);
            scrollObj.GetComponent<Mask>().showMaskGraphic = true;

            GameObject listObj = new GameObject("ItemListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObj.transform.SetParent(scrollObj.transform, false);
            RectTransform listRt = listObj.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 1f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = Vector2.zero;
            listRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup listVlg = listObj.GetComponent<VerticalLayoutGroup>();
            listVlg.padding = new RectOffset(6, 6, 6, 6);
            listVlg.spacing = 8f;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = true;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;

            ContentSizeFitter csf = listObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = listRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 28f;
            itemListContainer = listObj.transform;

            // Selected Item Detail Footer
            GameObject detailBox = new GameObject("SelectedItemDetail", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            detailBox.transform.SetParent(content.transform, false);
            detailBox.GetComponent<LayoutElement>().preferredHeight = 76f;
            detailBox.GetComponent<Image>().color = new Color(0.09f, 0.08f, 0.17f, 0.95f);
            selectedItemDetailText = AddTmp(detailBox.transform, "DetailText", "Tap an item to place on the Dungeon Grid.", 15,
                new Color(0.86f, 0.90f, 0.98f), TextAlignmentOptions.Left);
            selectedItemDetailText.textWrappingMode = TextWrappingModes.Normal;

            // Shop Shortcut Button
            CreateCompactPillButton(content.transform, "OpenActiveTabShopBtn", "OPEN SHOP FOR THIS TAB", 260f, 44f,
                new Color(0.42f, 0.24f, 0.12f, 1f), OpenShopForActiveTab);

            rightToolsPanel.SetActive(false);
        }

        private void BuildBottomToolbar(Transform parent)
        {
            // Status strip above bottom toolbar (non-blocking raycast so grid taps are never blocked)
            GameObject statusBar = new GameObject("StatusBanner", typeof(RectTransform), typeof(Image));
            statusBar.transform.SetParent(parent, false);
            RectTransform stRt = statusBar.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.02f, 0f);
            stRt.anchorMax = new Vector2(0.98f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.anchoredPosition = new Vector2(0f, 206f);
            stRt.sizeDelta = new Vector2(0f, 38f);
            Image stBg = statusBar.GetComponent<Image>();
            stBg.color = new Color(0.05f, 0.05f, 0.11f, 0.86f);
            stBg.raycastTarget = false;
            statusBannerText = AddTmp(statusBar.transform, "StatusText", "Select a tool or open Build Tools to construct your dungeon.", 17,
                new Color(0.92f, 0.92f, 0.98f));

            // Two-row mobile-friendly Bottom Toolbar
            GameObject toolbar = new GameObject("RedesignBottomToolbar", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            toolbar.transform.SetParent(parent, false);
            RectTransform tbRt = toolbar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 0f);
            tbRt.anchorMax = new Vector2(1f, 0f);
            tbRt.pivot = new Vector2(0.5f, 0f);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(0f, 200f);
            toolbar.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.11f, 0.96f);

            VerticalLayoutGroup vlg = toolbar.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 10);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // ROW 1 — Placement Group: Move, Rotate, Remove, Path (Trap removed)
            GameObject row1 = new GameObject("PlacementGroupRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row1.transform.SetParent(toolbar.transform, false);
            row1.GetComponent<LayoutElement>().preferredHeight = 76f;
            HorizontalLayoutGroup r1 = row1.GetComponent<HorizontalLayoutGroup>();
            r1.spacing = 10f;
            r1.childAlignment = TextAnchor.MiddleCenter;
            r1.childControlWidth = true;
            r1.childControlHeight = true;
            r1.childForceExpandWidth = true;

            moveBtnImg   = CreateSpriteActionButton(row1.transform, "Btn_Move",   uiAssets != null ? uiAssets.buttonMove   : null, "MOVE",   () => roomPlacement?.SelectMove()).GetComponent<Image>();
            rotateBtnImg = CreateSpriteActionButton(row1.transform, "Btn_Rotate", uiAssets != null ? uiAssets.buttonRotate : null, "ROTATE", () => roomPlacement?.Rotate()).GetComponent<Image>();
            removeBtnImg = CreateSpriteActionButton(row1.transform, "Btn_Remove", uiAssets != null ? uiAssets.buttonRemove : null, "REMOVE", () => roomPlacement?.SelectRemove()).GetComponent<Image>();
            pathBtnImg   = CreateSpriteActionButton(row1.transform, "Btn_Path",   uiAssets != null ? uiAssets.buttonPath   : null, "PATH",   () => roomPlacement?.SelectPath()).GetComponent<Image>();
            trapBtnImg   = null;

            // ROW 2 — History (Undo, Redo) + Main Action (Start Dungeon, Back)
            GameObject row2 = new GameObject("ActionHistoryRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row2.transform.SetParent(toolbar.transform, false);
            row2.GetComponent<LayoutElement>().preferredHeight = 92f;
            HorizontalLayoutGroup r2 = row2.GetComponent<HorizontalLayoutGroup>();
            r2.spacing = 10f;
            r2.childAlignment = TextAnchor.MiddleCenter;
            r2.childControlWidth = true;
            r2.childControlHeight = true;
            r2.childForceExpandWidth = false;

            Button backBtn = CreateSpriteActionButton(row2.transform, "Btn_Back", uiAssets != null ? uiAssets.buttonBack : null, "BACK", OnBackToModeSelect, 185f);
            undoBtn = CreateSpriteActionButton(row2.transform, "Btn_Undo", uiAssets != null ? uiAssets.buttonUndo : null, "UNDO", OnUndoPressed, 155f);
            redoBtn = CreateSpriteActionButton(row2.transform, "Btn_Redo", uiAssets != null ? uiAssets.buttonRedo : null, "REDO", OnRedoPressed, 155f);
            startDungeonBtn = CreateSpriteActionButton(row2.transform, "Btn_StartDungeon", uiAssets != null ? uiAssets.buttonStartDungeon : null, "START DUNGEON", OnStartDungeonPressed, 360f);
            startDungeonBtn.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void ToggleLeftInfoPanel()
        {
            if (leftInfoPanel == null) return;
            bool next = !leftInfoPanel.activeSelf;
            leftInfoPanel.SetActive(next);
            if (next)
            {
                if (rightToolsPanel != null) rightToolsPanel.SetActive(false);
                RefreshInfoPanel();
            }
        }

        private void ToggleRightToolsPanel()
        {
            if (rightToolsPanel == null) return;
            bool next = !rightToolsPanel.activeSelf;
            rightToolsPanel.SetActive(next);
            if (next)
            {
                if (leftInfoPanel != null) leftInfoPanel.SetActive(false);
                RebuildBuildToolsList();
            }
        }

        public void SelectTab(BuildToolTabType tab)
        {
            activeTab = tab;
            UpdateTabHighlights();
            RebuildBuildToolsList();
        }

        private void UpdateTabHighlights()
        {
            Color activeCol = new Color(0.46f, 0.24f, 0.78f, 1f);
            Color idleCol = new Color(0.16f, 0.14f, 0.28f, 1f);
            if (tabRoomsBg != null) tabRoomsBg.color = activeTab == BuildToolTabType.Rooms ? activeCol : idleCol;
            if (tabTrapsBg != null) tabTrapsBg.color = activeTab == BuildToolTabType.Traps ? activeCol : idleCol;
            if (tabMonstersBg != null) tabMonstersBg.color = activeTab == BuildToolTabType.Monsters ? activeCol : idleCol;
        }

        private void RebuildBuildToolsList()
        {
            if (itemListContainer == null) return;
            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(itemListContainer.GetChild(i).gameObject);
            }

            if (activeTab == BuildToolTabType.Rooms)
            {
                BuildRoomsItems();
            }
            else if (activeTab == BuildToolTabType.Traps)
            {
                BuildTrapsItems();
            }
            else
            {
                BuildMonstersItems();
            }
        }

        private void BuildRoomsItems()
        {
            if (roomManager == null) return;
            IReadOnlyList<RoomData> allRooms = roomManager.RoomsDatabase != null && roomManager.RoomsDatabase.AllRooms != null && roomManager.RoomsDatabase.AllRooms.Count > 0
                ? roomManager.RoomsDatabase.AllRooms
                : roomManager.RoomCatalog;
            if (allRooms == null) return;

            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            int idx = 0;
            foreach (RoomData room in allRooms)
            {
                if (room == null || room.roomType == DungeonRoomType.Entrance || room.roomType == DungeonRoomType.DungeonCore || room.roomType == DungeonRoomType.Path || room.roomType == DungeonRoomType.TrapRoom)
                    continue;

                bool unlocked = DungeonUnlockManager.Instance == null || DungeonUnlockManager.Instance.IsRoomUnlocked(room.roomId, currentLevel);
                int owned = roomInventory != null ? roomInventory.GetAvailable(room.roomId) : 0;
                Sprite icon = room.roomIcon != null ? room.roomIcon : (uiAssets != null ? uiAssets.GetRoomFallbackIcon(idx) : null);
                string costLine = !unlocked
                    ? $"LOCKED (Lv.{room.unlockDungeonLevel})"
                    : (owned > 0 ? $"Avail: {owned}  •  Tap to Place" : $"Buy in Shop ({room.goldCost}G)");
                bool isSelected = string.Equals(selectedItemId, room.roomId, StringComparison.OrdinalIgnoreCase);

                RoomData captured = room;
                int capturedOwned = owned;
                CreateItemSlot(icon, room.roomName, costLine, unlocked && owned > 0, isSelected, () =>
                {
                    selectedItemId = captured.roomId;
                    if (!unlocked)
                    {
                        SetStatus($"{captured.roomName} unlocks at Dungeon Level {captured.unlockDungeonLevel}.");
                        RebuildBuildToolsList();
                    }
                    else if (capturedOwned <= 0)
                    {
                        SetStatus($"No available {captured.roomName}. Opening Room Shop...");
                        if (rightToolsPanel != null) rightToolsPanel.SetActive(false);
                        editorUI?.OpenRoomShopPanel();
                    }
                    else
                    {
                        roomPlacement?.SelectRoom(captured);
                        if (selectedItemDetailText != null)
                            selectedItemDetailText.text = $"{captured.roomName} ({captured.goldCost}G)\n{captured.description}";
                        if (rightToolsPanel != null) rightToolsPanel.SetActive(false);
                        SetStatus($"Selected {captured.roomName}. Tap a green cell adjacent to a Path to place.");
                    }
                });
                idx++;
            }
        }

        private void BuildTrapsItems()
        {
            TrapDatabase db = roomManager != null ? roomManager.Traps : null;
            if (db == null || db.AllTraps == null) return;

            foreach (TrapData trap in db.AllTraps)
            {
                if (trap == null) continue;
                int avail = trapInventory != null ? trapInventory.GetAvailable(trap.trapId) : 0;
                bool unlocked = avail > 0;
                Sprite icon = trap.icon != null ? trap.icon : (uiAssets != null ? uiAssets.GetTrapFallbackIcon() : null);
                string costLine = unlocked ? $"Owned Avail: {avail}  •  DMG {trap.damage}" : $"Buy in Shop ({trap.goldCost}G)";
                bool isSelected = string.Equals(selectedItemId, trap.trapId, StringComparison.OrdinalIgnoreCase);

                TrapData captured = trap;
                CreateItemSlot(icon, trap.trapName, costLine, unlocked, isSelected, () =>
                {
                    selectedItemId = captured.trapId;
                    if (unlocked && trapPlacement != null)
                    {
                        trapPlacement.SelectTrap(captured.trapId);
                        if (selectedItemDetailText != null)
                            selectedItemDetailText.text = $"{captured.trapName} (DMG {captured.damage})\nTap a Trap Room on the Grid to place.";
                    }
                    else
                    {
                        SetStatus($"No available {captured.trapName}. Opening Trap Shop...");
                        editorUI?.OpenTrapShopPanel();
                    }
                    RebuildBuildToolsList();
                });
            }
        }

        private void BuildMonstersItems()
        {
            MonsterDatabase db = roomManager != null ? roomManager.Monsters : null;
            if (db == null || db.AllMonsters == null) return;

            int idx = 0;
            foreach (MonsterData mon in db.AllMonsters)
            {
                if (mon == null) continue;
                int avail = monsterInventory != null ? monsterInventory.GetAvailable(mon.monsterId) : 0;
                bool unlocked = avail > 0;
                Sprite icon = mon.icon != null ? mon.icon : (uiAssets != null ? uiAssets.GetMonsterFallbackIcon(idx) : null);
                string costLine = unlocked ? $"Owned Avail: {avail}  •  ATK {mon.attack} / HP {mon.maxHP}" : $"Buy in Shop ({mon.goldCost}G)";
                bool isSelected = string.Equals(selectedItemId, mon.monsterId, StringComparison.OrdinalIgnoreCase);

                MonsterData captured = mon;
                CreateItemSlot(icon, mon.monsterName, costLine, unlocked, isSelected, () =>
                {
                    selectedItemId = captured.monsterId;
                    if (unlocked && monsterPlacement != null)
                    {
                        monsterPlacement.SelectMonster(captured.monsterId);
                        if (selectedItemDetailText != null)
                            selectedItemDetailText.text = $"{captured.monsterName} (HP {captured.maxHP} / ATK {captured.attack})\nTap a Monster Room on the Grid to place.";
                        if (rightToolsPanel != null) rightToolsPanel.SetActive(false);
                        SetStatus($"Selected {captured.monsterName}. Tap a Monster Room on the Grid to place.");
                    }
                    else
                    {
                        SetStatus($"No available {captured.monsterName}. Opening Monster Shop...");
                        if (rightToolsPanel != null) rightToolsPanel.SetActive(false);
                        editorUI?.OpenMonsterShopPanel();
                    }
                    RebuildBuildToolsList();
                });
                idx++;
            }
        }

        private void CreateItemSlot(Sprite icon, string itemName, string subText, bool unlocked, bool isSelected, Action onClick)
        {
            GameObject slot = new GameObject($"Slot_{itemName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slot.transform.SetParent(itemListContainer, false);
            slot.GetComponent<LayoutElement>().preferredHeight = 76f;

            Image bg = slot.GetComponent<Image>();
            bg.color = isSelected
                ? new Color(0.42f, 0.30f, 0.14f, 0.98f)
                : (unlocked ? new Color(0.14f, 0.12f, 0.24f, 0.96f) : new Color(0.09f, 0.08f, 0.13f, 0.85f));
            slot.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slot.transform, false);
            RectTransform iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0f, 0.5f);
            iRt.anchoredPosition = new Vector2(8f, 0f);
            iRt.sizeDelta = new Vector2(58f, 58f);
            Image iconImg = iconObj.GetComponent<Image>();
            if (icon != null)
            {
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.60f, 0.8f);
            }
            else
            {
                iconImg.color = new Color(0.24f, 0.20f, 0.36f, 1f);
            }

            // Name
            GameObject nObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nObj.transform.SetParent(slot.transform, false);
            RectTransform nRt = nObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.50f);
            nRt.anchorMax = new Vector2(1f, 0.96f);
            nRt.offsetMin = new Vector2(74f, 0f);
            nRt.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI nTmp = nObj.GetComponent<TextMeshProUGUI>();
            nTmp.text = itemName;
            nTmp.fontSize = 18;
            nTmp.fontStyle = FontStyles.Bold;
            nTmp.color = unlocked ? new Color(1f, 0.90f, 0.58f) : new Color(0.65f, 0.65f, 0.72f);
            nTmp.alignment = TextAlignmentOptions.Left;

            // Cost / Status
            GameObject cObj = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI));
            cObj.transform.SetParent(slot.transform, false);
            RectTransform cRt = cObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.06f);
            cRt.anchorMax = new Vector2(1f, 0.50f);
            cRt.offsetMin = new Vector2(74f, 0f);
            cRt.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI cTmp = cObj.GetComponent<TextMeshProUGUI>();
            cTmp.text = subText;
            cTmp.fontSize = 14;
            cTmp.color = unlocked ? new Color(0.76f, 0.90f, 1f) : new Color(0.85f, 0.45f, 0.45f);
            cTmp.alignment = TextAlignmentOptions.Left;
        }

        public void RefreshAll()
        {
            SelectedGameModeManager mgr = SelectedGameModeManager.EnsureInstance();

            if (modeBadgeText != null)
                modeBadgeText.text = mgr.GetModeDisplayName(mgr.CurrentMode);

            int gold = roomManager != null && roomManager.Currency != null ? roomManager.Currency.Gold : (roomManager != null ? roomManager.Gold : 0);
            int soul = roomManager != null && roomManager.Currency != null ? roomManager.Currency.Soul : 0;
            int crystal = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance
                : (roomManager != null && roomManager.Currency != null ? roomManager.Currency.Crystal : 0);

            if (topGoldText != null) topGoldText.text = gold.ToString("N0");
            if (topCrystalText != null) topCrystalText.text = crystal.ToString("N0");
            if (topSoulText != null) topSoulText.text = $"S: {soul:N0}";

            RefreshInfoPanel();
            UpdateTabHighlights();
            if (rightToolsPanel != null && rightToolsPanel.activeSelf)
            {
                RebuildBuildToolsList();
            }

            if (roomPlacement != null)
            {
                OnToolChanged(roomPlacement.ActiveTool);
            }

            if (undoBtn != null && roomManager != null)
                undoBtn.interactable = roomManager.CanUndo;
            if (redoBtn != null && roomManager != null)
                redoBtn.interactable = roomManager.CanRedo;
        }

        private void RefreshInfoPanel()
        {
            SelectedGameModeManager mgr = SelectedGameModeManager.EnsureInstance();

            if (dungeonNameInput != null && !dungeonNameInput.isFocused)
                dungeonNameInput.text = mgr.DungeonName;

            int level = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            int dim = DungeonExpansionManager.Instance != null ? DungeonExpansionManager.Instance.CurrentDimension : 9;
            int rewardPct = Mathf.RoundToInt(mgr.GetRewardMultiplier() * 100f);

            int gold = roomManager != null && roomManager.Currency != null ? roomManager.Currency.Gold : 0;
            int soul = roomManager != null && roomManager.Currency != null ? roomManager.Currency.Soul : 0;
            int crystal = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance : 0;

            if (infoLevelText != null) infoLevelText.text = $"Dungeon Level: Lv. {level}";
            if (infoSizeText != null) infoSizeText.text = $"Size: {dim} x {dim}";
            if (infoDifficultyText != null) infoDifficultyText.text = mgr.SelectedDifficulty.ToString();
            if (infoRewardText != null) infoRewardText.text = $"Reward Multiplier: +{rewardPct}%";
            if (infoResourcesText != null) infoResourcesText.text = $"Gold: {gold:N0}  |  Soul: {soul:N0}  |  Cry: {crystal:N0}";
        }

        private void OnToolChanged(DungeonBuildTool tool)
        {
            Color sel = new Color(1f, 0.90f, 0.45f, 1f);
            Color norm = Color.white;
            if (moveBtnImg != null) moveBtnImg.color = tool == DungeonBuildTool.Move ? sel : norm;
            if (removeBtnImg != null) removeBtnImg.color = tool == DungeonBuildTool.Remove ? sel : norm;
            if (pathBtnImg != null) pathBtnImg.color = tool == DungeonBuildTool.Path ? sel : norm;
            if (rotateBtnImg != null) rotateBtnImg.color = norm;
            if (trapBtnImg != null) trapBtnImg.color = (trapPlacement != null && trapPlacement.HasPendingPlacement) ? sel : norm;
        }

        private void OnGoldChanged(int _) => RefreshAll();
        private void OnCurrenciesChanged(int g, int s, int c) => RefreshAll();
        private void OnModeChanged(SelectedGameMode _) => RefreshAll();
        private void OnDifficultyChanged(StageDifficulty _) => RefreshInfoPanel();

        private void SetStatus(string msg)
        {
            if (statusBannerText != null && !string.IsNullOrEmpty(msg))
                statusBannerText.text = msg;
        }

        private void OnUndoPressed()
        {
            if (roomManager != null && roomManager.TryUndo(out string msg))
                SetStatus(msg);
            else if (roomManager != null)
                SetStatus("Nothing to Undo.");
        }

        private void OnRedoPressed()
        {
            if (roomManager != null && roomManager.TryRedo(out string msg))
                SetStatus(msg);
            else if (roomManager != null)
                SetStatus("Nothing to Redo.");
        }

        private void OnStartDungeonPressed()
        {
            if (isBusyAction) return;
            isBusyAction = true;

            try
            {
                SelectedGameMode mode = SelectedGameModeManager.EnsureInstance().CurrentMode;
                switch (mode)
                {
                    case SelectedGameMode.Campaign:
                        editorUI?.OpenWorldMapPanel();
                        break;
                    case SelectedGameMode.Endless:
                        editorUI?.OpenEndlessPanel();
                        break;
                    case SelectedGameMode.Challenge:
                        editorUI?.OpenChallengePanel();
                        break;
                    case SelectedGameMode.Custom:
                    default:
                        editorUI?.TriggerStartRaid();
                        break;
                }
            }
            finally
            {
                isBusyAction = false;
            }
        }

        private void OnBackToModeSelect()
        {
            if (isBusyAction) return;
            isBusyAction = true;

            // Preserve Dungeon Layout, Selected Game Mode, and Resources before returning
            SaveManager.Instance?.SaveGame();
            SelectedGameModeManager.RequestReturnToModeSelect();
            SceneLoader.Load("MainMenu");
        }

        private void OpenShopForActiveTab()
        {
            switch (activeTab)
            {
                case BuildToolTabType.Rooms:
                    editorUI?.OpenRoomShopPanel();
                    break;
                case BuildToolTabType.Traps:
                    editorUI?.OpenTrapShopPanel();
                    break;
                case BuildToolTabType.Monsters:
                    editorUI?.OpenMonsterShopPanel();
                    break;
            }
        }

        private TextMeshProUGUI CreateCurrencyBadge(Transform parent, string name, Sprite sprite, Color valColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 196f;
            le.preferredHeight = 64f;
            Image img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.11f, 0.11f, 0.20f, 0.96f);
            }

            GameObject mask = new GameObject("ValMask", typeof(RectTransform), typeof(Image));
            mask.transform.SetParent(go.transform, false);
            RectTransform mRt = mask.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.27f, 0.16f);
            mRt.anchorMax = new Vector2(0.80f, 0.84f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            mask.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 1f);

            TextMeshProUGUI tmp = AddTmp(mask.transform, "Value", "0", 20, valColor);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 13;
            tmp.fontSizeMax = 21;
            return tmp;
        }

        private Button CreateSpriteActionButton(Transform parent, string name, Sprite sprite, string fallbackLabel, Action onClick, float preferredWidth = 0f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (preferredWidth > 0f) le.preferredWidth = preferredWidth;
            le.preferredHeight = 76f;

            Image img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.26f, 0.18f, 0.44f, 1f);
                AddTmp(go.transform, "Label", fallbackLabel, 18, Color.white);
            }

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private Image CreateTabButton(Transform parent, string name, string label, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.14f, 0.28f, 1f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
            AddTmp(go.transform, "Label", label, 16, new Color(1f, 0.92f, 0.65f));
            return img;
        }

        private void CreateCompactPillButton(Transform parent, string name, string label, float width, float height, Color color, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
            TextMeshProUGUI tmp = AddTmp(go.transform, "Label", label, 15, Color.white);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = 15;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void CreateIconButton(Transform parent, string name, Sprite icon, string fallback, float width, float height, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            Image img = go.GetComponent<Image>();
            if (icon != null)
            {
                img.sprite = icon;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.24f, 0.20f, 0.38f, 1f);
                AddTmp(go.transform, "Label", fallback, 20, Color.white);
            }
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
        }

        private TMP_InputField CreateNameInputField(Transform parent)
        {
            GameObject go = new GameObject("DungeonNameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 46f;
            go.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.19f, 0.98f);

            GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            Stretch(textArea, 10f, 4f, 10f, 4f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textArea.transform, false);
            Stretch(textObj, 0f, 0f, 0f, 0f);
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Left;

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = txt;
            input.characterLimit = 28;
            input.text = SelectedGameModeManager.EnsureInstance().DungeonName;
            input.onEndEdit.AddListener(val => SelectedGameModeManager.EnsureInstance().SetDungeonName(val));
            return input;
        }

        private void CreateSectionLabel(Transform parent, string label)
        {
            GameObject go = new GameObject($"Lbl_{label}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 24f;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.84f, 0.44f);
            tmp.alignment = TextAlignmentOptions.Left;
        }

        private TextMeshProUGUI CreateInfoRow(Transform parent, string initialText)
        {
            GameObject go = new GameObject("InfoRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 38f;
            go.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f, 0.85f);
            return AddTmp(go.transform, "Value", initialText, 16, new Color(0.90f, 0.94f, 1f), TextAlignmentOptions.Left);
        }

        private static TextMeshProUGUI AddTmp(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch(go, 8f, 2f, 8f, 2f);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(GameObject go, float left, float bottom, float right, float top)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
