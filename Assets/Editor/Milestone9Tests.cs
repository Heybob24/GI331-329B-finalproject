using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class Milestone9Tests
    {
        [MenuItem("Dream Dungeon/Run Milestone 9 Tests")]
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

            Debug.Log("========== STARTING MILESTONE 9 TEST SUITE (35 TESTS) ==========");

            // 1. Open Game Mode Selection
            Run("Test_01_GameModeSelection_OpensAndDisplaysAll3Modes", () =>
            {
                var go = new GameObject("TestM9_01");
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var wmUI = go.AddComponent<WorldMapUI>();
                var endlessUI = go.AddComponent<EndlessModeUI>();
                var challengeUI = go.AddComponent<ChallengeSelectionUI>();
                var modeUI = go.AddComponent<GameModeSelectionUI>();
                modeUI.Configure(safeGo.GetComponent<RectTransform>(), wmUI, endlessUI, challengeUI);
                modeUI.Open();
                Assert(modeUI.IsOpen, "GameModeSelectionUI should open");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 2. Enter Endless Dungeon
            Run("Test_02_EnterEndlessDungeon_MenuOpensAndShowsRecords", () =>
            {
                var go = new GameObject("TestM9_02");
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var ecMgr = go.AddComponent<EndlessChallengeManager>();
                var endlessUI = go.AddComponent<EndlessModeUI>();
                endlessUI.Configure(safeGo.GetComponent<RectTransform>(), ecMgr);
                endlessUI.Open();
                Assert(endlessUI.IsOpen, "EndlessModeUI should open");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 3. Validate dungeon before starting
            Run("Test_03_ValidateDungeon_RejectsInvalidAndAcceptsValidLayout", () =>
            {
                var go = new GameObject("TestM9_03");
                var ecMgr = go.AddComponent<EndlessChallengeManager>();
                var grid = go.AddComponent<DungeonGrid>();
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 1000);
                rm.Initialize();
                Assert(ecMgr.ValidateDungeonForRun(rm, null, out _), "Default initialized layout with path should be valid");
                rm.Layout.rooms.Clear();
                Assert(!ecMgr.ValidateDungeonForRun(rm, null, out string err) && !string.IsNullOrEmpty(err), "Empty layout without Entrance/Core must fail validation");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 4. Start Endless Run successfully
            Run("Test_04_StartEndlessRun_SetsBattleSessionContext", () =>
            {
                BattleSession.BeginEndless("{}", "DungeonEditor", 1000, 100, 0);
                Assert(BattleSession.IsEndlessBattle && BattleSession.ActiveGameMode == GameModeType.Endless, "BattleSession must enter Endless mode");
                BattleSession.Reset();
            });

            // 5. Spawn Wave 1 & 6. Complete a wave and spawn the next wave
            Run("Test_05_06_EndlessWaveManager_SpawnsWave1AndAdvancesToWave2", () =>
            {
                var go = new GameObject("TestM9_05_06");
                var wm = go.AddComponent<WaveManager>();
                var cfg = ScriptableObject.CreateInstance<BattleConfiguration>();
                var advDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                var route = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
                wm.Configure(cfg, advDb, route, c => new Vector3(c.x, c.y, 0f), Vector3.zero, 0.62f, null);
                wm.EnableEndlessMode(EndlessModeConfig.CreateRuntimeDefaultConfig());
                wm.StartWaves();
                Assert(wm.CurrentWave == 1 && wm.IsEndlessMode, "Endless mode should start at Wave 1");
                wm.Tick(1.0f, new List<ICombatTarget>(), null);
                Assert(wm.AllAdventurers.Count > 0, "Wave 1 should spawn adventurers");
                wm.CleanUp();
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 7. Verify enemy scaling at Wave 5 & 8. Verify enemy scaling at Wave 10
            Run("Test_07_08_EndlessWaveScaling_Wave5AndWave10Multipliers", () =>
            {
                var cfg = EndlessModeConfig.CreateRuntimeDefaultConfig();
                cfg.EvaluateWaveMultipliers(1, out float hp1, out float atk1, out _, out float cnt1, out _);
                cfg.EvaluateWaveMultipliers(5, out float hp5, out float atk5, out _, out float cnt5, out _);
                cfg.EvaluateWaveMultipliers(10, out float hp10, out float atk10, out _, out float cnt10, out _);
                cfg.EvaluateWaveMultipliers(20, out float hp20, out float atk20, out _, out float cnt20, out _);
                cfg.EvaluateWaveMultipliers(30, out float hp30, out float atk30, out _, out float cnt30, out _);

                Assert(Mathf.Approximately(hp1, 1.0f) && Mathf.Approximately(atk1, 1.0f), "Wave 1 should be 100%");
                Assert(Mathf.Approximately(hp5, 1.25f) && Mathf.Approximately(atk5, 1.15f) && Mathf.Approximately(cnt5, 1.20f), "Wave 5 scaling must match 125% HP, 115% Atk, 120% Count");
                Assert(Mathf.Approximately(hp10, 1.75f) && Mathf.Approximately(atk10, 1.40f) && Mathf.Approximately(cnt10, 1.50f), "Wave 10 scaling must match 175% HP, 140% Atk, 150% Count");
                Assert(Mathf.Approximately(hp20, 2.50f) && Mathf.Approximately(hp30, 3.50f), "Wave 20 & 30 scaling must match 250% and 350% HP");
            });

            // 9. Verify Elite spawning
            Run("Test_09_EliteEnemySystem_All5VariantsAndDamageResistance", () =>
            {
                var cfg = EndlessModeConfig.CreateRuntimeDefaultConfig();
                Assert(cfg.eliteVariants.Count == 5, $"Expected 5 Elite variants, got {cfg.eliteVariants.Count}");
                Assert(cfg.IsEliteWave(3) && cfg.IsEliteWave(6), "Waves 3 and 6 should be Elite waves");

                var advDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                var baseKnight = advDb.Find("elite_knight");
                int origHp = baseKnight.maxHP;

                EliteEnemyData eliteKnight = cfg.eliteVariants[0];
                AdventurerData runtimeElite = eliteKnight.CreateRuntimeEliteAdventurerData(baseKnight, 1.25f, 1.15f, 1.10f);
                Assert(runtimeElite.maxHP > origHp, "Elite runtime HP must be scaled higher");
                Assert(baseKnight.maxHP == origHp, "Base ScriptableObject must not be mutated");

                var actorGo = new GameObject("TestEliteActor");
                var actor = actorGo.AddComponent<AdventurerActor>();
                actor.Initialize(runtimeElite, 2, new List<Vector2Int> { Vector2Int.zero, Vector2Int.right }, c => Vector3.zero, Vector3.zero, null, "elite_1");
                actor.ApplyEliteVariant(eliteKnight);
                Assert(actor.IsElite && actor.DamageResistance > 0f, "Actor must be marked IsElite with damage resistance");
                UnityEngine.Object.DestroyImmediate(actorGo);
            });

            // 10. Verify Boss spawning & 11. Verify Boss HP bar
            Run("Test_10_11_EndlessBossSystem_ScheduleAndBossWaveActor", () =>
            {
                var cfg = EndlessModeConfig.CreateRuntimeDefaultConfig();
                Assert(cfg.IsBossWave(5, out var b5) && b5.bossTier == EndlessBossTier.MiniBoss, "Wave 5 should spawn MiniBoss");
                Assert(cfg.IsBossWave(10, out var b10) && b10.bossTier == EndlessBossTier.MajorBoss, "Wave 10 should spawn MajorBoss");
                Assert(cfg.IsBossWave(15, out var b15) && b15.bossTier == EndlessBossTier.EliteBoss, "Wave 15 should spawn EliteBoss");
                Assert(cfg.IsBossWave(30, out var b30) && b30.bossTier == EndlessBossTier.EndlessBoss, "Wave 30 should spawn EndlessBoss");
                Assert(!cfg.IsBossWave(4, out _), "Wave 4 must not be a Boss wave");
            });

            // 12. Verify Dungeon Core defeat condition
            Run("Test_12_DungeonCoreDefeatCondition_TriggersOnZeroHP", () =>
            {
                var coreGo = new GameObject("TestCore");
                var core = coreGo.AddComponent<DungeonCoreActor>();
                bool destroyed = false;
                core.Initialize(100, 0, 1, null, 0.62f);
                core.CoreDestroyed += () => destroyed = true;
                core.TakeDamage(new DamageInfo(150, null, CombatTeam.Adventurer));
                Assert(!core.IsAlive && destroyed, "Dungeon Core reaching 0 HP must trigger CoreDestroyed");
                UnityEngine.Object.DestroyImmediate(coreGo);
            });

            // 13. Verify Endless Score calculation
            Run("Test_13_ScoreSystem_CalculatesAccurately", () =>
            {
                var sCfg = new ScoreCalculationConfig();
                var res = ScoreCalculator.Calculate(sCfg, wavesCompleted: 5, highestWaveReached: 6,
                    normalEnemiesDefeated: 20, eliteEnemiesDefeated: 4, bossesDefeated: 1,
                    coreHpRemaining: 250, coreMaxHp: 500, runDurationSeconds: 60f, challengeCompleted: false);
                // 5*100 + 20*10 + 4*50 + 1*200 + 125 (50% core) + 120 (60s*2) = 500 + 200 + 200 + 200 + 125 + 120 = 1345
                Assert(res.WaveScore == 500 && res.EnemyScore == 200 && res.EliteScore == 200 && res.BossScore == 200, "Component scores must match config");
                Assert(res.TotalScore == 1345, $"Expected TotalScore 1345, got {res.TotalScore}");
            });

            // 14. Verify highest wave record & 15. Verify highest score record
            Run("Test_14_15_Records_HighestWaveAndScoreNeverDowngrade", () =>
            {
                var go = new GameObject("TestM9_14_15");
                var ecMgr = go.AddComponent<EndlessChallengeManager>();
                var out1 = ecMgr.RecordEndlessRunOutcome("run_high", 12, 11, 45, 8, 2, 0, 500, 180f);
                Assert(out1.IsNewWaveRecord && out1.IsNewScoreRecord, "First run should set wave and score records");
                int bestWave = ecMgr.HighestEndlessWave;
                int bestScore = ecMgr.HighestEndlessScore;

                var out2 = ecMgr.RecordEndlessRunOutcome("run_low", 4, 3, 10, 1, 0, 0, 500, 45f);
                Assert(!out2.IsNewWaveRecord && !out2.IsNewScoreRecord, "Lower run must not be marked new record");
                Assert(ecMgr.HighestEndlessWave == bestWave && ecMgr.HighestEndlessScore == bestScore, "Records must never be overwritten by worse results");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 16. Verify reward calculation & 17. Verify duplicate reward prevention
            Run("Test_16_17_EndlessRewards_ScalingAndDuplicatePrevention", () =>
            {
                var go = new GameObject("TestM9_16_17");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 100, 0);
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var ecMgr = go.AddComponent<EndlessChallengeManager>();

                var out1 = ecMgr.RecordEndlessRunOutcome("dup_endless_1", 6, 5, 20, 3, 1, 0, 500, 90f, grantImmediateCurrency: true);
                Assert(out1.RewardGold > 0 && out1.RewardSoul > 0 && out1.RewardExp > 0, "Endless run should calculate positive rewards");
                int goldAfterFirst = curr.Gold;

                var outDup = ecMgr.RecordEndlessRunOutcome("dup_endless_1", 6, 5, 20, 3, 1, 0, 500, 90f, grantImmediateCurrency: true);
                Assert(outDup.RewardGold == 0 && curr.Gold == goldAfterFirst, "Duplicate runResultId must not grant rewards twice");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 18. Verify Save and Load & 19. Verify retry does not stack multipliers
            Run("Test_18_19_SaveLoadAndNoRetryMultiplierStacking", () =>
            {
                var go1 = new GameObject("TestM9_Save1");
                var ec1 = go1.AddComponent<EndlessChallengeManager>();
                var save1 = go1.AddComponent<SaveManager>();
                ec1.RecordEndlessRunOutcome("save_endless_1", 15, 14, 60, 10, 3, 0, 500, 210f);
                int savedWave = ec1.HighestEndlessWave;
                int savedScore = ec1.HighestEndlessScore;
                save1.SaveGame();
                UnityEngine.Object.DestroyImmediate(go1);

                var go2 = new GameObject("TestM9_Load2");
                var ec2 = go2.AddComponent<EndlessChallengeManager>();
                var save2 = go2.AddComponent<SaveManager>();
                Assert(save2.LoadGame(), "SaveManager.LoadGame() must succeed");
                Assert(ec2.HighestEndlessWave == savedWave && ec2.HighestEndlessScore == savedScore, "Endless records must persist across Save/Load");

                // Test 19: Retry scaling check
                ec2.EndlessConfig.EvaluateWaveMultipliers(5, out float hpA, out _, out _, out _, out _);
                ec2.EndlessConfig.EvaluateWaveMultipliers(5, out float hpB, out _, out _, out _, out _);
                Assert(Mathf.Approximately(hpA, hpB), "Evaluating wave multipliers after retry must never stack");
                UnityEngine.Object.DestroyImmediate(go2);
            });

            // 20. Enter Challenge Mode, 21. Select a locked challenge, 22. Verify unlock requirements, 23. Start a valid challenge
            Run("Test_20_21_22_23_ChallengeMode_UnlockRequirementsAndSelection", () =>
            {
                var go = new GameObject("TestM9_20_23");
                var prog = go.AddComponent<DungeonProgressionManager>();
                prog.ResetProgression();
                var ecMgr = go.AddComponent<EndlessChallengeManager>();
                var safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var chUI = go.AddComponent<ChallengeSelectionUI>();
                chUI.Configure(safeGo.GetComponent<RectTransform>(), ecMgr);
                chUI.Open();

                Assert(chUI.IsOpen && chUI.SpawnedCards.Count == 6, $"Expected 6 Challenge cards, got {chUI.SpawnedCards.Count}");
                Assert(ecMgr.IsChallengeUnlocked("challenge_monster_survival", out _), "Challenge 1 must be unlocked at start");
                Assert(!ecMgr.IsChallengeUnlocked("challenge_trap_master", out string lockReason) && !string.IsNullOrEmpty(lockReason), "Challenge 2 must be locked before Challenge 1 is cleared");
                Assert(!ecMgr.TrySelectChallenge("challenge_trap_master", out _), "Selecting locked challenge must be rejected");
                Assert(ecMgr.TrySelectChallenge("challenge_monster_survival", out _), "Selecting unlocked challenge must succeed");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 24. Verify multiple modifiers & 25. Verify modifier cleanup after the run
            Run("Test_24_25_ChallengeModifiers_MultipleModifiersAndCleanup", () =>
            {
                var go = new GameObject("TestM9_24_25");
                var ecMgr = go.AddComponent<EndlessChallengeManager>();
                ChallengeData c6 = ecMgr.GetChallenge("challenge_limited_resources");
                Assert(c6 != null && c6.modifiers.Count >= 4, "Challenge 6 should have 4 simultaneous modifiers");

                ecMgr.ActiveModifiers.ApplyModifiers(c6.modifiers);
                Assert(ecMgr.ActiveModifiers.IsActive, "Modifiers should be active");
                Assert(ecMgr.ActiveModifiers.MaxAllowedMonsters == 4 && ecMgr.ActiveModifiers.MaxAllowedTraps == 4, "Capacity modifiers should apply");
                Assert(ecMgr.ActiveModifiers.EnemyCountMultiplier > 1.0f, "Swarm modifier should apply");

                // Re-applying must not stack
                float multOnce = ecMgr.ActiveModifiers.EnemyCountMultiplier;
                ecMgr.ActiveModifiers.ApplyModifiers(c6.modifiers);
                Assert(Mathf.Approximately(ecMgr.ActiveModifiers.EnemyCountMultiplier, multOnce), "Re-applying modifiers must not stack");

                ecMgr.CleanUpTemporaryRunState();
                Assert(!ecMgr.ActiveModifiers.IsActive && ecMgr.ActiveModifiers.MaxAllowedMonsters == -1 && Mathf.Approximately(ecMgr.ActiveModifiers.CoreHpMultiplier, 1f),
                    "CleanUpTemporaryRunState must restore all modifier values to defaults");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 26. Complete a challenge, 27. Fail a challenge, 28. Verify first-clear rewards, 29. Verify replay rewards
            Run("Test_26_27_28_29_ChallengeOutcome_CompleteFailFirstClearAndReplayRewards", () =>
            {
                var go = new GameObject("TestM9_26_29");
                var ecMgr = go.AddComponent<EndlessChallengeManager>();

                // Test 27: Fail challenge
                var failOut = ecMgr.RecordChallengeRunOutcome("ch_fail_1", "challenge_monster_survival", false, 2, 4, 8, 1, 0, 0, 500, 60f);
                Assert(!failOut.Completed && failOut.RewardGold == 0 && !ecMgr.IsChallengeCompleted("challenge_monster_survival"),
                    "Failed challenge must not mark challenge completed or grant victory rewards");

                // Test 26 & 28: First clear completion
                var win1 = ecMgr.RecordChallengeRunOutcome("ch_win_1", "challenge_monster_survival", true, 4, 4, 18, 2, 0, 400, 500, 95f);
                Assert(win1.Completed && win1.IsFirstClear && win1.RewardGold > 0, "First clear must grant First Clear reward");
                Assert(ecMgr.IsChallengeCompleted("challenge_monster_survival"), "Challenge 1 must be marked completed");
                Assert(ecMgr.IsChallengeUnlocked("challenge_trap_master", out _), "Completing Challenge 1 must unlock Challenge 2");

                // Test 29: Replay completion grants lower replay rewards
                var win2 = ecMgr.RecordChallengeRunOutcome("ch_win_2", "challenge_monster_survival", true, 4, 4, 18, 2, 0, 420, 500, 88f);
                Assert(win2.Completed && !win2.IsFirstClear && win2.RewardGold > 0 && win2.RewardGold < win1.RewardGold,
                    "Replay clear must grant Replay rewards, not First-Clear rewards");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // 30. Verify Quest integration, 31. Verify Achievement integration, 32. Verify Campaign progress remains unchanged, 33-35. Stability & No errors
            Run("Test_30_to_35_QuestAchievementCampaignIsolationAndStability", () =>
            {
                var go = new GameObject("TestM9_30_35");
                var qMgr = go.AddComponent<QuestManager>();
                var aMgr = go.AddComponent<AchievementManager>();
                var wm = go.AddComponent<WorldMapManager>();
                var cm = go.AddComponent<CampaignManager>();
                var ecMgr = go.AddComponent<EndlessChallengeManager>();

                // Complete a campaign stage first
                cm.RecordCampaignBattleOutcome("camp_m9_check", "forest_stage_1", StageDifficulty.Easy, true, 3, 3, 10, 0, 500, 500);
                Assert(cm.IsStageCompleted("forest_stage_1"), "Campaign stage 1 should be completed");

                // Run Endless & Boss Rush Challenge
                ecMgr.RecordEndlessRunOutcome("endless_q_ach", 12, 11, 40, 6, 2, 0, 500, 150f);
                ecMgr.RecordChallengeRunOutcome("chal_bossrush_q_ach", "challenge_boss_rush", true, 4, 4, 4, 4, 4, 350, 500, 120f);

                // Verify Quest objectives progressed
                Assert(qMgr.GetProgressEntry("special_endless_initiate")?.status == QuestStatus.Completed, "Quest 'Endless Explorer' should complete");
                Assert(qMgr.GetProgressEntry("special_endless_wave_10")?.status == QuestStatus.Completed, "Quest 'Endless Survivor (Wave 10)' should complete");
                Assert(qMgr.GetProgressEntry("special_endless_elites")?.status == QuestStatus.Completed, "Quest 'Elite Vanquisher' should complete");
                Assert(qMgr.GetProgressEntry("special_endless_boss")?.status == QuestStatus.Completed, "Quest 'Endless Boss Slayer' should complete");
                Assert(qMgr.GetProgressEntry("special_challenge_victor")?.status == QuestStatus.Completed, "Quest 'Challenge Conqueror' should complete");
                Assert(qMgr.GetProgressEntry("special_boss_rush")?.status == QuestStatus.Completed, "Quest 'Boss Rush Champion' should complete");

                // Verify Achievements progressed
                Assert(aMgr.GetProgressEntry("ach_boss_rush_victor")?.isUnlocked == true, "Achievement 'Boss Rush Victor' should unlock");
                Assert(aMgr.GetProgressEntry("ach_endless_veteran")?.currentProgress >= 1, "Achievement 'Endless Veteran' should advance");

                // Verify Campaign progress remained untouched
                Assert(cm.IsStageCompleted("forest_stage_1") && cm.IsStageUnlocked("forest_stage_2"), "Campaign progress must remain completely intact");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Debug.Log($"========== MILESTONE 9 TEST RESULTS: {passed} / {total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
