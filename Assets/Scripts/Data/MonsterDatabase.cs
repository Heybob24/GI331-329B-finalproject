using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Monster Database", fileName = "MonsterDatabase")]
    public sealed class MonsterDatabase : ScriptableObject
    {
        [SerializeField] private List<MonsterData> monsters = new List<MonsterData>();
        private readonly Dictionary<string, MonsterData> byId = new Dictionary<string, MonsterData>();
        public IReadOnlyList<MonsterData> AllMonsters => monsters;
        private void OnEnable() => RebuildCache();
        private void OnValidate() => RebuildCache();
        public void SetEntries(List<MonsterData> entries) { monsters = entries ?? new List<MonsterData>(); RebuildCache(); }
        public MonsterData FindById(string id) { if (byId.Count != monsters.Count) RebuildCache(); return !string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out MonsterData value) ? value : null; }
        public bool ValidateDatabase(out string error)
        {
            var ids = new HashSet<string>();
            foreach (MonsterData data in monsters)
            {
                if (data == null) { error = "Monster Database contains a missing entry."; return false; }
                if (string.IsNullOrWhiteSpace(data.monsterId) || !ids.Add(data.monsterId)) { error = $"Monster Database contains an empty or duplicate ID '{data.monsterId}'."; return false; }
                if (string.IsNullOrWhiteSpace(data.monsterName) || data.goldCost < 0 || data.maxHP <= 0 || data.attackSpeed <= 0 || data.attackRange <= 0 || data.maxLevel < 1)
                { error = $"Monster '{data.monsterId}' has invalid configuration."; return false; }
            }
            error = string.Empty; return true;
        }
        private void RebuildCache()
        {
            byId.Clear(); if (monsters == null) return;
            foreach (MonsterData data in monsters)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.monsterId)) continue;
                if (!byId.ContainsKey(data.monsterId)) byId.Add(data.monsterId, data);
                else Debug.LogError($"Duplicate monsterId '{data.monsterId}' in {name}.", this);
            }
        }
    }
}
