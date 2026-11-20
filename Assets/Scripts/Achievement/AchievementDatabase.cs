using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Achievement Database", fileName = "AchievementDatabase")]
    public sealed class AchievementDatabase : ScriptableObject
    {
        [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();

        private Dictionary<string, AchievementData> byId;
        private Dictionary<AchievementCategory, List<AchievementData>> byCategory;

        public IReadOnlyList<AchievementData> AllAchievements
        {
            get
            {
                EnsureCache();
                return achievements;
            }
        }

        public void SetAchievements(IEnumerable<AchievementData> entries)
        {
            achievements = entries != null ? new List<AchievementData>(entries) : new List<AchievementData>();
            RebuildCache();
        }

        public AchievementData FindById(string achievementId)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(achievementId)) return null;
            return byId.TryGetValue(achievementId.Trim(), out AchievementData data) ? data : null;
        }

        public IReadOnlyList<AchievementData> GetByCategory(AchievementCategory category)
        {
            EnsureCache();
            return byCategory.TryGetValue(category, out List<AchievementData> list)
                ? list
                : (IReadOnlyList<AchievementData>)Array.Empty<AchievementData>();
        }

        public bool ValidateDatabase(out string error)
        {
            EnsureCache();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < achievements.Count; i++)
            {
                AchievementData a = achievements[i];
                if (a == null)
                {
                    error = $"Achievement Database entry {i} is null.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(a.achievementId))
                {
                    error = $"Achievement '{a.name}' has an empty achievementId.";
                    return false;
                }
                if (!ids.Add(a.achievementId.Trim()))
                {
                    error = $"Duplicate achievementId '{a.achievementId}' in Achievement Database.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private void OnEnable() => RebuildCache();
        private void OnValidate() => RebuildCache();

        private void EnsureCache()
        {
            if (byId == null || byCategory == null)
                RebuildCache();
        }

        public void RebuildCache()
        {
            if (achievements == null) achievements = new List<AchievementData>();
            byId = new Dictionary<string, AchievementData>(StringComparer.OrdinalIgnoreCase);
            byCategory = new Dictionary<AchievementCategory, List<AchievementData>>();

            foreach (AchievementCategory cat in Enum.GetValues(typeof(AchievementCategory)))
            {
                byCategory[cat] = new List<AchievementData>();
            }

            foreach (AchievementData a in achievements)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.achievementId)) continue;
                string id = a.achievementId.Trim();
                if (byId.ContainsKey(id)) continue;
                byId[id] = a;
                byCategory[a.category].Add(a);
            }
        }

        public static AchievementDatabase CreateRuntimeDefaultDatabase()
        {
            AchievementDatabase db = CreateInstance<AchievementDatabase>();
            db.name = "DefaultAchievementDatabase";

            var list = new List<AchievementData>
            {
                // ==========================================
                // COMBAT ACHIEVEMENTS
                // ==========================================
                MakeAchievement("ach_first_raid", "First Raid", "Complete your first Raid.",
                    AchievementCategory.Combat, QuestObjectiveType.CompleteRaid, 1, 200, 40, 80),

                MakeAchievement("ach_monster_slayer", "Monster Slayer", "Defeat 100 Adventurers.",
                    AchievementCategory.Combat, QuestObjectiveType.DefeatAdventurer, 100, 800, 180, 300),

                MakeAchievement("ach_boss_hunter", "Boss Hunter", "Defeat 5 Bosses.",
                    AchievementCategory.Combat, QuestObjectiveType.DefeatBoss, 5, 1200, 300, 450),

                MakeAchievement("ach_wave_master", "Wave Master", "Complete 100 Waves.",
                    AchievementCategory.Combat, QuestObjectiveType.CompleteWave, 100, 1000, 250, 400),

                // ==========================================
                // DUNGEON BUILDER ACHIEVEMENTS
                // ==========================================
                MakeAchievement("ach_dungeon_architect", "Dungeon Architect", "Place 20 Rooms.",
                    AchievementCategory.Dungeon, QuestObjectiveType.PlaceRoom, 20, 600, 140, 220),

                MakeAchievement("ach_monster_commander", "Monster Commander", "Place 50 Monsters.",
                    AchievementCategory.Dungeon, QuestObjectiveType.PlaceMonster, 50, 750, 160, 260),

                MakeAchievement("ach_trap_master", "Trap Master", "Place 30 Traps.",
                    AchievementCategory.Dungeon, QuestObjectiveType.PlaceTrap, 30, 700, 150, 240),

                MakeAchievement("ach_dungeon_expansion", "Dungeon Expansion", "Expand Grid 3 Times.",
                    AchievementCategory.Dungeon, QuestObjectiveType.ExpandDungeon, 3, 1000, 250, 350),

                // ==========================================
                // PROGRESSION ACHIEVEMENTS
                // ==========================================
                MakeAchievement("ach_rising_dungeon", "Rising Dungeon", "Reach Dungeon Level 5.",
                    AchievementCategory.Progression, QuestObjectiveType.ReachDungeonLevel, 5, 500, 120, 200),

                MakeAchievement("ach_dungeon_master", "Dungeon Master", "Reach Dungeon Level 10.",
                    AchievementCategory.Progression, QuestObjectiveType.ReachDungeonLevel, 10, 2000, 500, 800),

                MakeAchievement("ach_core_power", "Core Power", "Upgrade Core to Level 10.",
                    AchievementCategory.Progression, QuestObjectiveType.UpgradeCore, 10, 1500, 400, 600),

                MakeAchievement("ach_facility_master", "Facility Master", "Maximize one Facility.",
                    AchievementCategory.Progression, QuestObjectiveType.MaximizeFacility, 1, 1200, 300, 500),

                // ==========================================
                // ECONOMY ACHIEVEMENTS
                // ==========================================
                MakeAchievement("ach_gold_collector", "Gold Collector", "Earn 10,000 Gold.",
                    AchievementCategory.Economy, QuestObjectiveType.EarnGold, 10000, 1000, 250, 350),

                MakeAchievement("ach_soul_collector", "Soul Collector", "Earn 1,000 Soul.",
                    AchievementCategory.Economy, QuestObjectiveType.EarnSoul, 1000, 1200, 300, 400)
            };

            db.SetAchievements(list);
            return db;
        }

        private static AchievementData MakeAchievement(string id, string title, string desc,
            AchievementCategory cat, QuestObjectiveType obj, int target, int gold, int soul, int exp)
        {
            AchievementData data = CreateInstance<AchievementData>();
            data.name = id;
            data.achievementId = id;
            data.achievementName = title;
            data.description = desc;
            data.category = cat;
            data.objectiveType = obj;
            data.targetAmount = target;
            data.rewardGold = gold;
            data.rewardSoul = soul;
            data.rewardDungeonEXP = exp;
            return data;
        }
    }
}
