using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class DungeonUpgradeUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;

        private RectTransform overlay;
        private RectTransform window;
        private Text balanceText;
        private Text statusText;

        private GameObject coreTabContent;
        private GameObject expansionTabContent;
        private GameObject facilityTabContent;

        private Text coreInfoText;
        private Text coreCostText;
        private Button coreUpgradeBtn;

        private Text expansionInfoText;
        private Text expansionCostText;
        private Button expansionUpgradeBtn;

        private Text goldFacilityInfoText;
        private Button goldFacilityBtn;
        private Text soulFacilityInfoText;
        private Button soulFacilityBtn;
        private Text capacityFacilityInfoText;
        private Button capacityFacilityBtn;

        private int activeTab = 0; // 0 = Core, 1 = Expansion, 2 = Facilities

        public void Configure(RectTransform safeArea)
        {
            safeAreaRoot = safeArea;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
        }

        public void Open()
        {
            if (overlay == null) Build();
            overlay.gameObject.SetActive(true);
            SetTab(activeTab);
            Refresh();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "DungeonUpgradeOverlay");

            window = RoomUIFactory.CreateWindow(overlay, "UpgradeWindow", new Vector2(980, 1600), new Color(0.08f, 0.09f, 0.13f, 0.98f));

            RoomUIFactory.CreateText(window, "Title", "DUNGEON UPGRADES", 40, new Vector2(0, 720), new Vector2(880, 60), new Color(1f, 0.85f, 0.35f));

            balanceText = RoomUIFactory.CreateText(window, "BalanceText", "Gold: 0   Soul: 0", 26, new Vector2(0, 660), new Vector2(880, 45), new Color(0.95f, 0.9f, 0.7f));

            // Tabs buttons
            Button tab1Btn = RoomUIFactory.CreateButton(window, "TabCore", "CORE", new Vector2(-280, 590), new Vector2(260, 65), new Color(0.20f, 0.30f, 0.45f), 24);
            Button tab2Btn = RoomUIFactory.CreateButton(window, "TabExpansion", "EXPANSION", new Vector2(0, 590), new Vector2(260, 65), new Color(0.20f, 0.35f, 0.30f), 24);
            Button tab3Btn = RoomUIFactory.CreateButton(window, "TabFacilities", "FACILITIES", new Vector2(280, 590), new Vector2(260, 65), new Color(0.35f, 0.25f, 0.40f), 24);

            tab1Btn.onClick.AddListener(() => SetTab(0));
            tab2Btn.onClick.AddListener(() => SetTab(1));
            tab3Btn.onClick.AddListener(() => SetTab(2));

            // Status message
            statusText = RoomUIFactory.CreateText(window, "StatusMessage", "", 22, new Vector2(0, 515), new Vector2(880, 45), new Color(0.8f, 0.85f, 0.95f));

            BuildCoreTab();
            BuildExpansionTab();
            BuildFacilityTab();

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseButton", "CLOSE", new Vector2(0, -710), new Vector2(400, 80), new Color(0.75f, 0.25f, 0.25f), 28);
            closeBtn.onClick.AddListener(Close);

            overlay.gameObject.SetActive(false);
        }

        private void BuildCoreTab()
        {
            coreTabContent = new GameObject("CoreTabContent", typeof(RectTransform));
            coreTabContent.transform.SetParent(window, false);
            RectTransform rt = coreTabContent.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(880, 950);

            RectTransform card = RoomUIFactory.CreatePanel(rt, "CoreCard", new Color(0.13f, 0.15f, 0.22f), new Vector2(0, 100), new Vector2(840, 500));

            RoomUIFactory.CreateText(card, "CoreHeader", "DUNGEON CORE REINFORCEMENT", 30, new Vector2(0, 190), new Vector2(800, 50), new Color(1f, 0.8f, 0.3f));

            coreInfoText = RoomUIFactory.CreateText(card, "CoreInfo", "Level: 1\nHP: 500\nDefense: 0", 26, new Vector2(0, 60), new Vector2(760, 160), Color.white, TextAnchor.MiddleCenter);

            coreCostText = RoomUIFactory.CreateText(card, "CoreCost", "Upgrade Cost: 200 Gold, 50 Soul", 24, new Vector2(0, -70), new Vector2(760, 60), new Color(0.9f, 0.85f, 0.6f));

            coreUpgradeBtn = RoomUIFactory.CreateButton(card, "CoreUpgradeBtn", "UPGRADE CORE", new Vector2(0, -165), new Vector2(450, 75), new Color(0.22f, 0.65f, 0.35f), 26);
            coreUpgradeBtn.onClick.AddListener(OnUpgradeCoreClicked);
        }

        private void BuildExpansionTab()
        {
            expansionTabContent = new GameObject("ExpansionTabContent", typeof(RectTransform));
            expansionTabContent.transform.SetParent(window, false);
            RectTransform rt = expansionTabContent.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(880, 950);

            RectTransform card = RoomUIFactory.CreatePanel(rt, "ExpansionCard", new Color(0.12f, 0.17f, 0.16f), new Vector2(0, 100), new Vector2(840, 500));

            RoomUIFactory.CreateText(card, "ExpHeader", "EXPAND DUNGEON GRID", 30, new Vector2(0, 190), new Vector2(800, 50), new Color(0.5f, 0.95f, 0.7f));

            expansionInfoText = RoomUIFactory.CreateText(card, "ExpInfo", "Current Dimension: 9x9\nNext Dimension: 11x11\nRequired Dungeon Level: 3", 26, new Vector2(0, 60), new Vector2(760, 160), Color.white, TextAnchor.MiddleCenter);

            expansionCostText = RoomUIFactory.CreateText(card, "ExpCost", "Expansion Cost: 500 Gold, 100 Soul", 24, new Vector2(0, -70), new Vector2(760, 60), new Color(0.9f, 0.85f, 0.6f));

            expansionUpgradeBtn = RoomUIFactory.CreateButton(card, "ExpansionUpgradeBtn", "EXPAND GRID", new Vector2(0, -165), new Vector2(450, 75), new Color(0.20f, 0.60f, 0.45f), 26);
            expansionUpgradeBtn.onClick.AddListener(OnExpandGridClicked);
        }

        private void BuildFacilityTab()
        {
            facilityTabContent = new GameObject("FacilityTabContent", typeof(RectTransform));
            facilityTabContent.transform.SetParent(window, false);
            RectTransform rt = facilityTabContent.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(880, 950);

            // Facility 1: Gold Production
            RectTransform f1 = RoomUIFactory.CreatePanel(rt, "GoldFacilityCard", new Color(0.16f, 0.15f, 0.20f), new Vector2(0, 240), new Vector2(840, 220));
            RoomUIFactory.CreateText(f1, "F1Title", "GOLD PRODUCTION", 26, new Vector2(-180, 60), new Vector2(420, 45), new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft);
            goldFacilityInfoText = RoomUIFactory.CreateText(f1, "F1Info", "Level 1 (+0% Raid Gold)\nCost: 150 Gold, 30 Soul", 22, new Vector2(-180, -15), new Vector2(420, 75), Color.white, TextAnchor.MiddleLeft);
            goldFacilityBtn = RoomUIFactory.CreateButton(f1, "F1Btn", "UPGRADE", new Vector2(280, 0), new Vector2(200, 70), new Color(0.25f, 0.60f, 0.35f), 22);
            goldFacilityBtn.onClick.AddListener(() => OnUpgradeFacilityClicked("gold"));

            // Facility 2: Soul Production
            RectTransform f2 = RoomUIFactory.CreatePanel(rt, "SoulFacilityCard", new Color(0.16f, 0.15f, 0.20f), new Vector2(0, 0), new Vector2(840, 220));
            RoomUIFactory.CreateText(f2, "F2Title", "SOUL PRODUCTION", 26, new Vector2(-180, 60), new Vector2(420, 45), new Color(0.6f, 0.85f, 1f), TextAnchor.MiddleLeft);
            soulFacilityInfoText = RoomUIFactory.CreateText(f2, "F2Info", "Level 1 (+0% Raid Soul)\nCost: 150 Gold, 30 Soul", 22, new Vector2(-180, -15), new Vector2(420, 75), Color.white, TextAnchor.MiddleLeft);
            soulFacilityBtn = RoomUIFactory.CreateButton(f2, "F2Btn", "UPGRADE", new Vector2(280, 0), new Vector2(200, 70), new Color(0.25f, 0.60f, 0.35f), 22);
            soulFacilityBtn.onClick.AddListener(() => OnUpgradeFacilityClicked("soul"));

            // Facility 3: Monster Capacity
            RectTransform f3 = RoomUIFactory.CreatePanel(rt, "CapFacilityCard", new Color(0.16f, 0.15f, 0.20f), new Vector2(0, -240), new Vector2(840, 220));
            RoomUIFactory.CreateText(f3, "F3Title", "MONSTER CAPACITY", 26, new Vector2(-180, 60), new Vector2(420, 45), new Color(0.85f, 0.5f, 1f), TextAnchor.MiddleLeft);
            capacityFacilityInfoText = RoomUIFactory.CreateText(f3, "F3Info", "Level 1 (+0 Bonus Capacity)\nCost: 400 Gold, 100 Soul", 22, new Vector2(-180, -15), new Vector2(420, 75), Color.white, TextAnchor.MiddleLeft);
            capacityFacilityBtn = RoomUIFactory.CreateButton(f3, "F3Btn", "UPGRADE", new Vector2(280, 0), new Vector2(200, 70), new Color(0.25f, 0.60f, 0.35f), 22);
            capacityFacilityBtn.onClick.AddListener(() => OnUpgradeFacilityClicked("capacity"));
        }

        public void SetTab(int tabIndex)
        {
            activeTab = tabIndex;
            if (coreTabContent != null) coreTabContent.SetActive(activeTab == 0);
            if (expansionTabContent != null) expansionTabContent.SetActive(activeTab == 1);
            if (facilityTabContent != null) facilityTabContent.SetActive(activeTab == 2);
            Refresh();
        }

        public void Refresh()
        {
            if (CurrencyManager.Instance != null && balanceText != null)
            {
                balanceText.text = $"Gold: <color=#ffd700>{CurrencyManager.Instance.Gold}</color>   Soul: <color=#66ccff>{CurrencyManager.Instance.Soul}</color>";
            }

            RefreshCoreTab();
            RefreshExpansionTab();
            RefreshFacilityTab();
        }

        private void RefreshCoreTab()
        {
            var mgr = DungeonCoreUpgradeManager.Instance;
            if (mgr == null) return;

            int level = mgr.CoreLevel;
            int hp = mgr.MaxHP;
            int def = mgr.Defense;

            if (level >= DungeonCoreUpgradeManager.MaxCoreLevel)
            {
                if (coreInfoText != null) coreInfoText.text = $"Current Level: {level} (MAX)\nHP: <color=#66ff66>{hp}</color>   |   Defense: <color=#66ccff>{def}</color>";
                if (coreCostText != null) coreCostText.text = "Maximum core level reached.";
                if (coreUpgradeBtn != null) coreUpgradeBtn.interactable = false;
            }
            else
            {
                int nextHp = mgr.GetMaxHPForLevel(level + 1);
                int nextDef = mgr.GetDefenseForLevel(level + 1);
                mgr.GetUpgradeCost(level, out int gCost, out int sCost);

                if (coreInfoText != null)
                {
                    coreInfoText.text = $"Level: {level}  ->  <color=#ffff55>{level + 1}</color>\n" +
                                        $"HP: {hp}  ->  <color=#66ff66>{nextHp}</color> (+{nextHp - hp})\n" +
                                        $"Defense: {def}  ->  <color=#66ccff>{nextDef}</color> (+{nextDef - def})";
                }

                if (coreCostText != null)
                {
                    coreCostText.text = $"Upgrade Cost: {gCost} Gold, {sCost} Soul";
                }

                if (coreUpgradeBtn != null)
                {
                    coreUpgradeBtn.interactable = mgr.CanUpgrade(out _);
                }
            }
        }

        private void RefreshExpansionTab()
        {
            var mgr = DungeonExpansionManager.Instance;
            if (mgr == null) return;

            int dim = mgr.CurrentDimension;
            if (mgr.GetNextExpansionTier(out int nextDim, out int reqLvl, out int gCost, out int sCost))
            {
                if (expansionInfoText != null)
                {
                    expansionInfoText.text = $"Current Grid: {dim}x{dim}\nNext Grid: <color=#66ff88>{nextDim}x{nextDim}</color>\nRequired Dungeon Level: <color=#ffff55>{reqLvl}</color>";
                }

                if (expansionCostText != null)
                {
                    expansionCostText.text = $"Cost: {gCost} Gold, {sCost} Soul";
                }

                if (expansionUpgradeBtn != null)
                {
                    expansionUpgradeBtn.interactable = mgr.CanExpand(out _);
                }
            }
            else
            {
                if (expansionInfoText != null) expansionInfoText.text = $"Current Grid: {dim}x{dim} (MAX EXPANSION)";
                if (expansionCostText != null) expansionCostText.text = "Maximum dungeon expansion reached.";
                if (expansionUpgradeBtn != null) expansionUpgradeBtn.interactable = false;
            }
        }

        private void RefreshFacilityTab()
        {
            var mgr = DungeonFacilityManager.Instance;
            if (mgr == null) return;

            // Gold
            int gLvl = mgr.GoldFacilityLevel;
            var (gG, gS) = mgr.GetFacilityUpgradeCost("gold", gLvl);
            int gBonus = (gLvl - 1) * 10;
            if (goldFacilityInfoText != null)
            {
                goldFacilityInfoText.text = gLvl >= DungeonFacilityManager.MaxFacilityLevel
                    ? $"Level {gLvl} (MAX) (+{gBonus}% Raid Gold)"
                    : $"Level {gLvl} (+{gBonus}% Raid Gold)  ->  Lv {gLvl + 1} (+{gBonus + 10}%)\nCost: {gG} Gold, {gS} Soul";
            }
            if (goldFacilityBtn != null) goldFacilityBtn.interactable = mgr.CanUpgradeFacility("gold", out _);

            // Soul
            int sLvl = mgr.SoulFacilityLevel;
            var (sG, sS) = mgr.GetFacilityUpgradeCost("soul", sLvl);
            int sBonus = (sLvl - 1) * 10;
            if (soulFacilityInfoText != null)
            {
                soulFacilityInfoText.text = sLvl >= DungeonFacilityManager.MaxFacilityLevel
                    ? $"Level {sLvl} (MAX) (+{sBonus}% Raid Soul)"
                    : $"Level {sLvl} (+{sBonus}% Raid Soul)  ->  Lv {sLvl + 1} (+{sBonus + 10}%)\nCost: {sG} Gold, {sS} Soul";
            }
            if (soulFacilityBtn != null) soulFacilityBtn.interactable = mgr.CanUpgradeFacility("soul", out _);

            // Capacity
            int cLvl = mgr.CapacityFacilityLevel;
            var (cG, cS) = mgr.GetFacilityUpgradeCost("capacity", cLvl);
            int cBonus = (cLvl - 1);
            if (capacityFacilityInfoText != null)
            {
                capacityFacilityInfoText.text = cLvl >= DungeonFacilityManager.MaxCapacityLevel
                    ? $"Level {cLvl} (MAX) (+{cBonus} Monster Capacity)"
                    : $"Level {cLvl} (+{cBonus} Monster Cap)  ->  Lv {cLvl + 1} (+{cBonus + 1})\nCost: {cG} Gold, {cS} Soul";
            }
            if (capacityFacilityBtn != null) capacityFacilityBtn.interactable = mgr.CanUpgradeFacility("capacity", out _);
        }

        private void OnUpgradeCoreClicked()
        {
            if (DungeonCoreUpgradeManager.Instance != null)
            {
                if (DungeonCoreUpgradeManager.Instance.TryUpgrade(out string msg))
                {
                    SetStatus(msg);
                }
                else
                {
                    SetStatus(msg);
                }
                Refresh();
            }
        }

        private void OnExpandGridClicked()
        {
            if (DungeonExpansionManager.Instance != null)
            {
                if (DungeonExpansionManager.Instance.TryExpand(out string msg))
                {
                    SetStatus(msg);
                }
                else
                {
                    SetStatus(msg);
                }
                Refresh();
            }
        }

        private void OnUpgradeFacilityClicked(string facilityType)
        {
            if (DungeonFacilityManager.Instance != null)
            {
                if (DungeonFacilityManager.Instance.TryUpgradeFacility(facilityType, out string msg))
                {
                    SetStatus(msg);
                }
                else
                {
                    SetStatus(msg);
                }
                Refresh();
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
