using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamDungeon
{
    public sealed class BattleManager : MonoBehaviour
    {
        [SerializeField] private AdventurerDatabase adventurerDatabase;
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private TrapDatabase trapDatabase;
        [SerializeField] private BattleConfiguration configuration;
        [SerializeField, Min(0.1f)] private float cellSize = 0.62f;
        [SerializeField] private Vector2 worldCenter = new Vector2(0f, 0.45f);
        [SerializeField] private BossDatabase bossDatabase;

        private readonly List<MonsterBattleActor> monsters = new List<MonsterBattleActor>();
        private readonly List<BossBattleActor> bosses = new List<BossBattleActor>();
        private readonly List<GameObject> worldTiles = new List<GameObject>();
        private readonly List<ICombatTarget> monsterCombatTargets = new List<ICombatTarget>();
        private int bossesDefeatedCount = 0;

        private WaveManager waveManager;
        private TrapBattleController trapController;
        private BattleSpeedController speedController;
        private DungeonCoreActor coreActor;
        private BattleHUD hud;
        private BattleDungeonRenderer dungeonRenderer;
        private BattleCameraController cameraController;

        private DungeonLayoutData layout;
        private List<Vector2Int> route;
        private BattleState state = BattleState.Preparing;
        private Sprite whiteSprite;
        private Camera battleCamera;
        private bool initialized;
        private string statusMessage = "Preparing raid…";
        private AdventurerFactionData activeFaction;
        private DifficultyData activeDifficulty;
        private CampaignStageData activeCampaignStage;
        private ChallengeData activeChallenge;
        private float runElapsedTime;
        private int liveScore;

        public BattleState State => state;
        public AdventurerFactionData ActiveFaction => activeFaction;
        public DifficultyData ActiveDifficulty => activeDifficulty;
        public CampaignStageData ActiveCampaignStage => activeCampaignStage;
        public ChallengeData ActiveChallenge => activeChallenge;
        public float RunElapsedTime => runElapsedTime;
        public int LiveScore => liveScore;
        public AdventurerActor ActiveWaveBoss => waveManager != null ? waveManager.ActiveBossWaveActor : null;
        public int CurrentWave => waveManager != null ? waveManager.CurrentWave : 1;
        public int TotalWaves => waveManager != null ? waveManager.TotalWaves : (configuration != null ? configuration.waves.Count : 0);
        public int LivingAdventurers => waveManager != null ? waveManager.LivingCount : 0;
        public int CoreHP => coreActor != null ? coreActor.CurrentHP : 0;
        public int CoreMaxHP => coreActor != null ? coreActor.MaxHP : (configuration != null ? configuration.coreMaxHP : 500);
        public float Speed => speedController != null ? speedController.Speed : 1f;
        public string Status => statusMessage;
        public IReadOnlyList<BossBattleActor> Bosses => bosses;

        public event Action Changed;

        public void Configure(AdventurerDatabase adventurers, MonsterDatabase monstersDb, TrapDatabase trapsDb, BattleConfiguration config, BossDatabase bossesDb = null)
        {
            adventurerDatabase = adventurers;
            monsterDatabase = monstersDb;
            trapDatabase = trapsDb;
            configuration = config;
            bossDatabase = bossesDb;
        }

        private void Awake()
        {
            hud = FindAnyObjectByType<BattleHUD>();
            battleCamera = Camera.main;
            if (battleCamera != null)
            {
                battleCamera.orthographic = true;
                battleCamera.transform.position = new Vector3(worldCenter.x, worldCenter.y, -10f);
                cameraController = battleCamera.GetComponent<BattleCameraController>() ?? battleCamera.gameObject.AddComponent<BattleCameraController>();
                cameraController.Configure(9, 9, cellSize, worldCenter);
            }

            dungeonRenderer = gameObject.GetComponent<BattleDungeonRenderer>() ?? gameObject.AddComponent<BattleDungeonRenderer>();

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            speedController = gameObject.GetComponent<BattleSpeedController>() ?? gameObject.AddComponent<BattleSpeedController>();
            speedController.SpeedChanged += _ => Notify();
            speedController.PauseChanged += p => { statusMessage = p ? "Paused" : "Raid in progress."; Notify(); };

            trapController = gameObject.GetComponent<TrapBattleController>() ?? gameObject.AddComponent<TrapBattleController>();
            waveManager = gameObject.GetComponent<WaveManager>() ?? gameObject.AddComponent<WaveManager>();
        }

        private void Start()
        {
            if (hud != null) hud.Bind(this);

            if (!BattleSession.IsReady)
            {
                Fail("No Dungeon Layout was supplied. Start the raid from Dungeon Editor.");
                return;
            }

            // Fallback load databases from Resources if unassigned in scene
            if (adventurerDatabase == null) adventurerDatabase = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase");
            if (configuration == null) configuration = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration");
            if (monsterDatabase == null) monsterDatabase = Resources.Load<MonsterDatabase>("DreamDungeon/MonsterDatabase") ?? Resources.Load<MonsterDatabase>("MonsterDatabase");
            if (trapDatabase == null) trapDatabase = Resources.Load<TrapDatabase>("DreamDungeon/TrapDatabase") ?? Resources.Load<TrapDatabase>("TrapDatabase");
            if (bossDatabase == null) bossDatabase = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");

            string error;
            if (configuration == null) { Fail("Battle Configuration is missing."); return; }
            if (!configuration.Validate(adventurerDatabase, out error)) { Fail(error); return; }
            if (monsterDatabase == null || !monsterDatabase.ValidateDatabase(out error)) { Fail(monsterDatabase == null ? "Monster Database is missing." : error); return; }
            if (trapDatabase == null || !trapDatabase.ValidateDatabase(out error)) { Fail(trapDatabase == null ? "Trap Database is missing." : error); return; }

            ApplyCampaignRuntimeOverridesIfNeeded();
            ApplyEndlessOrChallengeRuntimeOverridesIfNeeded();

            try
            {
                layout = DungeonLayoutData.FromJson(BattleSession.LayoutJson);
            }
            catch (Exception ex)
            {
                Fail("Dungeon Layout could not be loaded: " + ex.Message);
                return;
            }

            var pathfinder = new DungeonPathfinder();
            if (!ValidateInstances(out error) || !pathfinder.TryFindRoute(layout, out route))
            {
                Fail(string.IsNullOrEmpty(error) ? "No walkable route connects Entrance to Dungeon Core." : error);
                return;
            }

            InitializeBattleWorld();
            HookEvents();

            state = BattleState.InProgress;
            statusMessage = "Raid started.";
            initialized = true;
            waveManager.StartWaves();
            Notify();
        }

        private bool ValidateInstances(out string error)
        {
            var ids = new HashSet<string>();
            if (layout.rooms == null ||
                layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.Entrance).Count != 1 ||
                layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.DungeonCore).Count != 1)
            {
                error = "Dungeon must contain exactly one Entrance and one Core.";
                return false;
            }

            if (layout.monsters != null)
            {
                foreach (MonsterInstanceData item in layout.monsters)
                {
                    MonsterData d = item != null ? monsterDatabase.FindById(item.monsterId) : null;
                    DungeonRoomInstanceData room = item != null ? layout.rooms.Find(r => r.instanceId == item.roomInstanceId) : null;
                    if (item == null || string.IsNullOrWhiteSpace(item.instanceId) || !ids.Add(item.instanceId) ||
                        d == null || item.level < 1 || item.level > d.maxLevel ||
                        room == null || room.roomType != DungeonRoomType.MonsterRoom || !ValidLocal(item.localPosition))
                    {
                        error = "Dungeon contains an invalid or duplicate Monster instance.";
                        return false;
                    }
                }
            }

            if (layout.traps != null)
            {
                foreach (TrapInstanceData item in layout.traps)
                {
                    TrapData d = item != null ? trapDatabase.FindById(item.trapId) : null;
                    DungeonRoomInstanceData room = item != null ? layout.rooms.Find(r => r.instanceId == item.roomInstanceId) : null;
                    if (item == null || string.IsNullOrWhiteSpace(item.instanceId) || !ids.Add(item.instanceId) ||
                        d == null || item.level < 1 || item.level > d.maxLevel ||
                        room == null || room.roomType != DungeonRoomType.TrapRoom || !ValidLocal(item.localPosition))
                    {
                        error = "Dungeon contains an invalid or duplicate Trap instance.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private void InitializeBattleWorld()
        {
            // 1. Render Full Dungeon (Grid, Bedrock Walls, Floor, Room Outlines) via BattleDungeonRenderer
            if (dungeonRenderer == null) dungeonRenderer = gameObject.GetComponent<BattleDungeonRenderer>() ?? gameObject.AddComponent<BattleDungeonRenderer>();
            dungeonRenderer.Render(layout, cellSize, worldCenter, battleCamera);

            // 2. Adjust Camera to Full Dungeon View
            if (cameraController == null && battleCamera != null)
                cameraController = battleCamera.GetComponent<BattleCameraController>() ?? battleCamera.gameObject.AddComponent<BattleCameraController>();
            if (cameraController != null)
            {
                cameraController.Configure(layout.gridWidth, layout.gridHeight, cellSize, worldCenter);
                cameraController.SetMode(BattleCameraMode.FullDungeonView);
            }

            // 3. Spawn Dungeon Core
            DungeonRoomInstanceData coreRoom = layout.rooms.Find(r => r.roomType == DungeonRoomType.DungeonCore);
            Vector3 corePos = CellWorld(coreRoom.position);
            GameObject coreObj = new GameObject("DungeonCoreActor");
            coreObj.transform.position = corePos;
            coreActor = coreObj.AddComponent<DungeonCoreActor>();
            int coreHP = configuration.coreMaxHP;
            int coreDef = configuration.coreDefense;
            if (DungeonCoreUpgradeManager.Instance != null)
            {
                coreHP = DungeonCoreUpgradeManager.Instance.MaxHP;
                coreDef = DungeonCoreUpgradeManager.Instance.Defense;
            }
            if (BattleSession.IsChallengeBattle && EndlessChallengeManager.Instance != null &&
                EndlessChallengeManager.Instance.ActiveModifiers.IsActive)
            {
                coreHP = Mathf.Max(20, Mathf.RoundToInt(coreHP * EndlessChallengeManager.Instance.ActiveModifiers.CoreHpMultiplier));
            }
            coreActor.Initialize(coreHP, coreDef, coreRoom.upgradeLevel, whiteSprite, cellSize);
            worldTiles.Add(coreObj);

            // 4. Spawn Monster Defenders
            monsters.Clear();
            monsterCombatTargets.Clear();
            int maxMonstersCap = -1;
            if (BattleSession.IsChallengeBattle && EndlessChallengeManager.Instance != null &&
                EndlessChallengeManager.Instance.ActiveModifiers.IsActive)
            {
                maxMonstersCap = EndlessChallengeManager.Instance.ActiveModifiers.MaxAllowedMonsters;
            }
            if (layout.monsters != null)
            {
                foreach (MonsterInstanceData data in layout.monsters)
                {
                    if (maxMonstersCap >= 0 && monsters.Count >= maxMonstersCap) break;
                    MonsterData def = monsterDatabase.FindById(data.monsterId);
                    DungeonRoomInstanceData room = layout.rooms.Find(r => r.instanceId == data.roomInstanceId);
                    if (def == null || room == null) continue;

                    Vector3 pos = UnitWorld(room, data.localPosition, -0.15f);

                    GameObject go = def.monsterPrefab != null ? Instantiate(def.monsterPrefab, pos, Quaternion.identity) : new GameObject($"Monster_{def.monsterId}");
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * (cellSize * 0.45f);

                    MonsterBattleActor actor = go.GetComponent<MonsterBattleActor>() ?? go.AddComponent<MonsterBattleActor>();
                    actor.Initialize(def, data.level, data.instanceId, whiteSprite);
                    actor.Died += OnMonsterDied;

                    monsters.Add(actor);
                    monsterCombatTargets.Add(actor);
                    worldTiles.Add(go);
                }
            }

            // 4b. Spawn Boss Defenders
            bosses.Clear();
            bossesDefeatedCount = 0;
            if (layout.bosses != null)
            {
                foreach (BossInstanceData data in layout.bosses)
                {
                    BossData def = bossDatabase != null ? bossDatabase.GetBoss(data.bossId) : null;
                    if (def == null) def = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase")?.GetBoss(data.bossId);
                    DungeonRoomInstanceData room = layout.rooms.Find(r => r.instanceId == data.roomInstanceId);
                    if (room == null) continue;

                    Vector3 pos = UnitWorld(room, data.localPosition, -0.2f);
                    GameObject go = def != null && def.bossPrefab != null
                        ? Instantiate(def.bossPrefab, pos, Quaternion.identity)
                        : new GameObject($"Boss_{data.bossId}");
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * (cellSize * 0.7f);

                    BossBattleActor actor = go.GetComponent<BossBattleActor>() ?? go.AddComponent<BossBattleActor>();
                    actor.Initialize(def, data.level, data.instanceId, whiteSprite);
                    actor.Died += OnBossDied;

                    bosses.Add(actor);
                    monsterCombatTargets.Add(actor);
                    worldTiles.Add(go);
                }
            }

            // 5. Register Traps
            trapController.ClearTraps();
            int maxTrapsCap = -1;
            if (BattleSession.IsChallengeBattle && EndlessChallengeManager.Instance != null &&
                EndlessChallengeManager.Instance.ActiveModifiers.IsActive)
            {
                maxTrapsCap = EndlessChallengeManager.Instance.ActiveModifiers.MaxAllowedTraps;
            }
            int trapsRegistered = 0;
            if (layout.traps != null)
            {
                foreach (TrapInstanceData data in layout.traps)
                {
                    if (maxTrapsCap >= 0 && trapsRegistered >= maxTrapsCap) break;
                    TrapData def = trapDatabase.FindById(data.trapId);
                    DungeonRoomInstanceData room = layout.rooms.Find(r => r.instanceId == data.roomInstanceId);
                    if (def == null || room == null) continue;

                    Vector3 pos = UnitWorld(room, data.localPosition, -0.25f);

                    SpawnTile(pos, TrapBattleController.GetTrapColor(def.trapType), cellSize * 0.22f, 2, def.trapName);
                    trapController.RegisterTrap(data, def, pos);
                    trapsRegistered++;
                }
            }

            // 6. Configure WaveManager
            DungeonRoomInstanceData entrance = layout.rooms.Find(r => r.roomType == DungeonRoomType.Entrance);
            Vector3 entranceWorld = CellWorld(entrance.position);
            waveManager.Configure(configuration, adventurerDatabase, route, CellWorld, entranceWorld, cellSize, whiteSprite);
            if (BattleSession.IsEndlessBattle)
            {
                EndlessChallengeManager ecMgr = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
                waveManager.EnableEndlessMode(ecMgr.EndlessConfig);
            }
            runElapsedTime = 0f;
            liveScore = 0;
        }

        private void HookEvents()
        {
            if (coreActor != null)
            {
                coreActor.HPChanged += (hp, max) => Notify();
                coreActor.CoreDestroyed += OnCoreDestroyed;
            }

            if (waveManager != null)
            {
                waveManager.WaveStarted += wave => { statusMessage = $"Wave {wave} incoming!"; RefreshLiveScore(); Notify(); };
                waveManager.WaveCleared += wave => { statusMessage = $"Wave {wave} cleared!"; RefreshLiveScore(); Notify(); };
                waveManager.BossWaveTriggered += (wave, bossTitle) => { statusMessage = $"WARNING! BOSS WAVE {wave}: {bossTitle}!"; Notify(); };
                waveManager.AllWavesCleared += OnAllWavesCleared;
                waveManager.AdventurerDied += _ => { RefreshLiveScore(); Notify(); };
                waveManager.AdventurerSpawned += OnAdventurerSpawned;
            }
        }

        private void OnAdventurerSpawned(AdventurerActor actor)
        {
            if (actor != null && activeFaction != null)
            {
                actor.ApplyFactionPassive(activeFaction);
            }
        }

        private void ApplyCampaignRuntimeOverridesIfNeeded()
        {
            if (!BattleSession.IsCampaignBattle) return;

            CampaignManager campaignMgr = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>();
            if (campaignMgr == null)
            {
                campaignMgr = gameObject.GetComponent<CampaignManager>() ?? gameObject.AddComponent<CampaignManager>();
            }
            campaignMgr.EnsureInitialized();

            DifficultyManager diffMgr = DifficultyManager.Instance ?? FindAnyObjectByType<DifficultyManager>();
            if (diffMgr == null)
            {
                diffMgr = gameObject.GetComponent<DifficultyManager>() ?? gameObject.AddComponent<DifficultyManager>();
            }
            diffMgr.EnsureDefaultDifficulties();

            activeCampaignStage = campaignMgr.GetStage(BattleSession.StageId);
            activeDifficulty = diffMgr.GetDifficultyData(BattleSession.Difficulty);
            activeFaction = !string.IsNullOrWhiteSpace(BattleSession.FactionId)
                ? AdventurerFactionData.FindFaction(BattleSession.FactionId)
                : campaignMgr.GetPrimaryFactionForStage(activeCampaignStage);

            // Build a non-destructive runtime copy of AdventurerDatabase scaled by Difficulty and Faction passive
            AdventurerDatabase baseAdvDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase") ?? adventurerDatabase;
            if (baseAdvDb != null && activeDifficulty != null)
            {
                AdventurerDatabase runtimeAdvDb = ScriptableObject.CreateInstance<AdventurerDatabase>();
                runtimeAdvDb.name = "RuntimeAdventurerDatabase_" + activeDifficulty.difficultyId;
                var scaledEntries = new List<AdventurerData>();

                float fHp = activeFaction?.factionPassiveBonus != null ? activeFaction.factionPassiveBonus.healthMultiplier : 1f;
                float fAtk = activeFaction?.factionPassiveBonus != null ? activeFaction.factionPassiveBonus.attackMultiplier : 1f;
                float fDef = activeFaction?.factionPassiveBonus != null ? activeFaction.factionPassiveBonus.defenseMultiplier : 1f;
                float fSpd = activeFaction?.factionPassiveBonus != null ? activeFaction.factionPassiveBonus.attackSpeedMultiplier : 1f;
                float fMov = activeFaction?.factionPassiveBonus != null ? activeFaction.factionPassiveBonus.moveSpeedMultiplier : 1f;

                foreach (AdventurerData orig in baseAdvDb.Entries)
                {
                    if (orig == null) continue;
                    scaledEntries.Add(activeDifficulty.CreateRuntimeScaledCopy(orig, fHp, fAtk, fDef, fSpd, fMov));
                }
                runtimeAdvDb.SetEntries(scaledEntries);
                adventurerDatabase = runtimeAdvDb;
            }

            // Build a runtime BattleConfiguration from the stage's WaveConfiguration list
            BattleConfiguration baseCfg = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration") ?? configuration;
            IReadOnlyList<WaveConfiguration> stageWaves = activeCampaignStage != null ? activeCampaignStage.waveConfiguration : null;
            configuration = WaveConfiguration.BuildRuntimeBattleConfiguration(stageWaves, activeDifficulty, activeFaction, baseCfg);
        }

        private void ApplyEndlessOrChallengeRuntimeOverridesIfNeeded()
        {
            if (!BattleSession.IsEndlessBattle && !BattleSession.IsChallengeBattle) return;

            EndlessChallengeManager ecMgr = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>();
            if (ecMgr == null)
            {
                ecMgr = gameObject.GetComponent<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
            }
            ecMgr.EnsureInitialized();

            // Always start from clean base AdventurerDatabase so retries never stack multipliers
            AdventurerDatabase baseAdvDb = Resources.Load<AdventurerDatabase>("DreamDungeon/AdventurerDatabase") ?? adventurerDatabase;
            if (baseAdvDb != null)
            {
                AdventurerDatabase runtimeAdvDb = ScriptableObject.CreateInstance<AdventurerDatabase>();
                runtimeAdvDb.name = "RuntimeAdventurerDatabase_M9";
                var cleanEntries = new List<AdventurerData>();
                foreach (AdventurerData orig in baseAdvDb.Entries)
                {
                    if (orig != null) cleanEntries.Add(orig);
                }
                runtimeAdvDb.SetEntries(cleanEntries);
                adventurerDatabase = runtimeAdvDb;
            }

            BattleConfiguration baseCfg = Resources.Load<BattleConfiguration>("DreamDungeon/BattleConfiguration") ?? configuration;

            if (BattleSession.IsEndlessBattle)
            {
                activeChallenge = null;
                ecMgr.CleanUpTemporaryRunState();
                BattleConfiguration runtimeCfg = ScriptableObject.CreateInstance<BattleConfiguration>();
                runtimeCfg.name = "RuntimeEndlessBattleConfiguration";
                runtimeCfg.spawnInterval = ecMgr.EndlessConfig.baseSpawnInterval;
                runtimeCfg.coreMaxHP = baseCfg != null ? baseCfg.coreMaxHP : 500;
                runtimeCfg.coreDefense = baseCfg != null ? baseCfg.coreDefense : 1;
                runtimeCfg.waves = new List<AdventurerWave>();
                configuration = runtimeCfg;
                return;
            }

            if (BattleSession.IsChallengeBattle)
            {
                activeChallenge = ecMgr.GetChallenge(BattleSession.ChallengeId);
                if (activeChallenge != null)
                {
                    ecMgr.ActiveModifiers.ApplyModifiers(activeChallenge.modifiers);
                    DifficultyManager diffMgr = DifficultyManager.Instance ?? FindAnyObjectByType<DifficultyManager>() ?? gameObject.AddComponent<DifficultyManager>();
                    diffMgr.EnsureDefaultDifficulties();
                    activeDifficulty = diffMgr.GetDifficultyData(activeChallenge.difficulty);

                    BattleConfiguration runtimeCfg = WaveConfiguration.BuildRuntimeBattleConfiguration(
                        activeChallenge.waveConfiguration, activeDifficulty, null, baseCfg);

                    // Apply Challenge EnemyCountMultiplier if > 1
                    float countMult = ecMgr.ActiveModifiers.EnemyCountMultiplier;
                    if (countMult > 1.01f && runtimeCfg.waves != null)
                    {
                        foreach (AdventurerWave w in runtimeCfg.waves)
                        {
                            if (w?.entries == null) continue;
                            foreach (AdventurerWaveEntry e in w.entries)
                            {
                                if (e != null)
                                {
                                    e.count = Mathf.Clamp(Mathf.RoundToInt(e.count * countMult), 1, 40);
                                }
                            }
                        }
                    }

                    configuration = runtimeCfg;
                }
            }
        }

        private void RefreshLiveScore()
        {
            if (waveManager == null) return;
            EndlessChallengeManager ecMgr = EndlessChallengeManager.Instance;
            ScoreCalculationConfig sCfg = ecMgr != null ? ecMgr.ScoreConfig : null;
            int completedWaves = Mathf.Max(0, CurrentWave - 1);
            int elites = waveManager.EliteDefeatedCount;
            int normals = Mathf.Max(0, waveManager.DefeatedCount - elites);
            int totalBosses = bossesDefeatedCount + waveManager.BossUnitDefeatedCount;
            var breakdown = ScoreCalculator.Calculate(sCfg, completedWaves, CurrentWave, normals, elites, totalBosses, CoreHP, CoreMaxHP, runElapsedTime, false);
            liveScore = breakdown.TotalScore;
        }

        private void Update()
        {
            if (!initialized || state != BattleState.InProgress || Time.timeScale <= 0f) return;

            float dt = Time.deltaTime;
            runElapsedTime += dt;

            // Optional Challenge Time Limit check
            if (BattleSession.IsChallengeBattle && activeChallenge != null &&
                activeChallenge.optionalTimeLimitSeconds > 0f &&
                runElapsedTime >= activeChallenge.optionalTimeLimitSeconds)
            {
                statusMessage = "Time limit expired!";
                Finish(false);
                return;
            }

            // Tick wave manager (spawning and adventurer update loops)
            waveManager.Tick(dt, monsterCombatTargets, coreActor);

            // Tick monster defenders
            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterBattleActor monster = monsters[i];
                if (monster != null && monster.IsAlive)
                {
                    monster.Tick(dt, waveManager.AllAdventurers);
                }
            }

            // Tick boss defenders
            for (int i = 0; i < bosses.Count; i++)
            {
                BossBattleActor boss = bosses[i];
                if (boss != null && boss.IsAlive)
                {
                    boss.Tick(dt, waveManager.AllAdventurers);
                }
            }

            // Tick traps
            trapController.TickTraps(dt, waveManager.AllAdventurers);

            if (Time.frameCount % 6 == 0)
            {
                Notify();
            }
        }

        private void OnMonsterDied(MonsterBattleActor actor)
        {
            monsterCombatTargets.Remove(actor);
            Notify();
        }

        private void OnBossDied(BossBattleActor actor)
        {
            monsterCombatTargets.Remove(actor);
            bossesDefeatedCount++;
            Notify();
        }

        private void OnCoreDestroyed()
        {
            Finish(false);
        }

        private void OnAllWavesCleared()
        {
            Finish(true);
        }

        private void Finish(bool victory)
        {
            if (state == BattleState.Victory || state == BattleState.Defeat || state == BattleState.Finished) return;

            state = victory ? BattleState.Victory : BattleState.Defeat;
            speedController.ResetSpeed();

            int completedWaves = victory ? TotalWaves : Mathf.Max(0, CurrentWave - 1);
            int totalBossesKilled = bossesDefeatedCount + (waveManager != null ? waveManager.BossUnitDefeatedCount : 0);
            int elitesKilled = waveManager != null ? waveManager.EliteDefeatedCount : 0;
            int normalsKilled = waveManager != null ? Mathf.Max(0, waveManager.DefeatedCount - elitesKilled) : 0;

            if (BattleSession.IsEndlessBattle)
            {
                EndlessChallengeManager ecMgr = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
                int endlessWavesCleared = Mathf.Max(0, CurrentWave - 1);
                EndlessRunOutcome outcome = ecMgr.RecordEndlessRunOutcome(
                    BattleSession.CurrentBattleResultId,
                    CurrentWave,
                    endlessWavesCleared,
                    normalsKilled,
                    elitesKilled,
                    totalBossesKilled,
                    CoreHP,
                    CoreMaxHP,
                    runElapsedTime,
                    grantImmediateCurrency: false);

                liveScore = outcome.ScoreBreakdown.TotalScore;
                BattleSession.SetResult(
                    endlessWavesCleared > 0,
                    CurrentWave,
                    waveManager.DefeatedCount,
                    outcome.RewardGold,
                    outcome.RewardSoul,
                    outcome.RewardExp,
                    isFirstClear: false,
                    isPerfectClear: false,
                    score: outcome.ScoreBreakdown.TotalScore,
                    elitesDefeated: elitesKilled,
                    bossesDefeated: totalBossesKilled,
                    durationSeconds: runElapsedTime,
                    isNewRecord: outcome.IsNewWaveRecord || outcome.IsNewScoreRecord);

                DungeonProgressionManager.Instance?.RecordRaidResult(endlessWavesCleared > 0, CurrentWave, waveManager.DefeatedCount, totalBossesKilled);
                statusMessage = $"Endless Run Ended on Wave {CurrentWave}! Score: {outcome.ScoreBreakdown.TotalScore}";
                Notify();

                hud?.ShowResult(false, CurrentWave, waveManager.DefeatedCount, CoreHP,
                    outcome.RewardGold, outcome.RewardSoul, outcome.RewardExp);
                return;
            }

            if (BattleSession.IsChallengeBattle)
            {
                EndlessChallengeManager ecMgr = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
                ChallengeRunOutcome outcome = ecMgr.RecordChallengeRunOutcome(
                    BattleSession.CurrentBattleResultId,
                    BattleSession.ChallengeId,
                    victory,
                    completedWaves,
                    TotalWaves,
                    normalsKilled,
                    elitesKilled,
                    totalBossesKilled,
                    CoreHP,
                    CoreMaxHP,
                    runElapsedTime,
                    grantImmediateCurrency: false);

                liveScore = outcome.ScoreBreakdown.TotalScore;
                BattleSession.SetResult(
                    outcome.Completed,
                    CurrentWave,
                    waveManager.DefeatedCount,
                    outcome.RewardGold,
                    outcome.RewardSoul,
                    outcome.RewardExp,
                    isFirstClear: outcome.IsFirstClear,
                    isPerfectClear: false,
                    score: outcome.ScoreBreakdown.TotalScore,
                    elitesDefeated: elitesKilled,
                    bossesDefeated: totalBossesKilled,
                    durationSeconds: runElapsedTime,
                    isNewRecord: outcome.IsNewScoreRecord || outcome.IsNewTimeRecord);

                DungeonProgressionManager.Instance?.RecordRaidResult(outcome.Completed, CurrentWave, waveManager.DefeatedCount, totalBossesKilled);
                statusMessage = outcome.Completed ? "Challenge Completed!" : "Challenge Failed.";
                Notify();

                hud?.ShowResult(outcome.Completed, CurrentWave, waveManager.DefeatedCount, CoreHP,
                    outcome.RewardGold, outcome.RewardSoul, outcome.RewardExp);
                return;
            }

            if (BattleSession.IsCampaignBattle)
            {
                CampaignManager campaignMgr = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>() ?? gameObject.AddComponent<CampaignManager>();
                CampaignRewardBreakdown breakdown = campaignMgr.RecordCampaignBattleOutcome(
                    BattleSession.CurrentBattleResultId,
                    BattleSession.StageId,
                    BattleSession.Difficulty,
                    victory,
                    completedWaves,
                    TotalWaves,
                    waveManager.DefeatedCount,
                    bossesDefeatedCount,
                    CoreHP,
                    CoreMaxHP,
                    grantImmediateCurrency: false);

                BattleSession.SetResult(victory, CurrentWave, waveManager.DefeatedCount,
                    breakdown.TotalGold, breakdown.TotalSoul, breakdown.TotalEXP,
                    breakdown.IsFirstClear, breakdown.IsPerfectClear);

                int bossDefeatedForProg = (!string.IsNullOrWhiteSpace(BattleSession.StageBossId) && victory)
                    ? Mathf.Max(1, bossesDefeatedCount)
                    : bossesDefeatedCount;
                DungeonProgressionManager.Instance?.RecordRaidResult(victory, CurrentWave, waveManager.DefeatedCount, bossDefeatedForProg);

                statusMessage = victory ? "Campaign Stage Cleared. Victory!" : "Dungeon Core destroyed. Defeat.";
                Notify();

                hud?.ShowResult(victory, CurrentWave, waveManager.DefeatedCount, CoreHP,
                    breakdown.TotalGold, breakdown.TotalSoul, breakdown.TotalEXP);
                return;
            }

            float goldMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetGoldMultiplier() : 1.0f;
            float soulMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetSoulMultiplier() : 1.0f;

            BattleRewardResult reward = BattleRewardCalculator.Calculate(victory, CurrentWave, waveManager.DefeatedCount, configuration, goldMult, soulMult, bossesDefeatedCount);
            BattleSession.SetResult(victory, CurrentWave, waveManager.DefeatedCount, reward.Gold, reward.Soul, reward.Exp);

            DungeonProgressionManager.Instance?.RecordRaidResult(victory, CurrentWave, waveManager.DefeatedCount, bossesDefeatedCount);
            DungeonProgressionManager.Instance?.AddExp(reward.Exp);

            QuestManager.Instance?.NotifyBattleResult(BattleSession.CurrentBattleResultId, victory, completedWaves, waveManager.DefeatedCount, bossesDefeatedCount);
            AchievementManager.Instance?.NotifyBattleResult(BattleSession.CurrentBattleResultId, victory, completedWaves, waveManager.DefeatedCount, bossesDefeatedCount);

            statusMessage = victory ? "All waves defeated. Victory!" : "Dungeon Core destroyed. Defeat.";
            Notify();

            hud?.ShowResult(victory, CurrentWave, waveManager.DefeatedCount, CoreHP, reward.Gold, reward.Soul, reward.Exp);
        }

        public void SetPaused(bool paused)
        {
            if (state != BattleState.InProgress) return;
            speedController.SetPaused(paused);
        }

        public void CycleSpeed()
        {
            speedController.CycleSpeed();
        }

        public void Retry()
        {
            if (state != BattleState.Victory && state != BattleState.Defeat) return;
            ResetBattleState();
            if (BattleSession.IsEndlessBattle)
            {
                BattleSession.BeginEndless(BattleSession.LayoutJson, BattleSession.DungeonScene, BattleSession.Gold, BattleSession.Soul, BattleSession.Crystal);
            }
            else if (BattleSession.IsChallengeBattle)
            {
                BattleSession.BeginChallenge(BattleSession.LayoutJson, BattleSession.DungeonScene, BattleSession.Gold, BattleSession.Soul, BattleSession.Crystal, BattleSession.ChallengeId);
            }
            else if (BattleSession.IsCampaignBattle)
            {
                BattleSession.BeginCampaign(
                    BattleSession.LayoutJson,
                    BattleSession.DungeonScene,
                    BattleSession.Gold,
                    BattleSession.Soul,
                    BattleSession.Crystal,
                    BattleSession.RegionId,
                    BattleSession.CampaignId,
                    BattleSession.StageId,
                    BattleSession.Difficulty,
                    BattleSession.FactionId,
                    BattleSession.StageBossId);
            }
            else
            {
                BattleSession.Begin(BattleSession.LayoutJson, BattleSession.DungeonScene, BattleSession.Gold, BattleSession.Soul, BattleSession.Crystal);
            }
            SceneManager.LoadScene("Battle");
        }

        public void ReturnToEndlessMenu()
        {
            if (state != BattleState.Victory && state != BattleState.Defeat) return;
            ResetBattleState();
            BattleSession.RequestReturnToEndlessMenu();
            SceneManager.LoadScene(BattleSession.DungeonScene);
        }

        public void ReturnToChallengeMenu()
        {
            if (state != BattleState.Victory && state != BattleState.Defeat) return;
            ResetBattleState();
            BattleSession.RequestReturnToChallengeMenu();
            SceneManager.LoadScene(BattleSession.DungeonScene);
        }

        public void ContinueToDungeon()
        {
            if (state != BattleState.Victory && state != BattleState.Defeat) return;
            ResetBattleState();
            BattleSession.RequestReturn();
            SceneManager.LoadScene(BattleSession.DungeonScene);
        }

        public void ReturnToWorldMap(string nextStageId = null)
        {
            if (state != BattleState.Victory && state != BattleState.Defeat) return;
            ResetBattleState();
            BattleSession.RequestReturnToWorldMap(nextStageId);
            SceneManager.LoadScene(BattleSession.DungeonScene);
        }

        public void ResetBattleState()
        {
            StopAllCoroutines();
            if (speedController != null) speedController.ResetSpeed();
            if (waveManager != null)
            {
                waveManager.StopAllCoroutines();
                waveManager.CleanUp();
            }
            if (trapController != null)
            {
                trapController.StopAllCoroutines();
                trapController.ClearTraps();
            }
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] != null)
                {
                    monsters[i].Died -= OnMonsterDied;
                    monsters[i].CancelInvoke();
                    monsters[i].StopAllCoroutines();
                }
            }
            monsters.Clear();
            for (int i = 0; i < bosses.Count; i++)
            {
                if (bosses[i] != null)
                {
                    bosses[i].Died -= OnBossDied;
                    bosses[i].CancelInvoke();
                    bosses[i].StopAllCoroutines();
                }
            }
            bosses.Clear();
            monsterCombatTargets.Clear();
            if (coreActor != null)
            {
                coreActor.CancelInvoke();
                coreActor.StopAllCoroutines();
            }
            for (int i = 0; i < worldTiles.Count; i++)
            {
                if (worldTiles[i] != null) Destroy(worldTiles[i]);
            }
            worldTiles.Clear();
            dungeonRenderer?.Clear();
            bossesDefeatedCount = 0;
            initialized = false;
            state = BattleState.Preparing;
            statusMessage = "Idle";
        }

        private void SpawnTile(Vector3 pos, Color color, float size, int order, string label = null)
        {
            GameObject go = new GameObject(label ?? "BattleCell");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.color = color;
            sr.sortingOrder = order;
            worldTiles.Add(go);
        }

        private Vector3 CellWorld(Vector2Int cell) =>
            dungeonRenderer != null ? dungeonRenderer.CellToWorld(cell) :
            new Vector3(worldCenter.x - (layout.gridWidth - 1) * cellSize * 0.5f + cell.x * cellSize,
                        worldCenter.y - (layout.gridHeight - 1) * cellSize * 0.5f + cell.y * cellSize, 0f);

        private Vector3 UnitWorld(DungeonRoomInstanceData room, Vector2 local, float z) =>
            dungeonRenderer != null ? dungeonRenderer.GetUnitWorldPosition(room, local, z) :
            CellWorld(room.position) + new Vector3(local.x * cellSize, local.y * cellSize, z);

        private static bool ValidLocal(Vector2 pos) =>
            pos.x >= -0.5f && pos.x <= 0.5f && pos.y >= -0.5f && pos.y <= 0.5f;

        private void Fail(string message)
        {
            state = BattleState.Defeat;
            statusMessage = message;
            Debug.LogError("Battle initialization failed: " + message, this);
            hud?.ShowError(message);
            Notify();
        }

        private void Notify()
        {
            Changed?.Invoke();
            hud?.Refresh();
        }

        private void OnDestroy()
        {
            if (speedController != null) speedController.ResetSpeed();
            if (whiteSprite != null)
            {
                if (whiteSprite.texture != null) Destroy(whiteSprite.texture);
                Destroy(whiteSprite);
            }
        }
    }
}
