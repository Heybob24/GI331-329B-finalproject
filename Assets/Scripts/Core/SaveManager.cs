using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class DungeonSaveData
    {
        public int saveVersion = 1;
        public long saveTimestamp = 0;

        [Header("Currencies")]
        public int gold = 1000;
        public int soul = 200;
        public int crystal = 0;

        [Header("Progression")]
        public DungeonProgressionData progression;

        [Header("Upgrades")]
        public int coreLevel = 1;
        public int gridDimension = 9;
        public int facilityGoldLevel = 1;
        public int facilitySoulLevel = 1;
        public int facilityCapacityLevel = 1;

        [Header("Layout")]
        public string dungeonLayoutJson = string.Empty;

        [Header("Inventories")]
        public string roomInventoryJson = string.Empty;
        public string monsterInventoryJson = string.Empty;
        public string trapInventoryJson = string.Empty;

        [Header("Milestone 7: Quests & Achievements")]
        public QuestSaveState questSaveState;
        public AchievementSaveState achievementSaveState;

        [Header("Milestone 8: World Map & Campaign")]
        public CampaignSaveState campaignSaveState;

        public DungeonSaveData()
        {
            saveVersion = 3;
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            gold = 1000;
            soul = 200;
            crystal = 0;
            progression = new DungeonProgressionData();
            coreLevel = 1;
            gridDimension = 9;
            facilityGoldLevel = 1;
            facilitySoulLevel = 1;
            facilityCapacityLevel = 1;
            dungeonLayoutJson = string.Empty;
            roomInventoryJson = string.Empty;
            monsterInventoryJson = string.Empty;
            trapInventoryJson = string.Empty;
            questSaveState = new QuestSaveState();
            achievementSaveState = new AchievementSaveState();
            campaignSaveState = new CampaignSaveState();
        }
    }

    [DefaultExecutionOrder(-50)]
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveKey = "DreamDungeon_SaveData";
        private const string PermanentAchievementSaveKey = "DreamDungeon_AchievementData";
        private const string PermanentQuestSaveKey = "DreamDungeon_QuestData";
        private const string PermanentCampaignSaveKey = "DreamDungeon_CampaignData";

        public event Action OnGameSaved;
        public event Action OnGameLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Destroy(this);
                }
                return;
            }
            Instance = this;
            if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            LoadPermanentState();
            if (GameManager.IsNewGameRequested)
            {
                GameManager.ClearNewGameRequest();
                ResetGame();
                return;
            }
            if (!BattleSession.ReturnPending)
            {
                LoadGame();
            }
        }

        public bool HasSave()
        {
            return PlayerPrefs.HasKey(SaveKey);
        }

        public void SaveAchievements()
        {
            try
            {
                if (AchievementManager.Instance != null)
                {
                    var achState = AchievementManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentAchievementSaveKey, JsonUtility.ToJson(achState, false));
                }
                if (QuestManager.Instance != null)
                {
                    var qState = QuestManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentQuestSaveKey, JsonUtility.ToJson(qState, false));
                }
                if (CampaignManager.Instance != null)
                {
                    var cState = CampaignManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentCampaignSaveKey, JsonUtility.ToJson(cState, false));
                }
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save permanent state: {ex.Message}");
            }
        }

        public bool LoadPermanentState()
        {
            bool loadedAny = false;
            try
            {
                if (PlayerPrefs.HasKey(PermanentAchievementSaveKey) && AchievementManager.Instance != null)
                {
                    string achJson = PlayerPrefs.GetString(PermanentAchievementSaveKey, string.Empty);
                    if (!string.IsNullOrEmpty(achJson))
                    {
                        var achState = JsonUtility.FromJson<AchievementSaveState>(achJson);
                        if (achState != null)
                        {
                            AchievementManager.Instance.ImportSaveState(achState);
                            loadedAny = true;
                        }
                    }
                }
                if (PlayerPrefs.HasKey(PermanentQuestSaveKey) && QuestManager.Instance != null)
                {
                    string qJson = PlayerPrefs.GetString(PermanentQuestSaveKey, string.Empty);
                    if (!string.IsNullOrEmpty(qJson))
                    {
                        var qState = JsonUtility.FromJson<QuestSaveState>(qJson);
                        if (qState != null)
                        {
                            QuestManager.Instance.ImportSaveState(qState);
                            loadedAny = true;
                        }
                    }
                }
                if (PlayerPrefs.HasKey(PermanentCampaignSaveKey) && CampaignManager.Instance != null)
                {
                    string cJson = PlayerPrefs.GetString(PermanentCampaignSaveKey, string.Empty);
                    if (!string.IsNullOrEmpty(cJson))
                    {
                        var cState = JsonUtility.FromJson<CampaignSaveState>(cJson);
                        if (cState != null)
                        {
                            CampaignManager.Instance.ImportSaveState(cState);
                            loadedAny = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load permanent state: {ex.Message}");
            }
            return loadedAny;
        }

        public void SaveGame()
        {
            try
            {
                var saveData = new DungeonSaveData
                {
                    saveVersion = 3,
                    saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                if (CurrencyManager.Instance != null)
                {
                    saveData.gold = CurrencyManager.Instance.Gold;
                    saveData.soul = CurrencyManager.Instance.Soul;
                    saveData.crystal = CurrencyManager.Instance.Crystal;
                }

                if (DungeonProgressionManager.Instance != null)
                {
                    saveData.progression = DungeonProgressionManager.Instance.ExportData();
                }

                if (DungeonCoreUpgradeManager.Instance != null)
                {
                    saveData.coreLevel = DungeonCoreUpgradeManager.Instance.CoreLevel;
                }

                if (DungeonExpansionManager.Instance != null)
                {
                    saveData.gridDimension = DungeonExpansionManager.Instance.CurrentDimension;
                }

                if (DungeonFacilityManager.Instance != null)
                {
                    saveData.facilityGoldLevel = DungeonFacilityManager.Instance.GoldFacilityLevel;
                    saveData.facilitySoulLevel = DungeonFacilityManager.Instance.SoulFacilityLevel;
                    saveData.facilityCapacityLevel = DungeonFacilityManager.Instance.CapacityFacilityLevel;
                }

                if (RoomManager.Instance != null)
                {
                    saveData.dungeonLayoutJson = RoomManager.Instance.LayoutToJson();
                }

                var roomInv = FindAnyObjectByType<RoomInventoryManager>();
                if (roomInv != null) saveData.roomInventoryJson = roomInv.ToJson();
                var monsterInv = FindAnyObjectByType<MonsterInventoryManager>();
                if (monsterInv != null) saveData.monsterInventoryJson = monsterInv.ToJson();
                var trapInv = FindAnyObjectByType<TrapInventoryManager>();
                if (trapInv != null) saveData.trapInventoryJson = trapInv.ToJson();

                if (QuestManager.Instance != null)
                {
                    saveData.questSaveState = QuestManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentQuestSaveKey, JsonUtility.ToJson(saveData.questSaveState, false));
                }

                if (AchievementManager.Instance != null)
                {
                    saveData.achievementSaveState = AchievementManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentAchievementSaveKey, JsonUtility.ToJson(saveData.achievementSaveState, false));
                }

                if (CampaignManager.Instance != null)
                {
                    saveData.campaignSaveState = CampaignManager.Instance.ExportSaveState();
                    PlayerPrefs.SetString(PermanentCampaignSaveKey, JsonUtility.ToJson(saveData.campaignSaveState, false));
                }

                string json = JsonUtility.ToJson(saveData, false);
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();

                OnGameSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {ex.Message}");
            }
        }

        public bool LoadGame()
        {
            LoadPermanentState();
            if (!HasSave()) return false;

            try
            {
                string json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (string.IsNullOrEmpty(json)) return false;

                var saveData = JsonUtility.FromJson<DungeonSaveData>(json);
                if (saveData == null) return false;

                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.SetGold(saveData.gold);
                    CurrencyManager.Instance.SetSoul(saveData.soul);
                    CurrencyManager.Instance.SetCrystal(saveData.crystal);
                }

                if (DungeonProgressionManager.Instance != null && saveData.progression != null)
                {
                    DungeonProgressionManager.Instance.ImportData(saveData.progression);
                }

                if (DungeonCoreUpgradeManager.Instance != null)
                {
                    DungeonCoreUpgradeManager.Instance.SetLevel(saveData.coreLevel);
                }

                if (DungeonExpansionManager.Instance != null)
                {
                    DungeonExpansionManager.Instance.SetDimension(saveData.gridDimension);
                }

                if (DungeonFacilityManager.Instance != null)
                {
                    DungeonFacilityManager.Instance.SetLevels(saveData.facilityGoldLevel, saveData.facilitySoulLevel, saveData.facilityCapacityLevel);
                }

                if (RoomManager.Instance != null && !string.IsNullOrEmpty(saveData.dungeonLayoutJson))
                {
                    RoomManager.Instance.LoadLayoutJson(saveData.dungeonLayoutJson, out _);
                }

                if (!string.IsNullOrEmpty(saveData.roomInventoryJson))
                {
                    FindAnyObjectByType<RoomInventoryManager>()?.LoadFromJson(saveData.roomInventoryJson, out _);
                }
                if (!string.IsNullOrEmpty(saveData.monsterInventoryJson))
                {
                    FindAnyObjectByType<MonsterInventoryManager>()?.LoadFromJson(saveData.monsterInventoryJson, out _);
                }
                if (!string.IsNullOrEmpty(saveData.trapInventoryJson))
                {
                    FindAnyObjectByType<TrapInventoryManager>()?.LoadFromJson(saveData.trapInventoryJson, out _);
                }

                if (QuestManager.Instance != null && saveData.questSaveState != null &&
                    (saveData.questSaveState.entries.Count > 0 || saveData.questSaveState.activeDailyQuestIds.Count > 0))
                {
                    QuestManager.Instance.ImportSaveState(saveData.questSaveState);
                }

                if (AchievementManager.Instance != null && saveData.achievementSaveState != null && saveData.achievementSaveState.entries.Count > 0)
                {
                    AchievementManager.Instance.ImportSaveState(saveData.achievementSaveState);
                }

                if (CampaignManager.Instance != null && saveData.campaignSaveState != null &&
                    (saveData.campaignSaveState.unlockedRegionIds.Count > 0 || saveData.campaignSaveState.stageRecords.Count > 0))
                {
                    CampaignManager.Instance.ImportSaveState(saveData.campaignSaveState);
                }

                OnGameLoaded?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load game: {ex.Message}");
                return false;
            }
        }

        public void ResetGame()
        {
            // Preserve permanent Achievements & Quest progress across New Game / Play Again resets
            SaveAchievements();

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            BattleSession.Reset();
            GameManager.Instance?.ResetExperience();
            CurrencyManager.Instance?.ResetCurrencies();
            DungeonProgressionManager.Instance?.ResetProgression();
            DungeonCoreUpgradeManager.Instance?.ResetUpgrade();
            DungeonExpansionManager.Instance?.ResetExpansion();
            DungeonFacilityManager.Instance?.ResetFacilities();
            RoomManager.Instance?.ResetToInitialState();

            LoadPermanentState();

            OnGameLoaded?.Invoke();
        }
    }
}
