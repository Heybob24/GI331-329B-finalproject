using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
#if UGS_CLOUDSAVE_ENABLED
using Unity.Services.CloudSave;
#endif

namespace DreamDungeon
{
    public struct DatabaseOperationResult
    {
        public bool Success;
        public AuthErrorCode ErrorCode;
        public string Message;
        public PlayerDataModel Data;
    }

    public sealed class PlayerDatabaseManager : MonoBehaviour
    {
        public static PlayerDatabaseManager Instance { get; private set; }

        private const string CloudKeyAccount = "dd_account_data";
        private const string CloudKeyCurrency = "dd_currency_data";
        private const string CloudKeyDungeon = "dd_dungeon_data";
        private const string CloudKeyProgression = "dd_progression_data";
        private const string CloudKeyPurchases = "dd_purchase_history";
        private const string CloudKeyMeta = "dd_meta_revision";

        [Serializable]
        private sealed class MetaRevisionRecord
        {
            public int schemaVersion = 1;
            public long revision = 1;
            public long lastModifiedTimestampUtc = 0;
            public string lastDeviceId = string.Empty;
        }

        [Serializable]
        private sealed class PurchaseListWrapper
        {
            public List<CrystalPurchaseRecord> items = new List<CrystalPurchaseRecord>();
        }

        private PlayerDataModel currentPlayerData;
        private DataSyncState syncState = DataSyncState.Idle;
        private bool isBusy;
        private string simulatedOfflineReason = string.Empty;

        public PlayerDataModel CurrentData => currentPlayerData;
        public DataSyncState SyncState => syncState;
        public bool IsBusy => isBusy;
        public bool SimulateNetworkOffline { get; set; }

        public event Action<DataSyncState, string> OnSyncStateChanged;
        public event Action<PlayerDataModel> OnPlayerDataLoaded;
        public event Action<PlayerDataModel> OnPlayerDataSaved;

        private string LocalCloudFolder => Path.Combine(Application.persistentDataPath, "DreamDungeonCloudDB");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
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

