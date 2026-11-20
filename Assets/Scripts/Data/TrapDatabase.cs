using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Trap Database", fileName = "TrapDatabase")]
    public sealed class TrapDatabase : ScriptableObject
    {
        [SerializeField] private List<TrapData> traps = new List<TrapData>();
        private readonly Dictionary<string, TrapData> byId = new Dictionary<string, TrapData>();
        public IReadOnlyList<TrapData> AllTraps => traps;
        private void OnEnable() => RebuildCache();
        private void OnValidate() => RebuildCache();
        public void SetEntries(List<TrapData> entries) { traps = entries ?? new List<TrapData>(); RebuildCache(); }
        public TrapData FindById(string id) { if (byId.Count != traps.Count) RebuildCache(); return !string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out TrapData value) ? value : null; }
        public bool ValidateDatabase(out string error)
        {
            var ids = new HashSet<string>();
            foreach (TrapData data in traps)
            {
                if (data == null) { error = "Trap Database contains a missing entry."; return false; }
                if (string.IsNullOrWhiteSpace(data.trapId) || !ids.Add(data.trapId)) { error = $"Trap Database contains an empty or duplicate ID '{data.trapId}'."; return false; }
                if (string.IsNullOrWhiteSpace(data.trapName) || data.goldCost < 0 || data.maxLevel < 1 || data.cooldown < 0 || data.duration < 0 || data.triggerRadius < 0)
                { error = $"Trap '{data.trapId}' has invalid configuration."; return false; }
            }
            error = string.Empty; return true;
        }
        private void RebuildCache()
        {
            byId.Clear(); if (traps == null) return;
            foreach (TrapData data in traps)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.trapId)) continue;
                if (!byId.ContainsKey(data.trapId)) byId.Add(data.trapId, data);
                else Debug.LogError($"Duplicate trapId '{data.trapId}' in {name}.", this);
            }
        }
    }
}
