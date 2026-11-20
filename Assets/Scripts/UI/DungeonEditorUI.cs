using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DreamDungeon
{
    public sealed class DungeonEditorUI : MonoBehaviour
    {
        private RoomManager roomManager;
        private RoomPlacement placement;
        private DungeonGrid grid;
        private Text goldText;
        private Text selectionText;
        private Text statusText;
        private Text soulText;
        private Text crystalText;
        private Text noticeText;
        private GameObject noticePanel;
        private Image pathButtonImage;
        private Color pathButtonColor;
        private RoomShopUI shopUI;
        private RoomInventoryUI inventoryUI;
        private RoomDetailsUI detailsUI;
        private MonsterShopUI monsterShopUI;
        private MonsterInventoryUI monsterInventoryUI;
        private TrapShopUI trapShopUI;
        private TrapInventoryUI trapInventoryUI;
        private MonsterPlacementManager monsterPlacement;
        private TrapPlacementManager trapPlacement;
        private DungeonProgressionUI progressionUI;
        private BossSelectionUI bossUI;
        private DungeonUpgradeUI upgradeUI;
        private BossPlacementManager bossPlacement;
        private MonsterDetailsUI monsterDetailsUI;
        private TrapDetailsUI trapDetailsUI;
        private QuestUIManager questUI;
        private AchievementUIManager achievementUI;
        private QuestNotificationUI notificationUI;
        private WorldMapUI worldMapUI;
        private CampaignSelectionUI campaignSelectionUI;
        private StageDetailsUI stageDetailsUI;
        private Text headerText;
        private Text levelValueText;
        private Button startRaidButton;
        private bool isStartingRaid;

        public void Configure(RoomShopUI shop, RoomInventoryUI inventory, RoomDetailsUI details)
        {
            shopUI = shop;
            inventoryUI = inventory;
            detailsUI = details;
        }

        public void ConfigureMilestone4(MonsterShopUI monsterShop, MonsterInventoryUI monsterInventory,
            TrapShopUI trapShop, TrapInventoryUI trapInventory, MonsterPlacementManager monsterPlace, TrapPlacementManager trapPlace)
        {
            monsterShopUI = monsterShop; monsterInventoryUI = monsterInventory;
            trapShopUI = trapShop; trapInventoryUI = trapInventory;
            monsterPlacement = monsterPlace; trapPlacement = trapPlace;
        }

        public void ConfigureMilestone6(DungeonProgressionUI progUI, BossSelectionUI bUI, DungeonUpgradeUI upUI, BossPlacementManager bPlace)
        {
            progressionUI = progUI;
            bossUI = bUI;
            upgradeUI = upUI;
            bossPlacement = bPlace;
        }

        public void ConfigureMilestone7(QuestUIManager qUI, AchievementUIManager achUI, QuestNotificationUI notifUI = null)
        {
            questUI = qUI;
            achievementUI = achUI;
            notificationUI = notifUI;
        }

        public void ConfigureMilestone8(WorldMapUI mapUI, CampaignSelectionUI campUI, StageDetailsUI details)
        {
            worldMapUI = mapUI;
            campaignSelectionUI = campUI;
            stageDetailsUI = details;
        }

        private void Awake()
        {
            roomManager = FindAnyObjectByType<RoomManager>();
            placement = FindAnyObjectByType<RoomPlacement>();
            grid = FindAnyObjectByType<DungeonGrid>();
            if (shopUI == null) shopUI = FindAnyObjectByType<RoomShopUI>();
            if (inventoryUI == null) inventoryUI = FindAnyObjectByType<RoomInventoryUI>();
            if (detailsUI == null) detailsUI = FindAnyObjectByType<RoomDetailsUI>();
            if (monsterShopUI == null) monsterShopUI = FindAnyObjectByType<MonsterShopUI>();
            if (monsterInventoryUI == null) monsterInventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
            if (trapShopUI == null) trapShopUI = FindAnyObjectByType<TrapShopUI>();
            if (trapInventoryUI == null) trapInventoryUI = FindAnyObjectByType<TrapInventoryUI>();
            if (monsterPlacement == null) monsterPlacement = FindAnyObjectByType<MonsterPlacementManager>();
            if (trapPlacement == null) trapPlacement = FindAnyObjectByType<TrapPlacementManager>();
            if (progressionUI == null) progressionUI = FindAnyObjectByType<DungeonProgressionUI>();
            if (bossUI == null) bossUI = FindAnyObjectByType<BossSelectionUI>();
            if (upgradeUI == null) upgradeUI = FindAnyObjectByType<DungeonUpgradeUI>();
            if (bossPlacement == null) bossPlacement = FindAnyObjectByType<BossPlacementManager>();
            if (monsterDetailsUI == null) monsterDetailsUI = FindAnyObjectByType<MonsterDetailsUI>();
            if (trapDetailsUI == null) trapDetailsUI = FindAnyObjectByType<TrapDetailsUI>();
            EnsureMilestone7Systems();
            headerText = FindText("Header");
            levelValueText = FindText("LevelValue");
            goldText = FindText("GoldValue");
            soulText = FindText("SoulValue");
            crystalText = FindText("CrystalValue");
            selectionText = FindText("SelectedCellText");
            statusText = FindText("BuildStatusText");
            noticeText = FindText("NoticeText");
            noticePanel = GameObject.Find("NoticePanel");
            GameObject startRaidObj = GameObject.Find("StartRaidButton");
            startRaidButton = startRaidObj != null ? startRaidObj.GetComponent<Button>() : null;
            GameObject pathButton = GameObject.Find("PathButton");
            pathButtonImage = pathButton != null ? pathButton.GetComponent<Image>() : null;
            if (pathButtonImage != null) pathButtonColor = pathButtonImage.color;

            Wire("BackButton", () => SceneLoader.Load("MainMenu"));
            Wire("RoomButton", placement.SelectRoomTool);
            Wire("MonsterRoomButton", placement.SelectMonsterRoom);
            Wire("TreasureRoomButton", placement.SelectTreasureRoom);
            Wire("PathButton", placement.SelectPath);
            Wire("MoveButton", placement.SelectMove);
            Wire("RemoveButton", placement.SelectRemove);
            Wire("RotateButton", placement.Rotate);
            Wire("UndoButton", Undo);
            Wire("RedoButton", Redo);
            Wire("MonsterButton", () => monsterInventoryUI?.Open());
            Wire("TrapButton", () => trapInventoryUI?.Open());
            Wire("OpenMonsterShopButton", () => monsterShopUI?.Open());
            Wire("OpenMonsterInventoryButton", () => monsterInventoryUI?.Open());
            Wire("OpenTrapShopButton", () => trapShopUI?.Open());
            Wire("OpenTrapInventoryButton", () => trapInventoryUI?.Open());
            Wire("StartRaidButton", StartRaid);
            Wire("CloseNoticeButton", CloseNotice);
            Wire("OpenRoomShopButton", () => shopUI?.Open());
            Wire("OpenRoomInventoryButton", () => inventoryUI?.Open());
            Wire("ProgressionButton", () => progressionUI?.Open());
            Wire("OpenProgressionButton", () => progressionUI?.Open());
            Wire("BossButton", () => bossUI?.Open());
            Wire("OpenBossButton", () => bossUI?.Open());
            Wire("UpgradeButton", () => upgradeUI?.Open());
            Wire("OpenUpgradeButton", () => upgradeUI?.Open());
            Wire("QuestButton", () => questUI?.Open());
            Wire("OpenQuestButton", () => questUI?.Open());
            Wire("AchievementButton", () => achievementUI?.Open());
            Wire("OpenAchievementButton", () => achievementUI?.Open());
            Wire("WorldMapButton", () => worldMapUI?.Open());
            Wire("OpenWorldMapButton", () => worldMapUI?.Open());

            if (placement != null)
            {
                placement.StatusChanged += SetStatus;
                placement.ToolChanged += OnToolChanged;
                OnToolChanged(placement.ActiveTool);
            }
            if (monsterPlacement != null) monsterPlacement.StatusChanged += SetStatus;
            if (trapPlacement != null) trapPlacement.StatusChanged += SetStatus;
            if (bossPlacement != null) bossPlacement.StatusChanged += SetStatus;
            if (grid != null) grid.CellSelected += OnCellSelected;
            if (DungeonProgressionManager.Instance != null)
            {
                DungeonProgressionManager.Instance.OnLevelUp += OnDungeonLevelChanged;
                DungeonProgressionManager.Instance.OnExpChanged += OnDungeonExpChanged;
            }
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameLoaded += OnGameResetOrLoaded;
            }
            if (roomManager != null)
            {
                roomManager.GoldChanged += SetGold;
                roomManager.LayoutChanged += RefreshCellInfo;
                roomManager.Initialize();
                if (GameManager.IsNewGameRequested)
                {
                    GameManager.ClearNewGameRequest();
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.ResetGame();
                    else
                        ResetEditorForNewGame();
                }
                else if (BattleSession.RestoreEditor(roomManager))
                {
                    SetStatus($"Raid complete. Rewards: {BattleSession.RewardGold} Gold, {BattleSession.RewardSoul} Soul, {BattleSession.RewardExp} EXP.");
                }
                SetGold(roomManager.Gold);
                if (roomManager.Currency != null)
                {
                    roomManager.Currency.CurrencyChanged += SetCurrencies;
                    SetCurrencies(roomManager.Currency.Gold, roomManager.Currency.Soul, roomManager.Currency.Crystal);
                }
            }
            RefreshLevelTexts();
            CloseAllSubPanels();
            isStartingRaid = false;
            if (startRaidButton != null) startRaidButton.interactable = true;
            SetStatus("Select a room or path tool, then tap a green preview cell.");

            if (BattleSession.ReturnToWorldMapPending)
            {
                string nextStageId = BattleSession.NextStageToOpenPending;
                BattleSession.ClearWorldMapNavigationRequests();
                if (!string.IsNullOrEmpty(nextStageId) && CampaignManager.Instance != null)
                {
                    CampaignStageData nextStage = CampaignManager.Instance.GetStage(nextStageId);
                    if (nextStage != null && stageDetailsUI != null)
                    {
                        stageDetailsUI.OpenForStage(nextStage);
                    }
                    else
                    {
                        worldMapUI?.Open();
                    }
                }
                else
                {
                    worldMapUI?.Open();
                }
            }
        }

        private void OnDestroy()
        {
            if (placement != null)
            {
                placement.StatusChanged -= SetStatus;
                placement.ToolChanged -= OnToolChanged;
            }
            if (monsterPlacement != null) monsterPlacement.StatusChanged -= SetStatus;
            if (trapPlacement != null) trapPlacement.StatusChanged -= SetStatus;
            if (bossPlacement != null) bossPlacement.StatusChanged -= SetStatus;
            if (grid != null) grid.CellSelected -= OnCellSelected;
            if (DungeonProgressionManager.Instance != null)
            {
                DungeonProgressionManager.Instance.OnLevelUp -= OnDungeonLevelChanged;
                DungeonProgressionManager.Instance.OnExpChanged -= OnDungeonExpChanged;
            }
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameLoaded -= OnGameResetOrLoaded;
            }
            if (roomManager != null)
            {
                roomManager.GoldChanged -= SetGold;
                roomManager.LayoutChanged -= RefreshCellInfo;
                if (roomManager.Currency != null) roomManager.Currency.CurrencyChanged -= SetCurrencies;
            }
        }

        public void ResetEditorForNewGame()
        {
            BattleSession.Reset();
            GameManager.Instance?.ResetExperience();
            DungeonProgressionManager.Instance?.ResetProgression();
            DungeonCoreUpgradeManager.Instance?.ResetUpgrade();
            DungeonExpansionManager.Instance?.ResetExpansion();
            DungeonFacilityManager.Instance?.ResetFacilities();
            roomManager?.ResetToInitialState();
            OnGameResetOrLoaded();
        }

        private void OnGameResetOrLoaded()
        {
            CloseAllSubPanels();
            isStartingRaid = false;
            if (startRaidButton != null) startRaidButton.interactable = true;
            RefreshLevelTexts();
            RefreshCellInfo();
            if (roomManager != null)
            {
                if (roomManager.Currency != null)
                    SetCurrencies(roomManager.Currency.Gold, roomManager.Currency.Soul, roomManager.Currency.Crystal);
                else
                    SetGold(roomManager.Gold);
            }
        }

        public void CloseAllSubPanels()
        {
            CloseNotice();
            shopUI?.Close();
            inventoryUI?.Close();
            detailsUI?.Close();
            monsterShopUI?.Close();
            monsterInventoryUI?.Close();
            trapShopUI?.Close();
            trapInventoryUI?.Close();
            monsterDetailsUI?.Close();
            trapDetailsUI?.Close();
            progressionUI?.Close();
            bossUI?.Close();
            upgradeUI?.Close();
            questUI?.Close();
            achievementUI?.Close();
            worldMapUI?.Close();
            campaignSelectionUI?.Close();
            stageDetailsUI?.Close();
        }

        private void EnsureMilestone7Systems()
        {
            GameObject builderObj = roomManager != null ? roomManager.gameObject : GameObject.Find("DungeonBuilder");
            if (builderObj != null)
            {
                if (builderObj.GetComponent<QuestManager>() == null) builderObj.AddComponent<QuestManager>();
                if (builderObj.GetComponent<AchievementManager>() == null) builderObj.AddComponent<AchievementManager>();
                if (builderObj.GetComponent<DifficultyManager>() == null) builderObj.AddComponent<DifficultyManager>();
                if (builderObj.GetComponent<WorldMapManager>() == null) builderObj.AddComponent<WorldMapManager>();
                if (builderObj.GetComponent<CampaignManager>() == null) builderObj.AddComponent<CampaignManager>();
            }

            GameObject safeObj = GameObject.Find("SafeAreaRoot");
            RectTransform safe = safeObj != null ? safeObj.GetComponent<RectTransform>() : null;

            if (questUI == null) questUI = FindAnyObjectByType<QuestUIManager>();
            if (questUI == null)
            {
                questUI = gameObject.AddComponent<QuestUIManager>();
                if (safe != null) questUI.Configure(safe);
            }

            if (achievementUI == null) achievementUI = FindAnyObjectByType<AchievementUIManager>();
            if (achievementUI == null)
            {
                achievementUI = gameObject.AddComponent<AchievementUIManager>();
                if (safe != null) achievementUI.Configure(safe);
            }

            if (notificationUI == null) notificationUI = FindAnyObjectByType<QuestNotificationUI>();
            if (notificationUI == null)
            {
                notificationUI = gameObject.AddComponent<QuestNotificationUI>();
                if (safe != null) notificationUI.Configure(safe);
            }

            if (stageDetailsUI == null) stageDetailsUI = FindAnyObjectByType<StageDetailsUI>();
            if (stageDetailsUI == null)
            {
                stageDetailsUI = gameObject.AddComponent<StageDetailsUI>();
                if (safe != null) stageDetailsUI.Configure(safe);
            }

            if (campaignSelectionUI == null) campaignSelectionUI = FindAnyObjectByType<CampaignSelectionUI>();
            if (campaignSelectionUI == null)
            {
                campaignSelectionUI = gameObject.AddComponent<CampaignSelectionUI>();
                if (safe != null) campaignSelectionUI.Configure(safe, null, null, null, stageDetailsUI);
            }

            if (worldMapUI == null) worldMapUI = FindAnyObjectByType<WorldMapUI>();
            if (worldMapUI == null)
            {
                worldMapUI = gameObject.AddComponent<WorldMapUI>();
                if (safe != null) worldMapUI.Configure(safe, null, campaignSelectionUI);
            }

            if (safe != null)
            {
                if (GameObject.Find("QuestButton") == null && GameObject.Find("OpenQuestButton") == null)
                {
                    RoomUIFactory.CreateButton(safe, "QuestButton", "QUESTS", new Vector2(-280f, 435f), new Vector2(210f, 56f), new Color(0.32f, 0.25f, 0.14f), 15);
                }
                if (GameObject.Find("WorldMapButton") == null && GameObject.Find("OpenWorldMapButton") == null)
                {
                    RoomUIFactory.CreateButton(safe, "WorldMapButton", "WORLD MAP", new Vector2(0f, 435f), new Vector2(210f, 56f), new Color(0.16f, 0.34f, 0.42f), 15);
                }
                if (GameObject.Find("AchievementButton") == null && GameObject.Find("OpenAchievementButton") == null)
                {
                    RoomUIFactory.CreateButton(safe, "AchievementButton", "ACHIEVEMENTS", new Vector2(280f, 435f), new Vector2(220f, 56f), new Color(0.28f, 0.20f, 0.36f), 15);
                }
            }
        }

        private void OnDungeonLevelChanged(int newLevel) => RefreshLevelTexts();
        private void OnDungeonExpChanged(int exp, int req) => RefreshLevelTexts();

        private void RefreshLevelTexts()
        {
            int level = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            if (headerText != null) headerText.text = $"DUNGEON  •  LEVEL {level}";
            if (levelValueText != null) levelValueText.text = $"LEVEL  {level}";
        }

        private void Undo()
        {
            if (roomManager.TryUndo(out string message)) SetStatus(message);
            else SetStatus(message);
        }

        private void Redo()
        {
            if (roomManager.TryRedo(out string message)) SetStatus(message);
            else SetStatus(message);
        }

        private void ShowNotice(string message)
        {
            isStartingRaid = false;
            if (noticeText != null) noticeText.text = message;
            if (noticePanel != null) noticePanel.SetActive(true);
        }

        private void CloseNotice()
        {
            isStartingRaid = false;
            if (noticePanel != null) noticePanel.SetActive(false);
        }

        private void StartRaid()
        {
            if (isStartingRaid) return;
            isStartingRaid = true;

            if (roomManager == null) { ShowNotice("Dungeon Room System is unavailable."); return; }
            roomManager.Initialize();
            DungeonLayoutData layout = roomManager.Layout;
            if (layout == null || layout.rooms == null) { ShowNotice("Dungeon Layout is missing."); return; }
            if (layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.Entrance).Count != 1) { ShowNotice("Raid cannot start: place exactly one Entrance."); return; }
            if (layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.DungeonCore).Count != 1) { ShowNotice("Raid cannot start: place exactly one Dungeon Core."); return; }
            if (roomManager.Pathfinder == null || !roomManager.Pathfinder.TryFindRoute(layout, out List<Vector2Int> route) || route == null || route.Count < 2)
            { ShowNotice("Raid cannot start: no walkable Path connects Entrance to Dungeon Core."); return; }
            string error = string.Empty;
            if (roomManager.Monsters == null || !roomManager.Monsters.ValidateDatabase(out error)) { ShowNotice("Raid cannot start: " + (roomManager.Monsters == null ? "Monster Database is not configured." : error)); return; }
            if (roomManager.Traps == null || !roomManager.Traps.ValidateDatabase(out error)) { ShowNotice("Raid cannot start: " + (roomManager.Traps == null ? "Trap Database is not configured." : error)); return; }
            BattleConfiguration config = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
            AdventurerDatabase adventurers = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
            if (config == null || adventurers == null || !config.Validate(adventurers, out error))
            { ShowNotice("Raid cannot start: " + (config == null || adventurers == null ? "Battle Configuration or Adventurer Database is missing." : error)); return; }
            if (layout.monsters == null || layout.traps == null) { ShowNotice("Raid cannot start: Monster or Trap instance data is missing."); return; }
            var ids = new HashSet<string>();
            foreach (MonsterInstanceData instance in layout.monsters)
            {
                if (instance == null || string.IsNullOrWhiteSpace(instance.instanceId) || !ids.Add(instance.instanceId) || roomManager.Monsters.FindById(instance.monsterId) == null || roomManager.FindRoom(instance.roomInstanceId)?.roomType != DungeonRoomType.MonsterRoom)
                { ShowNotice("Raid cannot start: a Monster instance is invalid or duplicated."); return; }
            }
            foreach (TrapInstanceData instance in layout.traps)
            {
                if (instance == null || string.IsNullOrWhiteSpace(instance.instanceId) || !ids.Add(instance.instanceId) || roomManager.Traps.FindById(instance.trapId) == null || roomManager.FindRoom(instance.roomInstanceId)?.roomType != DungeonRoomType.TrapRoom)
                { ShowNotice("Raid cannot start: a Trap instance is invalid or duplicated."); return; }
            }
            CurrencyManager currency = roomManager.Currency;
            BattleSession.Begin(roomManager.LayoutToJson(), "DungeonEditor", currency != null ? currency.Gold : roomManager.Gold,
                currency != null ? currency.Soul : 0, currency != null ? currency.Crystal : 0);
            if (!SceneLoader.Load("Battle")) ShowNotice("Raid cannot start: Battle scene is not included in Build Settings.");
        }

        private void SetGold(int value)
        {
            if (goldText != null) goldText.text = value.ToString();
        }

        private void SetCurrencies(int gold, int soul, int crystal)
        {
            SetGold(gold);
            if (soulText != null) soulText.text = $"SOUL  {soul}";
            if (crystalText != null) crystalText.text = $"CRYSTAL  {crystal}";
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void OnToolChanged(DungeonBuildTool tool)
        {
            if (pathButtonImage != null)
                pathButtonImage.color = tool == DungeonBuildTool.Path
                    ? new Color(.76f, .66f, .30f, 1f)
                    : pathButtonColor;
        }

        private void RefreshCellInfo()
        {
            OnCellSelected(grid != null ? grid.SelectedCell : null);
        }

        private void OnCellSelected(DungeonCell cell)
        {
            if (selectionText == null) return;
            if (cell == null) { selectionText.text = "NO CELL SELECTED"; return; }
            selectionText.text = $"CELL  {cell.Position.x + 1}, {cell.Position.y + 1}   •   {cell.CellType}";
        }

        private static Text FindText(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Text>() : null;
        }

        private static void Wire(string objectName, UnityEngine.Events.UnityAction action)
        {
            GameObject go = GameObject.Find(objectName);
            Button button = go != null ? go.GetComponent<Button>() : null;
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
