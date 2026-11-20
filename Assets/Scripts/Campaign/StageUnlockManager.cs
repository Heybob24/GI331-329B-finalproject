using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class StageUnlockManager : MonoBehaviour
    {
        public static StageUnlockManager Instance { get; private set; }

        private CampaignManager campaignManager;
        private WorldMapManager worldMapManager;

        public event Action<CampaignStageData> OnStageUnlocked;

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

        public void Initialize(CampaignManager campaign, WorldMapManager worldMap)
        {
            campaignManager = campaign;
            worldMapManager = worldMap;
        }

        public bool CheckStageUnlockRequirements(CampaignStageData stage, out string unmetReason)
        {
            if (stage == null)
            {
                unmetReason = "Stage data is missing.";
                return false;
            }

            // 1. Region must be unlocked
            if (worldMapManager != null && !worldMapManager.IsRegionUnlocked(stage.regionId))
            {
                unmetReason = $"Region '{stage.regionId}' is locked.";
                return false;
            }

            // 2. First stage in region is unlocked once the region is unlocked
            if (stage.stageNumber <= 1 && string.IsNullOrWhiteSpace(stage.unlockRequirements))
            {
                unmetReason = string.Empty;
                return true;
            }

            // 3. Prerequisite stage completion
            if (!string.IsNullOrWhiteSpace(stage.unlockRequirements) && campaignManager != null)
            {
                if (!campaignManager.IsStageCompleted(stage.unlockRequirements))
                {
                    CampaignStageData prev = campaignManager.GetStage(stage.unlockRequirements);
                    string prevName = prev != null ? prev.stageName : stage.unlockRequirements;
                    unmetReason = $"Complete '{prevName}' first.";
                    return false;
                }
            }

            unmetReason = string.Empty;
            return true;
        }

        public void EvaluateAllStages(bool notifyNewUnlocks = true)
        {
            if (campaignManager == null) return;
            foreach (CampaignStageData stage in campaignManager.AllStages)
            {
                if (stage == null) continue;
                bool alreadyUnlocked = campaignManager.IsStageUnlocked(stage.stageId);
                if (!alreadyUnlocked && CheckStageUnlockRequirements(stage, out _))
                {
                    campaignManager.UnlockStageInternal(stage.stageId);
                    if (notifyNewUnlocks)
                    {
                        OnStageUnlocked?.Invoke(stage);
                    }
                }
            }
        }
    }
}
