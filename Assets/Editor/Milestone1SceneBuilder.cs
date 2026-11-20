using DreamDungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Milestone1SceneBuilder
{
    private const string SceneDir = "Assets/Scenes/";
    private static Font font;

    [MenuItem("Dream Dungeon/Build Milestone 1 Scenes")]
    public static void Build()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildBoot();
        BuildMenu();
        BuildEditor();
        BuildBattle();
        string[] paths = { "Assets/Scenes/Boot.unity", "Assets/Scenes/MainMenu.unity", "Assets/Scenes/DungeonEditor.unity", "Assets/Scenes/Battle.unity", "Assets/Scenes/SampleScene.unity" };
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (string path in paths) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 1 scenes generated.");
    }

    [MenuItem("Dream Dungeon/Build Milestone 2 Dungeon Editor")]
    public static void BuildMilestone2()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RoomData[] catalog = CreateDefaultRoomData();
        RoomDatabase database = CreateDefaultRoomDatabase(catalog);
        BuildEditor(catalog, database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 2 dungeon editor generated.");
    }

    [MenuItem("Dream Dungeon/Build Milestone 3 Room System")]
    public static void BuildMilestone3()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RoomData[] catalog = CreateDefaultRoomData();
        RoomDatabase database = CreateDefaultRoomDatabase(catalog);
        BuildEditor(catalog, database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 3 room system generated.");
    }

    [MenuItem("Dream Dungeon/Build Milestone 4 Monster & Trap Systems")]
    public static void BuildMilestone4()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RoomData[] catalog = CreateDefaultRoomData();
        RoomDatabase rooms = CreateDefaultRoomDatabase(catalog);
        MonsterDatabase monsters = CreateDefaultMonsterDatabase(CreateDefaultMonsterData());
        TrapDatabase traps = CreateDefaultTrapDatabase(CreateDefaultTrapData());
        BuildEditor(catalog, rooms, monsters, traps);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 4 Monster & Trap systems generated.");
    }

    [InitializeOnLoadMethod]
    private static void AutoEnsureMilestone5Assets()
    {
        EditorApplication.delayCall += () =>
        {
            const string folder = "Assets/Resources/DreamDungeon";
            if (!AssetDatabase.IsValidFolder("Assets/Resources") ||
                !AssetDatabase.IsValidFolder(folder) ||
                AssetDatabase.LoadAssetAtPath<AdventurerDatabase>($"{folder}/AdventurerDatabase.asset") == null ||
                AssetDatabase.LoadAssetAtPath<BossDatabase>($"{folder}/BossDatabase.asset") == null)
            {
                Debug.Log("[Milestone 6] Auto-generating Milestone 6 Assets & Scene...");
                BuildMilestone6();
            }
        };
    }

    [MenuItem("Dream Dungeon/Build Milestone 6 Dungeon Progression, Boss & Upgrade")]
    public static void BuildMilestone6()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RoomData[] rooms = CreateDefaultRoomData();
        RoomDatabase roomDb = CreateDefaultRoomDatabase(rooms);
        MonsterDatabase monsters = CreateDefaultMonsterDatabase(CreateDefaultMonsterData());
        TrapDatabase traps = CreateDefaultTrapDatabase(CreateDefaultTrapData());
        BossDatabase bosses = CreateDefaultBossDatabase(CreateDefaultBossData());
        CreateDefaultLevelData();
        CreateDefaultUpgradeConfiguration();
        AdventurerDatabase adventurers = CreateDefaultAdventurers();
        BattleConfiguration config = CreateDefaultBattleConfiguration();

        CopyAssetIfMissing("Assets/Data/MonsterDatabase.asset", "Assets/Resources/DreamDungeon/MonsterDatabase.asset");
        CopyAssetIfMissing("Assets/Data/TrapDatabase.asset", "Assets/Resources/DreamDungeon/TrapDatabase.asset");

        BuildEditor(rooms, roomDb, monsters, traps);
        BuildBattle(adventurers, monsters, traps, config);

        var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!sceneList.Exists(s => s.path == "Assets/Scenes/DungeonEditor.unity")) sceneList.Add(new EditorBuildSettingsScene("Assets/Scenes/DungeonEditor.unity", true));
        if (!sceneList.Exists(s => s.path == "Assets/Scenes/Battle.unity")) sceneList.Add(new EditorBuildSettingsScene("Assets/Scenes/Battle.unity", true));
        EditorBuildSettings.scenes = sceneList.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 6 generated successfully.");
    }

    [MenuItem("Dream Dungeon/Build Milestone 5 Adventurer & Auto Battle")]
    public static void BuildMilestone5()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RoomData[] rooms = CreateDefaultRoomData();
        CreateDefaultRoomDatabase(rooms);
        MonsterDatabase monsters = CreateDefaultMonsterDatabase(CreateDefaultMonsterData());
        TrapDatabase traps = CreateDefaultTrapDatabase(CreateDefaultTrapData());
        AdventurerDatabase adventurers = CreateDefaultAdventurers();
        BattleConfiguration config = CreateDefaultBattleConfiguration();

        // Also ensure MonsterDatabase and TrapDatabase are accessible via Resources
        CopyAssetIfMissing("Assets/Data/MonsterDatabase.asset", "Assets/Resources/DreamDungeon/MonsterDatabase.asset");
        CopyAssetIfMissing("Assets/Data/TrapDatabase.asset", "Assets/Resources/DreamDungeon/TrapDatabase.asset");

        BuildBattle(adventurers, monsters, traps, config);
        var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!sceneList.Exists(s => s.path == "Assets/Scenes/Battle.unity")) sceneList.Add(new EditorBuildSettingsScene("Assets/Scenes/Battle.unity", true));
        EditorBuildSettings.scenes = sceneList.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dream Dungeon milestone 5 Adventurer & Auto Battle generated.");
    }

    private static void CopyAssetIfMissing(string source, string destination)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination) == null &&
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source) != null)
        {
            AssetDatabase.CopyAsset(source, destination);
        }
    }

    private static void BuildBoot()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("BootLoader").AddComponent<BootLoader>();
        Save(scene, "Boot");
    }

    private static void BuildMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateCanvas();
        Panel(canvas.transform, "Background", new Color(.055f, .035f, .09f), false, true);
        RectTransform safe = Panel(canvas.transform, "SafeAreaRoot", Color.clear, false, false);
        safe.gameObject.AddComponent<SafeAreaController>();
        TextLabel(safe, "Title", "DREAM\nDUNGEON", 82, new Vector2(0, 590), new Vector2(900, 290), new Color(.96f, .78f, .43f));
        TextLabel(safe, "Subtitle", "BUILD YOUR LAIR  •  DEFEND YOUR DREAMS", 22, new Vector2(0, 425), new Vector2(900, 70), new Color(.72f, .68f, .82f));
        ButtonAt(safe, "PlayButton", "PLAY", new Vector2(0, 145), new Vector2(640, 120), new Color(.48f, .24f, .12f), 38);
        ButtonAt(safe, "MonstersButton", "MONSTERS", new Vector2(0, -5), new Vector2(640, 100), new Color(.19f, .16f, .28f), 30);
        ButtonAt(safe, "ShopButton", "SHOP", new Vector2(0, -130), new Vector2(300, 94), new Color(.19f, .16f, .28f), 28);
        ButtonAt(safe, "SettingsButton", "SETTINGS", new Vector2(0, -245), new Vector2(300, 94), new Color(.19f, .16f, .28f), 26);
        RectTransform overlay = Panel(safe, "ComingSoonPanel", new Color(.035f, .025f, .06f, .97f), false, false, blocksRaycasts: true);
        overlay.gameObject.SetActive(false);
        TextLabel(overlay, "ComingSoonTitle", "COMING SOON", 48, new Vector2(0, 55), new Vector2(820, 220), new Color(.96f, .78f, .43f));
        ButtonAt(overlay, "CloseComingSoonButton", "BACK", new Vector2(0, -110), new Vector2(420, 100), new Color(.48f, .24f, .12f), 32);
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var ui = new GameObject("MainMenuController").AddComponent<MainMenuUI>();
        ui.uiManager = new GameObject("UIManager").AddComponent<UIManager>();
        ui.comingSoonPanel = overlay.gameObject;
        ui.comingSoonTitle = overlay.Find("ComingSoonTitle").GetComponent<Text>();
        Save(scene, "MainMenu");
    }

    private static void BuildEditor()
    {
        RoomData[] catalog = CreateDefaultRoomData();
        BuildEditor(catalog, CreateDefaultRoomDatabase(catalog));
    }

    private static void BuildEditor(RoomData[] catalog, RoomDatabase database)
    {
        BuildEditor(catalog, database, null, null);
    }

    private static void BuildEditor(RoomData[] catalog, RoomDatabase database, MonsterDatabase monsterDatabase, TrapDatabase trapDatabase)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateCanvas();
        Panel(canvas.transform, "Background", new Color(.055f, .07f, .09f, .22f), false, true);
        RectTransform safe = Panel(canvas.transform, "SafeAreaRoot", Color.clear, false, false);
        safe.gameObject.AddComponent<SafeAreaController>();

        TextLabel(safe, "Header", "DUNGEON  •  LEVEL 1", 38, new Vector2(0, 890), new Vector2(1000, 80), new Color(.96f, .78f, .43f));
        TextLabel(safe, "LevelValue", "LEVEL  1", 22, new Vector2(-365, 790), new Vector2(190, 70), Color.white);
        TextLabel(safe, "GoldLabel", "GOLD", 20, new Vector2(-120, 800), new Vector2(100, 45), new Color(.95f, .78f, .34f));
        TextLabel(safe, "GoldValue", "1000", 30, new Vector2(-120, 755), new Vector2(150, 55), Color.white);
        TextLabel(safe, "SoulValue", "SOUL  0", 20, new Vector2(105, 785), new Vector2(150, 65), new Color(.66f, .78f, .96f));
        TextLabel(safe, "CrystalValue", "CRYSTAL  0", 20, new Vector2(350, 785), new Vector2(190, 65), new Color(.65f, .9f, .91f));
        if (monsterDatabase == null || trapDatabase == null)
        {
            ButtonAt(safe, "OpenRoomShopButton", "ROOM SHOP", new Vector2(-250, 680), new Vector2(300, 68), new Color(.30f, .24f, .16f), 18);
            ButtonAt(safe, "OpenRoomInventoryButton", "INVENTORY", new Vector2(250, 680), new Vector2(300, 68), new Color(.16f, .25f, .29f), 18);
        }
        else
        {
            ButtonAt(safe, "OpenRoomShopButton", "ROOM SHOP", new Vector2(-390, 680), new Vector2(205, 68), new Color(.30f, .24f, .16f), 15);
            ButtonAt(safe, "OpenMonsterShopButton", "MONSTER SHOP", new Vector2(-130, 680), new Vector2(235, 68), new Color(.30f, .20f, .18f), 15);
            ButtonAt(safe, "OpenTrapShopButton", "TRAP SHOP", new Vector2(130, 680), new Vector2(205, 68), new Color(.21f, .30f, .23f), 15);
            ButtonAt(safe, "OpenRoomInventoryButton", "ROOMS", new Vector2(390, 680), new Vector2(155, 68), new Color(.16f, .25f, .29f), 15);
            ButtonAt(safe, "OpenMonsterInventoryButton", "MONSTERS", new Vector2(-260, 590), new Vector2(235, 64), new Color(.24f, .19f, .28f), 15);
            ButtonAt(safe, "OpenTrapInventoryButton", "TRAPS", new Vector2(260, 590), new Vector2(235, 64), new Color(.18f, .29f, .24f), 15);
        }

        ButtonAt(safe, "ProgressionButton", "PROGRESSION", new Vector2(-280, 505), new Vector2(250, 60), new Color(.20f, .30f, .45f), 15);
        ButtonAt(safe, "BossButton", "BOSSES", new Vector2(0, 505), new Vector2(250, 60), new Color(.45f, .18f, .18f), 15);
        ButtonAt(safe, "UpgradeButton", "UPGRADES", new Vector2(280, 505), new Vector2(250, 60), new Color(.18f, .35f, .25f), 15);
        ButtonAt(safe, "QuestButton", "QUESTS", new Vector2(-280, 435), new Vector2(210, 56), new Color(.32f, .25f, .14f), 15);
        ButtonAt(safe, "WorldMapButton", "WORLD MAP", new Vector2(0, 435), new Vector2(210, 56), new Color(.16f, .34f, .42f), 15);
        ButtonAt(safe, "AchievementButton", "ACHIEVEMENTS", new Vector2(280, 435), new Vector2(220, 56), new Color(.28f, .20f, .36f), 15);

        TextLabel(safe, "SelectedCellText", "NO CELL SELECTED", 20, new Vector2(0, -455), new Vector2(930, 55), new Color(.87f, .86f, .79f));
        TextLabel(safe, "BuildStatusText", "Select a room or path tool.", 18, new Vector2(0, -515), new Vector2(990, 70), Color.white);

        ButtonAt(safe, "MonsterRoomButton", "MONSTER ROOM", new Vector2(-390, -600), new Vector2(225, 78), new Color(.40f, .17f, .17f), 19);
        ButtonAt(safe, "TreasureRoomButton", "TREASURE ROOM", new Vector2(-140, -600), new Vector2(235, 78), new Color(.42f, .34f, .12f), 18);
        ButtonAt(safe, "MoveButton", "MOVE", new Vector2(120, -600), new Vector2(150, 78), new Color(.16f, .30f, .36f), 20);
        ButtonAt(safe, "RemoveButton", "REMOVE", new Vector2(320, -600), new Vector2(165, 78), new Color(.37f, .18f, .21f), 20);
        ButtonAt(safe, "RotateButton", "ROTATE 90°", new Vector2(0, -690), new Vector2(260, 76), new Color(.27f, .23f, .38f), 20);

        ButtonAt(safe, "RoomButton", "ROOM", new Vector2(-390, -780), new Vector2(190, 82), new Color(.42f, .22f, .15f), 22);
        ButtonAt(safe, "MonsterButton", "MONSTER", new Vector2(-130, -780), new Vector2(220, 82), new Color(.20f, .20f, .30f), 20);
        ButtonAt(safe, "TrapButton", "TRAP", new Vector2(130, -780), new Vector2(190, 82), new Color(.20f, .20f, .30f), 22);
        ButtonAt(safe, "PathButton", "PATH", new Vector2(390, -780), new Vector2(190, 82), new Color(.30f, .28f, .25f), 22);

        ButtonAt(safe, "UndoButton", "UNDO", new Vector2(-330, -875), new Vector2(190, 72), new Color(.20f, .22f, .27f), 18);
        ButtonAt(safe, "RedoButton", "REDO", new Vector2(-110, -875), new Vector2(190, 72), new Color(.20f, .22f, .27f), 18);
        ButtonAt(safe, "StartRaidButton", "START RAID", new Vector2(150, -875), new Vector2(225, 72), new Color(.42f, .20f, .12f), 18);
        ButtonAt(safe, "BackButton", "BACK", new Vector2(390, -875), new Vector2(145, 72), new Color(.20f, .22f, .27f), 18);

        RectTransform notice = Panel(safe, "NoticePanel", new Color(.025f, .02f, .045f, .92f), false, true, blocksRaycasts: true);
        notice.gameObject.SetActive(false);
        Panel(notice, "NoticeCard", new Color(.13f, .11f, .18f), false, false, Vector2.zero, new Vector2(850, 530), blocksRaycasts: true);
        TextLabel(notice, "NoticeText", "COMING SOON", 38, new Vector2(0, 45), new Vector2(760, 250), new Color(.96f, .78f, .43f));
        ButtonAt(notice, "CloseNoticeButton", "OK", new Vector2(0, -155), new Vector2(360, 88), new Color(.42f, .22f, .15f), 26);

        GameObject cameraObject = new GameObject("DungeonCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f, .07f, .09f);
        cameraObject.transform.position = new Vector3(0, .45f, -10);

        GameObject builder = new GameObject("DungeonBuilder");
        DungeonGrid grid = builder.AddComponent<DungeonGrid>();
        grid.Configure(9, 9, .62f, new Vector2(0, .45f), camera);
        CurrencyManager currency = builder.AddComponent<CurrencyManager>();
        currency.Configure(1000, 0, 0);
        RoomManager manager = builder.AddComponent<RoomManager>();
        manager.Configure(grid, catalog, 9, 9, 1000);
        manager.ConfigureRoomSystems(database, currency, null);
        RoomInventoryManager inventory = builder.AddComponent<RoomInventoryManager>();
        inventory.Configure(database, manager);
        manager.ConfigureRoomSystems(database, currency, inventory);
        RoomShopManager shop = builder.AddComponent<RoomShopManager>();
        shop.Configure(database, currency, inventory, 1);
        RoomUpgradeManager upgrades = builder.AddComponent<RoomUpgradeManager>();
        upgrades.Configure(database, currency, manager);
        PlacementPreview preview = builder.AddComponent<PlacementPreview>();
        preview.Configure(grid);
        RoomPlacement placement = builder.AddComponent<RoomPlacement>();
        placement.Configure(grid, manager, preview);
        MonsterInventoryManager monsterInventory = null;
        TrapInventoryManager trapInventory = null;
        MonsterPlacementManager monsterPlacement = null;
        TrapPlacementManager trapPlacement = null;
        MonsterUpgradeManager monsterUpgrades = null;
        TrapUpgradeManager trapUpgrades = null;
        MonsterShopManager monsterShop = null;
        TrapShopManager trapShop = null;
        if (monsterDatabase != null && trapDatabase != null)
        {
            monsterInventory = builder.AddComponent<MonsterInventoryManager>();
            monsterInventory.Configure(monsterDatabase, manager);
            trapInventory = builder.AddComponent<TrapInventoryManager>();
            trapInventory.Configure(trapDatabase, manager);
            manager.ConfigureUnitInventories(monsterInventory, trapInventory);
            manager.ConfigureUnitDefinitions(monsterDatabase, trapDatabase);
            monsterShop = builder.AddComponent<MonsterShopManager>();
            monsterShop.Configure(monsterDatabase, monsterInventory, currency, 1);
            trapShop = builder.AddComponent<TrapShopManager>();
            trapShop.Configure(trapDatabase, trapInventory, currency, 1);
            monsterPlacement = builder.AddComponent<MonsterPlacementManager>();
            monsterPlacement.Configure(grid, manager, monsterDatabase, monsterInventory, placement);
            trapPlacement = builder.AddComponent<TrapPlacementManager>();
            trapPlacement.Configure(grid, manager, trapDatabase, trapInventory, placement);
            monsterUpgrades = builder.AddComponent<MonsterUpgradeManager>();
            monsterUpgrades.Configure(monsterDatabase, currency, manager);
            trapUpgrades = builder.AddComponent<TrapUpgradeManager>();
            trapUpgrades.Configure(trapDatabase, currency, manager);
        }
        BossDatabase bossDatabase = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase") ?? CreateDefaultBossDatabase(CreateDefaultBossData());
        BossPlacementManager bossPlacement = builder.AddComponent<BossPlacementManager>();
        bossPlacement.Configure(grid, manager, bossDatabase, placement);

        if (builder.GetComponent<DungeonProgressionManager>() == null) builder.AddComponent<DungeonProgressionManager>();
        if (builder.GetComponent<DungeonUnlockManager>() == null) builder.AddComponent<DungeonUnlockManager>();
        if (builder.GetComponent<DungeonCoreUpgradeManager>() == null) builder.AddComponent<DungeonCoreUpgradeManager>();
        if (builder.GetComponent<DungeonExpansionManager>() == null) builder.AddComponent<DungeonExpansionManager>();
        if (builder.GetComponent<DungeonFacilityManager>() == null) builder.AddComponent<DungeonFacilityManager>();
        if (builder.GetComponent<QuestManager>() == null) builder.AddComponent<QuestManager>();
        if (builder.GetComponent<AchievementManager>() == null) builder.AddComponent<AchievementManager>();
        if (builder.GetComponent<DifficultyManager>() == null) builder.AddComponent<DifficultyManager>();
        if (builder.GetComponent<WorldMapManager>() == null) builder.AddComponent<WorldMapManager>();
        if (builder.GetComponent<CampaignManager>() == null) builder.AddComponent<CampaignManager>();
        if (builder.GetComponent<SaveManager>() == null) builder.AddComponent<SaveManager>();

        DungeonCameraController cameraController = builder.AddComponent<DungeonCameraController>();
        cameraController.Configure(camera, grid);
        EditorUtility.SetDirty(grid);
        EditorUtility.SetDirty(currency);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(shop);
        EditorUtility.SetDirty(upgrades);
        EditorUtility.SetDirty(preview);
        EditorUtility.SetDirty(placement);
        EditorUtility.SetDirty(bossPlacement);
        EditorUtility.SetDirty(cameraController);

        GameObject editorController = new GameObject("DungeonEditorController");
        RoomShopUI shopUI = editorController.AddComponent<RoomShopUI>();
        shopUI.Configure(safe, shop, database, currency);
        RoomInventoryUI inventoryUI = editorController.AddComponent<RoomInventoryUI>();
        inventoryUI.Configure(safe, inventory, database, placement);
        RoomDetailsUI detailsUI = editorController.AddComponent<RoomDetailsUI>();
        detailsUI.Configure(safe, database, manager, placement, upgrades);
        MonsterShopUI monsterShopUI = null;
        MonsterInventoryUI monsterInventoryUI = null;
        TrapShopUI trapShopUI = null;
        TrapInventoryUI trapInventoryUI = null;
        if (monsterDatabase != null && trapDatabase != null)
        {
            monsterShopUI = editorController.AddComponent<MonsterShopUI>();
            monsterShopUI.Configure(safe, monsterShop, monsterDatabase, currency);
            MonsterDetailsUI monsterDetails = editorController.AddComponent<MonsterDetailsUI>();
            monsterDetails.Configure(safe, monsterDatabase, manager, monsterUpgrades, monsterPlacement);
            monsterInventoryUI = editorController.AddComponent<MonsterInventoryUI>();
            monsterInventoryUI.Configure(safe, monsterInventory, monsterDatabase, monsterPlacement, manager, monsterDetails);
            trapShopUI = editorController.AddComponent<TrapShopUI>();
            trapShopUI.Configure(safe, trapShop, trapDatabase, currency);
            TrapDetailsUI trapDetails = editorController.AddComponent<TrapDetailsUI>();
            trapDetails.Configure(safe, trapDatabase, manager, trapUpgrades, trapPlacement);
            trapInventoryUI = editorController.AddComponent<TrapInventoryUI>();
            trapInventoryUI.Configure(safe, trapInventory, trapDatabase, trapPlacement, manager, trapDetails);
        }

        DungeonProgressionUI progUI = editorController.AddComponent<DungeonProgressionUI>();
        progUI.Configure(safe);
        BossSelectionUI bossUI = editorController.AddComponent<BossSelectionUI>();
        bossUI.Configure(safe, bossPlacement, bossDatabase);
        DungeonUpgradeUI upgradeUI = editorController.AddComponent<DungeonUpgradeUI>();
        upgradeUI.Configure(safe);
        QuestUIManager questUI = editorController.AddComponent<QuestUIManager>();
        questUI.Configure(safe);
        AchievementUIManager achievementUI = editorController.AddComponent<AchievementUIManager>();
        achievementUI.Configure(safe);
        QuestNotificationUI notifUI = editorController.AddComponent<QuestNotificationUI>();
        notifUI.Configure(safe);
        StageDetailsUI stageDetailsUI = editorController.AddComponent<StageDetailsUI>();
        stageDetailsUI.Configure(safe);
        CampaignSelectionUI campaignUI = editorController.AddComponent<CampaignSelectionUI>();
        campaignUI.Configure(safe, null, null, null, stageDetailsUI);
        WorldMapUI worldMapUI = editorController.AddComponent<WorldMapUI>();
        worldMapUI.Configure(safe, null, campaignUI);

        DungeonEditorUI editorUI = editorController.AddComponent<DungeonEditorUI>();
        editorUI.Configure(shopUI, inventoryUI, detailsUI);
        editorUI.ConfigureMilestone4(monsterShopUI, monsterInventoryUI, trapShopUI, trapInventoryUI, monsterPlacement, trapPlacement);
        editorUI.ConfigureMilestone6(progUI, bossUI, upgradeUI, bossPlacement);
        editorUI.ConfigureMilestone7(questUI, achievementUI, notifUI);
        editorUI.ConfigureMilestone8(worldMapUI, campaignUI, stageDetailsUI);
        EditorUtility.SetDirty(editorController);
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorUtility.SetDirty(eventSystem);
        Save(scene, "DungeonEditor");
    }

    private static void BuildBattle()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateCanvas();
        Panel(canvas.transform, "Background", new Color(.055f, .035f, .09f), false, true);
        RectTransform safe = Panel(canvas.transform, "SafeAreaRoot", Color.clear, false, false);
        safe.gameObject.AddComponent<SafeAreaController>();
        TextLabel(safe, "Placeholder", "BATTLE\n\nFUTURE DEVELOPMENT", 52, Vector2.zero, new Vector2(900, 460), Color.white);
        Save(scene, "Battle");
    }

    private static void BuildBattle(AdventurerDatabase adventurers, MonsterDatabase monsters, TrapDatabase traps, BattleConfiguration config)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("BattleCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.84f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f, .07f, .09f);
        cameraObject.transform.position = new Vector3(0, .45f, -10);
        BattleCameraController cameraCtrl = cameraObject.AddComponent<BattleCameraController>();
        cameraCtrl.Configure(9, 9, 0.62f, new Vector2(0f, 0.45f));
        EditorUtility.SetDirty(cameraCtrl);

        GameObject managerObj = new GameObject("BattleManager");
        BattleDungeonRenderer renderer = managerObj.AddComponent<BattleDungeonRenderer>();
        BattleManager manager = managerObj.AddComponent<BattleManager>();
        manager.Configure(adventurers, monsters, traps, config);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(manager);

        new GameObject("BattleHUD").AddComponent<BattleHUD>();
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Save(scene, "Battle");
    }

    private static AdventurerDatabase CreateDefaultAdventurers()
    {
        const string folder = "Assets/Resources/DreamDungeon";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "DreamDungeon");
        var specs = new (string id, string title, int hp, int attack, int defense, AdventurerAttackType type, float range, float speed, float move)[]
        {
            ("swordsman", "Swordsman", 150, 20, 10, AdventurerAttackType.Melee, .72f, 1f, 1.05f),
            ("archer", "Archer", 90, 25, 3, AdventurerAttackType.Ranged, 2.4f, 1.1f, .95f),
            ("mage", "Mage", 80, 35, 2, AdventurerAttackType.Magic, 2.8f, .8f, .9f),
            ("knight", "Knight", 250, 15, 25, AdventurerAttackType.Melee, .8f, .8f, .8f)
        };
        var entries = new System.Collections.Generic.List<AdventurerData>();
        foreach (var spec in specs)
        {
            string path = $"{folder}/{spec.id}.asset";
            AdventurerData data = AssetDatabase.LoadAssetAtPath<AdventurerData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<AdventurerData>(); AssetDatabase.CreateAsset(data, path); }
            data.adventurerId = spec.id; data.adventurerName = spec.title; data.description = $"{spec.type} raider.";
            data.maxHP = spec.hp; data.attack = spec.attack; data.defense = spec.defense; data.attackType = spec.type;
            data.attackRange = spec.range; data.attackSpeed = spec.speed; data.moveSpeed = spec.move; data.targetPriority = AdventurerTargetPriority.Nearest;
            EditorUtility.SetDirty(data); entries.Add(data);
        }
        string dbPath = $"{folder}/AdventurerDatabase.asset";
        AdventurerDatabase db = AssetDatabase.LoadAssetAtPath<AdventurerDatabase>(dbPath);
        if (db == null) { db = ScriptableObject.CreateInstance<AdventurerDatabase>(); AssetDatabase.CreateAsset(db, dbPath); }
        db.SetEntries(entries); EditorUtility.SetDirty(db); return db;
    }

    private static BattleConfiguration CreateDefaultBattleConfiguration()
    {
        const string path = "Assets/Resources/DreamDungeon/BattleConfiguration.asset";
        BattleConfiguration config = AssetDatabase.LoadAssetAtPath<BattleConfiguration>(path);
        if (config == null) { config = ScriptableObject.CreateInstance<BattleConfiguration>(); AssetDatabase.CreateAsset(config, path); }
        config.spawnInterval = 1f; config.coreMaxHP = 500; config.coreDefense = 1;
        config.goldPerDefeatedAdventurer = 10; config.soulPerDefeatedAdventurer = 2;
        config.victoryBonusGold = 100; config.victoryBonusSoul = 20; config.victoryBonusExp = 50;
        config.defeatGoldPerDefeated = 5; config.defeatSoulPerDefeated = 1;
        config.waves = new System.Collections.Generic.List<AdventurerWave>
        {
            MakeWave("Wave 1", ("swordsman", 2)),
            MakeWave("Wave 2", ("swordsman", 2), ("archer", 1)),
            MakeWave("Wave 3", ("swordsman", 2), ("archer", 1), ("mage", 1)),
            MakeWave("Wave 4", ("knight", 1), ("swordsman", 2), ("archer", 2)),
            MakeWave("Wave 5", ("knight", 1), ("swordsman", 2), ("archer", 2), ("mage", 2))
        };
        EditorUtility.SetDirty(config); return config;
    }

    private static AdventurerWave MakeWave(string name, params (string id, int count)[] entries)
    {
        var wave = new AdventurerWave { waveName = name };
        foreach (var item in entries) wave.entries.Add(new AdventurerWaveEntry { adventurerId = item.id, count = item.count });
        return wave;
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    private static RectTransform Panel(Transform parent, string name, Color color, bool safeStretch, bool fullStretch, Vector2? position = null, Vector2? size = null, bool blocksRaycasts = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (fullStretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        else { rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = position ?? Vector2.zero; rt.sizeDelta = size ?? new Vector2(900, 150); }
        Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = blocksRaycasts;
        return rt;
    }

    private static Text TextLabel(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = position; rt.sizeDelta = size;
        Text text = go.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.fontStyle = FontStyle.Bold; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button ButtonAt(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, int fontSize)
    {
        RectTransform rt = Panel(parent, name, color, false, false, position, size, blocksRaycasts: true);
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(Mathf.Min(1, color.r + .15f), Mathf.Min(1, color.g + .15f), Mathf.Min(1, color.b + .15f)); button.colors = colors;
        TextLabel(rt, name + "Label", label, fontSize, Vector2.zero, size - new Vector2(24, 12), Color.white);
        return button;
    }

    private static RoomData[] CreateDefaultRoomData()
    {
        const string folder = "Assets/Data/Rooms";
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Data", "Rooms");

        var definitions = new (string assetName, string id, string title, string description, int width, int height,
            int cost, int unlockLevel, int maxLevel, int upgradeGold, int upgradeSoul, int capacity, DungeonRoomType type)[]
        {
            ("entrance", "entrance", "Entrance", "The required dungeon entrance.", 1, 1, 0, 1, 1, 0, 0, 1, DungeonRoomType.Entrance),
            ("dungeon-core", "dungeon_core", "Dungeon Core", "The protected heart of the dungeon.", 1, 1, 0, 1, 1, 0, 0, 1, DungeonRoomType.DungeonCore),
            ("monster-room", "monster_room", "Monster Room", "A room for defending monsters.", 1, 1, 200, 1, 5, 100, 0, 10, DungeonRoomType.MonsterRoom),
            ("treasure-room", "treasure_room", "Treasure Room", "A room for storing treasure.", 1, 1, 150, 1, 5, 75, 0, 100, DungeonRoomType.TreasureRoom),
            ("trap-room", "trap_room", "Trap Room", "A room prepared for traps.", 1, 1, 250, 1, 5, 125, 0, 5, DungeonRoomType.TrapRoom),
            ("magic-room", "magic_room", "Magic Room", "A chamber for magical research.", 2, 2, 400, 1, 5, 200, 0, 20, DungeonRoomType.MagicRoom),
            ("boss-room", "boss_room", "Boss Room", "A chamber for the dungeon boss.", 2, 2, 1000, 5, 5, 500, 0, 1, DungeonRoomType.BossRoom),
            ("path", "path", "Path", "Walkable dungeon path tile.", 1, 1, 0, 1, 1, 0, 0, 0, DungeonRoomType.Path)
        };

        var assets = new RoomData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i];
            string path = $"{folder}/{definition.assetName}.asset";
            RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<RoomData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.roomId = definition.id;
            data.roomName = definition.title;
            data.description = definition.description;
            data.width = definition.width;
            data.height = definition.height;
            data.goldCost = definition.cost;
            data.soulCost = 0;
            data.unlockDungeonLevel = definition.unlockLevel;
            data.maxLevel = definition.maxLevel;
            data.baseUpgradeGoldCost = definition.upgradeGold;
            data.baseUpgradeSoulCost = definition.upgradeSoul;
            data.maxCapacity = definition.capacity;
            data.roomType = definition.type;
            EditorUtility.SetDirty(data);
            assets[i] = data;
        }

        return assets;
    }

    private static RoomDatabase CreateDefaultRoomDatabase(RoomData[] catalog)
    {
        const string path = "Assets/Data/RoomDatabase.asset";
        RoomDatabase database = AssetDatabase.LoadAssetAtPath<RoomDatabase>(path);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<RoomDatabase>();
            AssetDatabase.CreateAsset(database, path);
        }
        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty rooms = serialized.FindProperty("rooms");
        rooms.arraySize = catalog != null ? catalog.Length : 0;
        for (int i = 0; i < rooms.arraySize; i++) rooms.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return database;
    }

    private static MonsterData[] CreateDefaultMonsterData()
    {
        EnsureDataFolder("Monsters");
        var definitions = new (string id, string title, MonsterRarity rarity, MonsterRole role, int hp, int attack, int defense, int gold, int unlock)[]
        {
            ("slime", "Slime", MonsterRarity.Common, MonsterRole.Tank, 100, 10, 5, 100, 1),
            ("goblin", "Goblin", MonsterRarity.Common, MonsterRole.Melee, 80, 20, 5, 200, 1),
            ("skeleton_archer", "Skeleton Archer", MonsterRarity.Rare, MonsterRole.Ranged, 70, 25, 3, 300, 2),
            ("dark_mage", "Dark Mage", MonsterRarity.Rare, MonsterRole.Magic, 60, 35, 2, 450, 3),
            ("orc", "Orc", MonsterRarity.Epic, MonsterRole.Tank, 250, 30, 20, 700, 4),
            ("baby_dragon", "Baby Dragon", MonsterRarity.Epic, MonsterRole.Boss, 300, 50, 15, 1200, 5)
        };
        var result = new MonsterData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            var d = definitions[i]; string path = $"Assets/Data/Monsters/{d.id}.asset";
            MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<MonsterData>(); AssetDatabase.CreateAsset(data, path); }
            data.monsterId = d.id; data.monsterName = d.title; data.description = $"{d.rarity} {d.role} defender for your dungeon.";
            data.rarity = d.rarity; data.role = d.role; data.maxHP = d.hp; data.attack = d.attack; data.defense = d.defense;
            data.attackRange = d.role == MonsterRole.Ranged || d.role == MonsterRole.Magic ? 3f : 1f;
            data.attackSpeed = d.role == MonsterRole.Melee ? 1.2f : 1f; data.moveSpeed = 1f;
            data.goldCost = d.gold; data.unlockDungeonLevel = d.unlock; data.maxLevel = 5; data.baseUpgradeGoldCost = 100; data.baseUpgradeSoulCost = 0;
            EditorUtility.SetDirty(data); result[i] = data;
        }
        return result;
    }

    private static MonsterDatabase CreateDefaultMonsterDatabase(MonsterData[] entries)
    {
        const string path = "Assets/Data/MonsterDatabase.asset";
        MonsterDatabase db = AssetDatabase.LoadAssetAtPath<MonsterDatabase>(path);
        if (db == null) { db = ScriptableObject.CreateInstance<MonsterDatabase>(); AssetDatabase.CreateAsset(db, path); }
        db.SetEntries(new System.Collections.Generic.List<MonsterData>(entries)); EditorUtility.SetDirty(db); return db;
    }

    private static TrapData[] CreateDefaultTrapData()
    {
        EnsureDataFolder("Traps");
        var definitions = new (string id, string title, TrapType type, int damage, float dps, float duration, float cooldown, float radius, float slow, int gold, int unlock)[]
        {
            ("spike_trap", "Spike Trap", TrapType.Spike, 20, 0, 0, 0, 1f, 0, 100, 1),
            ("fire_trap", "Fire Trap", TrapType.Fire, 0, 10, 3, 5, 1.2f, 0, 250, 2),
            ("poison_trap", "Poison Trap", TrapType.Poison, 0, 5, 5, 8, 1.4f, 0, 300, 3),
            ("slow_trap", "Slow Trap", TrapType.Slow, 0, 0, 3, 6, 1.5f, .4f, 200, 2),
            ("magic_trap", "Magic Trap", TrapType.Magic, 40, 0, 0, 4, 1.5f, 0, 400, 4)
        };
        var result = new TrapData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            var d = definitions[i]; string path = $"Assets/Data/Traps/{d.id}.asset";
            TrapData data = AssetDatabase.LoadAssetAtPath<TrapData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<TrapData>(); AssetDatabase.CreateAsset(data, path); }
            data.trapId = d.id; data.trapName = d.title; data.description = $"{d.type} trap for defending your dungeon.";
            data.trapType = d.type; data.damage = d.damage; data.damagePerSecond = d.dps; data.duration = d.duration;
            data.cooldown = d.cooldown; data.triggerRadius = d.radius; data.slowPercentage = d.slow;
            data.goldCost = d.gold; data.unlockDungeonLevel = d.unlock; data.maxLevel = 5; data.baseUpgradeGoldCost = 100; data.baseUpgradeSoulCost = 0;
            EditorUtility.SetDirty(data); result[i] = data;
        }
        return result;
    }

    private static TrapDatabase CreateDefaultTrapDatabase(TrapData[] entries)
    {
        const string path = "Assets/Data/TrapDatabase.asset";
        TrapDatabase db = AssetDatabase.LoadAssetAtPath<TrapDatabase>(path);
        if (db == null) { db = ScriptableObject.CreateInstance<TrapDatabase>(); AssetDatabase.CreateAsset(db, path); }
        db.SetEntries(new System.Collections.Generic.List<TrapData>(entries)); EditorUtility.SetDirty(db); return db;
    }

    private static void EnsureDataFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        string path = "Assets/Data/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/Data", name);
    }

    private static void Save(Scene scene, string name)
    {
        EditorSceneManager.SaveScene(scene, SceneDir + name + ".unity");
    }

    private static BossData[] CreateDefaultBossData()
    {
        EnsureResourcesFolder("DreamDungeon");

        var dragonSkill = new BossSkillData
        {
            skillName = "Fire Breath",
            skillType = BossSkillType.FireBreath,
            damageMultiplier = 1.5f,
            cooldown = 8f,
            radius = 3.5f,
            telegraphDuration = 1.2f,
            description = "Breaths scorching flame in a wide cone.",
            telegraphColor = new Color(1f, 0.3f, 0.1f, 0.45f)
        };

        var golemSkill = new BossSkillData
        {
            skillName = "Heavy Smash",
            skillType = BossSkillType.HeavySmash,
            damageMultiplier = 2.0f,
            cooldown = 10f,
            radius = 2.5f,
            telegraphDuration = 1.5f,
            description = "Slams the ground with crushing force.",
            telegraphColor = new Color(0.8f, 0.5f, 0.2f, 0.45f)
        };

        var lordSkill = new BossSkillData
        {
            skillName = "Dark Explosion",
            skillType = BossSkillType.DarkExplosion,
            damageMultiplier = 2.5f,
            cooldown = 12f,
            radius = 4.5f,
            telegraphDuration = 1.8f,
            description = "Unleashes apocalyptic dark energy.",
            telegraphColor = new Color(0.6f, 0.1f, 0.8f, 0.50f)
        };

        var definitions = new (string id, string name, int hp, int atk, int def, float range, float speed, int unlock, BossSkillData skill, Color color, int exp, int gold, int soul)[]
        {
            ("cave_dragon", "Cave Dragon", 1500, 60, 25, 2.5f, 1.0f, 5, dragonSkill, new Color(0.9f, 0.25f, 0.15f), 300, 500, 150),
            ("dark_golem", "Dark Golem", 2200, 85, 45, 1.8f, 0.8f, 7, golemSkill, new Color(0.35f, 0.35f, 0.45f), 300, 800, 250),
            ("demon_lord", "Demon Lord", 3500, 120, 60, 3.0f, 1.2f, 10, lordSkill, new Color(0.75f, 0.15f, 0.35f), 300, 1500, 500)
        };

        var result = new BossData[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            var d = definitions[i];
            string path = $"Assets/Resources/DreamDungeon/{d.id}.asset";
            BossData data = AssetDatabase.LoadAssetAtPath<BossData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BossData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.bossId = d.id;
            data.bossName = d.name;
            data.maxHP = d.hp;
            data.attack = d.atk;
            data.defense = d.def;
            data.attackRange = d.range;
            data.attackSpeed = d.speed;
            data.unlockDungeonLevel = d.unlock;
            data.bossColor = d.color;
            data.defeatExpReward = d.exp;
            data.defeatGoldReward = d.gold;
            data.defeatSoulReward = d.soul;
            data.skills = new System.Collections.Generic.List<BossSkillData> { d.skill };
            EditorUtility.SetDirty(data);
            result[i] = data;
        }
        return result;
    }

    private static BossDatabase CreateDefaultBossDatabase(BossData[] bosses)
    {
        EnsureResourcesFolder("DreamDungeon");
        const string path = "Assets/Resources/DreamDungeon/BossDatabase.asset";
        BossDatabase db = AssetDatabase.LoadAssetAtPath<BossDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<BossDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.bosses = new System.Collections.Generic.List<BossData>(bosses);
        EditorUtility.SetDirty(db);
        return db;
    }

    private static void CreateDefaultLevelData()
    {
        EnsureResourcesFolder("DreamDungeon/Levels");
        int[] expTable = { 100, 250, 450, 700, 1050, 1500, 2100, 2850, 3800, 0 };
        int[] goldTable = { 0, 200, 300, 450, 650, 900, 1200, 1600, 2100, 3000 };
        int[] soulTable = { 0, 50, 75, 100, 150, 200, 250, 350, 500, 800 };
        int[] gridTable = { 9, 9, 11, 11, 13, 13, 15, 15, 17, 17 };

        for (int i = 1; i <= 10; i++)
        {
            string path = $"Assets/Resources/DreamDungeon/Levels/DungeonLevel_{i}.asset";
            DungeonLevelData data = AssetDatabase.LoadAssetAtPath<DungeonLevelData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DungeonLevelData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.level = i;
            data.requiredExp = expTable[i - 1];
            data.rewardGold = goldTable[i - 1];
            data.rewardSoul = soulTable[i - 1];
            data.dungeonGridWidth = gridTable[i - 1];
            data.dungeonGridHeight = gridTable[i - 1];
            data.maxMonsterCapacity = 10 + (i - 1);
            data.maxTrapCapacity = 10 + (i - 1);
            EditorUtility.SetDirty(data);
        }
    }

    private static UpgradeConfiguration CreateDefaultUpgradeConfiguration()
    {
        EnsureResourcesFolder("DreamDungeon");
        const string path = "Assets/Resources/DreamDungeon/UpgradeConfiguration.asset";
        UpgradeConfiguration config = AssetDatabase.LoadAssetAtPath<UpgradeConfiguration>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<UpgradeConfiguration>();
            AssetDatabase.CreateAsset(config, path);
        }
        EditorUtility.SetDirty(config);
        return config;
    }

    private static void EnsureResourcesFolder(string subfolder)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        string[] parts = subfolder.Split('/');
        string cur = "Assets/Resources";
        foreach (string part in parts)
        {
            string next = cur + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, part);
            cur = next;
        }
    }
}
