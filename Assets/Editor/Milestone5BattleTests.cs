using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class Milestone5BattleTests
    {
        [MenuItem("Dream Dungeon/Run Milestone 5 Tests")]
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

            Debug.Log("========== STARTING MILESTONE 5 TEST SUITE ==========");

            // 1. Damage Calculator Tests
            Run("DamageCalculator_NormalDamage", () =>
            {
                int dmg = DamageCalculator.Calculate(20, 10);
                Assert(dmg == 10, $"Expected 10, got {dmg}");
            });

            Run("DamageCalculator_MinDamageClamp", () =>
            {
                int dmg = DamageCalculator.Calculate(5, 25);
                Assert(dmg == 1, $"Expected min 1, got {dmg}");
            });

            Run("DamageCalculator_Multiplier", () =>
            {
                int dmg = DamageCalculator.Calculate(30, 10, 1.5f);
                Assert(dmg == 30, $"Expected 30, got {dmg}");
            });

            // 2. Adventurer Data Verification
            Run("AdventurerData_StarterAdventurers", () =>
            {
                AdventurerDatabase db = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                Assert(db != null, "AdventurerDatabase not found in Resources/DreamDungeon");
                Assert(db.Validate(out string err), "AdventurerDatabase validation failed: " + err);

                var swordsman = db.Find("swordsman");
                Assert(swordsman != null && swordsman.maxHP == 150 && swordsman.attack == 20 && swordsman.defense == 10 && swordsman.attackType == AdventurerAttackType.Melee, "Swordsman stats incorrect");

                var archer = db.Find("archer");
                Assert(archer != null && archer.maxHP == 90 && archer.attack == 25 && archer.defense == 3 && archer.attackType == AdventurerAttackType.Ranged, "Archer stats incorrect");

                var mage = db.Find("mage");
                Assert(mage != null && mage.maxHP == 80 && mage.attack == 35 && mage.defense == 2 && mage.attackType == AdventurerAttackType.Magic, "Mage stats incorrect");

                var knight = db.Find("knight");
                Assert(knight != null && knight.maxHP == 250 && knight.attack == 15 && knight.defense == 25 && knight.attackType == AdventurerAttackType.Melee, "Knight stats incorrect");
            });

            // 3. Battle Configuration & Wave Verification
            Run("BattleConfiguration_WavesAndCore", () =>
            {
                AdventurerDatabase adventurers = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                BattleConfiguration config = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
                Assert(config != null, "BattleConfiguration not found in Resources/DreamDungeon");
                Assert(config.Validate(adventurers, out string err), "BattleConfiguration validation failed: " + err);
                Assert(config.waves.Count == 5, $"Expected 5 waves, got {config.waves.Count}");
                Assert(config.coreMaxHP == 500, $"Expected coreMaxHP 500, got {config.coreMaxHP}");

                // Wave 1: Swordsman x 2
                Assert(config.waves[0].entries.Count == 1 && config.waves[0].entries[0].adventurerId == "swordsman" && config.waves[0].entries[0].count == 2, "Wave 1 incorrect");
                // Wave 2: Swordsman x 2, Archer x 1
                Assert(config.waves[1].entries.Count == 2 && config.waves[1].entries[0].count == 2 && config.waves[1].entries[1].count == 1, "Wave 2 incorrect");
                // Wave 3: Swordsman x 2, Archer x 1, Mage x 1
                Assert(config.waves[2].entries.Count == 3, "Wave 3 incorrect");
                // Wave 4: Knight x 1, Swordsman x 2, Archer x 2
                Assert(config.waves[3].entries.Count == 3, "Wave 4 incorrect");
                // Wave 5: Knight x 1, Swordsman x 2, Archer x 2, Mage x 2
                Assert(config.waves[4].entries.Count == 4, "Wave 5 incorrect");
            });

            // 4. Battle Reward Calculator Tests
            Run("RewardCalculator_Victory", () =>
            {
                BattleConfiguration config = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
                var result = BattleRewardCalculator.Calculate(true, 5, 20, config);
                Assert(result.Won, "Result should be Won");
                Assert(result.Gold == 20 * 10 + 100, $"Expected 300 gold, got {result.Gold}");
                Assert(result.Soul == 20 * 2 + 20, $"Expected 60 soul, got {result.Soul}");
                Assert(result.Exp == 50, $"Expected 50 exp, got {result.Exp}");
            });

            Run("RewardCalculator_Defeat", () =>
            {
                BattleConfiguration config = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
                var result = BattleRewardCalculator.Calculate(false, 3, 7, config);
                Assert(!result.Won, "Result should not be Won");
                Assert(result.Gold == 7 * 5, $"Expected 35 gold, got {result.Gold}");
                Assert(result.Soul == 7 * 1, $"Expected 7 soul, got {result.Soul}");
                Assert(result.Exp == 0, $"Expected 0 exp, got {result.Exp}");
            });

            // 5. Dungeon Core HP & Damage Tests
            Run("DungeonCoreActor_DamageAndDefeat", () =>
            {
                GameObject coreObj = new GameObject("TestCore");
                var core = coreObj.AddComponent<DungeonCoreActor>();
                core.Initialize(500, 1, 1, null, 0.62f);
                Assert(core.CurrentHP == 500, $"Expected 500 HP, got {core.CurrentHP}");

                bool destroyed = false;
                core.CoreDestroyed += () => destroyed = true;

                core.TakeDamage(new DamageInfo(100, null, CombatTeam.Adventurer));
                Assert(core.CurrentHP == 400, $"Expected 400 HP, got {core.CurrentHP}");
                Assert(!destroyed, "Core should not be destroyed yet");

                core.TakeDamage(new DamageInfo(450, null, CombatTeam.Adventurer));
                Assert(core.CurrentHP == 0, $"Expected 0 HP, got {core.CurrentHP}");
                Assert(destroyed, "Core should be destroyed at 0 HP");

                UnityEngine.Object.DestroyImmediate(coreObj);
            });

            // 6. Adventurer Movement & Slow Clamp Tests
            Run("AdventurerMovement_PathAndSlow", () =>
            {
                GameObject advObj = new GameObject("TestAdventurer");
                var movement = advObj.AddComponent<AdventurerMovement>();
                var path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
                movement.Configure(path, cell => new Vector3(cell.x, cell.y, 0), 1f, Vector3.zero);

                Assert(!movement.HasReachedEnd, "Should not reach end at start");
                Assert(movement.EffectiveSpeed == 1f, $"Expected 1f speed, got {movement.EffectiveSpeed}");

                // Apply slow
                movement.ApplySlow(0.9f, 2f); // 90% slow
                Assert(movement.EffectiveSpeed >= 0.25f, $"Speed should be clamped >= 0.25f, got {movement.EffectiveSpeed}");

                UnityEngine.Object.DestroyImmediate(advObj);
            });

            // 7. Monster Defender Combat Tests
            Run("MonsterBattleActor_StatsAndDamage", () =>
            {
                MonsterDatabase monsterDb = Resources.Load<MonsterDatabase>("DreamDungeon/MonsterDatabase");
                Assert(monsterDb != null, "MonsterDatabase not found");
                MonsterData goblin = monsterDb.FindById("goblin");
                Assert(goblin != null, "Goblin data not found");

                GameObject monsterObj = new GameObject("TestMonster");
                var actor = monsterObj.AddComponent<MonsterBattleActor>();
                actor.Initialize(goblin, 1, "m1", null);

                Assert(actor.IsAlive, "Monster should be alive");
                Assert(actor.CurrentHP == goblin.MaxHPAtLevel(1), "Monster HP mismatch");

                actor.TakeDamage(new DamageInfo(25, null, CombatTeam.Adventurer));
                Assert(actor.CurrentHP == goblin.MaxHPAtLevel(1) - 25, "Monster damage mismatch");

                UnityEngine.Object.DestroyImmediate(monsterObj);
            });

            // 8. BattleSession Single-Claim & Persistence Tests
            Run("BattleSession_RewardIdempotency", () =>
            {
                BattleSession.Begin("{\"test\":1}", "DungeonEditor", 500, 10, 0);
                Assert(BattleSession.IsReady, "Session should be ready");

                BattleSession.SetResult(true, 5, 15, 250, 50, 50);
                Assert(BattleSession.Won, "Session should record win");
                Assert(BattleSession.RewardGold == 250, "Gold mismatch");

                // Duplicate SetResult should be ignored
                BattleSession.SetResult(true, 5, 20, 999, 999, 999);
                Assert(BattleSession.RewardGold == 250, "Duplicate SetResult should be ignored");
            });

            // 9. Camera Resolution & Full Dungeon View Framing Tests
            Run("BattleCamera_FullDungeonFraming", () =>
            {
                GameObject camObj = new GameObject("TestCam", typeof(Camera));
                Camera cam = camObj.GetComponent<Camera>();
                BattleCameraController camCtrl = camObj.AddComponent<BattleCameraController>();
                camCtrl.Configure(9, 9, 0.62f, new Vector2(0f, 0.45f));

                Assert(camCtrl.CurrentMode == BattleCameraMode.FullDungeonView, "Default mode must be FullDungeonView");
                Assert(cam.orthographic, "Camera must be orthographic");
                Assert(cam.orthographicSize >= 4.5f, $"Camera size {cam.orthographicSize} must be >= 4.5");

                UnityEngine.Object.DestroyImmediate(camObj);
            });

            // 10. Battle Dungeon Renderer & Unit Position Parity Tests
            Run("BattleDungeonRenderer_UnitPositionParity", () =>
            {
                GameObject renObj = new GameObject("TestRenderer");
                BattleDungeonRenderer renderer = renObj.AddComponent<BattleDungeonRenderer>();

                var room = new DungeonRoomInstanceData
                {
                    instanceId = "r1",
                    roomId = "monster_room",
                    roomType = DungeonRoomType.MonsterRoom,
                    position = new Vector2Int(3, 4),
                    width = 1,
                    height = 1,
                    rotation = 0
                };

                Vector2 localPos = new Vector2(0.28f, -0.28f);
                Vector3 world = renderer.GetUnitWorldPosition(room, localPos, -0.15f);

                Assert(world.z == -0.15f, "Z depth should be -0.15f");
                Assert(world.x != 0f && world.y != 0f, "Coordinates must not be zero");

                UnityEngine.Object.DestroyImmediate(renObj);
            });

            Debug.Log($"========== TEST RESULTS: {passed}/{total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
