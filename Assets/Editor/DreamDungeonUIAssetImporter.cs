using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon.Editor
{
    public static class DreamDungeonUIAssetImporter
    {
        private const string UiAssetsRoot = "Assets/Art/UI/DREAM_DUNGEON_UI_Assets";
        private const string DatabaseAssetPath = "Assets/Resources/DreamDungeon/DreamDungeonUIAssetDatabase.asset";
        private const string PrefabsFolder = "Assets/Prefabs/UI";

        [MenuItem("Dream Dungeon/UI Redesign/1. Import, Slice UI Assets & Build Database")]
        public static void ImportSliceAndBuildDatabase()
        {
            if (!Directory.Exists(UiAssetsRoot))
            {
                Debug.LogError($"[DreamDungeonUIAssetImporter] UI Assets folder not found at '{UiAssetsRoot}'.");
                return;
            }

            ConfigureSingleSprites();
            ConfigureAndSliceIconSets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DreamDungeonUIAssetDatabase db = BuildOrUpdateDatabase();
            if (db != null)
            {
                BuildReusableUIPrefabs(db);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[DreamDungeonUIAssetImporter] Successfully imported, sliced all UI assets, populated DreamDungeonUIAssetDatabase, and generated all 7 UI Prefabs!");
            }
        }

        [MenuItem("Dream Dungeon/UI Redesign/2. Generate Reusable UI Prefabs Only")]
        public static void GeneratePrefabsOnly()
        {
            DreamDungeonUIAssetDatabase db = AssetDatabase.LoadAssetAtPath<DreamDungeonUIAssetDatabase>(DatabaseAssetPath);
            if (db == null)
            {
                db = BuildOrUpdateDatabase();
            }
            if (db == null)
            {
                Debug.LogError("[DreamDungeonUIAssetImporter] DreamDungeonUIAssetDatabase not found. Run Step 1 first.");
                return;
            }
            BuildReusableUIPrefabs(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Dream Dungeon/UI Redesign/4. Generate Global Theme Asset & Common Prefabs")]
        public static void GenerateGlobalThemeAndCommonPrefabs()
        {
            DreamDungeonUIAssetDatabase db = AssetDatabase.LoadAssetAtPath<DreamDungeonUIAssetDatabase>(DatabaseAssetPath) ?? BuildOrUpdateDatabase();
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/DreamDungeon");

            string themePath = "Assets/Resources/DreamDungeon/DreamDungeonUITheme.asset";
            DreamDungeonUITheme theme = AssetDatabase.LoadAssetAtPath<DreamDungeonUITheme>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<DreamDungeonUITheme>();
                AssetDatabase.CreateAsset(theme, themePath);
            }

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            string commonDir = "Assets/Prefabs/UI/Common";
            EnsureFolder(commonDir);

            CreateSimpleThemedPrefab($"{commonDir}/PF_CommonButton.prefab", "PF_CommonButton", new Vector2(280f, 76f), db?.panelEmptyFrame, theme.secondaryButton, "BUTTON", true);
            CreateSimpleThemedPrefab($"{commonDir}/PF_PrimaryButton.prefab", "PF_PrimaryButton", new Vector2(360f, 84f), db?.panelEmptyFrame, theme.primaryButton, "CONFIRM", true);
            CreateSimpleThemedPrefab($"{commonDir}/PF_SecondaryButton.prefab", "PF_SecondaryButton", new Vector2(320f, 76f), db?.panelEmptyFrame, theme.secondaryButton, "OPTION", true);
            CreateSimpleThemedPrefab($"{commonDir}/PF_BackButton.prefab", "PF_BackButton", new Vector2(210f, 76f), db?.buttonBack, Color.white, "", true, preserveAspect: true);
            CreateSimpleThemedPrefab($"{commonDir}/PF_TopBar.prefab", "PF_TopBar", new Vector2(1080f, 108f), null, theme.mainBackground, "", false);
            CreateSimpleThemedPrefab($"{commonDir}/PF_GothicPanel.prefab", "PF_GothicPanel", new Vector2(880f, 960f), db?.panelEmptyFrame, Color.white, "", false);
            CreateSimpleThemedPrefab($"{commonDir}/PF_SectionHeader.prefab", "PF_SectionHeader", new Vector2(760f, 54f), null, theme.secondaryBackground, "SECTION TITLE", false);
            CreateSimpleThemedPrefab($"{commonDir}/PF_NotificationPopup.prefab", "PF_NotificationPopup", new Vector2(640f, 220f), db?.panelEmptyFrame, Color.white, "Notification", false);
            CreateSimpleThemedPrefab($"{commonDir}/PF_LoadingPopup.prefab", "PF_LoadingPopup", new Vector2(560f, 240f), db?.panelEmptyFrame, Color.white, "Connecting to Server...", false);
            CreateSimpleThemedPrefab($"{commonDir}/PF_TabButton.prefab", "PF_TabButton", new Vector2(180f, 54f), db?.panelEmptyFrame, theme.secondaryButton, "TAB", true);
            CreateSimpleThemedPrefab($"{commonDir}/PF_AccountBadge.prefab", "PF_AccountBadge", new Vector2(420f, 56f), null, theme.secondaryBackground, "[GUEST] Dungeon Master", false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DreamDungeonUIAssetImporter] Generated DreamDungeonUITheme.asset and 11 Global Common UI Prefabs under Assets/Prefabs/UI/Common/!");
        }

        private static void CreateSimpleThemedPrefab(string path, string name, Vector2 size, Sprite sprite, Color color, string label, bool addButton, bool preserveAspect = false)
        {
            GameObject root = NewUIRoot(name, size);
            Image img = root.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                if (preserveAspect) img.preserveAspect = true;
                else img.type = Image.Type.Sliced;
            }
            img.color = color;
            if (addButton)
            {
                Button btn = root.AddComponent<Button>();
                btn.targetGraphic = img;
            }
            if (!string.IsNullOrEmpty(label))
            {
                AddTmpLabel(root, "Label", label, 22, Color.white);
            }
            SaveAndDestroyPrefab(root, path);
        }

        private static void ConfigureSingleSprites()
        {
            string[] singleFolders =
            {
                "Logo", "ModeCards", "Buttons", "Currency", "Panels", "Decorations", "Badges", "Popups"
            };

            foreach (string sub in singleFolders)
            {
                string dir = $"{UiAssetsRoot}/{sub}";
                if (!Directory.Exists(dir)) continue;

                string[] files = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    bool isEmptyFrame = assetPath.EndsWith("Panel_EmptyFrame.png", StringComparison.OrdinalIgnoreCase);

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.spritePixelsPerUnit = 100f;
                    importer.npotScale = TextureImporterNPOTScale.None;

                    if (isEmptyFrame)
                    {
                        // 9-slice border preserving crown & gothic corners on 415x480 Panel_EmptyFrame
                        importer.spriteBorder = new Vector4(42f, 48f, 42f, 68f);
                    }

                    importer.SaveAndReimport();
                }
            }

            // Original sheet kept as reference only (Default texture, not used in UI)
            string origPath = $"{UiAssetsRoot}/Original/00_Original_Asset_Sheet.png";
            TextureImporter origImporter = AssetImporter.GetAtPath(origPath) as TextureImporter;
            if (origImporter != null)
            {
                origImporter.textureType = TextureImporterType.Default;
                origImporter.mipmapEnabled = false;
                origImporter.SaveAndReimport();
            }
        }

        private static void ConfigureAndSliceIconSets()
        {
            // 1. Icon_RoomTiles_Set.png (305 x 80) — 4 horizontal icons
            SliceTexture($"{UiAssetsRoot}/Icons/Icon_RoomTiles_Set.png", new[]
            {
                MakeRect("RoomTile_Stone",    8,   6, 68, 68),
                MakeRect("RoomTile_Wood",     84,  6, 68, 68),
                MakeRect("RoomTile_Brick",    158, 6, 68, 68),
                MakeRect("RoomTile_Obsidian", 232, 6, 68, 68)
            });

            // 2. Icon_Monsters_Set.png (305 x 80) — 4 horizontal icons
            SliceTexture($"{UiAssetsRoot}/Icons/Icon_Monsters_Set.png", new[]
            {
                MakeRect("Icon_SpikeTrap", 8,   6, 68, 68),
                MakeRect("Icon_Goblin",    84,  6, 68, 68),
                MakeRect("Icon_Slime",     158, 6, 68, 68),
                MakeRect("Icon_Spider",    232, 6, 68, 68)
            });

            // 3. GemIcons_Set.png (230 x 70) — 5 horizontal gems
            SliceTexture($"{UiAssetsRoot}/Icons/GemIcons_Set.png", new[]
            {
                MakeRect("Gem_Purple", 2,   12, 42, 54),
                MakeRect("Gem_Blue",   48,  12, 42, 54),
                MakeRect("Gem_Green",  94,  12, 42, 54),
                MakeRect("Gem_Gold",   140, 12, 42, 54),
                MakeRect("Gem_Amber",  186, 12, 42, 54)
            });

            // 4. TopBar_UtilityIcons.png (230 x 60) — 5 circular icons
            SliceTexture($"{UiAssetsRoot}/Icons/TopBar_UtilityIcons.png", new[]
            {
                MakeRect("Utility_Settings", 2,   4, 44, 52),
                MakeRect("Utility_Info",     48,  4, 44, 52),
                MakeRect("Utility_Stats",    93,  4, 44, 52),
                MakeRect("Utility_Crown",    138, 4, 44, 52),
                MakeRect("Utility_Bell",     184, 4, 44, 52)
            });

            // 5. Navigation_Pagination.png (290 x 130) — arrows, close, compass, dot
            SliceTexture($"{UiAssetsRoot}/Icons/Navigation_Pagination.png", new[]
            {
                MakeRect("Nav_ArrowLeft",  14,  70, 50, 50),
                MakeRect("Nav_ArrowRight", 96,  70, 50, 50),
                MakeRect("Nav_Close",      150, 16, 62, 62),
                MakeRect("Nav_Compass",    218, 40, 66, 66),
                MakeRect("Nav_Dot",        66,  38, 24, 24)
            });
        }

        private static SpriteRect MakeRect(string name, float x, float y, float w, float h)
        {
            return new SpriteRect
            {
                name = name,
                rect = new Rect(x, y, w, h),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = SpriteAlignment.Center,
                spriteID = GUID.Generate()
            };
        }

        private static void SliceTexture(string assetPath, SpriteRect[] rects)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[DreamDungeonUIAssetImporter] Texture not found for slicing: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.spritePixelsPerUnit = 100f;
            importer.npotScale = TextureImporterNPOTScale.None;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider != null)
            {
                dataProvider.InitSpriteEditorDataProvider();
                dataProvider.SetSpriteRects(rects);
                dataProvider.Apply();
            }

            importer.SaveAndReimport();
        }

        private static DreamDungeonUIAssetDatabase BuildOrUpdateDatabase()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/DreamDungeon");

            DreamDungeonUIAssetDatabase db = AssetDatabase.LoadAssetAtPath<DreamDungeonUIAssetDatabase>(DatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<DreamDungeonUIAssetDatabase>();
                AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            }

            db.logoDreamDungeon = LoadSprite($"{UiAssetsRoot}/Logo/Logo_DreamDungeon.png");

            db.modeCardCampaign  = LoadSprite($"{UiAssetsRoot}/ModeCards/ModeCard_Campaign.png");
            db.modeCardEndless   = LoadSprite($"{UiAssetsRoot}/ModeCards/ModeCard_Endless.png");
            db.modeCardChallenge = LoadSprite($"{UiAssetsRoot}/ModeCards/ModeCard_Challenge.png");
            db.modeCardCustom    = LoadSprite($"{UiAssetsRoot}/ModeCards/ModeCard_Custom.png");

            db.buttonBack         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Back.png");
            db.buttonPlay         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Play.png");
            db.buttonStartDungeon = LoadSprite($"{UiAssetsRoot}/Buttons/Button_StartDungeon.png");
            db.buttonMove         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Move.png");
            db.buttonRemove       = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Remove.png");
            db.buttonRotate       = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Rotate.png");
            db.buttonPath         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Path.png");
            db.buttonTrap         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Trap.png");
            db.buttonUndo         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Undo.png");
            db.buttonRedo         = LoadSprite($"{UiAssetsRoot}/Buttons/Button_Redo.png");

            db.currencyCrystalCounter = LoadSprite($"{UiAssetsRoot}/Currency/Currency_CrystalCounter.png");
            db.currencyGoldCounter    = LoadSprite($"{UiAssetsRoot}/Currency/Currency_GoldCounter.png");

            db.panelDungeonInfo = LoadSprite($"{UiAssetsRoot}/Panels/Panel_DungeonInfo.png");
            db.panelBuildTools  = LoadSprite($"{UiAssetsRoot}/Panels/Panel_BuildTools.png");
            db.panelEmptyFrame  = LoadSprite($"{UiAssetsRoot}/Panels/Panel_EmptyFrame.png");

            db.decorBannerPurple  = LoadSprite($"{UiAssetsRoot}/Decorations/Decor_Banner_Purple.png");
            db.decorBannerRed     = LoadSprite($"{UiAssetsRoot}/Decorations/Decor_Banner_Red.png");
            db.decorScroll        = LoadSprite($"{UiAssetsRoot}/Decorations/Decor_Scroll.png");
            db.decorSkullEmblem   = LoadSprite($"{UiAssetsRoot}/Decorations/Decor_SkullEmblem.png");
            db.decorTreasureChest = LoadSprite($"{UiAssetsRoot}/Decorations/Decor_TreasureChest.png");

            db.badgeBestValue = LoadSprite($"{UiAssetsRoot}/Badges/Badge_BestValue.png");
            db.badgeHot       = LoadSprite($"{UiAssetsRoot}/Badges/Badge_Hot.png");
            db.badgeNew       = LoadSprite($"{UiAssetsRoot}/Badges/Badge_New.png");
            db.badgeSale      = LoadSprite($"{UiAssetsRoot}/Badges/Badge_Sale.png");

            db.popupPurchaseSuccess = LoadSprite($"{UiAssetsRoot}/Popups/Popup_PurchaseSuccess.png");
            db.popupPurchaseFailed  = LoadSprite($"{UiAssetsRoot}/Popups/Popup_PurchaseFailed.png");

            // Sliced Sub-Sprites
            var roomIcons = LoadSubSprites($"{UiAssetsRoot}/Icons/Icon_RoomTiles_Set.png");
            db.iconRoomStone    = FindSub(roomIcons, "RoomTile_Stone");
            db.iconRoomWood     = FindSub(roomIcons, "RoomTile_Wood");
            db.iconRoomBrick    = FindSub(roomIcons, "RoomTile_Brick");
            db.iconRoomObsidian = FindSub(roomIcons, "RoomTile_Obsidian");

            var monIcons = LoadSubSprites($"{UiAssetsRoot}/Icons/Icon_Monsters_Set.png");
            db.iconSpikeTrap = FindSub(monIcons, "Icon_SpikeTrap");
            db.iconGoblin    = FindSub(monIcons, "Icon_Goblin");
            db.iconSlime     = FindSub(monIcons, "Icon_Slime");
            db.iconSpider    = FindSub(monIcons, "Icon_Spider");

            var gemIcons = LoadSubSprites($"{UiAssetsRoot}/Icons/GemIcons_Set.png");
            db.iconGemPurple = FindSub(gemIcons, "Gem_Purple");
            db.iconGemBlue   = FindSub(gemIcons, "Gem_Blue");
            db.iconGemGreen  = FindSub(gemIcons, "Gem_Green");
            db.iconGemGold   = FindSub(gemIcons, "Gem_Gold");
            db.iconGemAmber  = FindSub(gemIcons, "Gem_Amber");

            var utilIcons = LoadSubSprites($"{UiAssetsRoot}/Icons/TopBar_UtilityIcons.png");
            db.iconSettings = FindSub(utilIcons, "Utility_Settings");
            db.iconInfo     = FindSub(utilIcons, "Utility_Info");
            db.iconStats    = FindSub(utilIcons, "Utility_Stats");
            db.iconCrown    = FindSub(utilIcons, "Utility_Crown");
            db.iconBell     = FindSub(utilIcons, "Utility_Bell");

            var navIcons = LoadSubSprites($"{UiAssetsRoot}/Icons/Navigation_Pagination.png");
            db.iconArrowLeft  = FindSub(navIcons, "Nav_ArrowLeft");
            db.iconArrowRight = FindSub(navIcons, "Nav_ArrowRight");
            db.iconClose      = FindSub(navIcons, "Nav_Close");
            db.iconCompass    = FindSub(navIcons, "Nav_Compass");
            db.iconDot        = FindSub(navIcons, "Nav_Dot");

            EditorUtility.SetDirty(db);
            return db;
        }

        private static void BuildReusableUIPrefabs(DreamDungeonUIAssetDatabase db)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabsFolder);

            CreateModeCardPrefab(db);
            CreateCurrencyCounterPrefab(db);
            CreateBuildToolTabPrefab(db);
            CreateItemSlotPrefab(db);
            CreateDungeonInfoPanelPrefab(db);
            CreateBuildToolsPanelPrefab(db);
            CreateConfirmationPopupPrefab(db);
        }

        private static void CreateModeCardPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_ModeCard", new Vector2(460f, 620f));
            Image highlight = root.AddComponent<Image>();
            highlight.color = new Color(1f, 0.82f, 0.28f, 0f); // Activated on Selected state

            GameObject cardArtObj = NewChild(root, "CardArt");
            Stretch(cardArtObj, 6f, 6f, 6f, 6f);
            Image cardImg = cardArtObj.AddComponent<Image>();
            cardImg.sprite = db.modeCardCampaign;
            cardImg.preserveAspect = true;
            cardImg.raycastTarget = true;
            root.AddComponent<Button>().targetGraphic = cardImg;

            GameObject progressBar = NewChild(root, "ProgressBadge");
            RectTransform pRt = progressBar.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.08f, 0.17f);
            pRt.anchorMax = new Vector2(0.92f, 0.25f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;
            Image pBg = progressBar.AddComponent<Image>();
            pBg.color = new Color(0.05f, 0.05f, 0.10f, 0.88f);

            AddTmpLabel(progressBar, "ProgressText", "Stage 1-1  •  0 Victories", 18, new Color(0.98f, 0.86f, 0.52f));

            GameObject playObj = NewChild(root, "PlayButton");
            RectTransform playRt = playObj.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.16f, 0.03f);
            playRt.anchorMax = new Vector2(0.84f, 0.15f);
            playRt.offsetMin = Vector2.zero;
            playRt.offsetMax = Vector2.zero;
            Image playImg = playObj.AddComponent<Image>();
            playImg.sprite = db.buttonPlay;
            playImg.preserveAspect = true;
            playObj.AddComponent<Button>().targetGraphic = playImg;

            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_ModeCard.prefab");
        }

        private static void CreateCurrencyCounterPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_CurrencyCounter", new Vector2(230f, 70f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.currencyGoldCounter;
            bg.preserveAspect = true;

            GameObject mask = NewChild(root, "ValueBackdrop");
            RectTransform mRt = mask.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.28f, 0.18f);
            mRt.anchorMax = new Vector2(0.76f, 0.82f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            mask.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 0.98f);

            AddTmpLabel(mask, "ValueText", "0", 24, Color.white);
            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_CurrencyCounter.prefab");
        }

        private static void CreateBuildToolTabPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_BuildToolTab", new Vector2(140f, 52f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.panelEmptyFrame;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.22f, 0.16f, 0.42f, 1f);
            root.AddComponent<Button>().targetGraphic = bg;
            AddTmpLabel(root, "TabLabel", "ROOMS", 20, new Color(1f, 0.90f, 0.62f));
            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_BuildToolTab.prefab");
        }

        private static void CreateItemSlotPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_ItemSlot", new Vector2(260f, 84f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.panelEmptyFrame;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.14f, 0.12f, 0.22f, 0.96f);
            root.AddComponent<Button>().targetGraphic = bg;

            GameObject iconObj = NewChild(root, "Icon");
            RectTransform iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0f, 0.5f);
            iRt.anchoredPosition = new Vector2(10f, 0f);
            iRt.sizeDelta = new Vector2(62f, 62f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = db.iconRoomStone;
            iconImg.preserveAspect = true;

            GameObject nameObj = NewChild(root, "NameText");
            RectTransform nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.5f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.offsetMin = new Vector2(80f, 0f);
            nRt.offsetMax = new Vector2(-10f, -6f);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Room Item";
            nameTmp.fontSize = 18;
            nameTmp.color = new Color(1f, 0.92f, 0.70f);
            nameTmp.alignment = TextAlignmentOptions.Left;

            GameObject costObj = NewChild(root, "CostText");
            RectTransform cRt = costObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.offsetMin = new Vector2(80f, 6f);
            cRt.offsetMax = new Vector2(-10f, 0f);
            var costTmp = costObj.AddComponent<TextMeshProUGUI>();
            costTmp.text = "100 Gold";
            costTmp.fontSize = 15;
            costTmp.color = new Color(0.76f, 0.88f, 1f);
            costTmp.alignment = TextAlignmentOptions.Left;

            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_ItemSlot.prefab");
        }

        private static void CreateDungeonInfoPanelPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_DungeonInfoPanel", new Vector2(320f, 540f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.panelEmptyFrame;
            bg.type = Image.Type.Sliced;

            GameObject title = NewChild(root, "TitleText");
            RectTransform tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.08f, 0.88f);
            tRt.anchorMax = new Vector2(0.92f, 0.96f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tmp = title.AddComponent<TextMeshProUGUI>();
            tmp.text = "DUNGEON INFO";
            tmp.fontSize = 24;
            tmp.color = new Color(1f, 0.84f, 0.42f);
            tmp.alignment = TextAlignmentOptions.Center;

            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_DungeonInfoPanel.prefab");
        }

        private static void CreateBuildToolsPanelPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_BuildToolsPanel", new Vector2(320f, 580f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.panelEmptyFrame;
            bg.type = Image.Type.Sliced;

            GameObject title = NewChild(root, "TitleText");
            RectTransform tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.08f, 0.88f);
            tRt.anchorMax = new Vector2(0.92f, 0.96f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tmp = title.AddComponent<TextMeshProUGUI>();
            tmp.text = "BUILD TOOLS";
            tmp.fontSize = 24;
            tmp.color = new Color(1f, 0.84f, 0.42f);
            tmp.alignment = TextAlignmentOptions.Center;

            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_BuildToolsPanel.prefab");
        }

        private static void CreateConfirmationPopupPrefab(DreamDungeonUIAssetDatabase db)
        {
            GameObject root = NewUIRoot("PF_ConfirmationPopup", new Vector2(540f, 340f));
            Image bg = root.AddComponent<Image>();
            bg.sprite = db.panelEmptyFrame;
            bg.type = Image.Type.Sliced;

            GameObject iconObj = NewChild(root, "StatusGraphic");
            RectTransform iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.25f, 0.56f);
            iRt.anchorMax = new Vector2(0.75f, 0.88f);
            iRt.offsetMin = Vector2.zero;
            iRt.offsetMax = Vector2.zero;
            Image statusImg = iconObj.AddComponent<Image>();
            statusImg.sprite = db.popupPurchaseSuccess;
            statusImg.preserveAspect = true;

            GameObject msgObj = NewChild(root, "MessageText");
            RectTransform mRt = msgObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.08f, 0.24f);
            mRt.anchorMax = new Vector2(0.92f, 0.54f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            var msgTmp = msgObj.AddComponent<TextMeshProUGUI>();
            msgTmp.text = "Confirm Action";
            msgTmp.fontSize = 22;
            msgTmp.color = Color.white;
            msgTmp.alignment = TextAlignmentOptions.Center;

            SaveAndDestroyPrefab(root, $"{PrefabsFolder}/PF_ConfirmationPopup.prefab");
        }

        private static GameObject NewUIRoot(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject NewChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Stretch(GameObject go, float left, float bottom, float right, float top)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static TextMeshProUGUI AddTmpLabel(GameObject parent, string name, string text, int fontSize, Color color)
        {
            GameObject go = NewChild(parent, name);
            Stretch(go, 4f, 2f, 4f, 2f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SaveAndDestroyPrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static List<Sprite> LoadSubSprites(string path)
        {
            var list = new List<Sprite>();
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                foreach (UnityEngine.Object obj in all)
                {
                    if (obj is Sprite sp) list.Add(sp);
                }
            }
            return list;
        }

        private static Sprite FindSub(List<Sprite> sprites, string name)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] != null && string.Equals(sprites[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return sprites[i];
            }
            return sprites.Count > 0 ? sprites[0] : null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
            {
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
