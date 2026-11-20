using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class WaveManager : MonoBehaviour
    {
        private BattleConfiguration configuration;
        private AdventurerDatabase adventurerDatabase;
        private List<Vector2Int> route;
        private Func<Vector2Int, Vector3> worldConverter;
        private Sprite whiteSprite;
        private float cellSize = 0.62f;
        private Vector3 entranceWorldPos;

        private readonly List<AdventurerActor> allAdventurers = new List<AdventurerActor>();
        private int currentWaveIndex;
        private int entryIndex;
        private int entrySpawnedCount;
        private float spawnTimer;
        private int defeatedCount;
        private bool isSpawningActive;

        public int CurrentWave => Mathf.Clamp(currentWaveIndex + 1, 1, TotalWaves > 0 ? TotalWaves : 1);
        public int TotalWaves => configuration != null && configuration.waves != null ? configuration.waves.Count : 0;
        public int LivingCount => CountLiving();
        public int DefeatedCount => defeatedCount;
        public IReadOnlyList<AdventurerActor> AllAdventurers => allAdventurers;
        public bool IsRunning => isSpawningActive;

        public event Action<AdventurerActor> AdventurerSpawned;
        public event Action<AdventurerActor> AdventurerDied;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action AllWavesCleared;

        public void Configure(BattleConfiguration config, AdventurerDatabase database, List<Vector2Int> pathRoute,
                              Func<Vector2Int, Vector3> converter, Vector3 entrancePos, float size, Sprite sprite)
        {
            configuration = config;
            adventurerDatabase = database;
            route = pathRoute;
            worldConverter = converter;
            entranceWorldPos = entrancePos;
            cellSize = Mathf.Max(0.1f, size);
            whiteSprite = sprite;

            currentWaveIndex = 0;
            entryIndex = 0;
            entrySpawnedCount = 0;
            spawnTimer = 0.2f;
            defeatedCount = 0;
            allAdventurers.Clear();
            isSpawningActive = false;
        }

        public void StartWaves()
        {
            currentWaveIndex = 0;
            entryIndex = 0;
            entrySpawnedCount = 0;
            spawnTimer = 0.25f;
            defeatedCount = 0;
            isSpawningActive = true;
            WaveStarted?.Invoke(CurrentWave);
        }

        public void Tick(float deltaTime, IReadOnlyList<ICombatTarget> monsterTargets, ICombatTarget coreTarget)
        {
            if (!isSpawningActive || configuration == null || configuration.waves == null) return;

            // 1. Spawning
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f && currentWaveIndex < configuration.waves.Count)
            {
                SpawnNextAdventurer();
            }

            // 2. Tick living adventurers
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive)
                {
                    actor.Tick(deltaTime, monsterTargets, coreTarget);
                }
            }

            // 3. Wave completion check
            if (IsCurrentWaveSpawnComplete())
            {
                if (CountLivingInCurrentWave() == 0)
                {
                    WaveCleared?.Invoke(CurrentWave);
                    currentWaveIndex++;
                    entryIndex = 0;
                    entrySpawnedCount = 0;
                    spawnTimer = 0.8f;

                    if (currentWaveIndex >= configuration.waves.Count)
                    {
                        isSpawningActive = false;
                        AllWavesCleared?.Invoke();
                    }
                    else
                    {
                        WaveStarted?.Invoke(CurrentWave);
                    }
                }
            }
        }

        private void SpawnNextAdventurer()
        {
            if (currentWaveIndex >= configuration.waves.Count) return;
            AdventurerWave wave = configuration.waves[currentWaveIndex];
            if (wave == null || wave.entries == null || wave.entries.Count == 0) return;

            while (entryIndex < wave.entries.Count && entrySpawnedCount >= wave.entries[entryIndex].count)
            {
                entryIndex++;
                entrySpawnedCount = 0;
            }

            if (entryIndex >= wave.entries.Count) return;

            AdventurerWaveEntry entry = wave.entries[entryIndex];
            AdventurerData data = adventurerDatabase.Find(entry.adventurerId);
            if (data == null) return;

            // Compute lane offset perpendicular to entrance direction
            int spawnSlot = allAdventurers.Count % 3;
            float lateralOffset = (spawnSlot - 1) * (cellSize * 0.22f);
            Vector3 laneOffset = new Vector3(0f, lateralOffset, 0f);
            Vector3 spawnPos = entranceWorldPos + new Vector3(-cellSize * 0.4f, lateralOffset, -0.1f);

            GameObject go = data.prefab != null ? Instantiate(data.prefab, spawnPos, Quaternion.identity) : new GameObject($"Adventurer_{data.adventurerId}");
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * (cellSize * 0.45f);

            AdventurerActor actor = go.GetComponent<AdventurerActor>() ?? go.AddComponent<AdventurerActor>();
            string uniqueId = Guid.NewGuid().ToString("N");
            actor.Initialize(data, currentWaveIndex, route, worldConverter, laneOffset, whiteSprite, uniqueId);
            actor.Died += OnActorDied;

            allAdventurers.Add(actor);
            entrySpawnedCount++;
            spawnTimer = configuration.spawnInterval;

            if (entrySpawnedCount >= entry.count)
            {
                entryIndex++;
                entrySpawnedCount = 0;
            }

            AdventurerSpawned?.Invoke(actor);
        }

        private void OnActorDied(AdventurerActor actor)
        {
            defeatedCount++;
            AdventurerDied?.Invoke(actor);
        }

        private bool IsCurrentWaveSpawnComplete()
        {
            if (currentWaveIndex >= configuration.waves.Count) return true;
            AdventurerWave wave = configuration.waves[currentWaveIndex];
            if (wave == null || wave.entries == null) return true;
            return entryIndex >= wave.entries.Count;
        }

        private int CountLivingInCurrentWave()
        {
            int count = 0;
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive && actor.WaveIndex == currentWaveIndex)
                {
                    count++;
                }
            }
            return count;
        }

        private int CountLiving()
        {
            int count = 0;
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        public void CleanUp()
        {
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                if (allAdventurers[i] != null)
                {
                    allAdventurers[i].Died -= OnActorDied;
                    Destroy(allAdventurers[i].gameObject);
                }
            }
            allAdventurers.Clear();
            isSpawningActive = false;
        }

        private void OnDestroy()
        {
            CleanUp();
        }
    }
}
