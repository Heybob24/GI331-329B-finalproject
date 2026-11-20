using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class WorldMapManager : MonoBehaviour
    {
        public static WorldMapManager Instance { get; private set; }

        [SerializeField] private WorldRegionDatabase database;
        [SerializeField] private RegionUnlockManager unlockManager;

        private readonly HashSet<string> unlockedRegionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> completedRegionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> claimedRegionRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string selectedRegionId = "forgotten_forest";

        public WorldRegionDatabase Database => database;
        public RegionUnlockManager UnlockManager => unlockManager;
        public string SelectedRegionId => selectedRegionId;
        public WorldRegionData SelectedRegion => database != null ? database.FindById(selectedRegionId) : null;
        public IReadOnlyCollection<string> UnlockedRegionIds => unlockedRegionIds;
        public IReadOnlyCollection<string> CompletedRegionIds => completedRegionIds;

        public event Action OnWorldMapUpdated;
        public event Action<WorldRegionData> OnRegionSelected;
        public event Action<WorldRegionData> OnRegionCompleted;

        public void Configure(WorldRegionDatabase regionDb, RegionUnlockManager unlockMgr = null)
        {
            database = regionDb;
            if (unlockMgr != null) unlockManager = unlockMgr;
        }

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

            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureInitialized()
        {
            if (database == null)
            {
                database = Resources.Load<WorldRegionDatabase>("DreamDungeon/WorldRegionDatabase");
                if (database == null)
                {
                    database = WorldRegionDatabase.CreateRuntimeDefaultDatabase();
                }
            }

            if (unlockManager == null)
            {
                unlockManager = GetComponent<RegionUnlockManager>() ?? gameObject.AddComponent<RegionUnlockManager>();
            }

            foreach (WorldRegionData region in database.AllRegions)
            {
                if (region != null && region.isStartingRegion)
                {
                    unlockedRegionIds.Add(region.regionId.Trim());
                    if (string.IsNullOrEmpty(selectedRegionId))
                        selectedRegionId = region.regionId.Trim();
                }
            }
        }

        public IReadOnlyList<WorldRegionData> GetAllRegions()
        {
            EnsureInitialized();
            return database != null ? database.AllRegions : Array.Empty<WorldRegionData>();
        }

        public WorldRegionData GetRegion(string regionId)
        {
            EnsureInitialized();
            return database != null ? database.FindById(regionId) : null;
        }

        public bool IsRegionUnlocked(string regionId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(regionId)) return false;
            WorldRegionData region = database != null ? database.FindById(regionId) : null;
            if (region != null && region.isStartingRegion) return true;
            return unlockedRegionIds.Contains(regionId.Trim());
        }

        public bool IsRegionCompleted(string regionId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(regionId)) return false;
            return completedRegionIds.Contains(regionId.Trim());
        }

        public bool IsRegionRewardClaimed(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId)) return false;
            return claimedRegionRewardIds.Contains(regionId.Trim());
        }

        public void UnlockRegionInternal(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId)) return;
            if (unlockedRegionIds.Add(regionId.Trim()))
            {
                OnWorldMapUpdated?.Invoke();
            }
        }

        public void MarkRegionCompleted(string regionId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(regionId)) return;
            string id = regionId.Trim();
            unlockedRegionIds.Add(id);
            if (completedRegionIds.Add(id))
            {
                WorldRegionData region = database != null ? database.FindById(id) : null;
                if (region != null)
                {
                    OnRegionCompleted?.Invoke(region);
                }
            }
            unlockManager?.EvaluateAllRegions(notifyNewUnlocks: true);
            OnWorldMapUpdated?.Invoke();
        }

        public void MarkRegionRewardClaimed(string regionId)
        {
            if (!string.IsNullOrWhiteSpace(regionId))
                claimedRegionRewardIds.Add(regionId.Trim());
        }

        public bool CanEnterRegion(string regionId, out string error)
        {
            error = string.Empty;
            EnsureInitialized();
            WorldRegionData region = database != null ? database.FindById(regionId) : null;
            if (region == null)
            {
                error = $"Region '{regionId}' does not exist.";
                return false;
            }

            if (IsRegionUnlocked(region.regionId))
            {
                error = string.Empty;
                return true;
            }

            if (unlockManager != null && unlockManager.CheckUnlockRequirements(region, out error))
            {
                unlockedRegionIds.Add(region.regionId.Trim());
                error = string.Empty;
                return true;
            }

            if (string.IsNullOrEmpty(error))
            {
                error = $"{region.regionName} is locked.";
            }
            return false;
        }

        public bool TrySelectRegion(string regionId, out string message)
        {
            if (!CanEnterRegion(regionId, out message))
            {
                return false;
            }

            WorldRegionData region = database.FindById(regionId);
            selectedRegionId = region.regionId;
            message = $"Selected region: {region.regionName}.";
            OnRegionSelected?.Invoke(region);
            OnWorldMapUpdated?.Invoke();
            return true;
        }

        public float GetRegionCompletionRatio(string regionId)
        {
            EnsureInitialized();
            WorldRegionData region = database != null ? database.FindById(regionId) : null;
            if (region == null || region.availableStageIds == null || region.availableStageIds.Count == 0)
                return 0f;

            CampaignManager campaign = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>();
            if (campaign == null) return IsRegionCompleted(regionId) ? 1f : 0f;

            int completedCount = 0;
            foreach (string stageId in region.availableStageIds)
            {
                if (campaign.IsStageCompleted(stageId)) completedCount++;
            }
            return Mathf.Clamp01((float)completedCount / region.availableStageIds.Count);
        }

        public void ExportToSave(CampaignSaveState save)
        {
            if (save == null) return;
            EnsureInitialized();
            save.unlockedRegionIds.Clear();
            save.unlockedRegionIds.AddRange(unlockedRegionIds);
            save.completedRegionIds.Clear();
            save.completedRegionIds.AddRange(completedRegionIds);
            save.claimedRegionRewardIds.Clear();
            save.claimedRegionRewardIds.AddRange(claimedRegionRewardIds);
        }

        public void ImportFromSave(CampaignSaveState save)
        {
            EnsureInitialized();
            if (save == null) return;

            if (save.unlockedRegionIds != null)
            {
                foreach (string id in save.unlockedRegionIds)
                    if (!string.IsNullOrWhiteSpace(id)) unlockedRegionIds.Add(id.Trim());
            }

            if (save.completedRegionIds != null)
            {
                foreach (string id in save.completedRegionIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        completedRegionIds.Add(id.Trim());
                        unlockedRegionIds.Add(id.Trim());
                    }
                }
            }

            if (save.claimedRegionRewardIds != null)
            {
                foreach (string id in save.claimedRegionRewardIds)
                    if (!string.IsNullOrWhiteSpace(id)) claimedRegionRewardIds.Add(id.Trim());
            }

            unlockManager?.EvaluateAllRegions(notifyNewUnlocks: false);
            OnWorldMapUpdated?.Invoke();
        }
    }
}