        public bool HasNetworkConnection()
        {
            if (SimulateNetworkOffline)
            {
                simulatedOfflineReason = "No internet connection detected.";
                return false;
            }
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public void ClearActivePlayerDataInMemory()
        {
            currentPlayerData = null;
            SetSyncState(DataSyncState.Idle, "Signed out.");
        }

        public async Task<DatabaseOperationResult> LoadOrCreatePlayerDataAsync(PlayerProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.playerId))
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.PlayerDataLoadFailed,
                    Message = "Cannot load player data: Player ID is missing."
                };
            }

            if (!HasNetworkConnection())
            {
                SetSyncState(DataSyncState.Offline, "Offline — cannot reach Player Database.");
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.NoInternet,
                    Message = "No internet connection. Please check your network and retry."
                };
            }

            if (isBusy)
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.PlayerDataLoadFailed,
                    Message = "Another database sync operation is already in progress."
                };
            }

            isBusy = true;
            SetSyncState(DataSyncState.Syncing, "Loading player data...");

            try
            {
                PlayerDataModel loaded = await LoadFromBackendAsync(profile.playerId);
                if (loaded == null)
                {
                    loaded = PlayerDataModel.CreateInitial(profile, SystemInfo.deviceUniqueIdentifier);
                    CaptureActiveGameStateIntoModel(loaded);
                    bool savedInitial = await SaveToBackendAsync(loaded);
                    if (!savedInitial)
                    {
                        SetSyncState(DataSyncState.Error, "Failed to initialize cloud profile.");
                        return new DatabaseOperationResult
                        {
                            Success = false,
                            ErrorCode = AuthErrorCode.CloudSaveFailed,
                            Message = "Failed to save initial player profile to Player Database."
                        };
                    }
                }
                else
                {
                    // Refresh last login timestamp while preserving existing player identity and progress
                    loaded.accountData.lastLoginUtc = DateTime.UtcNow.ToString("o");
                    if (profile.loginType == AuthLoginType.Google || profile.loginType == AuthLoginType.GoogleLinked)
                    {
                        loaded.accountData.loginType = profile.loginType;
                        loaded.accountData.authenticationProvider = "Google";
                    }
                }

                currentPlayerData = loaded;
                ApplyModelToActiveGameSystems(currentPlayerData);
                SetSyncState(DataSyncState.Synced, $"Synced (Rev {currentPlayerData.revision})");
                OnPlayerDataLoaded?.Invoke(currentPlayerData);

                return new DatabaseOperationResult
                {
                    Success = true,
                    ErrorCode = AuthErrorCode.None,
                    Message = "Player data loaded successfully.",
                    Data = currentPlayerData
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDatabaseManager] Load failed: {ex.Message}");
                SetSyncState(DataSyncState.Error, "Load failed.");
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.PlayerDataLoadFailed,
                    Message = $"Failed to load player data: {ex.Message}"
                };
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task<DatabaseOperationResult> SaveCurrentPlayerDataAsync(bool captureFromGameSystems = true, bool allowOverwriteOnConflict = false)
        {
            if (currentPlayerData == null || string.IsNullOrWhiteSpace(currentPlayerData.accountData?.playerId))
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.CloudSaveFailed,
                    Message = "No active player profile to save."
                };
            }

            if (!HasNetworkConnection())
            {
                SetSyncState(DataSyncState.Offline, "Offline — changes pending network.");
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.NoInternet,
                    Message = "No internet connection. Cloud Save paused until online."
                };
            }

            if (isBusy)
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.CloudSaveFailed,
                    Message = "Sync already in progress. Duplicate save prevented."
                };
            }

            isBusy = true;
            SetSyncState(DataSyncState.Syncing, "Saving to Player Database...");

            try
            {
                string playerId = currentPlayerData.accountData.playerId;

                // Revision check: prevent stale local data from overwriting newer server data
                if (!allowOverwriteOnConflict)
                {
                    PlayerDataModel serverLatest = await LoadFromBackendAsync(playerId);
                    if (serverLatest != null && serverLatest.revision > currentPlayerData.revision)
                    {
                        SetSyncState(DataSyncState.Conflict, $"Server has newer revision ({serverLatest.revision} > {currentPlayerData.revision}).");
                        return new DatabaseOperationResult
                        {
                            Success = false,
                            ErrorCode = AuthErrorCode.StaleRevisionConflict,
                            Message = $"Cloud data is newer (Server Rev {serverLatest.revision} vs Local Rev {currentPlayerData.revision}). Pull latest data before saving.",
                            Data = serverLatest
                        };
                    }
                }

                if (captureFromGameSystems)
                {
                    CaptureActiveGameStateIntoModel(currentPlayerData);
                }

                currentPlayerData.revision++;
                currentPlayerData.lastModifiedTimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                currentPlayerData.lastDeviceId = SystemInfo.deviceUniqueIdentifier;

                bool ok = await SaveToBackendAsync(currentPlayerData);
                if (!ok)
                {
                    currentPlayerData.revision = Math.Max(1, currentPlayerData.revision - 1);
                    SetSyncState(DataSyncState.Error, "Cloud save failed.");
                    return new DatabaseOperationResult
                    {
                        Success = false,
                        ErrorCode = AuthErrorCode.CloudSaveFailed,
                        Message = "Failed to write player data to Cloud Save."
                    };
                }

                SetSyncState(DataSyncState.Synced, $"Saved (Rev {currentPlayerData.revision})");
                OnPlayerDataSaved?.Invoke(currentPlayerData);

                return new DatabaseOperationResult
                {
                    Success = true,
                    ErrorCode = AuthErrorCode.None,
                    Message = "Player data saved successfully.",
                    Data = currentPlayerData
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDatabaseManager] Save failed: {ex.Message}");
                SetSyncState(DataSyncState.Error, "Cloud save failed.");
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.CloudSaveFailed,
                    Message = $"Cloud Save failed: {ex.Message}"
                };
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task<DatabaseOperationResult> DeletePlayerDataAsync(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.CloudSaveFailed,
                    Message = "Invalid Player ID."
                };
            }

            if (!HasNetworkConnection())
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.NoInternet,
                    Message = "No internet connection. Cannot delete account data while offline."
                };
            }

            try
            {
                await DeleteFromBackendAsync(playerId);
                if (currentPlayerData != null && string.Equals(currentPlayerData.accountData.playerId, playerId, StringComparison.OrdinalIgnoreCase))
                {
                    currentPlayerData = null;
                }
                SetSyncState(DataSyncState.Idle, "Account data deleted.");
                return new DatabaseOperationResult
                {
                    Success = true,
                    ErrorCode = AuthErrorCode.None,
                    Message = "Player database records deleted."
                };
            }
            catch (Exception ex)
            {
                return new DatabaseOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.CloudSaveFailed,
                    Message = $"Failed to delete player data: {ex.Message}"
                };
            }
        }

        public void CaptureActiveGameStateIntoModel(PlayerDataModel model)
        {
            if (model == null) return;
            if (model.currencyData == null) model.currencyData = new CurrencyDataRecord();
            if (model.dungeonData == null) model.dungeonData = new DungeonDataRecord();
            if (model.progressionData == null) model.progressionData = new ProgressionDataRecord();

            if (CurrencyManager.Instance != null)
            {
                model.currencyData.gold = CurrencyManager.Instance.Gold;
                model.currencyData.soul = CurrencyManager.Instance.Soul;
                // Crystal balance is authoritative via CrystalManager -> model.currencyData.crystalBalance
                if (CrystalManager.Instance != null)
                {
                    model.currencyData.crystalBalance = CrystalManager.Instance.CrystalBalance;
                }
            }

            if (DungeonProgressionManager.Instance != null)
            {
                model.dungeonData.dungeonLevel = DungeonProgressionManager.Instance.CurrentLevel;
                model.progressionData.playerLevel = DungeonProgressionManager.Instance.CurrentLevel;
                model.progressionData.exp = DungeonProgressionManager.Instance.CurrentExp;
            }

            if (DungeonCoreUpgradeManager.Instance != null)
                model.dungeonData.dungeonCoreLevel = DungeonCoreUpgradeManager.Instance.CoreLevel;
            if (DungeonExpansionManager.Instance != null)
                model.dungeonData.gridDimension = DungeonExpansionManager.Instance.CurrentDimension;
            if (DungeonFacilityManager.Instance != null)
            {
                model.dungeonData.facilityGoldLevel = DungeonFacilityManager.Instance.GoldFacilityLevel;
                model.dungeonData.facilitySoulLevel = DungeonFacilityManager.Instance.SoulFacilityLevel;
                model.dungeonData.facilityCapacityLevel = DungeonFacilityManager.Instance.CapacityFacilityLevel;
            }

            if (RoomManager.Instance != null)
            {
                model.dungeonData.dungeonLayoutJson = RoomManager.Instance.LayoutToJson();
                model.dungeonData.placedMonsters.Clear();
                model.dungeonData.placedTraps.Clear();
                if (RoomManager.Instance.Layout?.monsters != null)
                {
                    foreach (var m in RoomManager.Instance.Layout.monsters)
                        if (m != null) model.dungeonData.placedMonsters.Add(m.monsterId);
                }
                if (RoomManager.Instance.Layout?.traps != null)
                {
                    foreach (var t in RoomManager.Instance.Layout.traps)
                        if (t != null) model.dungeonData.placedTraps.Add(t.trapId);
                }
            }

            var roomInv = FindAnyObjectByType<RoomInventoryManager>();
            if (roomInv != null) model.dungeonData.roomInventoryJson = roomInv.ToJson();
            var monInv = FindAnyObjectByType<MonsterInventoryManager>();
            if (monInv != null) model.dungeonData.monsterInventoryJson = monInv.ToJson();
            var trapInv = FindAnyObjectByType<TrapInventoryManager>();
            if (trapInv != null) model.dungeonData.trapInventoryJson = trapInv.ToJson();

            if (CampaignManager.Instance != null)
            {
                var cState = CampaignManager.Instance.ExportSaveState();
                model.progressionData.campaignSaveJson = JsonUtility.ToJson(cState, false);
                model.progressionData.completedCampaignStages = new List<string>(cState.completedStageIds);
            }
            if (QuestManager.Instance != null)
            {
                model.progressionData.questSaveJson = JsonUtility.ToJson(QuestManager.Instance.ExportSaveState(), false);
            }
            if (AchievementManager.Instance != null)
            {
                model.progressionData.achievementSaveJson = JsonUtility.ToJson(AchievementManager.Instance.ExportSaveState(), false);
            }
            if (EndlessChallengeManager.Instance != null)
            {
                model.progressionData.endlessChallengeSaveJson = JsonUtility.ToJson(EndlessChallengeManager.Instance.ExportSaveState(), false);
            }
        }

        public void ApplyModelToActiveGameSystems(PlayerDataModel model)
        {
            if (model == null) return;

            if (CrystalManager.Instance != null && model.currencyData != null)
            {
                CrystalManager.Instance.SyncFromPlayerDatabase(
                    model.currencyData.crystalBalance,
                    model.currencyData.totalCrystalPurchased,
                    model.currencyData.totalCrystalSpent);
            }

            if (CurrencyManager.Instance != null && model.currencyData != null)
            {
                CurrencyManager.Instance.SetGold(model.currencyData.gold);
                CurrencyManager.Instance.SetSoul(model.currencyData.soul);
                CurrencyManager.Instance.SetCrystal(model.currencyData.crystalBalance);
            }

            if (DungeonCoreUpgradeManager.Instance != null && model.dungeonData != null)
                DungeonCoreUpgradeManager.Instance.SetLevel(Mathf.Max(1, model.dungeonData.dungeonCoreLevel));

            if (DungeonExpansionManager.Instance != null && model.dungeonData != null)
                DungeonExpansionManager.Instance.SetDimension(Mathf.Max(9, model.dungeonData.gridDimension));

            if (DungeonFacilityManager.Instance != null && model.dungeonData != null)
                DungeonFacilityManager.Instance.SetLevels(
                    Mathf.Max(1, model.dungeonData.facilityGoldLevel),
                    Mathf.Max(1, model.dungeonData.facilitySoulLevel),
                    Mathf.Max(1, model.dungeonData.facilityCapacityLevel));

            if (RoomManager.Instance != null && !string.IsNullOrEmpty(model.dungeonData?.dungeonLayoutJson))
                RoomManager.Instance.LoadLayoutJson(model.dungeonData.dungeonLayoutJson, out _);

            if (!string.IsNullOrEmpty(model.dungeonData?.roomInventoryJson))
                FindAnyObjectByType<RoomInventoryManager>()?.LoadFromJson(model.dungeonData.roomInventoryJson, out _);
            if (!string.IsNullOrEmpty(model.dungeonData?.monsterInventoryJson))
                FindAnyObjectByType<MonsterInventoryManager>()?.LoadFromJson(model.dungeonData.monsterInventoryJson, out _);
            if (!string.IsNullOrEmpty(model.dungeonData?.trapInventoryJson))
                FindAnyObjectByType<TrapInventoryManager>()?.LoadFromJson(model.dungeonData.trapInventoryJson, out _);

            if (CampaignManager.Instance != null && !string.IsNullOrEmpty(model.progressionData?.campaignSaveJson))
            {
                var cState = JsonUtility.FromJson<CampaignSaveState>(model.progressionData.campaignSaveJson);
                if (cState != null) CampaignManager.Instance.ImportSaveState(cState);
            }
            if (QuestManager.Instance != null && !string.IsNullOrEmpty(model.progressionData?.questSaveJson))
            {
                var qState = JsonUtility.FromJson<QuestSaveState>(model.progressionData.questSaveJson);
                if (qState != null) QuestManager.Instance.ImportSaveState(qState);
            }
            if (AchievementManager.Instance != null && !string.IsNullOrEmpty(model.progressionData?.achievementSaveJson))
            {
                var aState = JsonUtility.FromJson<AchievementSaveState>(model.progressionData.achievementSaveJson);
                if (aState != null) AchievementManager.Instance.ImportSaveState(aState);
            }
            if (EndlessChallengeManager.Instance != null && !string.IsNullOrEmpty(model.progressionData?.endlessChallengeSaveJson))
            {
                var ecState = JsonUtility.FromJson<EndlessChallengeSaveState>(model.progressionData.endlessChallengeSaveJson);
                if (ecState != null) EndlessChallengeManager.Instance.ImportSaveState(ecState);
            }
        }

        private async Task<PlayerDataModel> LoadFromBackendAsync(string playerId)
        {
#if UGS_CLOUDSAVE_ENABLED
            var keys = new HashSet<string>
            {
                CloudKeyMeta, CloudKeyAccount, CloudKeyCurrency,
                CloudKeyDungeon, CloudKeyProgression, CloudKeyPurchases
            };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (results == null || results.Count == 0 || !results.ContainsKey(CloudKeyAccount))
            {
                return null;
            }

            var model = new PlayerDataModel();
            if (results.TryGetValue(CloudKeyMeta, out var metaItem))
            {
                var meta = JsonUtility.FromJson<MetaRevisionRecord>(metaItem.Value.GetAsString());
                if (meta != null)
                {
                    model.schemaVersion = meta.schemaVersion;
                    model.revision = meta.revision;
                    model.lastModifiedTimestampUtc = meta.lastModifiedTimestampUtc;
                    model.lastDeviceId = meta.lastDeviceId;
                }
            }
            if (results.TryGetValue(CloudKeyAccount, out var accItem))
                model.accountData = JsonUtility.FromJson<AccountDataRecord>(accItem.Value.GetAsString()) ?? new AccountDataRecord();
            if (results.TryGetValue(CloudKeyCurrency, out var curItem))
                model.currencyData = JsonUtility.FromJson<CurrencyDataRecord>(curItem.Value.GetAsString()) ?? new CurrencyDataRecord();
            if (results.TryGetValue(CloudKeyDungeon, out var dunItem))
                model.dungeonData = JsonUtility.FromJson<DungeonDataRecord>(dunItem.Value.GetAsString()) ?? new DungeonDataRecord();
            if (results.TryGetValue(CloudKeyProgression, out var progItem))
                model.progressionData = JsonUtility.FromJson<ProgressionDataRecord>(progItem.Value.GetAsString()) ?? new ProgressionDataRecord();
            if (results.TryGetValue(CloudKeyPurchases, out var purItem))
            {
                var wrap = JsonUtility.FromJson<PurchaseListWrapper>(purItem.Value.GetAsString());
                model.purchaseHistory = wrap?.items ?? new List<CrystalPurchaseRecord>();
            }
            return model;
#else
            await Task.Yield();
            string path = GetLocalCloudFilePath(playerId);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<PlayerDataModel>(json);
#endif
        }

        private async Task<bool> SaveToBackendAsync(PlayerDataModel model)
        {
#if UGS_CLOUDSAVE_ENABLED
            var meta = new MetaRevisionRecord
            {
                schemaVersion = model.schemaVersion,
                revision = model.revision,
                lastModifiedTimestampUtc = model.lastModifiedTimestampUtc,
                lastDeviceId = model.lastDeviceId
            };
            var payload = new Dictionary<string, object>
            {
                { CloudKeyMeta, JsonUtility.ToJson(meta, false) },
                { CloudKeyAccount, JsonUtility.ToJson(model.accountData, false) },
                { CloudKeyCurrency, JsonUtility.ToJson(model.currencyData, false) },
                { CloudKeyDungeon, JsonUtility.ToJson(model.dungeonData, false) },
                { CloudKeyProgression, JsonUtility.ToJson(model.progressionData, false) },
                { CloudKeyPurchases, JsonUtility.ToJson(new PurchaseListWrapper { items = model.purchaseHistory }, false) }
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
            return true;
#else
            await Task.Yield();
            Directory.CreateDirectory(LocalCloudFolder);
            string path = GetLocalCloudFilePath(model.accountData.playerId);
            string json = JsonUtility.ToJson(model, true);
            File.WriteAllText(path, json);
            return true;
#endif
        }

        private async Task DeleteFromBackendAsync(string playerId)
        {
#if UGS_CLOUDSAVE_ENABLED
            await CloudSaveService.Instance.Data.Player.DeleteAllAsync();
#else
            await Task.Yield();
            string path = GetLocalCloudFilePath(playerId);
            if (File.Exists(path)) File.Delete(path);
#endif
        }

        private string GetLocalCloudFilePath(string playerId)
        {
            string safeId = string.IsNullOrWhiteSpace(playerId) ? "unknown" : playerId.Trim().Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(LocalCloudFolder, $"player_{safeId}.json");
        }

        private void SetSyncState(DataSyncState nextState, string detail)
        {
            syncState = nextState;
            OnSyncStateChanged?.Invoke(syncState, detail);
        }
    }
}
