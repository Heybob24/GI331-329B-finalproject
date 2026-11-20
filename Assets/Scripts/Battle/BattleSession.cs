using UnityEngine;
using System;

namespace DreamDungeon
{
    public enum BattleState { Preparing, Starting, InProgress, WaveCompleted, Victory, Defeat, Finished }
    public enum AdventurerState { Spawning, Moving, SearchingTarget, Attacking, TakingDamage, Dead, ReachingCore, AttackingCore }

    public static class BattleSession
    {
        public static string LayoutJson { get; private set; }
        public static string DungeonScene { get; private set; } = "DungeonEditor";
        public static int Gold { get; private set; }
        public static int Soul { get; private set; }
        public static int Crystal { get; private set; }
        public static int WaveReached { get; private set; }
        public static int DefeatedCount { get; private set; }
        public static bool Won { get; private set; }
        public static int RewardGold { get; private set; }
        public static int RewardSoul { get; private set; }
        public static int RewardExp { get; private set; }
        public static bool RewardClaimed { get; private set; }
        public static bool ReturnPending { get; private set; }
        public static string CurrentBattleResultId => rewardId;
        private static string rewardId;
        public static bool IsReady => !string.IsNullOrEmpty(LayoutJson);

        // Milestone 8: Campaign & World Map Context
        public static bool IsCampaignBattle { get; private set; }
        public static string RegionId { get; private set; }
        public static string CampaignId { get; private set; }
        public static string StageId { get; private set; }
        public static StageDifficulty Difficulty { get; private set; } = StageDifficulty.Easy;
        public static string FactionId { get; private set; }
        public static string StageBossId { get; private set; }
        public static bool IsFirstClearResult { get; private set; }
        public static bool IsPerfectClearResult { get; private set; }
        public static bool ReturnToWorldMapPending { get; private set; }
        public static string NextStageToOpenPending { get; private set; }

        public static void Begin(string layoutJson, string scene, int gold, int soul, int crystal)
        {
            LayoutJson = layoutJson; DungeonScene = scene; Gold = gold; Soul = soul; Crystal = crystal;
            WaveReached = 0; DefeatedCount = 0; Won = false; RewardGold = RewardSoul = RewardExp = 0;
            RewardClaimed = false; ReturnPending = false;
            IsCampaignBattle = false; RegionId = null; CampaignId = null; StageId = null;
            Difficulty = StageDifficulty.Easy; FactionId = null; StageBossId = null;
            IsFirstClearResult = false; IsPerfectClearResult = false;
            ReturnToWorldMapPending = false; NextStageToOpenPending = null;
            rewardId = Guid.NewGuid().ToString("N");
        }

        public static void BeginCampaign(string layoutJson, string scene, int gold, int soul, int crystal,
            string regionId, string campaignId, string stageId, StageDifficulty difficulty, string factionId, string bossId)
        {
            Begin(layoutJson, scene, gold, soul, crystal);
            IsCampaignBattle = true;
            RegionId = regionId;
            CampaignId = campaignId;
            StageId = stageId;
            Difficulty = difficulty;
            FactionId = factionId;
            StageBossId = bossId;
        }

        public static void SetResult(bool won, int waveReached, int defeated, int gold, int soul, int exp,
            bool isFirstClear = false, bool isPerfectClear = false)
        {
            if (RewardGold + RewardSoul + RewardExp > 0 || Won || RewardClaimed) return;
            Won = won; WaveReached = waveReached; DefeatedCount = defeated;
            RewardGold = Mathf.Max(0, gold); RewardSoul = Mathf.Max(0, soul); RewardExp = Mathf.Max(0, exp);
            IsFirstClearResult = isFirstClear;
            IsPerfectClearResult = isPerfectClear;
        }

        public static void RequestReturn() => ReturnPending = true;

        public static void RequestReturnToWorldMap(string nextStageId = null)
        {
            ReturnPending = true;
            ReturnToWorldMapPending = true;
            NextStageToOpenPending = nextStageId;
        }

        public static void ClearWorldMapNavigationRequests()
        {
            ReturnToWorldMapPending = false;
            NextStageToOpenPending = null;
        }

        public static void Reset()
        {
            LayoutJson = null;
            DungeonScene = "DungeonEditor";
            Gold = 0;
            Soul = 0;
            Crystal = 0;
            WaveReached = 0;
            DefeatedCount = 0;
            Won = false;
            RewardGold = 0;
            RewardSoul = 0;
            RewardExp = 0;
            RewardClaimed = false;
            ReturnPending = false;
            IsCampaignBattle = false;
            RegionId = null;
            CampaignId = null;
            StageId = null;
            Difficulty = StageDifficulty.Easy;
            FactionId = null;
            StageBossId = null;
            IsFirstClearResult = false;
            IsPerfectClearResult = false;
            ReturnToWorldMapPending = false;
            NextStageToOpenPending = null;
            rewardId = null;
        }

        public static bool RestoreEditor(RoomManager manager)
        {
            if (!ReturnPending || manager == null) return false;
            if (!manager.LoadLayoutJson(LayoutJson, out string error)) Debug.LogError($"Could not restore dungeon after raid: {error}");
            CurrencyManager currency = manager.Currency;
            if (currency != null)
            {
                currency.Configure(Gold, Soul, Crystal);
                if (!RewardClaimed)
                {
                    currency.TryAdd(RewardGold, RewardSoul, 0, out _);
                    if (GameManager.Instance == null) new GameObject("GameManager").AddComponent<GameManager>();
                    GameManager.Instance.GrantExperience(rewardId, RewardExp);
                }
            }
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.NotifyBattleResult(rewardId, Won, WaveReached, DefeatedCount, 0);
            }
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.NotifyBattleResult(rewardId, Won, WaveReached, DefeatedCount, 0);
            }
            RewardClaimed = true; ReturnPending = false; LayoutJson = null;
            SaveManager.Instance?.SaveGame();
            return true;
        }
    }
}
