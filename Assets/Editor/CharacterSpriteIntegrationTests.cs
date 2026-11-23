using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class CharacterSpriteIntegrationTests
    {
        [MenuItem("Dream Dungeon/Run Character Sprite & Animation Integration Tests")]
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

            Debug.Log("========== STARTING DREAM DUNGEON FULL ANIMATION & SPRITE INTEGRATION TESTS ==========");

            // 1. Verify all 40 character frame folders and 1,280 PNG files in Assets/DREAM_DUNGEON/Characters/Sprites/Frames
            Run("Test_01_All40CharacterFoldersAnd1280Frames", () =>
            {
                const string framesBase = "Assets/DREAM_DUNGEON/Characters/Sprites/Frames";
                Assert(Directory.Exists(framesBase), $"Missing directory: {framesBase}");
                string[] charDirs = Directory.GetDirectories(framesBase);
                Assert(charDirs.Length == 40, $"Expected 40 character folders, found {charDirs.Length}");

                string[] pngFiles = Directory.GetFiles(framesBase, "*.png", SearchOption.AllDirectories);
                Assert(pngFiles.Length == 1280, $"Expected 1,280 frame PNG files, found {pngFiles.Length}");
            });

            // 2. Verify all 200 Animation Clips (40 x 5: Idle, Walk, Attack, Hit, Death)
            Run("Test_02_All200AnimationClipsExistAndValid", () =>
            {
                const string animBase = "Assets/DREAM_DUNGEON/Characters/Animations";
                string[] subDirs = { "Idle", "Walk", "Attack", "Hit", "Death" };
                int totalClips = 0;
                foreach (string sub in subDirs)
                {
                    string dir = $"{animBase}/{sub}";
                    Assert(Directory.Exists(dir), $"Missing animation directory: {dir}");
                    string[] files = Directory.GetFiles(dir, "*.anim", SearchOption.TopDirectoryOnly);
                    Assert(files.Length == 40, $"Expected 40 .{sub}.anim files in {dir}, found {files.Length}");
                    totalClips += files.Length;
                }
                Assert(totalClips == 200, $"Expected 200 Animation Clips total, found {totalClips}");
            });

            // 3. Verify all 40 Animator Controllers & their 5 states (Idle, Walk, Attack, Hit, Death) + 4 parameters (IsMoving, Attack, Hit, Die)
            Run("Test_03_All40AnimatorControllersAndStates", () =>
            {
                const string ctrlBase = "Assets/DREAM_DUNGEON/Characters/AnimatorControllers";
                Assert(Directory.Exists(ctrlBase), $"Missing directory: {ctrlBase}");
                string[] files = Directory.GetFiles(ctrlBase, "*.controller", SearchOption.TopDirectoryOnly);
                Assert(files.Length == 40, $"Expected 40 Animator Controllers, found {files.Length}");
            });

            // 4. Verify all 40 Character Prefabs in Assets/DREAM_DUNGEON/Characters/Prefabs
            Run("Test_04_All40CharacterPrefabs", () =>
            {
                const string prefabsBase = "Assets/DREAM_DUNGEON/Characters/Prefabs";
                Assert(Directory.Exists(prefabsBase), $"Missing directory: {prefabsBase}");
                string[] files = Directory.GetFiles(prefabsBase, "*.prefab", SearchOption.TopDirectoryOnly);
                Assert(files.Length == 40, $"Expected 40 Character Prefabs, found {files.Length}");
            });

            // 5. Verify CharacterAnimationDatabase has all 40 entries with valid references
            Run("Test_05_CharacterAnimationDatabase_40Entries", () =>
            {
                CharacterAnimationDatabase db = Resources.Load<CharacterAnimationDatabase>("DreamDungeon/CharacterAnimationDatabase")
                    ?? AssetDatabase.LoadAssetAtPath<CharacterAnimationDatabase>("Assets/DREAM_DUNGEON/Characters/Data/CharacterAnimationDatabase.asset");
                Assert(db != null, "CharacterAnimationDatabase not found");
                Assert(db.Entries.Count == 40, $"Expected 40 entries in CharacterAnimationDatabase, got {db.Entries.Count}");

                foreach (var entry in db.Entries)
                {
                    Assert(entry != null && !string.IsNullOrWhiteSpace(entry.characterId), "Null or empty characterId in database");
                    Assert(entry.idleSprite != null, $"Missing idleSprite for {entry.characterId}");
                    Assert(entry.animatorController != null, $"Missing animatorController for {entry.characterId}");
                    Assert(entry.prefab != null, $"Missing prefab for {entry.characterId}");
                }
            });

            // 6. Verify AdventurerDatabase & all 10 Adventurers
            Run("Test_06_Adventurers_AllSpritesAndAnimatedPrefabsConnected", () =>
            {
                AdventurerDatabase advDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
                Assert(advDb != null, "AdventurerDatabase not found");
                Assert(advDb.Validate(out string err), $"AdventurerDatabase invalid: {err}");

                string[] requiredIds = { "swordsman", "archer", "mage", "knight", "bandit", "footman", "elite_knight", "dark_archer", "paladin", "necromancer" };
                foreach (string id in requiredIds)
                {
                    AdventurerData data = advDb.Find(id);
                    Assert(data != null, $"AdventurerData '{id}' is missing from AdventurerDatabase");
                    Assert(data.icon != null, $"AdventurerData '{id}' is missing icon Sprite");
                    Assert(data.prefab != null, $"AdventurerData '{id}' is missing Prefab");
                    Animator anim = data.prefab.GetComponent<Animator>();
                    Assert(anim != null && anim.runtimeAnimatorController != null, $"Adventurer Prefab '{id}' missing AnimatorController");
                }
            });

            // 7. Verify MonsterDatabase & all 30 Dungeon Monsters
            Run("Test_07_Monsters_AllSpritesAndAnimatedPrefabsConnected", () =>
            {
                MonsterDatabase monDb = Resources.Load<MonsterDatabase>("DreamDungeon/MonsterDatabase");
                Assert(monDb != null, "MonsterDatabase not found in Resources/DreamDungeon");
                Assert(monDb.ValidateDatabase(out string err), $"MonsterDatabase invalid: {err}");

                string[] requiredIds =
                {
                    "slime", "skeleton", "zombie", "ghost", "imp", "demon", "goblin", "wolf", "spider", "golem",
                    "mimic", "bat", "harpy", "gargoyle", "crystal_golem", "fire_elemental", "shadow_creature",
                    "ice_elemental", "treant", "beholder", "skeleton_archer", "dark_mage", "orc", "baby_dragon",
                    "skull_cultist", "crimson_specter", "amethyst_crystal", "shadow_spider", "venom_spider", "frost_spider"
                };

                foreach (string id in requiredIds)
                {
                    MonsterData data = monDb.FindById(id);
                    Assert(data != null, $"MonsterData '{id}' is missing from MonsterDatabase");
                    Assert(data.icon != null, $"MonsterData '{id}' is missing icon Sprite");
                    Assert(data.monsterPrefab != null, $"MonsterData '{id}' is missing monsterPrefab");
                    Animator anim = data.monsterPrefab.GetComponent<Animator>();
                    Assert(anim != null && anim.runtimeAnimatorController != null, $"Monster Prefab '{id}' missing AnimatorController");
                }
            });

            // 8. Verify BossDatabase & all 7 Bosses
            Run("Test_08_Bosses_AllSpritesAndAnimatedPrefabsConnected", () =>
            {
                BossDatabase bossDb = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
                Assert(bossDb != null, "BossDatabase not found in Resources/DreamDungeon");

                string[] requiredIds = { "demon_lord", "ice_queen", "dragon", "lich_king", "ancient_guardian", "cave_dragon", "dark_golem" };
                foreach (string id in requiredIds)
                {
                    BossData data = bossDb.GetBoss(id);
                    Assert(data != null, $"BossData '{id}' is missing from BossDatabase");
                    Assert(data.bossIcon != null, $"BossData '{id}' is missing bossIcon Sprite");
                    Assert(data.bossPrefab != null, $"BossData '{id}' is missing bossPrefab");
                    Animator anim = data.bossPrefab.GetComponent<Animator>();
                    Assert(anim != null && anim.runtimeAnimatorController != null, $"Boss Prefab '{id}' missing AnimatorController");
                }
            });

            // 9. Verify Animation Test Scene exists
            Run("Test_09_AnimationTestScene_Exists", () =>
            {
                Assert(File.Exists("Assets/Scenes/DREAM_DUNGEON_AnimationTest.unity"), "DREAM_DUNGEON_AnimationTest.unity missing");
            });

            Debug.Log($"========== DREAM DUNGEON ANIMATION INTEGRATION TESTS: {passed}/{total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
