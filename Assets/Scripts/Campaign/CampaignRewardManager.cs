using System;
using UnityEngine;

namespace DreamDungeon
{
    public struct CampaignRewardBreakdown
    {
        public string StageId;
        public string RegionId;
        public StageDifficulty Difficulty;
        public bool Victory;
        public bool IsFirstClear;
        public bool IsPerfectClear;
        public bool IsRegionCompletedNow;

        public int BaseGold;
        public int BaseSoul;
        public int BaseEXP;

        public int FirstClearGold;
        public int FirstClearSoul;
        public int FirstClearEXP;

        public int BossBonusGold;
        public int BossBonusSoul;
        public int BossBonusEXP;

        public int RegionCompletionGold;
        public int RegionCompletionSoul;

        public int TotalGold => BaseGold + FirstClearGold + BossBonusGold + RegionCompletionGold;
        public int TotalSoul => BaseSoul + FirstClearSoul + BossBonusSoul + RegionCompletionSoul;
        public int TotalEXP => BaseEXP + FirstClearEXP + BossBonusEXP;
    }

    public sealed class CampaignRewardManager : MonoBehaviour
    {
        public static CampaignRewardManager Instance { get; private set; }

        public event Action<CampaignRewardBreakdown> OnCampaignRewardGranted;

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

        public static CampaignRewardBreakdown PreviewStageReward(
            CampaignStageData stage,
            DifficultyData difficulty,
            AdventurerFactionData faction,
            bool isFirstClearAvailable,
            bool includeBossBonus = true)
        {
            var result = new CampaignRewardBreakdown
            {
                StageId = stage != null ? stage.stageId : string.Empty,
                RegionId = stage != null ? stage.regionId : string.Empty,
                Difficulty = difficulty != null ? difficulty.difficultyLevel : (stage != null ? stage.difficulty : StageDifficulty.Easy),
                Victory = true,
                IsFirstClear = isFirstClearAvailable,
                IsPerfectClear = false
            };

            if (stage == null) return result;

            float diffGoldMult = difficulty != null ? difficulty.rewardGoldMultiplier : 1f;
            float diffSoulMult = difficulty != null ? difficulty.rewardSoulMultiplier : 1f;
            float diffExpMult = difficulty != null ? difficulty.rewardEXPMultiplier : 1f;

            float facGoldMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetGoldMultiplier() : 1f;
            float facSoulMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetSoulMultiplier() : 1f;

            int factionGold = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusGold : 0;
            int factionSoul = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusSoul : 0;
            int factionExp = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusEXP : 0;

            result.BaseGold = Mathf.RoundToInt((stage.rewardGold + factionGold) * diffGoldMult * facGoldMult);
            result.BaseSoul = Mathf.RoundToInt((stage.rewardSoul + factionSoul) * diffSoulMult * facSoulMult);
            result.BaseEXP = Mathf.RoundToInt((stage.rewardDungeonEXP + factionExp) * diffExpMult);

            if (isFirstClearAvailable && stage.firstClearReward != null)
            {
                result.FirstClearGold = stage.firstClearReward.gold;
                result.FirstClearSoul = stage.firstClearReward.soul;
                result.FirstClearEXP = stage.firstClearReward.exp;
            }

            if (includeBossBonus && !string.IsNullOrWhiteSpace(stage.bossId))
            {
                result.BossBonusGold = Mathf.RoundToInt(150 * diffGoldMult);
                result.BossBonusSoul = Mathf.RoundToInt(40 * diffSoulMult);
                result.BossBonusEXP = Mathf.RoundToInt(300 * diffExpMult);
            }

            return result;
        }

