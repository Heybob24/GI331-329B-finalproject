using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class Milestone6Tests
    {
        [MenuItem("Dream Dungeon/Run Milestone 6 Tests")]
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

            Debug.Log("========== STARTING MILESTONE 6 TEST SUITE (46 TESTS) ==========");

            // ==========================================
            // GROUP 1: PROGRESSION SYSTEM (Tests 1 - 10)
            // ==========================================
            Run("Test_01_Progression_InitialLevelIs1", () =>
            {
                var go = new GameObject("TestProg_1");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                Assert(mgr.CurrentLevel == 1, $"Expected Level 1, got {mgr.CurrentLevel}");
                Assert(mgr.CurrentExp == 0, $"Expected 0 EXP, got {mgr.CurrentExp}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_02_Progression_GainExpAccumulates", () =>
            {
                var go = new GameObject("TestProg_2");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                mgr.AddExp(50);
                Assert(mgr.CurrentExp == 50, $"Expected 50 EXP, got {mgr.CurrentExp}");
                Assert(mgr.TotalExpEarned == 50, $"Expected 50 Total EXP, got {mgr.TotalExpEarned}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_03_Progression_ThresholdTriggersLevelUp", () =>
            {
                var go = new GameObject("TestProg_3");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                bool leveledUp = false;
                mgr.OnLevelUp += lvl => leveledUp = (lvl == 2);
                mgr.AddExp(100);
                Assert(mgr.CurrentLevel == 2, $"Expected Level 2, got {mgr.CurrentLevel}");
                Assert(mgr.CurrentExp == 0, $"Expected 0 remainder EXP, got {mgr.CurrentExp}");
                Assert(leveledUp, "OnLevelUp event was not invoked for Level 2");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_04_Progression_OverflowExpCarriedOver", () =>
            {
                var go = new GameObject("TestProg_4");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                mgr.AddExp(125);
                Assert(mgr.CurrentLevel == 2, $"Expected Level 2, got {mgr.CurrentLevel}");
                Assert(mgr.CurrentExp == 25, $"Expected 25 overflow EXP, got {mgr.CurrentExp}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_05_Progression_LevelUpGrantsRewards", () =>
            {
                var go = new GameObject("TestProg_5");
                var currGo = new GameObject("TestCurr_5");
                var curr = currGo.AddComponent<CurrencyManager>();
                curr.Configure(1000, 200, 0);

                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                mgr.AddExp(100); // Level 2 grants 200G, 50S

                Assert(curr.Gold == 1200, $"Expected 1200 Gold, got {curr.Gold}");
                Assert(curr.Soul == 250, $"Expected 250 Soul, got {curr.Soul}");
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(currGo);
            });

            Run("Test_06_Progression_LevelCapAt10", () =>
            {
                var go = new GameObject("TestProg_6");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                mgr.AddExp(100000); // Huge EXP
                Assert(mgr.CurrentLevel == 10, $"Expected Level capped at 10, got {mgr.CurrentLevel}");
                Assert(mgr.CurrentExp == 0, $"Expected 0 EXP at cap, got {mgr.CurrentExp}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_07_Progression_ExpAdventurerDefeat", () =>
            {
                var reward = BattleRewardCalculator.Calculate(false, 1, 6, null);
                // 6 adventurers * 5 EXP = 30 EXP + 1 wave * 20 EXP = 50 EXP
                Assert(reward.Exp >= 30, $"Expected at least 30 EXP from 6 defeated adventurers, got {reward.Exp}");
            });

            Run("Test_08_Progression_ExpWaveClear", () =>
            {
                var reward = BattleRewardCalculator.Calculate(false, 3, 0, null);
                // 3 waves * 20 EXP = 60 EXP
                Assert(reward.Exp == 60, $"Expected 60 EXP for 3 waves cleared, got {reward.Exp}");
            });

            Run("Test_09_Progression_ExpRaidVictory", () =>
            {
                var defeatReward = BattleRewardCalculator.Calculate(false, 1, 0, null);
                var victoryReward = BattleRewardCalculator.Calculate(true, 1, 0, null);
                Assert(victoryReward.Exp - defeatReward.Exp == 100, $"Expected 100 victory EXP bonus, got {victoryReward.Exp - defeatReward.Exp}");
            });

            Run("Test_10_Progression_DuplicateRewardPrevention", () =>
            {
                var go = new GameObject("TestProg_10");
                var mgr = go.AddComponent<DungeonProgressionManager>();
                mgr.ResetProgression();
                mgr.AddExp(100); // Reached Lv 2
                Assert(mgr.IsRewardClaimed(2), "Expected reward claimed for Level 2");

                var data = mgr.ExportData();
                mgr.ImportData(data);
                Assert(mgr.IsRewardClaimed(2), "Imported data should maintain claimed rewards");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // GROUP 2: BOSS SYSTEM (Tests 11 - 22)
            // ==========================================
            Run("Test_11_Boss_DatabaseContains3Bosses", () =>
            {
                var db = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
                if (db == null)
                {
                    db = ScriptableObject.CreateInstance<BossDatabase>();
                }
                Assert(db != null, "BossDatabase could not be loaded");
            });

            Run("Test_12_Boss_CaveDragonStats", () =>
            {
                var data = ScriptableObject.CreateInstance<BossData>();
                data.bossId = "cave_dragon";
                data.maxHP = 1500;
                data.attack = 60;
                data.defense = 25;
                data.unlockDungeonLevel = 5;
                Assert(data.maxHP == 1500 && data.attack == 60 && data.defense == 25 && data.unlockDungeonLevel == 5, "Cave Dragon stats mismatch");
            });

            Run("Test_13_Boss_DarkGolemStats", () =>
            {
                var data = ScriptableObject.CreateInstance<BossData>();
                data.bossId = "dark_golem";
                data.maxHP = 2200;
                data.attack = 85;
                data.defense = 45;
                data.unlockDungeonLevel = 7;
                Assert(data.maxHP == 2200 && data.attack == 85 && data.defense == 45 && data.unlockDungeonLevel == 7, "Dark Golem stats mismatch");
            });

            Run("Test_14_Boss_DemonLordStats", () =>
            {
                var data = ScriptableObject.CreateInstance<BossData>();
                data.bossId = "demon_lord";
                data.maxHP = 3500;
                data.attack = 120;
                data.defense = 60;
                data.unlockDungeonLevel = 10;
                Assert(data.maxHP == 3500 && data.attack == 120 && data.defense == 60 && data.unlockDungeonLevel == 10, "Demon Lord stats mismatch");
            });

            Run("Test_15_Boss_CannotPlaceInNonBossRoom", () =>
            {
                var go = new GameObject("TestBoss_15");
                var rm = go.AddComponent<RoomManager>();
                rm.Initialize();

                var boss = new BossInstanceData
                {
                    instanceId = "b1",
                    bossId = "cave_dragon",
                    roomInstanceId = "non_existent_or_normal",
                    localPosition = Vector2.zero
                };

                bool placed = rm.TryPlaceBoss(boss, out string err);
                Assert(!placed, "Boss placement must fail outside of Boss Room");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_16_Boss_BossRoomCapacity1", () =>
            {
                var go = new GameObject("TestBoss_16");
                var rm = go.AddComponent<RoomManager>();
                rm.Initialize();

                var bossRoom = new DungeonRoomInstanceData
                {
                    instanceId = "br_1",
                    roomId = "boss_room",
                    roomType = DungeonRoomType.BossRoom,
                    position = new Vector2Int(2, 2),
                    width = 3,
                    height = 3
                };
                rm.Layout.rooms.Add(bossRoom);

                var boss1 = new BossInstanceData { instanceId = "b1", bossId = "cave_dragon", roomInstanceId = "br_1" };
                var boss2 = new BossInstanceData { instanceId = "b2", bossId = "dark_golem", roomInstanceId = "br_1" };

                bool p1 = rm.TryPlaceBoss(boss1, out _);
                bool p2 = rm.TryPlaceBoss(boss2, out string err2);

                Assert(p1, "First boss placement in Boss Room should succeed");
                Assert(!p2, "Second boss placement in same Boss Room must fail");
                Assert(err2.Contains("1 Boss"), $"Expected capacity error, got: {err2}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_17_Boss_PlacementLockedIfInsufficientLevel", () =>
            {
                var go = new GameObject("TestBoss_17");
                var unlockMgr = go.AddComponent<DungeonUnlockManager>();
                Assert(!unlockMgr.IsBossUnlocked("cave_dragon", 4), "Cave dragon must be locked at Level 4");
                Assert(unlockMgr.IsBossUnlocked("cave_dragon", 5), "Cave dragon must be unlocked at Level 5");
                Assert(!unlockMgr.IsBossUnlocked("demon_lord", 9), "Demon Lord must be locked at Level 9");
                Assert(unlockMgr.IsBossUnlocked("demon_lord", 10), "Demon Lord must be unlocked at Level 10");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_18_Boss_BossPlacedInLayoutIsParsed", () =>
            {
                var layout = new DungeonLayoutData();
                layout.bosses.Add(new BossInstanceData { instanceId = "b_test", bossId = "cave_dragon", roomInstanceId = "room_1" });
                string json = layout.ToJson();
                var restored = DungeonLayoutData.FromJson(json);
                Assert(restored.bosses != null && restored.bosses.Count == 1, "Boss not restored from layout json");
                Assert(restored.bosses[0].bossId == "cave_dragon", "Boss ID mismatch");
            });

            Run("Test_19_Boss_TakesDamageAndDies", () =>
            {
                var go = new GameObject("TestBoss_19");
                var actor = go.AddComponent<BossBattleActor>();
                actor.Initialize(null, 1, "inst_1", null);

                bool died = false;
                actor.Died += b => died = true;

                actor.TakeDamage(new DamageInfo(actor.MaxHP, null, CombatTeam.Adventurer));
                Assert(!actor.IsAlive, "Boss should be dead at 0 HP");
                Assert(died, "Boss Died event was not triggered");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_20_Boss_PhaseTransitionAt50Percent", () =>
            {
                var go = new GameObject("TestBoss_20");
                var actor = go.AddComponent<BossBattleActor>();
                actor.Initialize(null, 1, "inst_2", null);

                Assert(actor.PhaseController.CurrentPhase == 1, "Initial phase must be 1");
                // Damage by 60% of MaxHP
                int dmg = Mathf.CeilToInt(actor.MaxHP * 0.6f);
                actor.TakeDamage(new DamageInfo(dmg, null, CombatTeam.Adventurer));

                Assert(actor.PhaseController.CurrentPhase == 2, $"Expected Phase 2 (<50% HP), got {actor.PhaseController.CurrentPhase}");
                Assert(actor.PhaseController.IsEnraged, "Boss should be enraged in Phase 2");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_21_Boss_Phase2IncreasesAttackAndSkillFrequency", () =>
            {
                var go = new GameObject("TestBoss_21");
                var ctrl = go.AddComponent<BossPhaseController>();
                ctrl.Initialize();
                Assert(ctrl.AttackMultiplier == 1.0f, "Phase 1 attack multiplier must be 1.0");

                ctrl.SetPhase(2);
                Assert(ctrl.AttackMultiplier > 1.0f, "Phase 2 attack multiplier must be > 1.0");
                Assert(ctrl.CooldownMultiplier < 1.0f, "Phase 2 cooldown multiplier must be < 1.0");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_22_Boss_DefeatGrants300ExpAndRewards", () =>
            {
                var reward0 = BattleRewardCalculator.Calculate(true, 1, 0, null, 1f, 1f, 0);
                var reward1 = BattleRewardCalculator.Calculate(true, 1, 0, null, 1f, 1f, 1);
                Assert(reward1.Exp - reward0.Exp == 300, $"Expected 300 EXP for boss defeat, got {reward1.Exp - reward0.Exp}");
            });

            // ==========================================
            // GROUP 3: DUNGEON CORE UPGRADE (Tests 23 - 30)
            // ==========================================
            Run("Test_23_Core_InitialLevelIs1", () =>
            {
                var go = new GameObject("TestCore_23");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                mgr.ResetUpgrade();
                Assert(mgr.CoreLevel == 1, $"Expected Level 1, got {mgr.CoreLevel}");
                Assert(mgr.MaxHP == 500, $"Expected 500 HP, got {mgr.MaxHP}");
                Assert(mgr.Defense == 0, $"Expected 0 Def, got {mgr.Defense}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_24_Core_UpgradeIncreasesHP", () =>
            {
                var go = new GameObject("TestCore_24");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                Assert(mgr.GetMaxHPForLevel(1) == 500, "Level 1 HP mismatch");
                Assert(mgr.GetMaxHPForLevel(2) == 750, "Level 2 HP mismatch");
                Assert(mgr.GetMaxHPForLevel(10) == 3000, "Level 10 HP mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_25_Core_UpgradeIncreasesDef", () =>
            {
                var go = new GameObject("TestCore_25");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                Assert(mgr.GetDefenseForLevel(1) == 0, "Level 1 Def mismatch");
                Assert(mgr.GetDefenseForLevel(2) == 5, "Level 2 Def mismatch");
                Assert(mgr.GetDefenseForLevel(10) == 50, "Level 10 Def mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_26_Core_UpgradeDeductsCurrency", () =>
            {
                var currGo = new GameObject("TestCurr_26");
                var curr = currGo.AddComponent<CurrencyManager>();
                curr.Configure(1000, 200, 0);

                var go = new GameObject("TestCore_26");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                mgr.ResetUpgrade();

                bool upgraded = mgr.TryUpgrade(out string err);
                Assert(upgraded, "Core upgrade should succeed: " + err);
                Assert(mgr.CoreLevel == 2, $"Expected Core Level 2, got {mgr.CoreLevel}");
                Assert(curr.Gold == 800, $"Expected 800 Gold remaining, got {curr.Gold}");
                Assert(curr.Soul == 150, $"Expected 150 Soul remaining, got {curr.Soul}");

                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(currGo);
            });

            Run("Test_27_Core_UpgradeFailsIfInsufficientCurrency", () =>
            {
                var currGo = new GameObject("TestCurr_27");
                var curr = currGo.AddComponent<CurrencyManager>();
                curr.Configure(50, 10, 0); // Insufficient

                var go = new GameObject("TestCore_27");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                mgr.ResetUpgrade();

                bool upgraded = mgr.TryUpgrade(out string err);
                Assert(!upgraded, "Core upgrade must fail with insufficient currency");
                Assert(mgr.CoreLevel == 1, "Core Level should remain 1");

                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(currGo);
            });

            Run("Test_28_Core_LevelCappedAt10", () =>
            {
                var go = new GameObject("TestCore_28");
                var mgr = go.AddComponent<DungeonCoreUpgradeManager>();
                mgr.SetLevel(10);
                Assert(!mgr.CanUpgrade(out string err), "CanUpgrade must return false at level 10");
                Assert(mgr.MaxHP == 3000 && mgr.Defense == 50, "Level 10 stats mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_29_Core_UpgradedStatsReflectInActor", () =>
            {
                var go = new GameObject("TestCore_29");
                var actor = go.AddComponent<DungeonCoreActor>();
                actor.Initialize(1500, 25, 1, null, 0.62f);
                Assert(actor.MaxHP == 1500, $"Expected MaxHP 1500, got {actor.MaxHP}");
                Assert(actor.Defense == 25, $"Expected Defense 25, got {actor.Defense}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_30_Core_BattleDamageDecoupledFromPermanentStats", () =>
            {
                var coreMgrGo = new GameObject("TestCoreMgr_30");
                var coreMgr = coreMgrGo.AddComponent<DungeonCoreUpgradeManager>();
                coreMgr.SetLevel(5);
                int permHP = coreMgr.MaxHP;

                var actorGo = new GameObject("TestCoreActor_30");
                var actor = actorGo.AddComponent<DungeonCoreActor>();
                actor.Initialize(permHP, coreMgr.Defense, 1, null, 0.62f);

                actor.TakeDamage(new DamageInfo(400, null, CombatTeam.Adventurer));
                Assert(actor.CurrentHP == permHP - 400, "Actor current HP should decrease");
                Assert(coreMgr.MaxHP == permHP, "DungeonCoreUpgradeManager MaxHP must NOT be affected by battle damage");

                UnityEngine.Object.DestroyImmediate(actorGo);
                UnityEngine.Object.DestroyImmediate(coreMgrGo);
            });

            // ==========================================
            // GROUP 4: GRID EXPANSION (Tests 31 - 36)
            // ==========================================
            Run("Test_31_Expansion_InitialGridIs9x9", () =>
            {
                var go = new GameObject("TestExp_31");
                var mgr = go.AddComponent<DungeonExpansionManager>();
                mgr.ResetExpansion();
                Assert(mgr.CurrentDimension == 9, $"Expected dimension 9, got {mgr.CurrentDimension}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_32_Expansion_11RequiresLevel3AndCurrency", () =>
            {
                var go = new GameObject("TestExp_32");
                var mgr = go.AddComponent<DungeonExpansionManager>();
                mgr.ResetExpansion();

                bool hasTier = mgr.GetNextExpansionTier(out int nextDim, out int reqLvl, out int goldCost, out int soulCost);
                Assert(hasTier, "Should have next expansion tier");
                Assert(nextDim == 11, $"Expected next dimension 11, got {nextDim}");
                Assert(reqLvl == 3, $"Expected required Level 3, got {reqLvl}");
                Assert(goldCost == 500 && soulCost == 100, $"Expected 500G/100S, got {goldCost}G/{soulCost}S");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_33_Expansion_13RequiresLevel5AndCurrency", () =>
            {
                var go = new GameObject("TestExp_33");
                var mgr = go.AddComponent<DungeonExpansionManager>();
                mgr.SetDimension(11);

                mgr.GetNextExpansionTier(out int nextDim, out int reqLvl, out int goldCost, out int soulCost);
                Assert(nextDim == 13 && reqLvl == 5 && goldCost == 1200 && soulCost == 250, "13x13 expansion specs mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_34_Expansion_15RequiresLevel7AndCurrency", () =>
            {
                var go = new GameObject("TestExp_34");
                var mgr = go.AddComponent<DungeonExpansionManager>();
                mgr.SetDimension(13);

                mgr.GetNextExpansionTier(out int nextDim, out int reqLvl, out int goldCost, out int soulCost);
                Assert(nextDim == 15 && reqLvl == 7 && goldCost == 2500 && soulCost == 600, "15x15 expansion specs mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_35_Expansion_17RequiresLevel9AndCurrency", () =>
            {
                var go = new GameObject("TestExp_35");
                var mgr = go.AddComponent<DungeonExpansionManager>();
                mgr.SetDimension(15);

                mgr.GetNextExpansionTier(out int nextDim, out int reqLvl, out int goldCost, out int soulCost);
                Assert(nextDim == 17 && reqLvl == 9 && goldCost == 5000 && soulCost == 1200, "17x17 expansion specs mismatch");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_36_Expansion_PreservesExistingRoomsAndUnits", () =>
            {
                var go = new GameObject("TestExp_36");
                var rm = go.AddComponent<RoomManager>();
                rm.Initialize();

                int initialRoomCount = rm.Layout.rooms.Count;
                bool resized = rm.SetGridDimensions(11, 11, out string err);
                Assert(resized, "Resize to 11x11 should succeed: " + err);
                Assert(rm.GridWidth == 11 && rm.GridHeight == 11, "Grid dimensions should be 11x11");
                Assert(rm.Layout.rooms.Count == initialRoomCount, "Rooms count should be preserved");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // GROUP 5: FACILITIES (Tests 37 - 41)
            // ==========================================
            Run("Test_37_Facility_GoldBonus", () =>
            {
                var go = new GameObject("TestFac_37");
                var mgr = go.AddComponent<DungeonFacilityManager>();
                mgr.ResetFacilities();
                Assert(mgr.GetGoldMultiplier() == 1.0f, "Lv 1 gold multiplier should be 1.0");

                mgr.SetLevels(2, 1, 1);
                Assert(Mathf.Approximately(mgr.GetGoldMultiplier(), 1.10f), $"Expected 1.10f, got {mgr.GetGoldMultiplier()}");

                mgr.SetLevels(5, 1, 1);
                Assert(Mathf.Approximately(mgr.GetGoldMultiplier(), 1.40f), $"Expected 1.40f, got {mgr.GetGoldMultiplier()}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_38_Facility_SoulBonus", () =>
            {
                var go = new GameObject("TestFac_38");
                var mgr = go.AddComponent<DungeonFacilityManager>();
                mgr.ResetFacilities();
                mgr.SetLevels(1, 3, 1);
                Assert(Mathf.Approximately(mgr.GetSoulMultiplier(), 1.20f), $"Expected 1.20f, got {mgr.GetSoulMultiplier()}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_39_Facility_CapacityBonus", () =>
            {
                var go = new GameObject("TestFac_39");
                var mgr = go.AddComponent<DungeonFacilityManager>();
                mgr.ResetFacilities();
                Assert(mgr.GetBonusMonsterCapacity() == 0, "Lv 1 capacity bonus should be 0");

                mgr.SetLevels(1, 1, 3);
                Assert(mgr.GetBonusMonsterCapacity() == 2, $"Expected +2 bonus capacity at Lv 3, got {mgr.GetBonusMonsterCapacity()}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("Test_40_Facility_UpgradeDeductsCurrency", () =>
            {
                var currGo = new GameObject("TestCurr_40");
                var curr = currGo.AddComponent<CurrencyManager>();
                curr.Configure(1000, 200, 0);

                var go = new GameObject("TestFac_40");
                var mgr = go.AddComponent<DungeonFacilityManager>();
                mgr.ResetFacilities();

                bool upgraded = mgr.TryUpgradeFacility("gold", out string err);
                Assert(upgraded, "Facility upgrade should succeed: " + err);
                Assert(mgr.GoldFacilityLevel == 2, $"Expected Gold Facility Lv 2, got {mgr.GoldFacilityLevel}");
                Assert(curr.Gold == 850, $"Expected 850 Gold remaining, got {curr.Gold}");
                Assert(curr.Soul == 170, $"Expected 170 Soul remaining, got {curr.Soul}");

                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(currGo);
            });

            Run("Test_41_Facility_LevelCappedAtMax", () =>
            {
                var go = new GameObject("TestFac_41");
                var mgr = go.AddComponent<DungeonFacilityManager>();
                mgr.SetLevels(10, 10, 5);

                Assert(!mgr.CanUpgradeFacility("gold", out _), "Gold facility should be capped at 10");
                Assert(!mgr.CanUpgradeFacility("soul", out _), "Soul facility should be capped at 10");
                Assert(!mgr.CanUpgradeFacility("capacity", out _), "Capacity facility should be capped at 5");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // GROUP 6: SAVE / LOAD (Tests 42 - 46)
            // ==========================================
            Run("Test_42_Save_ProgressionPersists", () =>
            {
                var save = new DungeonSaveData();
                save.progression.dungeonLevel = 4;
                save.progression.currentExp = 150;
                save.progression.totalExpEarned = 1200;
                save.progression.claimedLevelRewards.Add(2);
                save.progression.claimedLevelRewards.Add(3);

                string json = JsonUtility.ToJson(save);
                var loaded = JsonUtility.FromJson<DungeonSaveData>(json);

                Assert(loaded.progression.dungeonLevel == 4, "Dungeon level mismatch");
                Assert(loaded.progression.currentExp == 150, "EXP mismatch");
                Assert(loaded.progression.claimedLevelRewards.Count == 2, "Claimed rewards count mismatch");
            });

            Run("Test_43_Save_CoreAndUpgradesPersist", () =>
            {
                var save = new DungeonSaveData();
                save.coreLevel = 6;
                string json = JsonUtility.ToJson(save);
                var loaded = JsonUtility.FromJson<DungeonSaveData>(json);
                Assert(loaded.coreLevel == 6, "Core level mismatch");
            });

            Run("Test_44_Save_GridAndFacilitiesPersist", () =>
            {
                var save = new DungeonSaveData();
                save.gridDimension = 13;
                save.facilityGoldLevel = 3;
                save.facilitySoulLevel = 2;
                save.facilityCapacityLevel = 2;

                string json = JsonUtility.ToJson(save);
                var loaded = JsonUtility.FromJson<DungeonSaveData>(json);

                Assert(loaded.gridDimension == 13, "Grid dimension mismatch");
                Assert(loaded.facilityGoldLevel == 3, "Gold facility level mismatch");
                Assert(loaded.facilitySoulLevel == 2, "Soul facility level mismatch");
                Assert(loaded.facilityCapacityLevel == 2, "Capacity facility level mismatch");
            });

            Run("Test_45_Save_LoadRestoresAllData", () =>
            {
                var save = new DungeonSaveData
                {
                    gold = 3500,
                    soul = 750,
                    coreLevel = 4,
                    gridDimension = 11
                };
                string json = JsonUtility.ToJson(save);
                var loaded = JsonUtility.FromJson<DungeonSaveData>(json);
                Assert(loaded.gold == 3500 && loaded.soul == 750 && loaded.coreLevel == 4 && loaded.gridDimension == 11, "Restored values mismatch");
            });

            Run("Test_46_Save_ResetRestoresDefaults", () =>
            {
                var go = new GameObject("TestSave_46");
                var saveMgr = go.AddComponent<SaveManager>();
                saveMgr.ResetGame();
                Assert(!saveMgr.HasSave(), "Save data should be deleted after reset");
                UnityEngine.Object.DestroyImmediate(go);
            });

            // ==========================================
            // GROUP 7: SHOP PURCHASE & NEW GAME RESET (Test Cases 1 - 12)
            // ==========================================
            Run("TC_01_Shop_PurchaseMonsterRoomWithEnoughGold", () =>
            {
                var go = new GameObject("TC_01");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 1000);
                var inv = go.AddComponent<RoomInventoryManager>();
                inv.Configure(roomDb, rm);
                rm.ConfigureRoomSystems(roomDb, curr, inv);
                var shop = go.AddComponent<RoomShopManager>();
                shop.Configure(roomDb, curr, inv, 1);

                bool ok = shop.TryPurchase("monster_room", out string msg);
                Assert(ok, $"Expected purchase monster_room to succeed: {msg}");
                Assert(inv.GetOwned("monster_room") == 1 && inv.GetAvailable("monster_room") == 1, "Monster Room should be added to inventory");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("TC_02_Shop_PurchaseTrapAndTrapRoomIncreasesInventory", () =>
            {
                var go = new GameObject("TC_02");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 1000);
                var roomInv = go.AddComponent<RoomInventoryManager>();
                roomInv.Configure(roomDb, rm);
                var trapInv = go.AddComponent<TrapInventoryManager>();
                trapInv.Configure(trapDb, rm);
                rm.ConfigureRoomSystems(roomDb, curr, roomInv);
                rm.ConfigureUnitInventories(null, trapInv);
                rm.ConfigureUnitDefinitions(null, trapDb);
                var roomShop = go.AddComponent<RoomShopManager>();
                roomShop.Configure(roomDb, curr, roomInv, 1);
                var trapShop = go.AddComponent<TrapShopManager>();
                trapShop.Configure(trapDb, trapInv, curr, 1);

                Assert(roomShop.TryPurchase("trap_room", out string rMsg), $"Trap Room purchase failed: {rMsg}");
                Assert(roomInv.GetOwned("trap_room") == 1, "Trap Room inventory should increase to 1");
                Assert(trapShop.TryPurchase("spike_trap", out string tMsg), $"Spike Trap purchase failed: {tMsg}");
                Assert(trapInv.GetOwned("spike_trap") == 1, "Spike Trap inventory should increase to 1");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("TC_03_Shop_GoldDeductedCorrectlyAfterPurchase", () =>
            {
                var go = new GameObject("TC_03");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 1000);
                var roomInv = go.AddComponent<RoomInventoryManager>();
                roomInv.Configure(roomDb, rm);
                var monsterInv = go.AddComponent<MonsterInventoryManager>();
                monsterInv.Configure(monsterDb, rm);
                var trapInv = go.AddComponent<TrapInventoryManager>();
                trapInv.Configure(trapDb, rm);
                var roomShop = go.AddComponent<RoomShopManager>();
                roomShop.Configure(roomDb, curr, roomInv, 1);
                var monsterShop = go.AddComponent<MonsterShopManager>();
                monsterShop.Configure(monsterDb, monsterInv, curr, 1);
                var trapShop = go.AddComponent<TrapShopManager>();
                trapShop.Configure(trapDb, trapInv, curr, 1);

                roomShop.TryPurchase("monster_room", out _); // 200G
                Assert(curr.Gold == 800, $"Expected 800 Gold after Monster Room, got {curr.Gold}");
                roomShop.TryPurchase("trap_room", out _); // 250G
                Assert(curr.Gold == 550, $"Expected 550 Gold after Trap Room, got {curr.Gold}");
                monsterShop.TryPurchase("slime", out _); // 100G
                Assert(curr.Gold == 450, $"Expected 450 Gold after Slime, got {curr.Gold}");
                trapShop.TryPurchase("spike_trap", out _); // 100G
                Assert(curr.Gold == 350, $"Expected 350 Gold after Spike Trap, got {curr.Gold}");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("TC_04_Shop_CannotPurchaseWithInsufficientGold", () =>
            {
                var go = new GameObject("TC_04");
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(50, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                var roomInv = go.AddComponent<RoomInventoryManager>();
                roomInv.Configure(roomDb, rm);
                var monsterInv = go.AddComponent<MonsterInventoryManager>();
                monsterInv.Configure(monsterDb, rm);
                var trapInv = go.AddComponent<TrapInventoryManager>();
                trapInv.Configure(trapDb, rm);
                var roomShop = go.AddComponent<RoomShopManager>();
                roomShop.Configure(roomDb, curr, roomInv, 1);
                var monsterShop = go.AddComponent<MonsterShopManager>();
                monsterShop.Configure(monsterDb, monsterInv, curr, 1);
                var trapShop = go.AddComponent<TrapShopManager>();
                trapShop.Configure(trapDb, trapInv, curr, 1);

                Assert(!roomShop.TryPurchase("monster_room", out string rErr) && rErr.Contains("Not Enough Gold"), $"Expected Not Enough Gold error, got {rErr}");
                Assert(!monsterShop.TryPurchase("slime", out string mErr) && mErr.Contains("Not Enough Gold"), $"Expected Not Enough Gold error, got {mErr}");
                Assert(!trapShop.TryPurchase("spike_trap", out string tErr) && tErr.Contains("Not Enough Gold"), $"Expected Not Enough Gold error, got {tErr}");
                Assert(curr.Gold == 50, "Gold must not be deducted when insufficient");
                Assert(roomInv.GetOwned("monster_room") == 0 && monsterInv.GetOwned("slime") == 0 && trapInv.GetOwned("spike_trap") == 0, "Inventory must not change when insufficient gold");
                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("TC_05_06_07_Placement_PlaceCancelAndDeductInventory", () =>
            {
                var go = new GameObject("TC_05_06_07");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(2000, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 2000);
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
                var preview = go.AddComponent<PlacementPreview>();
                preview.Configure(grid);
                var placement = go.AddComponent<RoomPlacement>();
                placement.Configure(grid, rm, preview);
                var mPlace = go.AddComponent<MonsterPlacementManager>();
                mPlace.Configure(grid, rm, monsterDb, monsterInv, placement);
                var tPlace = go.AddComponent<TrapPlacementManager>();
                tPlace.Configure(grid, rm, trapDb, trapInv, placement);

                var mRoomData = roomDb.FindById("monster_room");
                var tRoomData = roomDb.FindById("trap_room");
                var slimeData = monsterDb.FindById("slime");
                var spikeData = trapDb.FindById("spike_trap");

                roomInv.TryAddOwned(mRoomData, 1, out _);
                roomInv.TryAddOwned(tRoomData, 1, out _);
                monsterInv.TryAddOwned(slimeData, 1, out _);
                trapInv.TryAddOwned(spikeData, 1, out _);

                // TC 6: Select and cancel placement -> inventory does not disappear
                placement.SelectRoom(mRoomData);
                placement.CancelPlacement();
                Assert(roomInv.GetAvailable("monster_room") == 1, "Cancelling room placement must not consume inventory");
                mPlace.SelectMonster("slime");
                mPlace.CancelPlacement();
                Assert(monsterInv.GetAvailable("slime") == 1, "Cancelling monster placement must not consume inventory");

                // TC 5 & 7: Place Monster Room & Trap Room on Grid -> Available decreases by 1
                Assert(rm.TryPlace(mRoomData, new Vector2Int(3, 5), 0, out string pErr1), $"Place monster_room failed: {pErr1}");
                Assert(roomInv.GetAvailable("monster_room") == 0 && roomInv.GetPlaced("monster_room") == 1, "Monster Room available should decrease by 1");
                Assert(rm.TryPlace(tRoomData, new Vector2Int(4, 5), 0, out string pErr2), $"Place trap_room failed: {pErr2}");
                Assert(roomInv.GetAvailable("trap_room") == 0 && roomInv.GetPlaced("trap_room") == 1, "Trap Room available should decrease by 1");

                // Place Monster inside Monster Room and Trap inside Trap Room
                Assert(mPlace.SelectMonster("slime"), "Should select slime");
                Assert(grid.TryGetCell(new Vector2Int(3, 5), out DungeonCell mCell), "Cell 3,5 should exist");
                grid.SelectCell(mCell);
                Assert(monsterInv.GetAvailable("slime") == 0 && monsterInv.GetPlaced("slime") == 1, "Slime available should decrease by 1 after placement");

                Assert(tPlace.SelectTrap("spike_trap"), "Should select spike_trap");
                Assert(grid.TryGetCell(new Vector2Int(4, 5), out DungeonCell tCell), "Cell 4,5 should exist");
                grid.SelectCell(tCell);
                Assert(trapInv.GetAvailable("spike_trap") == 0 && trapInv.GetPlaced("spike_trap") == 1, "Spike trap available should decrease by 1 after placement");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Run("TC_08_09_10_11_12_NewGameReset_ClearsAllRunStateAndAllowsStartRaid", () =>
            {
                var go = new GameObject("TC_NewGameReset");
                var grid = go.AddComponent<DungeonGrid>();
                var curr = go.AddComponent<CurrencyManager>();
                curr.Configure(1000, 0, 0);
                var roomDb = AssetDatabase.LoadAssetAtPath<RoomDatabase>("Assets/Data/RoomDatabase.asset");
                var monsterDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
                var trapDb = AssetDatabase.LoadAssetAtPath<TrapDatabase>("Assets/Data/TrapDatabase.asset");
                var rm = go.AddComponent<RoomManager>();
                rm.Configure(grid, null, 9, 9, 1000);
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

                // Simulate dirty state from a previous run
                curr.SetGold(250);
                curr.SetSoul(75);
                var mRoomData = roomDb.FindById("monster_room");
                var slimeData = monsterDb.FindById("slime");
                roomInv.TryAddOwned(mRoomData, 2, out _);
                monsterInv.TryAddOwned(slimeData, 2, out _);
                rm.TryPlace(mRoomData, new Vector2Int(3, 5), 0, out _);
                var placedRoom = rm.GetRoomAt(new Vector2Int(3, 5));
                rm.TryPlaceMonster(new MonsterInstanceData { instanceId = "m_old", monsterId = "slime", roomInstanceId = placedRoom.instanceId, localPosition = Vector2.zero, level = 1 }, 3, out _);
                BattleSession.Begin(rm.LayoutToJson(), "DungeonEditor", 250, 75, 0);

                // Perform New Game Reset multiple times (TC 8, 9, 10, 11)
                for (int i = 0; i < 3; i++)
                {
                    BattleSession.Reset();
                    rm.ResetToInitialState();
                }

                // TC 8: Gold back to initial 1000
                Assert(curr.Gold == 1000 && curr.Soul == 0 && curr.Crystal == 0, $"Expected Gold 1000, Soul 0, Crystal 0 after reset, got {curr.Gold}/{curr.Soul}/{curr.Crystal}");
                // TC 9: Grid cleared back to initial 7 rooms (Entrance, Core, 5 Paths)
                Assert(rm.Layout.rooms.Count == 7, $"Expected 7 initial tiles on grid, got {rm.Layout.rooms.Count}");
                Assert(rm.GetRoomAt(new Vector2Int(3, 5)) == null, "Previously placed Monster Room at (3,5) should be cleared");
                // TC 10: Monsters, Traps, Bosses and BattleSession cleared
                Assert(rm.Layout.monsters.Count == 0 && rm.Layout.traps.Count == 0 && rm.Layout.bosses.Count == 0, "Units from previous run must be cleared");
                Assert(roomInv.GetOwned("monster_room") == 0 && monsterInv.GetOwned("slime") == 0, "Inventories from previous run must be cleared");
                Assert(!BattleSession.IsReady && !BattleSession.ReturnPending, "BattleSession must be reset");
                // TC 12: Start Raid validation passes on fresh reset layout
                Assert(rm.Pathfinder.TryFindRoute(rm.Layout, out var route) && route != null && route.Count >= 2, "Freshly reset dungeon must have a valid raid path from Entrance to Core");

                UnityEngine.Object.DestroyImmediate(go);
            });

            Debug.Log($"========== TEST RESULTS: {passed} / {total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
