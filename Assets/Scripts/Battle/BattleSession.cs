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
        private static string rewardId;
        public static bool IsReady => !string.IsNullOrEmpty(LayoutJson);

        public static void Begin(string layoutJson, string scene, int gold, int soul, int crystal)
        {
            LayoutJson = layoutJson; DungeonScene = scene; Gold = gold; Soul = soul; Crystal = crystal;
            WaveReached = 0; DefeatedCount = 0; Won = false; RewardGold = RewardSoul = RewardExp = 0;
            RewardClaimed = false; ReturnPending = false;
            rewardId = Guid.NewGuid().ToString("N");
        }

        public static void SetResult(bool won, int waveReached, int defeated, int gold, int soul, int exp)
        {
            if (RewardGold + RewardSoul + RewardExp > 0 || Won || RewardClaimed) return;
            Won = won; WaveReached = waveReached; DefeatedCount = defeated;
            RewardGold = Mathf.Max(0, gold); RewardSoul = Mathf.Max(0, soul); RewardExp = Mathf.Max(0, exp);
        }

        public static void RequestReturn() => ReturnPending = true;

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
            RewardClaimed = true; ReturnPending = false; LayoutJson = null;
            return true;
        }
    }
}
