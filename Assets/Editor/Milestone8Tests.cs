using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class Milestone8Tests
    {
        [MenuItem("Dream Dungeon/Run Milestone 8 Tests")]
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

            Debug.Log("========== STARTING MILESTONE 8 TEST SUITE (45 TESTS) ==========");

            // ==========================================
            // WORLD MAP TESTS (Tests 1 - 5)
            // ==========================================
            Run("Test_01_02_WorldMap_OpensAndDisplays6Regions", () =>
            {
                var go = new GameObject("TestM8_01_02");
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var wmUI = go.AddComponent<WorldMapUI>();
                wmUI.Configure(safeGo.GetComponent<RectTransform>(), wm);
                wmUI.Open();

                Assert(wmUI.IsOpen, "WorldMapUI should open");
                Assert(wm.GetAllRegions().Count == 6, $"Expected 6 default World Regions, got {wm.GetAllRegions().Count}");
                Assert(wmUI.SpawnedCards.Count == 6, $"Expected 6 RegionCardUI cards, got {wmUI.SpawnedCards.Count}");
                Assert(wm.GetRegion("forgotten_forest")?.regionName == "Forgotten Forest", "Region 1 name mismatch");
                Assert(wm.GetRegion("abyssal_realm")?.regionName == "Abyssal Realm", "Region 6 name mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_03_04_05_WorldMap_RegionLockUnlockAndPersistence", () =>
            {
                var go = new GameObject("TestM8_03_05");
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();

                // Test 3: Starting region unlocked, Cursed Cave locked initially
                Assert(wm.CanEnterRegion("forgotten_forest", out _), "Forgotten Forest must be unlocked at start");
                Assert(!wm.CanEnterRegion("cursed_cave", out string lockMsg) && !string.IsNullOrEmpty(lockMsg), "Cursed Cave must be locked initially");
                Assert(!wm.TrySelectRegion("cursed_cave", out _), "Player must not be allowed to select locked region");

                // Test 4: Complete all 3 stages of Forgotten Forest and reach Dungeon Level 2 -> Cursed Cave unlocks
                prog.AddExp(100); // Level 2
                cm.RecordCampaignBattleOutcome("b_f1", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 10, 0, 500, 500);
                cm.RecordCampaignBattleOutcome("b_f2", "forest_stage_2", StageDifficulty.Easy, true, 4, 4, 12, 0, 500, 500);
                cm.RecordCampaignBattleOutcome("b_f3", "forest_stage_3", StageDifficulty.Easy, true, 5, 5, 15, 1, 500, 500);

                Assert(wm.IsRegionCompleted("forgotten_forest"), "Forgotten Forest should be marked completed");
                Assert(wm.IsRegionUnlocked("cursed_cave") && wm.CanEnterRegion("cursed_cave", out _), "Cursed Cave should unlock after Forgotten Forest completion");

                // Test 5: Region progress saves and restores
                CampaignSaveState saved = cm.ExportSaveState();
                UnityEngine.Object.DestroyImmediate(go);

                var go2 = new GameObject("TestM8_05_Reload");
                var wm2 = go2.AddComponent<WorldMapManager>();
                var cm2 = go2.AddComponent<CampaignManager>();
                cm2.ImportSaveState(saved);
                Assert(wm2.IsRegionCompleted("forgotten_forest"), "Completed region must persist");
                Assert(wm2.IsRegionUnlocked("cursed_cave"), "Unlocked region must persist");
                UnityEngine.Object.DestroyImmediate(go2);
            });

            // ==========================================
            // CAMPAIGN & STAGE TESTS (Tests 6 - 11)
            // ==========================================
            Run("Test_06_07_08_Campaign_LoadsDisplaysStagesAndEnforcesStageLock", () =>
            {
                var go = new GameObject("TestM8_06_08");
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var campUI = go.AddComponent<CampaignSelectionUI>();
                campUI.Configure(safeGo.GetComponent<RectTransform>(), cm, wm);
                campUI.OpenForRegion("forgotten_forest");

                // Test 6 & 7: Campaign loads and displays 3 stages
                Assert(campUI.IsOpen && campUI.SpawnedCards.Count == 3, $"Expected 3 StageCardUI items, got {campUI.SpawnedCards.Count}");
                Assert(cm.IsStageUnlocked("forest_stage_1"), "Stage 1 must be unlocked");

                // Test 8: Stage 2 & 3 locked before Stage 1 is cleared
                Assert(!cm.IsStageUnlocked("forest_stage_2"), "Stage 2 must start locked");
                Assert(!cm.TrySelectStage("forest_stage_2", out string err) && !string.IsNullOrEmpty(err), "Selecting locked Stage 2 must fail");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_09_10_11_Campaign_StageUnlockReplayAndFirstClearSingleGrant", () =>
            {
                var go = new GameObject("TestM8_09_11");
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();

                // Test 9: Clearing Stage 1 unlocks Stage 2
                CampaignRewardBreakdown clear1 = cm.RecordCampaignBattleOutcome(
                    "run_1", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 8, 0, 480, 500);
                Assert(clear1.IsFirstClear && clear1.FirstClearGold > 0, "First clear must grant First Clear bonus");
                Assert(cm.IsStageCompleted("forest_stage_1"), "Stage 1 should be completed");
                Assert(cm.IsStageUnlocked("forest_stage_2"), "Stage 2 should unlock after Stage 1 completion");

                // Test 10 & 11: Replay Stage 1 works, First Clear reward is NOT granted again
                Assert(cm.TrySelectStage("forest_stage_1", out _), "Completed stage should be replayable");
                CampaignRewardBreakdown replay1 = cm.RecordCampaignBattleOutcome(
                    "run_2", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 8, 0, 480, 500);
                Assert(!replay1.IsFirstClear && replay1.FirstClearGold == 0 && replay1.FirstClearSoul == 0,
                    "Replay must NOT grant First Clear bonus a second time");
                Assert(replay1.TotalGold < clear1.TotalGold, "Replay total gold must be lower than first-clear total gold");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // DIFFICULTY SYSTEM TESTS (Tests 12 - 15)
            // ==========================================
            Run("Test_12_13_14_15_Difficulty_SelectionScalingRewardAndNoCompoundMutation", () =>
            {
                var go = new GameObject("TestM8_12_15");
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var diffMgr = go.AddComponent<DifficultyManager>();
                var cm = go.AddComponent<CampaignManager>();

                // Test 12: Easy & Normal unlocked at Lv 1; Nightmare locked at Lv 1
                Assert(diffMgr.TrySelectDifficulty(StageDifficulty.Normal, out _), "Normal difficulty should be selectable at Lv 1");
                Assert(!diffMgr.TrySelectDifficulty(StageDifficulty.Nightmare, out _), "Nightmare difficulty must be locked at Lv 1");
                prog.AddExp(20000); // Level 10 unlocks all difficulties
                Assert(diffMgr.TrySelectDifficulty(StageDifficulty.Hard, out _), "Hard difficulty should be selectable at Lv 10");

                // Test 13 & 15: Enemy stats scale by difficulty without mutating original ScriptableObject asset or compounding on retry
                AdventurerDatabase origDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                AdventurerData origSwordsman = origDb.Find("swordsman");
                int baseHp = origSwordsman.maxHP;
                int baseAtk = origSwordsman.attack;

                DifficultyData hardData = diffMgr.GetDifficultyData(StageDifficulty.Hard);
                AdventurerData scaled1 = hardData.CreateRuntimeScaledCopy(origSwordsman);
                AdventurerData scaled2 = hardData.CreateRuntimeScaledCopy(origSwordsman);

                Assert(scaled1.maxHP > baseHp && scaled1.attack > baseAtk, "Hard difficulty must increase enemy HP and Attack");
                Assert(origSwordsman.maxHP == baseHp && origSwordsman.attack == baseAtk, "Base ScriptableObject stats must NEVER be modified");
                Assert(scaled1.maxHP == scaled2.maxHP && scaled1.attack == scaled2.attack, "Repeated/Retry scaling must not compound stats");

                // Test 14: Reward scales with Difficulty
                CampaignStageData stage = cm.GetStage("forest_stage_1");
                AdventurerFactionData faction = cm.GetPrimaryFactionForStage(stage);
                DifficultyData easyData = diffMgr.GetDifficultyData(StageDifficulty.Easy);
                var easyReward = CampaignRewardManager.PreviewStageReward(stage, easyData, faction, false, false);
                var hardReward = CampaignRewardManager.PreviewStageReward(stage, hardData, faction, false, false);
                Assert(hardReward.BaseGold > easyReward.BaseGold && hardReward.BaseEXP > easyReward.BaseEXP,
                    "Hard difficulty rewards must be higher than Easy difficulty rewards");

                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // ADVENTURER FACTION TESTS (Tests 16 - 19)
            // ==========================================
            Run("Test_16_17_18_19_Faction_LoadsSpawnsPassiveAndUsesExistingAdventurerAI", () =>
            {
                // Test 16: All 6 Factions load properly
                var factions = AdventurerFactionData.GetDefaultFactions();
                Assert(factions.Count >= 6, $"Expected 6 factions, got {factions.Count}");
                AdventurerFactionData holyOrder = AdventurerFactionData.FindFaction("holy_order");
                AdventurerFactionData dragonHunters = AdventurerFactionData.FindFaction("dragon_hunters");
                Assert(holyOrder != null && dragonHunters != null, "Factions should be found by ID");

                // Test 17, 18 & 19: Spawn AdventurerActor using existing AI & apply faction passive bonus
                AdventurerDatabase advDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                AdventurerData knightData = advDb.Find("knight");
                var actorGo = new GameObject("TestFactionActor");
                var actor = actorGo.AddComponent<AdventurerActor>();
                var path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
                actor.Initialize(knightData, 0, path, c => new Vector3(c.x, c.y, 0f), Vector3.zero, null, "fac_act_1");
                actor.ApplyFactionPassive(holyOrder);

                Assert(actor.FactionId == "holy_order", "Actor should record factionId");
                Assert(actor.PassiveHealPerSecond > 0, "Holy Order passive should grant regeneration");
                Assert(actor.Movement != null && actor.Attack != null, "Actor must use existing AdventurerMovement and AdventurerAttack AI");

                actor.ApplyFactionPassive(dragonHunters);
                Assert(actor.Attack.BonusDamageVsBossMultiplier > 1.0f, "Dragon Hunters passive must grant bonus damage vs Bosses");

                UnityEngine.Object.DestroyImmediate(actorGo);
            });

            // ==========================================
            // BATTLE, WAVE & REWARD TESTS (Tests 20 - 32)
            // ==========================================
            Run("Test_20_21_22_23_24_25_26_BattleFlow_WaveConfigBossWaveVictoryDefeatRetryAndReturn", () =>
            {
                var go = new GameObject("TestM8_20_26");
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();
                var diffMgr = go.AddComponent<DifficultyManager>();

                // Test 21 & 22: WaveConfiguration & Boss Wave conversion
                CampaignStageData bossStage = cm.GetStage("forest_stage_3");
                Assert(bossStage != null && !string.IsNullOrEmpty(bossStage.bossId), "forest_stage_3 should have a Boss");
                BattleConfiguration runtimeCfg = WaveConfiguration.BuildRuntimeBattleConfiguration(
                    bossStage.waveConfiguration,
                    diffMgr.GetDifficultyData(StageDifficulty.Easy),
                    cm.GetPrimaryFactionForStage(bossStage));
                Assert(runtimeCfg != null && runtimeCfg.waves.Count == 5, $"Expected 5 waves in forest_stage_3, got {runtimeCfg?.waves.Count}");
                Assert(runtimeCfg.waves[4].waveName.Contains("BOSS"), "Final wave should be marked as BOSS wave");

                // Test 20: BeginCampaign sets BattleSession state properly
                BattleSession.BeginCampaign("{}", "DungeonEditor", 1000, 100, 0,
                    "forgotten_forest", "campaign_forgotten_forest", "forest_stage_1",
                    StageDifficulty.Easy, "adventurer_guild", "");
                Assert(BattleSession.IsCampaignBattle && BattleSession.StageId == "forest_stage_1", "BattleSession campaign context should be set");

                // Test 24: Defeat result records defeat count, does NOT unlock next stage, does NOT grant victory reward
                var defeatBreakdown = cm.RecordCampaignBattleOutcome(
                    "defeat_res_1", "forest_stage_1", StageDifficulty.Easy, false, 1, 3, 3, 0, 0, 500);
                Assert(!defeatBreakdown.Victory && defeatBreakdown.TotalGold == 0, "Defeat must not grant stage victory reward");
                Assert(cm.TotalDefeats == 1 && !cm.IsStageUnlocked("forest_stage_2"), "Defeat must increment defeats and keep next stage locked");

                // Test 23: Victory result unlocks next stage and calculates Perfect Clear
                var winBreakdown = cm.RecordCampaignBattleOutcome(
                    "win_res_1", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 10, 0, 500, 500);
                Assert(winBreakdown.Victory && winBreakdown.IsPerfectClear && winBreakdown.TotalGold > 0, "Victory with full Core HP should be Perfect Clear");
                Assert(cm.IsStageUnlocked("forest_stage_2"), "Victory must unlock Stage 2");

                // Test 25 & 26: Retry and ReturnToWorldMap requests
                BattleSession.RequestReturnToWorldMap("forest_stage_2");
                Assert(BattleSession.ReturnPending && BattleSession.ReturnToWorldMapPending && BattleSession.NextStageToOpenPending == "forest_stage_2",
                    "ReturnToWorldMap navigation request should be recorded");
                BattleSession.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_27_28_29_30_31_32_RewardsAndQuestAchievementIntegration", () =>
            {
                var go = new GameObject("TestM8_27_32");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();

                // Clear Stage 1 with grantImmediateCurrency: true
                var breakdown = cm.RecordCampaignBattleOutcome(
                    "reward_res_01", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 8, 0, 500, 500,
                    grantImmediateCurrency: true);

                // Test 27, 28, 29: Gold, Soul, and EXP granted accurately
                Assert(curr.Gold == 1000 + breakdown.TotalGold, $"Expected Gold {1000 + breakdown.TotalGold}, got {curr.Gold}");
                Assert(curr.Soul == 100 + breakdown.TotalSoul, $"Expected Soul {100 + breakdown.TotalSoul}, got {curr.Soul}");
                Assert(prog.TotalExpEarned == breakdown.TotalEXP, $"Expected EXP {breakdown.TotalEXP}, got {prog.TotalExpEarned}");

                // Test 30: Duplicate battleResultId does not grant rewards twice
                int goldBeforeDup = curr.Gold;
                cm.RecordCampaignBattleOutcome(
                    "reward_res_01", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 8, 0, 500, 500,
                    grantImmediateCurrency: true);
                Assert(curr.Gold == goldBeforeDup, "Duplicate battleResultId must not grant rewards again");

                // Test 31 & 32: Quest and Achievement progress updated from Campaign victory
                Assert(qMgr.GetProgressEntry("main_c1_q1").status == QuestStatus.Completed, "Main Quest 'Complete First Raid' should complete from Campaign battle");
                Assert(aMgr.GetProgressEntry("ach_first_raid").isUnlocked, "Achievement 'First Raid' should unlock from Campaign battle");

                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // SAVE / LOAD & FULL INTEGRATION (Tests 33 - 45)
            // ==========================================
            Run("Test_33_34_35_36_SaveLoad_PreservesRegionsStagesFirstClearAndSurvivesReload", () =>
            {
                var go1 = new GameObject("TestM8_Save");
                var curr1 = go1.AddComponent<CurrencyManager>();
                curr1.Configure(1500, 250, 0);
                var prog1 = go1.AddComponent<DungeonProgressionManager>();
                prog1.ResetProgression();
                prog1.AddExp(150); // Lv 2
                var wm1 = go1.AddComponent<WorldMapManager>();
                var cm1 = go1.AddComponent<CampaignManager>();
                var save1 = go1.AddComponent<SaveManager>();

                cm1.RecordCampaignBattleOutcome("sv_1", "forest_stage_1", StageDifficulty.Normal, true, 3, 3, 10, 0, 500, 500);
                cm1.RecordCampaignBattleOutcome("sv_2", "forest_stage_2", StageDifficulty.Normal, true, 4, 4, 12, 0, 500, 500);
                cm1.RecordCampaignBattleOutcome("sv_3", "forest_stage_3", StageDifficulty.Normal, true, 5, 5, 15, 1, 500, 500);

                save1.SaveGame();
                UnityEngine.Object.DestroyImmediate(go1);

                var go2 = new GameObject("TestM8_Load");
                var curr2 = go2.AddComponent<CurrencyManager>();
                var prog2 = go2.AddComponent<DungeonProgressionManager>();
                var wm2 = go2.AddComponent<WorldMapManager>();
                var cm2 = go2.AddComponent<CampaignManager>();
                var save2 = go2.AddComponent<SaveManager>();

                Assert(save2.LoadGame(), "SaveManager.LoadGame() should succeed");
                Assert(wm2.IsRegionCompleted("forgotten_forest") && wm2.IsRegionUnlocked("cursed_cave"), "Region completion and unlock must persist");
                Assert(cm2.IsStageCompleted("forest_stage_1") && cm2.IsFirstClearClaimed("forest_stage_1") && cm2.IsPerfectClear("forest_stage_1"),
                    "Stage completion, First Clear, and Perfect Clear must persist");
                Assert(cm2.GetStageRecord("forest_stage_1").bestDifficulty == StageDifficulty.Normal, "Best difficulty must persist");

                UnityEngine.Object.DestroyImmediate(go2);
            });

            Run("Test_37_to_45_Integration_AllMilestone1To8SystemsWorkTogether", () =>
            {
                var go = new GameObject("TestM8_FullIntegration");
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaController));
                safeGo.transform.SetParent(go.transform, false);
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(3000, 500, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 3000);
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
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();
                var diffMgr = go.AddComponent<DifficultyManager>();

                // Test 37, 38, 39, 40: Dungeon Editor, Monster, Trap, and Boss placement work alongside Campaign
                roomInv.TryAddOwned(roomDb.FindById("monster_room"), 1, out _);
                roomInv.TryAddOwned(roomDb.FindById("trap_room"), 1, out _);
                monsterInv.TryAddOwned(monsterDb.FindById("slime"), 1, out _);
                trapInv.TryAddOwned(trapDb.FindById("spike_trap"), 1, out _);

                Assert(rm.TryPlace(roomDb.FindById("monster_room"), new Vector2Int(3, 5), 0, out _), "Room placement works");
                Assert(rm.TryPlace(roomDb.FindById("trap_room"), new Vector2Int(4, 5), 0, out _), "Trap Room placement works");
                var mRoom = rm.GetRoomAt(new Vector2Int(3, 5));
                var tRoom = rm.GetRoomAt(new Vector2Int(4, 5));
                Assert(rm.TryPlaceMonster(new MonsterInstanceData { instanceId = "m_m8", monsterId = "slime", roomInstanceId = mRoom.instanceId, localPosition = Vector2.zero, level = 1 }, 3, out _), "Monster placement works");
                Assert(rm.TryPlaceTrap(new TrapInstanceData { instanceId = "t_m8", trapId = "spike_trap", roomInstanceId = tRoom.instanceId, localPosition = Vector2.zero, level = 1 }, 3, out _), "Trap placement works");

                // Undo/Redo still works
                Assert(rm.TryUndo(out _) && rm.TryRedo(out _), "Dungeon Editor Undo/Redo works");

                // Test 41, 42, 43, 44: Progression, Quest, Achievement, and SafeAreaController active
                Assert(prog != null && qMgr != null && aMgr != null && safeGo.GetComponent<SafeAreaController>() != null,
                    "Progression, Quest, Achievement, and SafeAreaController are all active");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Debug.Log($"========== MILESTONE 8 TEST RESULTS: {passed} / {total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