        public static CampaignRewardBreakdown CalculateBattleOutcome(
            CampaignStageData stage,
            DifficultyData difficulty,
            AdventurerFactionData faction,
            bool victory,
            int wavesCleared,
            int totalWaves,
            int adventurersDefeated,
            int bossesDefeated,
            int coreHpRemaining,
            int coreMaxHp,
            bool isFirstClearAvailable,
            bool isRegionCompletedNow,
            WorldRegionData regionData)
        {
            var breakdown = new CampaignRewardBreakdown
            {
                StageId = stage != null ? stage.stageId : string.Empty,
                RegionId = stage != null ? stage.regionId : string.Empty,
                Difficulty = difficulty != null ? difficulty.difficultyLevel : StageDifficulty.Easy,
                Victory = victory,
                IsFirstClear = victory && isFirstClearAvailable,
                IsRegionCompletedNow = victory && isRegionCompletedNow
            };

            float diffGoldMult = difficulty != null ? difficulty.rewardGoldMultiplier : 1f;
            float diffSoulMult = difficulty != null ? difficulty.rewardSoulMultiplier : 1f;
            float diffExpMult = difficulty != null ? difficulty.rewardEXPMultiplier : 1f;

            float facGoldMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetGoldMultiplier() : 1f;
            float facSoulMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetSoulMultiplier() : 1f;

            if (!victory)
            {
                // Defeat grants no victory stage rewards or unlocks
                breakdown.BaseGold = 0;
                breakdown.BaseSoul = 0;
                breakdown.BaseEXP = 0;
                return breakdown;
            }

            // Evaluate Perfect Clear: All waves cleared, Core alive, Core HP ratio >= threshold
            float requiredRatio = stage != null ? Mathf.Clamp(stage.perfectClearCoreHpRatio, 0.1f, 1f) : 0.70f;
            float currentRatio = coreMaxHp > 0 ? (float)coreHpRemaining / coreMaxHp : 1f;
            breakdown.IsPerfectClear = coreHpRemaining > 0 &&
                                       (totalWaves <= 0 || wavesCleared >= totalWaves) &&
                                       currentRatio >= requiredRatio;

            int stageGold = stage != null ? stage.rewardGold : 150;
            int stageSoul = stage != null ? stage.rewardSoul : 30;
            int stageExp = stage != null ? stage.rewardDungeonEXP : 80;

            int factionGold = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusGold : 0;
            int factionSoul = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusSoul : 0;
            int factionExp = faction?.factionRewardBonus != null ? faction.factionRewardBonus.bonusEXP : 0;

            BattleConfiguration baseCfg = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
            BattleRewardResult combatReward = BattleRewardCalculator.Calculate(true, wavesCleared, adventurersDefeated, baseCfg, facGoldMult, facSoulMult, 0);

            breakdown.BaseGold = Mathf.RoundToInt((stageGold + factionGold) * diffGoldMult * facGoldMult) + combatReward.Gold;
            breakdown.BaseSoul = Mathf.RoundToInt((stageSoul + factionSoul) * diffSoulMult * facSoulMult) + combatReward.Soul;
            breakdown.BaseEXP = Mathf.RoundToInt((stageExp + factionExp) * diffExpMult) + combatReward.Exp;

            if (breakdown.IsFirstClear && stage?.firstClearReward != null)
            {
                breakdown.FirstClearGold = stage.firstClearReward.gold;
                breakdown.FirstClearSoul = stage.firstClearReward.soul;
                breakdown.FirstClearEXP = stage.firstClearReward.exp;
            }

            bool hasBossVictory = bossesDefeated > 0 || (stage != null && !string.IsNullOrWhiteSpace(stage.bossId));
            if (hasBossVictory)
            {
                int bossCount = Mathf.Max(1, bossesDefeated);
                breakdown.BossBonusGold = Mathf.RoundToInt(150 * bossCount * diffGoldMult);
                breakdown.BossBonusSoul = Mathf.RoundToInt(40 * bossCount * diffSoulMult);
                breakdown.BossBonusEXP = Mathf.RoundToInt(300 * bossCount * diffExpMult);
            }

            if (breakdown.IsRegionCompletedNow && regionData != null)
            {
                breakdown.RegionCompletionGold = regionData.regionRewardGold;
                breakdown.RegionCompletionSoul = regionData.regionRewardSoul;
            }

            return breakdown;
        }

        public void GrantBreakdownRewards(CampaignRewardBreakdown breakdown, bool grantCurrenciesToCurrentScene = true)
        {
            if (!breakdown.Victory) return;

            if (grantCurrenciesToCurrentScene)
            {
                CurrencyManager currency = CurrencyManager.Instance ?? FindAnyObjectByType<CurrencyManager>();
                if (currency != null && (breakdown.TotalGold > 0 || breakdown.TotalSoul > 0))
                {
                    currency.TryAdd(breakdown.TotalGold, breakdown.TotalSoul, 0, out _);
                }
            }

            DungeonProgressionManager progression = DungeonProgressionManager.Instance ?? FindAnyObjectByType<DungeonProgressionManager>();
            if (progression != null && breakdown.TotalEXP > 0)
            {
                progression.AddExp(breakdown.TotalEXP);
            }

            OnCampaignRewardGranted?.Invoke(breakdown);
        }
    }
}
