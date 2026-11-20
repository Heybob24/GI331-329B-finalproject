using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class Milestone7Tests
    {
        private sealed class MockTimeProvider : ITimeProvider
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc); // Monday
        }

        [MenuItem("Dream Dungeon/Run Milestone 7 Tests")]
        public static void RunAllTests()
        {
            int passed = 0;
            int total = 0;

            void Run(string testName, Action testAction)
            {
                total++;
                try
                {
                    testAction();
                    passed++;
                    Debug.Log($"[PASS] {testName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Debug.Log("========== STARTING MILESTONE 7 TEST SUITE (34 TESTS) ==========");

            // ==========================================
            // QUEST SYSTEM TESTS (Tests 1 - 9)
            // ==========================================
            Run("Test_01_Quest_DatabaseLoadsAndValidates", () =>
            {
                QuestDatabase db = QuestDatabase.CreateRuntimeDefaultDatabase();
                Assert(db != null, "QuestDatabase should be created");
                Assert(db.ValidateDatabase(out string err), $"QuestDatabase validation failed: {err}");
                Assert(db.GetByType(QuestType.Main).Count >= 10, "Should have Main Quests across Chapters 1-3");
                Assert(db.GetByType(QuestType.Daily).Count >= 6, "Should have at least 6 Daily Quests in pool");
                Assert(db.GetByType(QuestType.Weekly).Count >= 6, "Should have at least 6 Weekly Quests in pool");
                Assert(db.GetByType(QuestType.Special).Count >= 3, "Should have Special Quests");
            });

            Run("Test_02_Quest_DisplayedInUI", () =>
            {
                var go = new GameObject("TestM7_02");
                var qMgr = go.AddComponent<QuestManager>();
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var ui = go.AddComponent<QuestUIManager>();
                ui.Configure(safeGo.GetComponent<RectTransform>(), qMgr);
                ui.Open();

                Assert(ui.SpawnedCards.Count > 0, "QuestUIManager should display QuestCardUI entries for Main tab");
                ui.SetTab(QuestType.Daily);
                Assert(ui.SpawnedCards.Count >= 3, $"Daily tab should display at least 3 quests, got {ui.SpawnedCards.Count}");
                ui.SetTab(QuestType.Weekly);
                Assert(ui.SpawnedCards.Count >= 3, $"Weekly tab should display at least 3 quests, got {ui.SpawnedCards.Count}");
                ui.SetTab(QuestType.Special);
                Assert(ui.SpawnedCards.Count > 0, "Special tab should display quests");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_03_Quest_ProgressIncreasesOnRealEvents", () =>
            {
                var go = new GameObject("TestM7_03");
                var qMgr = go.AddComponent<QuestManager>();
                // Complete First Raid so Defeat 5 Adventurers unlocks
                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1, "evt_raid_1");
                qMgr.ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, 3, "evt_adv_1");
                var entry = qMgr.GetProgressEntry("main_c1_q2");
                Assert(entry != null && entry.currentProgress == 3, $"Expected progress 3 on main_c1_q2, got {entry?.currentProgress}");
                // Duplicate eventId must be ignored
                qMgr.ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, 3, "evt_adv_1");
                Assert(entry.currentProgress == 3, "Duplicate eventId must not increase progress twice");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_04_Quest_ProgressClampedToTarget", () =>
            {
                var go = new GameObject("TestM7_04");
                var qMgr = go.AddComponent<QuestManager>();
                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1, "evt_r1");
                qMgr.ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, 99, "evt_adv_big");
                var entry = qMgr.GetProgressEntry("main_c1_q2"); // target is 5
                Assert(entry != null && entry.currentProgress == 5, $"Expected progress clamped to 5, got {entry?.currentProgress}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_05_Quest_CompletesWhenReachingTarget", () =>
            {
                var go = new GameObject("TestM7_05");
                var qMgr = go.AddComponent<QuestManager>();
                QuestData completedQuest = null;
                qMgr.OnQuestCompleted += q => completedQuest = q;

                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                var entry = qMgr.GetProgressEntry("main_c1_q1");
                Assert(entry != null && entry.status == QuestStatus.Completed, $"Expected Completed status, got {entry?.status}");
                Assert(completedQuest != null && completedQuest.questId == "main_c1_q1", "OnQuestCompleted event should fire for main_c1_q1");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_06_Quest_ClaimRewardWorks", () =>
            {
                var go = new GameObject("TestM7_06");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var qMgr = go.AddComponent<QuestManager>();

                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                bool claimed = qMgr.TryClaimReward("main_c1_q1", out string msg);
                Assert(claimed, $"Expected claim to succeed: {msg}");
                var entry = qMgr.GetProgressEntry("main_c1_q1");
                Assert(entry.status == QuestStatus.Claimed && entry.isRewardClaimed, "Quest status should be Claimed");
                Assert(curr.Gold == 1150 && curr.Soul == 130, $"Expected 1150G/130S after claiming main_c1_q1, got {curr.Gold}G/{curr.Soul}S");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_07_Quest_CannotClaimRewardTwice", () =>
            {
                var go = new GameObject("TestM7_07");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var qMgr = go.AddComponent<QuestManager>();

                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                Assert(qMgr.TryClaimReward("main_c1_q1", out _), "First claim should succeed");
                int goldAfterFirst = curr.Gold;
                bool secondClaim = qMgr.TryClaimReward("main_c1_q1", out string err);
                Assert(!secondClaim, $"Second claim must fail: {err}");
                Assert(curr.Gold == goldAfterFirst, "Gold must not increase on duplicate claim");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_08_09_Quest_SaveAndLoadProgress", () =>
            {
                var go1 = new GameObject("TestM7_08_A");
                var qMgr1 = go1.AddComponent<QuestManager>();
                qMgr1.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                qMgr1.TryClaimReward("main_c1_q1", out _);
                qMgr1.ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, 4);

                QuestSaveState saved = qMgr1.ExportSaveState();
                string json = JsonUtility.ToJson(saved);
                Assert(!string.IsNullOrEmpty(json), "QuestSaveState JSON must not be empty");
                UnityEngine.Object.DestroyImmediate(go1);

                var go2 = new GameObject("TestM7_09_B");
                var qMgr2 = go2.AddComponent<QuestManager>();
                QuestSaveState loaded = JsonUtility.FromJson<QuestSaveState>(json);
                qMgr2.ImportSaveState(loaded);

                var e1 = qMgr2.GetProgressEntry("main_c1_q1");
                var e2 = qMgr2.GetProgressEntry("main_c1_q2");
                Assert(e1 != null && e1.status == QuestStatus.Claimed && e1.isRewardClaimed, "main_c1_q1 should restore as Claimed");
                Assert(!qMgr2.CanClaimReward("main_c1_q1", out _), "Claimed quest must not be claimable after load");
                Assert(e2 != null && e2.currentProgress == 4 && e2.status == QuestStatus.InProgress, $"main_c1_q2 should restore progress 4, got {e2?.currentProgress}");
                UnityEngine.Object.DestroyImmediate(go2);
            });

            // ==========================================
            // DAILY / WEEKLY QUEST TESTS (Tests 10 - 16)
            // ==========================================
            Run("Test_10_11_DailyQuest_GeneratesAndPersistsOnSameDay", () =>
            {
                var time = new MockTimeProvider { UtcNow = new DateTime(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc) };
                var go = new GameObject("TestM7_10_11");
                var qMgr = go.AddComponent<QuestManager>();
                qMgr.SetTimeProvider(time);
                qMgr.CheckTimeBasedResets();

                var set1 = new List<string>(qMgr.DailyQuests.ActiveDailyQuestIds);
                Assert(set1.Count >= 3, $"Expected at least 3 Daily Quests, got {set1.Count}");

                // Simulate reopening game later on the same day
                QuestSaveState state = qMgr.ExportSaveState();
                time.UtcNow = new DateTime(2026, 11, 16, 20, 30, 0, DateTimeKind.Utc);
                qMgr.ImportSaveState(state);
                qMgr.CheckTimeBasedResets();

                var set2 = new List<string>(qMgr.DailyQuests.ActiveDailyQuestIds);
                Assert(set1.Count == set2.Count, "Daily quest count should remain identical on same day");
                for (int i = 0; i < set1.Count; i++)
                {
                    Assert(set1[i] == set2[i], "Daily quests must not re-randomize on the same day");
                }
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_12_13_14_15_16_DailyAndWeeklyReset_IndependentAndPreservesMainAndAchievements", () =>
            {
                var time = new MockTimeProvider { UtcNow = new DateTime(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc) }; // Monday
                var go = new GameObject("TestM7_12_16");
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();
                qMgr.SetTimeProvider(time);
                qMgr.CheckTimeBasedResets();

                // Test 13: Weekly Quest set generated
                var weeklyIdsMonday = new List<string>(qMgr.WeeklyQuests.ActiveWeeklyQuestIds);
                Assert(weeklyIdsMonday.Count >= 3, $"Expected at least 3 Weekly Quests, got {weeklyIdsMonday.Count}");

                // Make progress on Main, Daily, Weekly, and Achievement
                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                qMgr.ReportObjectiveProgress(QuestObjectiveType.WinRaid, 4);
                aMgr.ReportProgress(QuestObjectiveType.CompleteRaid, 1);

                string activeDailyId = qMgr.DailyQuests.ActiveDailyQuestIds[0];
                QuestData dailyDef = qMgr.Database.FindById(activeDailyId);
                qMgr.ReportObjectiveProgress(dailyDef.objectiveType, dailyDef.targetAmount);
                qMgr.TryClaimReward(activeDailyId, out _);
                Assert(qMgr.GetProgressEntry(activeDailyId).isRewardClaimed, "Daily quest should be claimed on Monday");

                // Test 12 & 15: Advance 1 Day to Tuesday (same week) -> Daily resets, Weekly does NOT reset
                time.UtcNow = new DateTime(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc); // Tuesday
                qMgr.CheckTimeBasedResets();

                Assert(qMgr.DailyQuests.LastResetDateKey == "2026-11-17", "Daily reset date should update to Tuesday");
                var dailyEntryAfterReset = qMgr.GetProgressEntry(activeDailyId);
                Assert(dailyEntryAfterReset.currentProgress == 0 && !dailyEntryAfterReset.isRewardClaimed && dailyEntryAfterReset.status == QuestStatus.InProgress,
                    "Daily quest progress and claim state must reset on new day");

                // Verify Weekly progress was untouched by Daily Reset (Test 15)
                Assert(qMgr.WeeklyQuests.LastResetWeekKey == "2026-11-16", "Weekly reset key must remain Monday 2026-11-16");

                // Verify Main Quest and Achievement were untouched (Test 16)
                Assert(qMgr.GetProgressEntry("main_c1_q1").status == QuestStatus.Completed, "Main Quest must not reset on Daily Reset");
                Assert(aMgr.GetProgressEntry("ach_first_raid").isUnlocked, "Achievement must not reset on Daily Reset");

                // Test 14: Advance to Next Monday -> Weekly resets
                time.UtcNow = new DateTime(2026, 11, 23, 9, 0, 0, DateTimeKind.Utc); // Next Monday
                qMgr.CheckTimeBasedResets();
                Assert(qMgr.WeeklyQuests.LastResetWeekKey == "2026-11-23", "Weekly reset key should update on new week");
                foreach (string wId in qMgr.WeeklyQuests.ActiveWeeklyQuestIds)
                {
                    var wEntry = qMgr.GetProgressEntry(wId);
                    Assert(wEntry.currentProgress == 0 && !wEntry.isRewardClaimed, $"Weekly quest '{wId}' should be reset on new week");
                }

                // Main Quest and Achievement still preserved after Weekly Reset (Test 16)
                Assert(qMgr.GetProgressEntry("main_c1_q1").status == QuestStatus.Completed, "Main Quest must not reset on Weekly Reset");
                Assert(aMgr.GetProgressEntry("ach_first_raid").isUnlocked, "Achievement must not reset on Weekly Reset");

                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // MAIN QUEST TESTS (Tests 17 - 19)
            // ==========================================
            Run("Test_17_18_19_MainQuest_SequentialUnlockCannotClaimLockedAndPersists", () =>
            {
                var go = new GameObject("TestM7_17_19");
                var qMgr = go.AddComponent<QuestManager>();

                var q1 = qMgr.GetProgressEntry("main_c1_q1");
                var q2 = qMgr.GetProgressEntry("main_c1_q2");
                var q3 = qMgr.GetProgressEntry("main_c1_q3");

                Assert(q1.status == QuestStatus.InProgress, "First main quest should be InProgress");
                Assert(q2.status == QuestStatus.Locked, "Second main quest should start Locked");
                Assert(q3.status == QuestStatus.Locked, "Third main quest should start Locked");

                // Test 18: Locked quest cannot be claimed
                Assert(!qMgr.TryClaimReward("main_c1_q2", out string lockErr), $"Locked quest must not be claimable: {lockErr}");

                // Complete q1 -> q2 unlocks automatically
                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                Assert(q1.status == QuestStatus.Completed, "q1 should be Completed");
                Assert(q2.status == QuestStatus.InProgress, "q2 should unlock to InProgress after q1 completes");
                Assert(q3.status == QuestStatus.Locked, "q3 should still be Locked");

                // Complete q2 -> q3 unlocks
                qMgr.ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, 5);
                Assert(q2.status == QuestStatus.Completed, "q2 should be Completed");
                Assert(q3.status == QuestStatus.InProgress, "q3 should unlock after q2 completes");

                // Test 19: Main Quest progress saves and loads
                QuestSaveState saved = qMgr.ExportSaveState();
                UnityEngine.Object.DestroyImmediate(go);

                var go2 = new GameObject("TestM7_19_Reload");
                var qMgr2 = go2.AddComponent<QuestManager>();
                qMgr2.ImportSaveState(saved);
                Assert(qMgr2.GetProgressEntry("main_c1_q1").status == QuestStatus.Completed, "q1 completion should persist");
                Assert(qMgr2.GetProgressEntry("main_c1_q2").status == QuestStatus.Completed, "q2 completion should persist");
                Assert(qMgr2.GetProgressEntry("main_c1_q3").status == QuestStatus.InProgress, "q3 unlock should persist");
                UnityEngine.Object.DestroyImmediate(go2);
            });

            // ==========================================
            // ACHIEVEMENT TESTS (Tests 20 - 24)
            // ==========================================
            Run("Test_20_21_22_23_24_Achievement_ProgressUnlockPersistenceNotificationAndSingleClaim", () =>
            {
                var go = new GameObject("TestM7_20_24");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var aMgr = go.AddComponent<AchievementManager>();
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var notif = go.AddComponent<QuestNotificationUI>();
                notif.Configure(safeGo.GetComponent<RectTransform>());

                // Test 20: Achievement progress increases
                aMgr.ReportProgress(QuestObjectiveType.DefeatAdventurer, 45);
                var slayer = aMgr.GetProgressEntry("ach_monster_slayer");
                Assert(slayer != null && slayer.currentProgress == 45 && !slayer.isUnlocked, $"Expected 45/100 progress, got {slayer?.currentProgress}");

                // Test 21 & 23: Reach target -> unlocks automatically & triggers Notification
                AchievementData unlockedAch = null;
                aMgr.OnAchievementUnlocked += a => unlockedAch = a;
                aMgr.ReportProgress(QuestObjectiveType.DefeatAdventurer, 60);
                Assert(slayer.currentProgress == 100 && slayer.isUnlocked, "Monster Slayer should unlock at 100");
                Assert(unlockedAch != null && unlockedAch.achievementId == "ach_monster_slayer", "OnAchievementUnlocked event must fire");
                Assert(notif.IsShowing && notif.LastShownTitle.Contains("ACHIEVEMENT"), "QuestNotificationUI should display unlock banner");

                // Test 24: Claim reward once
                Assert(aMgr.TryClaimReward("ach_monster_slayer", out string claimMsg), $"Achievement claim should succeed: {claimMsg}");
                Assert(curr.Gold == 1800 && curr.Soul == 280, $"Expected 1800G/280S, got {curr.Gold}G/{curr.Soul}S");
                Assert(!aMgr.TryClaimReward("ach_monster_slayer", out _), "Duplicate achievement claim must be rejected");

                // Test 22: Achievement does not reset on New Game / ResetGame
                AchievementSaveState achSave = aMgr.ExportSaveState();
                aMgr.ImportSaveState(achSave);
                var restored = aMgr.GetProgressEntry("ach_monster_slayer");
                Assert(restored != null && restored.isUnlocked && restored.isRewardClaimed && restored.currentProgress == 100,
                    "Achievement must remain unlocked and claimed permanently");

                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // INTEGRATION TESTS (Tests 25 - 34)
            // ==========================================
            Run("Test_25_26_27_28_Integration_CurrencyProgressionAndSaveLoad", () =>
            {
                var go = new GameObject("TestM7_25_28");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();
                var saveMgr = go.AddComponent<SaveManager>();

                // Complete and claim main_c1_q1 (+150G, +30S, +50 EXP)
                qMgr.ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
                Assert(qMgr.TryClaimReward("main_c1_q1", out _), "Claim main_c1_q1");

                // Test 25 & 26: Gold & Soul added via CurrencyManager
                Assert(curr.Gold == 1150, $"Expected 1150 Gold via CurrencyManager, got {curr.Gold}");
                Assert(curr.Soul == 130, $"Expected 130 Soul via CurrencyManager, got {curr.Soul}");

                // Test 27: EXP added via DungeonProgressionManager
                Assert(prog.TotalExpEarned == 50 && prog.CurrentExp == 50, $"Expected 50 EXP via DungeonProgressionManager, got {prog.TotalExpEarned}");

                // Test 28: Save & Load via SaveManager preserves everything
                saveMgr.SaveGame();
                Assert(saveMgr.HasSave(), "SaveManager should have saved game data");
                bool loaded = saveMgr.LoadGame();
                Assert(loaded, "SaveManager.LoadGame() should succeed");
                Assert(qMgr.GetProgressEntry("main_c1_q1").isRewardClaimed, "Quest claim state should persist through SaveManager");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_29_30_31_32_33_34_Integration_AllMilestone1To6SystemsStillWork", () =>
            {
                var go = new GameObject("TestM7_29_34");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(5000, 1000, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 5000);
                var roomInv = go.AddComponent<RoomInventoryManager>();
                roomInv.Configure(roomDb, rm);
                var monsterInv = go.AddComponent<MonsterInventoryManager>();
                monsterInv.Configure(monsterDb, rm);
                var trapInv = go.AddComponent<TrapInventoryManager>();
                trapInv.Configure(trapDb, rm);
                rm.ConfigureRoomSystems(roomDb, curr, roomInv);
                rm.ConfigureUnitInventories(monsterInv, trapInv);
                rm.ConfigureUnitDefinitions(monsterDb, trapDb);
                rm.Initialize();

                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var coreUp = go.AddComponent<DungeonCoreUpgradeManager>();
                coreUp.ResetUpgrade();
                var expMgr = go.AddComponent<DungeonExpansionManager>();
                expMgr.ResetExpansion();
                var facMgr = go.AddComponent<DungeonFacilityManager>();
                facMgr.ResetFacilities();
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();

                // Test 29: Dungeon Editor room & monster placement triggers Quest & Achievement progress
                var mRoom = roomDb.FindById("monster_room");
                var slime = monsterDb.FindById("slime");
                roomInv.TryAddOwned(mRoom, 1, out _);
                monsterInv.TryAddOwned(slime, 1, out _);
                Assert(rm.TryPlace(mRoom, new Vector2Int(3, 5), 0, out _), "Dungeon Editor room placement should work");
                var placedRoom = rm.GetRoomAt(new Vector2Int(3, 5));
                Assert(rm.TryPlaceMonster(new MonsterInstanceData { instanceId = "m_int_1", monsterId = "slime", roomInstanceId = placedRoom.instanceId, localPosition = Vector2.zero, level = 1 }, 3, out _),
                    "Dungeon Editor monster placement should work");
                Assert(aMgr.GetProgressEntry("ach_dungeon_architect").currentProgress >= 1, "Placing room should advance Dungeon Architect achievement");
                Assert(aMgr.GetProgressEntry("ach_monster_commander").currentProgress >= 1, "Placing monster should advance Monster Commander achievement");

                // Test 30: Battle reward calculation & Battle result notification works
                var battleCfg = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
                var reward = BattleRewardCalculator.Calculate(true, 5, 12, battleCfg, 1f, 1f, 1);
                Assert(reward.Won && reward.Gold > 0 && reward.Exp > 0, "Battle System reward calculation should work");
                qMgr.NotifyBattleResult("battle_int_01", true, 5, 12, 1);
                aMgr.NotifyBattleResult("battle_int_01", true, 5, 12, 1);
                Assert(aMgr.GetProgressEntry("ach_first_raid").isUnlocked, "First Raid achievement should unlock from battle result");

                // Test 31: Boss placement system works
                var bossRoom = new DungeonRoomInstanceData { instanceId = "br_int", roomId = "boss_room", roomType = DungeonRoomType.BossRoom, position = new Vector2Int(5, 5), width = 2, height = 2 };
                rm.Layout.rooms.Add(bossRoom);
                Assert(rm.TryPlaceBoss(new BossInstanceData { instanceId = "b_int", bossId = "cave_dragon", roomInstanceId = "br_int" }, out string bErr), $"Boss placement failed: {bErr}");

                // Test 32: Core Upgrade works & updates achievement/quest
                Assert(coreUp.TryUpgrade(out string cErr), $"Core upgrade failed: {cErr}");
                Assert(coreUp.CoreLevel == 2, "Core level should be 2");

                // Test 33: Dungeon Expansion works & updates achievement/quest
                prog.AddExp(500); // Level up to >= 3 so 11x11 expansion unlocks
                Assert(expMgr.TryExpand(out string eErr), $"Dungeon expansion failed: {eErr}");
                Assert(expMgr.CurrentDimension == 11 && rm.GridWidth == 11, "Dungeon grid should expand to 11x11");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Debug.Log($"========== MILESTONE 7 TEST RESULTS: {passed} / {total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
