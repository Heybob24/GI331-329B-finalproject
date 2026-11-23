using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamDungeon.EditorTools
{
    public sealed class DreamDungeonAnimationImporter : EditorWindow
    {
        private const string SourcePackDir = "Assets/DREAM_DUNGEON_Animations_All";
        private const string ManifestRelPath = "Assets/DREAM_DUNGEON_Animations_All/manifest.json";

        private const string TargetRoot = "Assets/DREAM_DUNGEON";
        private const string CharsRoot = "Assets/DREAM_DUNGEON/Characters";
        private const string SpritesRoot = "Assets/DREAM_DUNGEON/Characters/Sprites";
        private const string AnimsRoot = "Assets/DREAM_DUNGEON/Characters/Animations";
        private const string CtrlsRoot = "Assets/DREAM_DUNGEON/Characters/AnimatorControllers";
        private const string PrefabsRoot = "Assets/DREAM_DUNGEON/Characters/Prefabs";
        private const string DataRoot = "Assets/DREAM_DUNGEON/Characters/Data";
        private const string PreviewsRoot = "Assets/DREAM_DUNGEON/Previews";
        private const string TestScenePath = "Assets/Scenes/DREAM_DUNGEON_AnimationTest.unity";

        private string lastReport = "Click 'Run Full Animation Import & Integration' to validate and import all 40 characters (1,280 frames, 200 clips, 40 controllers, 40 prefabs).";
        private Vector2 scrollPos;

#pragma warning disable CS0649
        [Serializable]
        private sealed class ManifestRoot
        {
            public string project;
            public int total_characters;
            public int frames_per_character;
            public int total_frames;
            public List<ManifestCharacterEntry> characters;
        }

        [Serializable]
        private sealed class ManifestCharacterEntry
        {
            public string id;
            public string source_file;
            public string move_type;
            public string attack_type;
            public int[] cell_size;
            public string sprite_sheet;
            public string metadata;
            public string preview_gif;
            public string frames_dir;
        }
#pragma warning restore CS0649

        private readonly struct CharacterMapSpec
        {
            public readonly string CharacterId;
            public readonly string DisplayName;
            public readonly CharacterCategory Category;
            public readonly string PrimaryGameplayId;
            public readonly string[] AllGameplayIds;
            public readonly string[] ExistingPrefabNames;

            public CharacterMapSpec(string charId, string displayName, CharacterCategory category, string primaryGameplayId, string[] allGameplayIds, string[] existingPrefabNames)
            {
                CharacterId = charId;
                DisplayName = displayName;
                Category = category;
                PrimaryGameplayId = primaryGameplayId;
                AllGameplayIds = allGameplayIds;
                ExistingPrefabNames = existingPrefabNames;
            }
        }

        private static readonly CharacterMapSpec[] AuthoritativeMap =
        {
            new CharacterMapSpec("sprite_01_blue_knight", "Blue Knight (Elite Knight)", CharacterCategory.Adventurer, "elite_knight", new[] { "elite_knight", "knight" }, new[] { "EliteKnight", "Knight" }),
            new CharacterMapSpec("sprite_02_dark_mage", "Dark Mage (Necromancer)", CharacterCategory.Adventurer, "necromancer", new[] { "necromancer" }, new[] { "Necromancer" }),
            new CharacterMapSpec("sprite_03_green_ranger", "Green Ranger (Archer)", CharacterCategory.Adventurer, "archer", new[] { "archer" }, new[] { "Archer" }),
            new CharacterMapSpec("sprite_04_skeleton_warrior", "Skeleton Warrior", CharacterCategory.Monster, "skeleton", new[] { "skeleton", "skeleton_archer" }, new[] { "Skeleton", "SkeletonArcher" }),
            new CharacterMapSpec("sprite_05_red_imp", "Red Imp", CharacterCategory.Monster, "imp", new[] { "imp" }, new[] { "Imp" }),
            new CharacterMapSpec("sprite_06_horned_berserker", "Horned Berserker (Demon)", CharacterCategory.Monster, "demon", new[] { "demon", "orc" }, new[] { "Demon", "Orc" }),
            new CharacterMapSpec("sprite_07_rogue_adventurer", "Rogue Adventurer (Bandit)", CharacterCategory.Adventurer, "bandit", new[] { "bandit" }, new[] { "Bandit" }),
            new CharacterMapSpec("sprite_08_zombie", "Zombie", CharacterCategory.Monster, "zombie", new[] { "zombie" }, new[] { "Zombie" }),
            new CharacterMapSpec("sprite_09_blue_ghost", "Blue Ghost", CharacterCategory.Monster, "ghost", new[] { "ghost" }, new[] { "Ghost" }),
            new CharacterMapSpec("sprite_10a_dire_wolf", "Dire Wolf", CharacterCategory.Monster, "wolf", new[] { "wolf" }, new[] { "Wolf" }),
            new CharacterMapSpec("sprite_10b_goblin", "Goblin", CharacterCategory.Monster, "goblin", new[] { "goblin" }, new[] { "Goblin" }),
            new CharacterMapSpec("sprite_11_crimson_spider", "Crimson Spider", CharacterCategory.Monster, "spider", new[] { "spider" }, new[] { "Spider" }),
            new CharacterMapSpec("sprite_12_dark_archer_knight", "Dark Archer Knight (Footman)", CharacterCategory.Adventurer, "footman", new[] { "footman", "swordsman" }, new[] { "Footman", "Swordsman" }),
            new CharacterMapSpec("sprite_13_holy_paladin", "Holy Paladin", CharacterCategory.Adventurer, "paladin", new[] { "paladin" }, new[] { "Paladin" }),
            new CharacterMapSpec("sprite_14_stone_golem", "Stone Golem", CharacterCategory.Monster, "golem", new[] { "golem" }, new[] { "Golem" }),
            new CharacterMapSpec("sprite_15_harpy", "Harpy", CharacterCategory.Monster, "harpy", new[] { "harpy" }, new[] { "Harpy" }),
            new CharacterMapSpec("sprite_16_shadow_ranger", "Shadow Ranger (Dark Archer)", CharacterCategory.Adventurer, "dark_archer", new[] { "dark_archer" }, new[] { "DarkArcher" }),
            new CharacterMapSpec("sprite_17_gargoyle", "Gargoyle", CharacterCategory.Monster, "gargoyle", new[] { "gargoyle", "baby_dragon" }, new[] { "Gargoyle", "BabyDragon" }),
            new CharacterMapSpec("sprite_18_skull_cultist", "Skull Cultist (Skeleton Mage)", CharacterCategory.Monster, "skull_cultist", new[] { "skull_cultist" }, new[] { "SkullCultist" }),
            new CharacterMapSpec("sprite_19_purple_sorceress", "Purple Sorceress (Mage)", CharacterCategory.Adventurer, "mage", new[] { "mage" }, new[] { "Mage" }),
            new CharacterMapSpec("sprite_20_crystal_golem", "Crystal Golem", CharacterCategory.Monster, "crystal_golem", new[] { "crystal_golem" }, new[] { "CrystalGolem" }),
            new CharacterMapSpec("sprite_21_shadow_bat", "Shadow Bat", CharacterCategory.Monster, "bat", new[] { "bat" }, new[] { "Bat" }),
            new CharacterMapSpec("sprite_22_mimic_chest", "Mimic Chest", CharacterCategory.Monster, "mimic", new[] { "mimic" }, new[] { "Mimic" }),
            new CharacterMapSpec("sprite_23_green_slime", "Green Slime", CharacterCategory.Monster, "slime", new[] { "slime" }, new[] { "Slime" }),
            new CharacterMapSpec("sprite_24_boss_demon_lord", "Boss Demon Lord", CharacterCategory.Boss, "demon_lord", new[] { "demon_lord" }, new[] { "DemonLord" }),
            new CharacterMapSpec("sprite_25_boss_lich_king", "Boss Lich King", CharacterCategory.Boss, "lich_king", new[] { "lich_king" }, new[] { "LichKing" }),
            new CharacterMapSpec("sprite_26_boss_red_dragon", "Boss Red Dragon", CharacterCategory.Boss, "dragon", new[] { "dragon", "cave_dragon" }, new[] { "Dragon", "CaveDragon" }),
            new CharacterMapSpec("sprite_27_boss_ice_queen", "Boss Ice Queen", CharacterCategory.Boss, "ice_queen", new[] { "ice_queen" }, new[] { "IceQueen" }),
            new CharacterMapSpec("sprite_28_boss_rune_golem", "Boss Rune Golem (Ancient Guardian)", CharacterCategory.Boss, "ancient_guardian", new[] { "ancient_guardian", "dark_golem" }, new[] { "AncientGuardian", "DarkGolem" }),
            new CharacterMapSpec("sprite_29_forest_treant", "Forest Treant", CharacterCategory.Monster, "treant", new[] { "treant" }, new[] { "Treant" }),
            new CharacterMapSpec("sprite_30_void_warlock", "Void Warlock (Dark Sorcerer)", CharacterCategory.Monster, "dark_mage", new[] { "dark_mage" }, new[] { "DarkMage" }),
            new CharacterMapSpec("sprite_31_fire_elemental", "Fire Elemental", CharacterCategory.Monster, "fire_elemental", new[] { "fire_elemental" }, new[] { "FireElemental" }),
            new CharacterMapSpec("sprite_32_shadow_wraith", "Shadow Wraith (Shadow Creature)", CharacterCategory.Monster, "shadow_creature", new[] { "shadow_creature" }, new[] { "ShadowCreature" }),
            new CharacterMapSpec("sprite_33_frost_golem", "Frost Golem (Ice Elemental)", CharacterCategory.Monster, "ice_elemental", new[] { "ice_elemental" }, new[] { "IceElemental" }),
            new CharacterMapSpec("sprite_34_crimson_specter", "Crimson Specter", CharacterCategory.Monster, "crimson_specter", new[] { "crimson_specter" }, new[] { "CrimsonSpecter" }),
            new CharacterMapSpec("sprite_35_eye_beholder", "Eye Beholder", CharacterCategory.Monster, "beholder", new[] { "beholder" }, new[] { "Beholder" }),
            new CharacterMapSpec("sprite_36_amethyst_crystal", "Amethyst Crystal", CharacterCategory.Monster, "amethyst_crystal", new[] { "amethyst_crystal" }, new[] { "AmethystCrystal" }),
            new CharacterMapSpec("sprite_37_shadow_spider", "Shadow Spider", CharacterCategory.Monster, "shadow_spider", new[] { "shadow_spider" }, new[] { "ShadowSpider" }),
            new CharacterMapSpec("sprite_38a_venom_spider", "Venom Spider", CharacterCategory.Monster, "venom_spider", new[] { "venom_spider" }, new[] { "VenomSpider" }),
            new CharacterMapSpec("sprite_38b_frost_spider", "Frost Spider", CharacterCategory.Monster, "frost_spider", new[] { "frost_spider" }, new[] { "FrostSpider" })
        };

        [MenuItem("Dream Dungeon/DREAM DUNGEON Animation Importer")]
        public static void ShowWindow()
        {
            var win = GetWindow<DreamDungeonAnimationImporter>("DREAM DUNGEON Animation Importer");
            win.minSize = new Vector2(680, 520);
        }

        [MenuItem("Dream Dungeon/Run Full Character Animation Integration")]
        public static void RunFullImportMenu()
        {
            string report = ExecuteFullImport(buildTestScene: true);
            Debug.Log(report);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("DREAM DUNGEON Animation Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Imports all 40 characters (1,280 frames), creates 200 Animation Clips, 40 Animator Controllers, 40 Prefabs, connects existing gameplay databases, and builds DREAM_DUNGEON_AnimationTest.unity.", MessageType.Info);
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Run Full Animation Import & Integration", GUILayout.Height(36)))
            {
                lastReport = ExecuteFullImport(buildTestScene: true);
                Debug.Log(lastReport);
            }

            if (GUILayout.Button("Validate Imported Animation Assets Only", GUILayout.Height(28)))
            {
                lastReport = ValidateAllAssets();
                Debug.Log(lastReport);
            }

            if (GUILayout.Button("Open Animation Test Scene", GUILayout.Height(28)))
            {
                if (!File.Exists(TestScenePath))
                {
                    BuildAnimationTestScene();
                }
                EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Importer Report:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(lastReport, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        public static string ExecuteFullImport(bool buildTestScene = true)
        {
            string manifestPath = File.Exists(DataRoot + "/manifest.json") ? DataRoot + "/manifest.json" : ManifestRelPath;
            if (!File.Exists(manifestPath))
            {
                return $"[DREAM DUNGEON Animation Importer] ERROR: manifest.json not found at {ManifestRelPath}";
            }

            string jsonText = File.ReadAllText(manifestPath);
            ManifestRoot manifest = JsonUtility.FromJson<ManifestRoot>(jsonText);
            if (manifest == null || manifest.characters == null || manifest.characters.Count == 0)
            {
                return "[DREAM DUNGEON Animation Importer] ERROR: Failed to parse manifest.json.";
            }

            EnsureFolderStructure();

            var mapDict = new Dictionary<string, CharacterMapSpec>(StringComparer.OrdinalIgnoreCase);
            foreach (var spec in AuthoritativeMap)
            {
                mapDict[spec.CharacterId] = spec;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int duplicateIds = 0;
            int totalChars = 0;
            int totalFrames = 0;
            int missingFrames = 0;
            int totalClips = 0;
            int totalControllers = 0;
            int totalPrefabs = 0;
            int connectedGameplay = 0;

            var animSpecs = new (string folderTitle, string subDir, int count, float fps, bool loop)[]
            {
                ("Idle", "idle", 6, 8f, true),
                ("Walk", "walk", 8, 10f, true),
                ("Attack", "attack", 8, 12f, false),
                ("Hit", "hit", 4, 12f, false),
                ("Death", "death", 6, 8f, false)
            };

            var dbEntries = new List<CharacterAnimationEntry>();

            foreach (ManifestCharacterEntry ch in manifest.characters)
            {
                if (ch == null || string.IsNullOrWhiteSpace(ch.id)) continue;
                if (!seenIds.Add(ch.id))
                {
                    duplicateIds++;
                    Debug.LogWarning($"[DreamDungeonAnimationImporter] Duplicate character ID detected: {ch.id}");
                    continue;
                }

                if (!mapDict.TryGetValue(ch.id, out CharacterMapSpec mapSpec))
                {
                    Debug.LogWarning($"[DreamDungeonAnimationImporter] Unmapped character ID: {ch.id}");
                    continue;
                }

                bool isBoss = mapSpec.Category == CharacterCategory.Boss;
                bool isFloating = string.Equals(ch.move_type, "floating", StringComparison.OrdinalIgnoreCase);
                float targetPpu = isBoss ? 150f : 100f;
                Vector2 targetPivot = isFloating ? new Vector2(0.5f, 0.35f) : new Vector2(0.5f, 0.15f);

                var clipsByType = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
                Sprite idleFirstSprite = null;

                foreach (var aSpec in animSpecs)
                {
                    var frameSprites = new List<Sprite>(aSpec.count);
                    for (int f = 1; f <= aSpec.count; f++)
                    {
                        string frameFileName = $"{ch.id}_{aSpec.subDir}_{f:00}.png";
                        string dstFramePath = $"{SpritesRoot}/Frames/{ch.id}/{aSpec.subDir}/{frameFileName}";
                        string srcFramePath = $"{SourcePackDir}/frames/{ch.id}/{aSpec.subDir}/{frameFileName}";

                        if (!File.Exists(dstFramePath) && File.Exists(srcFramePath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(dstFramePath) ?? string.Empty);
                            File.Copy(srcFramePath, dstFramePath, overwrite: true);
                            AssetDatabase.ImportAsset(dstFramePath, ImportAssetOptions.ForceSynchronousImport);
                        }

                        string activePath = File.Exists(dstFramePath) ? dstFramePath : srcFramePath;
                        if (!File.Exists(activePath))
                        {
                            missingFrames++;
                            Debug.LogError($"[DreamDungeonAnimationImporter] Missing frame PNG: {activePath}");
                            continue;
                        }

                        ConfigureSingleSpriteImporter(activePath, targetPpu, targetPivot);
                        Sprite sp = LoadSingleSprite(activePath);
                        if (sp == null)
                        {
                            missingFrames++;
                            Debug.LogError($"[DreamDungeonAnimationImporter] Could not load Sprite from: {activePath}");
                            continue;
                        }

                        frameSprites.Add(sp);
                        totalFrames++;

                        if (aSpec.subDir == "idle" && f == 1)
                        {
                            idleFirstSprite = sp;
                        }
                    }

                    string clipPath = $"{AnimsRoot}/{aSpec.folderTitle}/{ch.id}_{aSpec.folderTitle}.anim";
                    AnimationClip clip = CreateOrUpdateSpriteClip(clipPath, $"{ch.id}_{aSpec.folderTitle}", frameSprites, aSpec.fps, aSpec.loop);
                    if (clip != null)
                    {
                        clipsByType[aSpec.folderTitle] = clip;
                        totalClips++;
                    }
                }

                string ctrlPath = $"{CtrlsRoot}/{ch.id}.controller";
                AnimatorController controller = CreateOrUpdateController(ctrlPath, clipsByType);
                if (controller != null)
                {
                    totalControllers++;
                }

                string prefabPath = $"{PrefabsRoot}/{ch.id}.prefab";
                GameObject charPrefab = CreateOrUpdateCharacterPrefab(prefabPath, ch.id, idleFirstSprite, controller, mapSpec.Category);
                if (charPrefab != null)
                {
                    totalPrefabs++;
                }

                if (ConnectCharacterToGameplay(mapSpec, idleFirstSprite, controller))
                {
                    connectedGameplay++;
                }

                dbEntries.Add(new CharacterAnimationEntry
                {
                    characterId = ch.id,
                    displayName = mapSpec.DisplayName,
                    characterType = mapSpec.Category,
                    movementType = ch.move_type,
                    attackType = ch.attack_type,
                    gameplayEntityId = mapSpec.PrimaryGameplayId,
                    idleSprite = idleFirstSprite,
                    animatorController = controller,
                    prefab = charPrefab
                });

                totalChars++;
            }

            SaveCharacterAnimationDatabase(dbEntries);

            if (buildTestScene)
            {
                BuildAnimationTestScene();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report =
                "========== DREAM DUNGEON ANIMATION IMPORTER REPORT ==========\n" +
                $"Characters Imported:           {totalChars} / 40\n" +
                $"Frame PNG Files Validated:     {totalFrames} / 1,280\n" +
                $"Animation Clips Created:       {totalClips} / 200\n" +
                $"Animator Controllers Created:  {totalControllers} / 40\n" +
                $"Character Prefabs Created:     {totalPrefabs} / 40\n" +
                $"Characters Connected to Game:  {connectedGameplay} / 40\n" +
                $"Missing or Invalid Frames:     {missingFrames}\n" +
                $"Duplicate Character IDs:       {duplicateIds}\n" +
                $"Animation Test Scene:          {TestScenePath}\n" +
                "=============================================================";

            File.WriteAllText($"{DataRoot}/AnimationImportReport.txt", report);
            return report;
        }

        public static string ValidateAllAssets()
        {
            int charFolders = Directory.Exists($"{SpritesRoot}/Frames")
                ? Directory.GetDirectories($"{SpritesRoot}/Frames").Length
                : 0;
            int frameFiles = Directory.Exists($"{SpritesRoot}/Frames")
                ? Directory.GetFiles($"{SpritesRoot}/Frames", "*.png", SearchOption.AllDirectories).Length
                : 0;
            int animClips = Directory.Exists(AnimsRoot)
                ? Directory.GetFiles(AnimsRoot, "*.anim", SearchOption.AllDirectories).Length
                : 0;
            int controllers = Directory.Exists(CtrlsRoot)
                ? Directory.GetFiles(CtrlsRoot, "*.controller", SearchOption.TopDirectoryOnly).Length
                : 0;
            int prefabs = Directory.Exists(PrefabsRoot)
                ? Directory.GetFiles(PrefabsRoot, "*.prefab", SearchOption.TopDirectoryOnly).Length
                : 0;

            return
                "========== DREAM DUNGEON ANIMATION VALIDATION ==========\n" +
                $"Character Frame Folders: {charFolders} (Expected: 40)\n" +
                $"Frame PNG Files:         {frameFiles} (Expected: 1280)\n" +
                $"Animation Clips (.anim): {animClips} (Expected: 200)\n" +
                $"Animator Controllers:    {controllers} (Expected: 40)\n" +
                $"Character Prefabs:       {prefabs} (Expected: 40)\n" +
                $"Test Scene Exists:       {File.Exists(TestScenePath)}\n" +
                "========================================================";
        }

        private static void ConfigureSingleSpriteImporter(string assetPath, float ppu, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool needsReimport =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.filterMode != FilterMode.Point ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                !importer.alphaIsTransparency ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f;

            if (!needsReimport) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit = ppu;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = ppu;
            settings.filterMode = FilterMode.Point;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(platform);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string clipPath, string clipName, List<Sprite> frames, float fps, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            bool isNew = false;
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                isNew = true;
            }

            clip.frameRate = fps;

            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                keyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = frames.Count / fps;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew)
            {
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        private static AnimatorController CreateOrUpdateController(string ctrlPath, Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            }

            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = FindOrCreateState(sm, "Idle");
            AnimatorState walkState = FindOrCreateState(sm, "Walk");
            AnimatorState attackState = FindOrCreateState(sm, "Attack");
            AnimatorState hitState = FindOrCreateState(sm, "Hit");
            AnimatorState deathState = FindOrCreateState(sm, "Death");

            sm.defaultState = idleState;

            if (clips.TryGetValue("Idle", out var idleClip)) idleState.motion = idleClip;
            if (clips.TryGetValue("Walk", out var walkClip)) walkState.motion = walkClip;
            if (clips.TryGetValue("Attack", out var atkClip)) attackState.motion = atkClip;
            if (clips.TryGetValue("Hit", out var hitClip)) hitState.motion = hitClip;
            if (clips.TryGetValue("Death", out var dieClip)) deathState.motion = dieClip;

            if (idleState.transitions.Length == 0)
            {
                var toWalk = idleState.AddTransition(walkState);
                toWalk.hasExitTime = false;
                toWalk.duration = 0f;
                toWalk.canTransitionToSelf = false;
                toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            }

            if (walkState.transitions.Length == 0)
            {
                var toIdle = walkState.AddTransition(idleState);
                toIdle.hasExitTime = false;
                toIdle.duration = 0f;
                toIdle.canTransitionToSelf = false;
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            }

            if (attackState.transitions.Length == 0)
            {
                var toIdle = attackState.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 1f;
                toIdle.duration = 0f;
                toIdle.canTransitionToSelf = false;
            }

            if (hitState.transitions.Length == 0)
            {
                var toIdle = hitState.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 1f;
                toIdle.duration = 0f;
                toIdle.canTransitionToSelf = false;
            }

            // Death must not transition back to Idle or Walk
            deathState.transitions = Array.Empty<AnimatorStateTransition>();

            if (sm.anyStateTransitions.Length == 0)
            {
                var anyToDeath = sm.AddAnyStateTransition(deathState);
                anyToDeath.hasExitTime = false;
                anyToDeath.duration = 0f;
                anyToDeath.canTransitionToSelf = false;
                anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");

                var anyToAttack = sm.AddAnyStateTransition(attackState);
                anyToAttack.hasExitTime = false;
                anyToAttack.duration = 0f;
                anyToAttack.canTransitionToSelf = false;
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

                var anyToHit = sm.AddAnyStateTransition(hitState);
                anyToHit.hasExitTime = false;
                anyToHit.duration = 0f;
                anyToHit.canTransitionToSelf = false;
                anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateOrUpdateCharacterPrefab(string prefabPath, string name, Sprite idleSprite, AnimatorController controller, CharacterCategory category)
        {
            bool isBoss = category == CharacterCategory.Boss;
            bool isAdv = category == CharacterCategory.Adventurer;
            int sortingOrder = (isBoss || isAdv) ? 6 : 5;

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                SpriteRenderer sr = existing.GetComponent<SpriteRenderer>();
                if (sr != null && idleSprite != null) sr.sprite = idleSprite;
                Animator anim = existing.GetComponent<Animator>();
                if (anim != null && controller != null) anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            GameObject temp = new GameObject(name);
            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = idleSprite;
            renderer.color = Color.white;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;

            Animator animator = temp.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = isBoss ? 0.55f : 0.35f;
            col.offset = new Vector2(0f, isBoss ? 0.45f : 0.3f);

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.gravityScale = 0f;

            if (isAdv)
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

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            DestroyImmediate(temp);
            return saved;
        }

        private static bool ConnectCharacterToGameplay(CharacterMapSpec spec, Sprite idleSprite, AnimatorController controller)
        {
            if (spec.Category == CharacterCategory.Adventurer)
            {
                foreach (string pfName in spec.ExistingPrefabNames)
                {
                    CreateOrUpdateCharacterPrefab($"Assets/Prefabs/Adventurers/{pfName}.prefab", pfName, idleSprite, controller, CharacterCategory.Adventurer);
                }
                foreach (string gpId in spec.AllGameplayIds)
                {
                    AdventurerData data = AssetDatabase.LoadAssetAtPath<AdventurerData>($"Assets/Resources/DreamDungeon/{gpId}.asset");
                    if (data != null && idleSprite != null)
                    {
                        data.icon = idleSprite;
                        EditorUtility.SetDirty(data);
                    }
                }
                return true;
            }

            if (spec.Category == CharacterCategory.Boss)
            {
                foreach (string pfName in spec.ExistingPrefabNames)
                {
                    CreateOrUpdateCharacterPrefab($"Assets/Prefabs/Bosses/{pfName}.prefab", pfName, idleSprite, controller, CharacterCategory.Boss);
                }
                foreach (string gpId in spec.AllGameplayIds)
                {
                    BossData data = AssetDatabase.LoadAssetAtPath<BossData>($"Assets/Resources/DreamDungeon/{gpId}.asset");
                    if (data != null && idleSprite != null)
                    {
                        data.bossIcon = idleSprite;
                        EditorUtility.SetDirty(data);
                    }
                }
                return true;
            }

            foreach (string pfName in spec.ExistingPrefabNames)
            {
                CreateOrUpdateCharacterPrefab($"Assets/Prefabs/Monsters/{pfName}.prefab", pfName, idleSprite, controller, CharacterCategory.Monster);
            }
            foreach (string gpId in spec.AllGameplayIds)
            {
                MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/Data/Monsters/{gpId}.asset");
                if (data != null && idleSprite != null)
                {
                    data.icon = idleSprite;
                    EditorUtility.SetDirty(data);
                }
            }
            return true;
        }

        private static void SaveCharacterAnimationDatabase(List<CharacterAnimationEntry> entries)
        {
            string[] paths =
            {
                $"{DataRoot}/CharacterAnimationDatabase.asset",
                "Assets/Resources/DreamDungeon/CharacterAnimationDatabase.asset"
            };

            foreach (string path in paths)
            {
                CharacterAnimationDatabase db = AssetDatabase.LoadAssetAtPath<CharacterAnimationDatabase>(path);
                if (db == null)
                {
                    db = CreateInstance<CharacterAnimationDatabase>();
                    AssetDatabase.CreateAsset(db, path);
                }
                db.SetEntries(new List<CharacterAnimationEntry>(entries));
                EditorUtility.SetDirty(db);
            }
        }

        public static void BuildAnimationTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            Camera cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.11f, 1f);
            camObj.transform.position = new Vector3(0f, 0.2f, -10f);

            GameObject spawnRoot = new GameObject("PreviewSpawnRoot");
            spawnRoot.transform.position = new Vector3(0f, -0.35f, 0f);

            GameObject ctrlObj = new GameObject("AnimationTestSceneController");
            ctrlObj.AddComponent<AnimationTestSceneController>();

            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var inputMod = es.GetComponent<InputSystemUIInputModule>();
            if (inputMod != null) inputMod.AssignDefaultActions();

            EditorSceneManager.SaveScene(scene, TestScenePath);
        }

        private static Sprite LoadSingleSprite(string path)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Sprite s) return s;
            }
            return null;
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

        private static void EnsureFolderStructure()
        {
            string[] dirs =
            {
                TargetRoot,
                CharsRoot,
                SpritesRoot,
                $"{SpritesRoot}/Frames",
                $"{SpritesRoot}/SpriteSheets",
                AnimsRoot,
                $"{AnimsRoot}/Idle",
                $"{AnimsRoot}/Walk",
                $"{AnimsRoot}/Attack",
                $"{AnimsRoot}/Hit",
                $"{AnimsRoot}/Death",
                CtrlsRoot,
                PrefabsRoot,
                DataRoot,
                "Assets/DREAM_DUNGEON/Editor",
                PreviewsRoot
            };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }
    }
}
