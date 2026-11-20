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

        public DungeonSaveData()
        {
            saveVersion = 1;
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            gold = 1000;
            soul = 200;
            progression = new DungeonProgressionData();
            coreLevel = 1;
            gridDimension = 9;
            facilityGoldLevel = 1;
            facilitySoulLevel = 1;
            facilityCapacityLevel = 1;
            dungeonLayoutJson = string.Empty;
        }
    }

    [DefaultExecutionOrder(-50)]
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveKey = "DreamDungeon_SaveData";

        public event Action OnGameSaved;
        public event Action OnGameLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadGame();
        }

        public bool HasSave()
        {
            return PlayerPrefs.HasKey(SaveKey);
        }

        public void SaveGame()
        {
            try
            {
                var saveData = new DungeonSaveData
                {
                    saveVersion = 1,
                    saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                if (CurrencyManager.Instance != null)
                {
                    saveData.gold = CurrencyManager.Instance.Gold;
                    saveData.soul = CurrencyManager.Instance.Soul;
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
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            CurrencyManager.Instance?.ResetCurrencies();
            DungeonProgressionManager.Instance?.ResetProgression();
            DungeonCoreUpgradeManager.Instance?.ResetUpgrade();
            DungeonExpansionManager.Instance?.ResetExpansion();
            DungeonFacilityManager.Instance?.ResetFacilities();

            OnGameLoaded?.Invoke();
        }
    }
}
