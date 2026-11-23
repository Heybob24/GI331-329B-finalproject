using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Quest Database", fileName = "QuestDatabase")]
    public sealed class QuestDatabase : ScriptableObject
    {
        [Header("Configuration")]
        [SerializeField, Min(1)] private int dailyQuestCount = 3;
        [SerializeField, Min(1)] private int weeklyQuestCount = 3;

        [Header("Quest Entries")]
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        private Dictionary<string, QuestData> byId;
        private Dictionary<QuestType, List<QuestData>> byType;

        public int DailyQuestCount => Mathf.Max(1, dailyQuestCount);
        public int WeeklyQuestCount => Mathf.Max(1, weeklyQuestCount);

        public IReadOnlyList<QuestData> AllQuests
        {
            get
            {
                EnsureCache();
                return quests;
            }
        }

        public void SetConfiguration(int dailyCount, int weeklyCount)
        {
            dailyQuestCount = Mathf.Max(1, dailyCount);
            weeklyQuestCount = Mathf.Max(1, weeklyCount);
        }

        public void SetQuests(IEnumerable<QuestData> entries)
        {
            quests = entries != null ? new List<QuestData>(entries) : new List<QuestData>();
            RebuildCache();
        }

        public QuestData FindById(string questId)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(questId)) return null;
            return byId.TryGetValue(questId.Trim(), out QuestData data) ? data : null;
        }

        public IReadOnlyList<QuestData> GetByType(QuestType type)
        {
            EnsureCache();
            return byType.TryGetValue(type, out List<QuestData> list) ? list : (IReadOnlyList<QuestData>)Array.Empty<QuestData>();
        }

        public bool ValidateDatabase(out string error)
        {
            EnsureCache();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < quests.Count; i++)
            {
                QuestData q = quests[i];
                if (q == null)
                {
                    error = $"Quest Database entry {i} is null.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(q.questId))
                {
                    error = $"Quest '{q.name}' has an empty questId.";
                    return false;
                }
                if (!ids.Add(q.questId.Trim()))
                {
                    error = $"Duplicate questId '{q.questId}' in Quest Database.";
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
            if (byId == null || byType == null)
            {
                RebuildCache();
            }
        }

        public void RebuildCache()
        {
            if (quests == null) quests = new List<QuestData>();
            byId = new Dictionary<string, QuestData>(StringComparer.OrdinalIgnoreCase);
            byType = new Dictionary<QuestType, List<QuestData>>();

            foreach (QuestType qt in Enum.GetValues(typeof(QuestType)))
            {
                byType[qt] = new List<QuestData>();
            }

            foreach (QuestData q in quests)
            {
                if (q == null || string.IsNullOrWhiteSpace(q.questId)) continue;
                string id = q.questId.Trim();
                if (byId.ContainsKey(id)) continue;
                byId[id] = q;
                byType[q.questType].Add(q);
            }

            byType[QuestType.Main].Sort((a, b) =>
            {
                int cmp = a.chapterIndex.CompareTo(b.chapterIndex);
                return cmp != 0 ? cmp : a.stepOrder.CompareTo(b.stepOrder);
            });
        }

        public static QuestDatabase CreateRuntimeDefaultDatabase()
        {
            QuestDatabase db = CreateInstance<QuestDatabase>();
            db.name = "DefaultQuestDatabase";
            db.dailyQuestCount = 3;
            db.weeklyQuestCount = 3;

            var list = new List<QuestData>
            {
                // ==========================================
                // MAIN QUESTS (Chapters 1 - 3)
                // ==========================================
                MakeQuest("main_c1_q1", "Complete First Raid", "Test your dungeon defenses by completing your first raid.",
                    QuestType.Main, QuestObjectiveType.CompleteRaid, 1, 150, 30, 50,
                    chapter: 1, chapterTitle: "Chapter 1: Awaken the Dungeon", order: 1, unlockReq: ""),

                MakeQuest("main_c1_q2", "Defeat 5 Adventurers", "Stop 5 invading adventurers inside your dungeon.",
                    QuestType.Main, QuestObjectiveType.DefeatAdventurer, 5, 200, 40, 75,
                    chapter: 1, chapterTitle: "Chapter 1: Awaken the Dungeon", order: 2, unlockReq: "main_c1_q1"),

                MakeQuest("main_c1_q3", "Place 2 Monsters", "Deploy 2 monster defenders into Monster Rooms.",
                    QuestType.Main, QuestObjectiveType.PlaceMonster, 2, 250, 50, 100,
                    chapter: 1, chapterTitle: "Chapter 1: Awaken the Dungeon", order: 3, unlockReq: "main_c1_q2"),

                MakeQuest("main_c2_q1", "Reach Dungeon Level 2", "Earn Dungeon EXP to reach Dungeon Level 2.",
                    QuestType.Main, QuestObjectiveType.ReachDungeonLevel, 2, 300, 60, 100,
                    chapter: 2, chapterTitle: "Chapter 2: Strengthen the Dungeon", order: 1, unlockReq: "main_c1_q3"),

                MakeQuest("main_c2_q2", "Upgrade Core to Level 2", "Reinforce your Dungeon Core to Level 2.",
                    QuestType.Main, QuestObjectiveType.UpgradeCore, 1, 350, 75, 120,
                    chapter: 2, chapterTitle: "Chapter 2: Strengthen the Dungeon", order: 2, unlockReq: "main_c2_q1"),

                MakeQuest("main_c2_q3", "Upgrade One Facility", "Upgrade any Dungeon Facility once.",
                    QuestType.Main, QuestObjectiveType.UpgradeFacility, 1, 400, 90, 150,
                    chapter: 2, chapterTitle: "Chapter 2: Strengthen the Dungeon", order: 3, unlockReq: "main_c2_q2"),

                MakeQuest("main_c3_q1", "Reach Dungeon Level 5", "Grow your dungeon power to reach Dungeon Level 5.",
                    QuestType.Main, QuestObjectiveType.ReachDungeonLevel, 5, 600, 150, 200,
                    chapter: 3, chapterTitle: "Chapter 3: Prepare for Battle", order: 1, unlockReq: "main_c2_q3"),

                MakeQuest("main_c3_q2", "Unlock Boss Room", "Unlock or acquire a Boss Room at Dungeon Level 5.",
                    QuestType.Main, QuestObjectiveType.UnlockRoom, 1, 700, 180, 220,
                    chapter: 3, chapterTitle: "Chapter 3: Prepare for Battle", order: 2, unlockReq: "main_c3_q1", filter: "boss_room"),

                MakeQuest("main_c3_q3", "Place a Trap", "Install 1 Trap inside a Trap Room to ambush raiders.",
                    QuestType.Main, QuestObjectiveType.PlaceTrap, 1, 750, 200, 250,
                    chapter: 3, chapterTitle: "Chapter 3: Prepare for Battle", order: 3, unlockReq: "main_c3_q2"),

                MakeQuest("main_c3_q4", "Defeat First Boss", "Win a raid or complete a Boss encounter.",
                    QuestType.Main, QuestObjectiveType.DefeatBoss, 1, 1000, 300, 400,
                    chapter: 3, chapterTitle: "Chapter 3: Prepare for Battle", order: 4, unlockReq: "main_c3_q3"),

                // ==========================================
                // DAILY QUEST POOL (6 Quests)
                // ==========================================
                MakeQuest("daily_complete_raid", "Complete 1 Raid", "Finish 1 dungeon raid today.",
                    QuestType.Daily, QuestObjectiveType.CompleteRaid, 1, 150, 25, 40, repeatable: true),

                MakeQuest("daily_defeat_adventurers", "Defeat 10 Adventurers", "Defeat 10 invading adventurers in battle.",
                    QuestType.Daily, QuestObjectiveType.DefeatAdventurer, 10, 200, 35, 50, repeatable: true),

                MakeQuest("daily_place_monsters", "Place 3 Monsters", "Deploy 3 monsters into Monster Rooms.",
                    QuestType.Daily, QuestObjectiveType.PlaceMonster, 3, 180, 30, 45, repeatable: true),

                MakeQuest("daily_place_traps", "Place 2 Traps", "Install 2 traps inside Trap Rooms.",
                    QuestType.Daily, QuestObjectiveType.PlaceTrap, 2, 180, 30, 45, repeatable: true),

                MakeQuest("daily_earn_gold", "Earn 500 Gold", "Accumulate 500 Gold from raids or rewards.",
                    QuestType.Daily, QuestObjectiveType.EarnGold, 500, 150, 40, 50, repeatable: true),

                MakeQuest("daily_complete_waves", "Complete 5 Waves", "Clear 5 waves of adventurers.",
                    QuestType.Daily, QuestObjectiveType.CompleteWave, 5, 220, 40, 60, repeatable: true),

                // ==========================================
                // WEEKLY QUEST POOL (6 Quests)
                // ==========================================
                MakeQuest("weekly_win_raids", "Win 10 Raids", "Achieve victory in 10 dungeon raids this week.",
                    QuestType.Weekly, QuestObjectiveType.WinRaid, 10, 1200, 250, 350, repeatable: true),

                MakeQuest("weekly_defeat_bosses", "Defeat 3 Bosses", "Complete 3 boss encounters in raids.",
                    QuestType.Weekly, QuestObjectiveType.DefeatBoss, 3, 1500, 350, 450, repeatable: true),

                MakeQuest("weekly_complete_waves", "Complete 30 Waves", "Clear 30 waves of adventurers across raids.",
                    QuestType.Weekly, QuestObjectiveType.CompleteWave, 30, 1000, 220, 300, repeatable: true),

                MakeQuest("weekly_upgrade_core", "Upgrade Dungeon Core 3 Times", "Reinforce the Dungeon Core 3 times.",
                    QuestType.Weekly, QuestObjectiveType.UpgradeCore, 3, 1300, 280, 350, repeatable: true),

                MakeQuest("weekly_earn_gold", "Earn 5,000 Gold", "Earn 5,000 Gold across the week.",
                    QuestType.Weekly, QuestObjectiveType.EarnGold, 5000, 1000, 300, 320, repeatable: true),

                MakeQuest("weekly_expand_dungeon", "Expand Dungeon 1 Time", "Expand your dungeon grid size once.",
                    QuestType.Weekly, QuestObjectiveType.ExpandDungeon, 1, 1400, 300, 400, repeatable: true),

                // ==========================================
                // SPECIAL QUESTS (3 Quests)
                // ==========================================
                MakeQuest("special_unlock_monster", "Monster Collector", "Purchase or unlock 3 monsters for your army.",
                    QuestType.Special, QuestObjectiveType.UnlockMonster, 3, 400, 100, 120),

                MakeQuest("special_unlock_trap", "Trap Engineer", "Purchase or unlock 3 traps for your corridors.",
                    QuestType.Special, QuestObjectiveType.UnlockTrap, 3, 400, 100, 120),

                MakeQuest("special_soul_harvester", "Soul Harvester", "Collect 300 Soul from raids and rewards.",
                    QuestType.Special, QuestObjectiveType.EarnSoul, 300, 600, 150, 200),

                // ==========================================
                // MILESTONE 9: ENDLESS & CHALLENGE QUESTS
                // ==========================================
                MakeQuest("special_endless_initiate", "Endless Explorer", "Complete 1 Endless Dungeon Run.",
                    QuestType.Special, QuestObjectiveType.CompleteEndlessRun, 1, 300, 75, 120),

                MakeQuest("special_endless_wave_10", "Endless Survivor", "Reach Wave 10 in Endless Dungeon Mode.",
                    QuestType.Special, QuestObjectiveType.ReachEndlessWave, 10, 600, 150, 250),

                MakeQuest("special_endless_elites", "Elite Vanquisher", "Defeat 5 Elite enemies in Endless or Challenge Mode.",
                    QuestType.Special, QuestObjectiveType.DefeatEndlessElite, 5, 500, 120, 200),

                MakeQuest("special_endless_boss", "Endless Boss Slayer", "Defeat 1 Endless Boss wave.",
                    QuestType.Special, QuestObjectiveType.DefeatEndlessBoss, 1, 700, 180, 300),

                MakeQuest("special_challenge_victor", "Challenge Conqueror", "Complete any Challenge in Challenge Mode.",
                    QuestType.Special, QuestObjectiveType.CompleteChallenge, 1, 550, 140, 220),

                MakeQuest("special_endless_score_1000", "High Scorer", "Earn a score of 1,000 or more in Endless Mode.",
                    QuestType.Special, QuestObjectiveType.EarnEndlessScore, 1000, 650, 160, 260),

                MakeQuest("special_boss_rush", "Boss Rush Champion", "Complete the Boss Rush challenge.",
                    QuestType.Special, QuestObjectiveType.CompleteBossRush, 1, 1000, 250, 400)
            };

            db.SetQuests(list);
            return db;
        }

        private static QuestData MakeQuest(string id, string title, string desc, QuestType type, QuestObjectiveType obj,
            int target, int gold, int soul, int exp, int chapter = 1, string chapterTitle = "", int order = 0,
            string unlockReq = "", bool repeatable = false, string filter = "")
        {
            QuestData data = CreateInstance<QuestData>();
            data.name = id;
            data.questId = id;
            data.questName = title;
            data.description = desc;
            data.questType = type;
            data.objectiveType = obj;
            data.targetAmount = target;
            data.rewardGold = gold;
            data.rewardSoul = soul;
            data.rewardDungeonEXP = exp;
            data.chapterIndex = chapter;
            data.chapterTitle = chapterTitle;
            data.stepOrder = order;
            data.unlockRequirement = unlockReq;
            data.isRepeatable = repeatable;
            data.isActive = true;
            data.targetIdFilter = filter;
            return data;
        }
    }
}
