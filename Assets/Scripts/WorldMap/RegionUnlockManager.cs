using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class RegionUnlockManager : MonoBehaviour
    {
        public static RegionUnlockManager Instance { get; private set; }

        private WorldMapManager worldMapManager;
        private CampaignManager campaignManager;

        public event Action<WorldRegionData> OnRegionUnlocked;

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(WorldMapManager worldMap, CampaignManager campaign)
        {
            worldMapManager = worldMap;
            campaignManager = campaign;
        }

        public bool CheckUnlockRequirements(WorldRegionData region, out string unmetReason)
        {
            if (region == null)
            {
                unmetReason = "Region data is missing.";
                return false;
            }

            if (region.isStartingRegion)
            {
                unmetReason = string.Empty;
                return true;
            }

            RegionUnlockRequirement req = region.unlockRequirements;
            if (req == null)
            {
                unmetReason = string.Empty;
                return true;
            }

            // 1. Required previous region completion
            if (!string.IsNullOrWhiteSpace(req.requiredPreviousRegionId) && worldMapManager != null)
            {
                if (!worldMapManager.IsRegionCompleted(req.requiredPreviousRegionId))
                {
                    WorldRegionData prev = worldMapManager.Database != null
                        ? worldMapManager.Database.FindById(req.requiredPreviousRegionId)
                        : null;
                    string prevName = prev != null ? prev.regionName : req.requiredPreviousRegionId;
                    unmetReason = $"Complete all stages in {prevName}.";
                    return false;
                }
            }

            // 2. Required previous campaign completion
            if (!string.IsNullOrWhiteSpace(req.requiredCampaignId) && campaignManager != null)
            {
                if (!campaignManager.IsCampaignCompleted(req.requiredCampaignId))
                {
                    unmetReason = $"Complete campaign '{req.requiredCampaignId}'.";
                    return false;
                }
            }

            // 3. Required Dungeon Level
            int currentDungeonLevel = DungeonProgressionManager.Instance != null
                ? DungeonProgressionManager.Instance.CurrentLevel
                : 1;
            if (currentDungeonLevel < req.requiredDungeonLevel)
            {
                unmetReason = $"Requires Dungeon Level {req.requiredDungeonLevel}.";
                return false;
            }

            // 4. Required Boss Defeated
            if (!string.IsNullOrWhiteSpace(req.requiredBossId) && campaignManager != null)
            {
                if (!campaignManager.HasDefeatedBoss(req.requiredBossId))
                {
                    unmetReason = $"Defeat Boss '{req.requiredBossId}'.";
                    return false;
                }
            }

            // 5. Required Quest Completed (if specified)
            if (!string.IsNullOrWhiteSpace(req.requiredQuestId) && QuestManager.Instance != null)
            {
                QuestProgressEntry qEntry = QuestManager.Instance.GetProgressEntry(req.requiredQuestId);
                if (qEntry == null || (qEntry.status != QuestStatus.Completed && qEntry.status != QuestStatus.Claimed))
                {
                    unmetReason = $"Complete Quest '{req.requiredQuestId}'.";
                    return false;
                }
            }

            unmetReason = string.Empty;
            return true;
        }

        public void EvaluateAllRegions(bool notifyNewUnlocks = true)
        {
            if (worldMapManager == null || worldMapManager.Database == null) return;

            foreach (WorldRegionData region in worldMapManager.Database.AllRegions)
            {
                if (region == null) continue;
                bool alreadyUnlocked = worldMapManager.IsRegionUnlocked(region.regionId);
                if (!alreadyUnlocked && CheckUnlockRequirements(region, out _))
                {
                    worldMapManager.UnlockRegionInternal(region.regionId);
                    if (notifyNewUnlocks)
                    {
                        OnRegionUnlocked?.Invoke(region);
                    }
                }
            }
        }
    }
}
