using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DreamDungeon.EditorTools
{
    public static class CharacterSpriteIntegrator
    {
        private const string BaseArtPath = "Assets/Art/Characters";
        private const string AnimControllerPath = "Assets/Art/Characters/CharacterAnimator.controller";

        [InitializeOnLoadMethod]
        private static void AutoRunIntegrationOnReload()
        {
            // Disabled automatic overwrite of AnimatorControllers; DreamDungeonAnimationImporter now manages per-character 5-clip controllers.
        }

        [MenuItem("Dream Dungeon/Import & Integrate Character Sprites")]
        public static void ExecuteIntegration()
        {
            Debug.Log("========== [CharacterSpriteIntegrator] Starting Character Sprite Import & Integration ==========");

            // 1. Configure TextureImporter for all character PNGs
            AssetDatabase.Refresh();
            int importedSprites = ConfigureAllCharacterTextures();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Ensure base AnimatorController with Idle, Walk, Attack, and Death states
            AnimatorController animCtrl = EnsureCharacterAnimatorController();

            // 3. Ensure base default ScriptableObjects exist via Milestone1SceneBuilder
            Milestone1SceneBuilder.BuildMilestone6();

            // 4. Integrate Adventurers (4 existing + 6 new = 10 total, covering all 8 requested types)
            int advCount = IntegrateAdventurers(animCtrl);

            // 5. Integrate Dungeon Monsters (6 existing + 18 new = 24 total, covering all 20 requested types)
            int monCount = IntegrateMonsters(animCtrl);

            // 6. Integrate Boss Monsters (3 existing + 4 new = 7 total, covering all 5 requested types)
            int bossCount = IntegrateBosses(animCtrl);

            // 7. Sync MonsterDatabase to Resources/DreamDungeon/MonsterDatabase.asset
            SyncMonsterDatabaseToResources();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"========== [CharacterSpriteIntegrator] Completed! Sprites={importedSprites}, Adventurers={advCount}, Monsters={monCount}, Bosses={bossCount} ==========");
        }

        private static int ConfigureAllCharacterTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BaseArtPath });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isBoss = path.Contains("/Bosses/");
                bool isFlying = path.Contains("/Flying/") || path.EndsWith("Beholder.png", StringComparison.OrdinalIgnoreCase) || path.EndsWith("Ghost.png", StringComparison.OrdinalIgnoreCase);

                // Boss textures are ~320-346px tall, regular characters are ~140-210px tall.
                // With cellSize=0.62, BattleManager scales monsters/adventurers by (cellSize * 0.45) = 0.279,
                // and bosses by (cellSize * 0.7) = 0.434.
                // Setting PPU=100 for standard characters and PPU=150 for bosses makes standard units ~0.48 world units (~77% of a cell)
                // and bosses ~0.95 world units (~1.53 cells inside a 2x2 Boss Room), visually prominent and proportional!
                float targetPpu = isBoss ? 150f : 100f;
                Vector2 targetPivot = isFlying ? new Vector2(0.5f, 0.35f) : new Vector2(0.5f, 0.15f);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritePixelsPerUnit = targetPpu;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = targetPivot;
                settings.spritePixelsPerUnit = targetPpu;
                settings.filterMode = FilterMode.Point;
                settings.alphaIsTransparency = true;
                settings.mipmapEnabled = false;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);

                TextureImporterPlatformSettings defaultPlatform = importer.GetDefaultPlatformTextureSettings();
                defaultPlatform.textureCompression = TextureImporterCompression.Uncompressed;
                defaultPlatform.format = TextureImporterFormat.RGBA32;
                defaultPlatform.maxTextureSize = 2048;
                importer.SetPlatformTextureSettings(defaultPlatform);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        private static AnimatorController EnsureCharacterAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimControllerPath);
            }

            // Ensure parameters: IsMoving (Bool), Attack (Trigger), Die (Trigger)
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = FindOrCreateState(sm, "Idle");
            AnimatorState walkState = FindOrCreateState(sm, "Walk");
            AnimatorState attackState = FindOrCreateState(sm, "Attack");
            AnimatorState deathState = FindOrCreateState(sm, "Death");

            sm.defaultState = idleState;

            if (idleState.transitions.Length == 0)
            {
                var toWalk = idleState.AddTransition(walkState);
                toWalk.hasExitTime = false;
                toWalk.duration = 0f;
                toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

                var toAttack = idleState.AddTransition(attackState);
                toAttack.hasExitTime = false;
                toAttack.duration = 0f;
                toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            }

            if (walkState.transitions.Length == 0)
            {
                var toIdle = walkState.AddTransition(idleState);
                toIdle.hasExitTime = false;
                toIdle.duration = 0f;
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

                var toAttack = walkState.AddTransition(attackState);
                toAttack.hasExitTime = false;
                toAttack.duration = 0f;
                toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            }

            if (attackState.transitions.Length == 0)
            {
                var backToIdle = attackState.AddTransition(idleState);
                backToIdle.hasExitTime = true;
                backToIdle.exitTime = 1f;
                backToIdle.duration = 0f;
            }

            if (sm.anyStateTransitions.Length == 0)
            {
                var toDeath = sm.AddAnyStateTransition(deathState);
                toDeath.hasExitTime = false;
                toDeath.duration = 0f;
                toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name)
        {
            foreach (var child in sm.states)
            {
                if (child.state != null && child.state.name == name) return child.state;
            }
            return sm.AddState(name);
        }

        private static int IntegrateAdventurers(AnimatorController animCtrl)
        {
            EnsureFolder("Assets/Prefabs", "Adventurers");

            var adventurerMap = new (string id, string spriteRelPath, string prefabName)[]
            {
                // Existing 4 core Adventurer IDs (used in waves & tests)
                ("swordsman", "Adventurers/Footman.png", "Swordsman"),
                ("archer", "Adventurers/Archer.png", "Archer"),
                ("mage", "Adventurers/Mage.png", "Mage"),
                ("knight", "Adventurers/EliteKnight.png", "Knight"),

                // 6 Additional Adventurer IDs from prompt list
                ("bandit", "Adventurers/Bandit.png", "Bandit"),
                ("footman", "Adventurers/Footman.png", "Footman"),
                ("elite_knight", "Adventurers/EliteKnight.png", "EliteKnight"),
                ("dark_archer", "Adventurers/DarkArcher.png", "DarkArcher"),
                ("paladin", "Adventurers/Paladin.png", "Paladin"),
                ("necromancer", "Adventurers/Necromancer.png", "Necromancer")
            };

            AdventurerDatabase db = AssetDatabase.LoadAssetAtPath<AdventurerDatabase>("Assets/Resources/DreamDungeon/AdventurerDatabase.asset");
            var entries = new List<AdventurerData>();
            int count = 0;

            foreach (var item in adventurerMap)
            {
                string dataPath = $"Assets/Resources/DreamDungeon/{item.id}.asset";
                AdventurerData data = AssetDatabase.LoadAssetAtPath<AdventurerData>(dataPath);
                if (data == null) continue;

                Sprite sprite = LoadSprite($"{BaseArtPath}/{item.spriteRelPath}");
                if (sprite == null)
                {
                    Debug.LogError($"Missing Adventurer sprite at {BaseArtPath}/{item.spriteRelPath}");
                    continue;
                }

                string prefabPath = $"Assets/Prefabs/Adventurers/{item.prefabName}.prefab";
                GameObject prefab = CreateOrUpdateCharacterPrefab(prefabPath, item.prefabName, sprite, animCtrl, sortingOrder: 6, isAdventurer: true, isBoss: false);

                data.icon = sprite;
                data.prefab = prefab;
                EditorUtility.SetDirty(data);
                entries.Add(data);
                count++;
            }

            if (db != null)
            {
                db.SetEntries(entries);
                EditorUtility.SetDirty(db);
            }

            return count;
        }

        private static int IntegrateMonsters(AnimatorController animCtrl)
        {
            EnsureFolder("Assets/Prefabs", "Monsters");

            var monsterMap = new (string id, string spriteRelPath, string prefabName)[]
            {
                // Existing 6 Monster IDs (used in tests & starter roster)
                ("slime", "Special/Slime.png", "Slime"),
                ("goblin", "Special/Goblin.png", "Goblin"),
                ("skeleton_archer", "Undead/Skeleton.png", "SkeletonArcher"),
                ("dark_mage", "Magic/DarkSorcerer.png", "DarkMage"),
                ("orc", "Demons/Demon.png", "Orc"),
                ("baby_dragon", "Flying/Gargoyle.png", "BabyDragon"),

                // 18 Additional Dungeon Monster IDs from prompt list (total 20 distinct prompt monsters + 4 existing aliases)
                ("skeleton", "Undead/Skeleton.png", "Skeleton"),
                ("zombie", "Undead/Zombie.png", "Zombie"),
                ("ghost", "Undead/Ghost.png", "Ghost"),
                ("imp", "Demons/Imp.png", "Imp"),
                ("demon", "Demons/Demon.png", "Demon"),
                ("wolf", "Beasts/Wolf.png", "Wolf"),
                ("spider", "Beasts/Spider.png", "Spider"),
                ("golem", "Elite/Golem.png", "Golem"),
                ("mimic", "Special/Mimic.png", "Mimic"),
                ("bat", "Flying/Bat.png", "Bat"),
                ("harpy", "Flying/Harpy.png", "Harpy"),
                ("gargoyle", "Flying/Gargoyle.png", "Gargoyle"),
                ("crystal_golem", "Elite/CrystalGolem.png", "CrystalGolem"),
                ("fire_elemental", "Magic/FireElemental.png", "FireElemental"),
                ("shadow_creature", "Magic/ShadowCreature.png", "ShadowCreature"),
                ("ice_elemental", "Magic/IceElemental.png", "IceElemental"),
                ("treant", "Elite/Treant.png", "Treant"),
                ("beholder", "Magic/Beholder.png", "Beholder")
            };

            MonsterDatabase db = AssetDatabase.LoadAssetAtPath<MonsterDatabase>("Assets/Data/MonsterDatabase.asset");
            var entries = new List<MonsterData>();
            int count = 0;

            foreach (var item in monsterMap)
            {
                string dataPath = $"Assets/Data/Monsters/{item.id}.asset";
                MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(dataPath);
                if (data == null) continue;

                Sprite sprite = LoadSprite($"{BaseArtPath}/{item.spriteRelPath}");
                if (sprite == null)
                {
                    Debug.LogError($"Missing Monster sprite at {BaseArtPath}/{item.spriteRelPath}");
                    continue;
                }

                string prefabPath = $"Assets/Prefabs/Monsters/{item.prefabName}.prefab";
                GameObject prefab = CreateOrUpdateCharacterPrefab(prefabPath, item.prefabName, sprite, animCtrl, sortingOrder: 5, isAdventurer: false, isBoss: false);

                data.icon = sprite;
                data.monsterPrefab = prefab;
                EditorUtility.SetDirty(data);
                entries.Add(data);
                count++;
            }

            if (db != null)
            {
                db.SetEntries(entries);
                EditorUtility.SetDirty(db);
            }

            return count;
        }

        private static int IntegrateBosses(AnimatorController animCtrl)
        {
            EnsureFolder("Assets/Prefabs", "Bosses");

            var bossMap = new (string id, string spriteRelPath, string prefabName, string desc)[]
            {
                // Existing 3 Boss IDs
                ("cave_dragon", "Bosses/Dragon.png", "CaveDragon", "A fearsome fire-breathing dragon residing in deep caverns."),
                ("dark_golem", "Bosses/AncientGuardian.png", "DarkGolem", "A colossal rune-carved stone guardian awakened from slumber."),
                ("demon_lord", "Bosses/DemonLord.png", "DemonLord", "The supreme lord of the abyssal legion wielding a blazing hellblade."),

                // 4 Additional Boss IDs from prompt list
                ("dragon", "Bosses/Dragon.png", "Dragon", "An ancient red dragon whose wings scorch the dungeon halls."),
                ("ice_queen", "Bosses/IceQueen.png", "IceQueen", "Sovereign of eternal frost who freezes invaders in their tracks."),
                ("ancient_guardian", "Bosses/AncientGuardian.png", "AncientGuardian", "An indestructible monolith infused with primordial arcane energy."),
                ("lich_king", "Bosses/LichKing.png", "LichKing", "Undying monarch of necromancy commanding soulfire and bone.")
            };

            BossDatabase db = AssetDatabase.LoadAssetAtPath<BossDatabase>("Assets/Resources/DreamDungeon/BossDatabase.asset");
            var entries = new List<BossData>();
            int count = 0;

            foreach (var item in bossMap)
            {
                string dataPath = $"Assets/Resources/DreamDungeon/{item.id}.asset";
                BossData data = AssetDatabase.LoadAssetAtPath<BossData>(dataPath);
                if (data == null) continue;

                Sprite sprite = LoadSprite($"{BaseArtPath}/{item.spriteRelPath}");
                if (sprite == null)
                {
                    Debug.LogError($"Missing Boss sprite at {BaseArtPath}/{item.spriteRelPath}");
                    continue;
                }

                string prefabPath = $"Assets/Prefabs/Bosses/{item.prefabName}.prefab";
                GameObject prefab = CreateOrUpdateCharacterPrefab(prefabPath, item.prefabName, sprite, animCtrl, sortingOrder: 6, isAdventurer: false, isBoss: true);

                data.bossIcon = sprite;
                data.bossPrefab = prefab;
                data.description = item.desc;
                EditorUtility.SetDirty(data);
                entries.Add(data);
                count++;
            }

            if (db != null)
            {
                db.bosses = entries;
                EditorUtility.SetDirty(db);
            }

            return count;
        }

        private static GameObject CreateOrUpdateCharacterPrefab(string prefabPath, string name, Sprite sprite, AnimatorController animCtrl, int sortingOrder, bool isAdventurer, bool isBoss)
        {
            GameObject temp = new GameObject(name);

            // 1. SpriteRenderer
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = sortingOrder;

            // 2. Animator (with Idle/Walk/Attack/Death state structure ready)
            Animator anim = temp.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;

            // 3. Collider2D & Rigidbody2D (Kinematic, trigger so it never interferes with custom grid movement)
            CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = isBoss ? 0.55f : 0.35f;
            col.offset = new Vector2(0f, isBoss ? 0.45f : 0.3f);

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.gravityScale = 0f;

            // 4. Game Actor / Combat & AI Components (no duplicates)
            if (isAdventurer)
            {
                temp.AddComponent<AdventurerMovement>();
                temp.AddComponent<AdventurerAttack>();
                temp.AddComponent<AdventurerActor>();
            }
            else if (isBoss)
            {
                temp.AddComponent<BossPhaseController>();
                temp.AddComponent<BossSkillController>();
                temp.AddComponent<BossBattleActor>();
            }
            else
            {
                temp.AddComponent<MonsterBattleActor>();
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            UnityEngine.Object.DestroyImmediate(temp);
            return savedPrefab;
        }

        private static void SyncMonsterDatabaseToResources()
        {
            const string src = "Assets/Data/MonsterDatabase.asset";
            const string dst = "Assets/Resources/DreamDungeon/MonsterDatabase.asset";
            MonsterDatabase srcDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>(src);
            MonsterDatabase dstDb = AssetDatabase.LoadAssetAtPath<MonsterDatabase>(dst);
            if (srcDb != null && dstDb != null)
            {
                dstDb.SetEntries(new List<MonsterData>(srcDb.AllMonsters));
                EditorUtility.SetDirty(dstDb);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object obj in all)
            {
                if (obj is Sprite sp) return sp;
            }
            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string[] parts = parent.Split('/');
                string cur = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(cur + "/" + parts[i]))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur += "/" + parts[i];
                }
            }
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
