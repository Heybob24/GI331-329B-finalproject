using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Adventurer Database", fileName = "AdventurerDatabase")]
    public sealed class AdventurerDatabase : ScriptableObject
    {
        [SerializeField] private List<AdventurerData> entries = new List<AdventurerData>();
        public IReadOnlyList<AdventurerData> Entries => entries;
        public AdventurerData Find(string id) => entries.Find(x => x != null && x.adventurerId == id);
        public bool Validate(out string error)
        {
            var ids = new HashSet<string>();
            foreach (AdventurerData d in entries)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.adventurerId) || !ids.Add(d.adventurerId) || string.IsNullOrWhiteSpace(d.adventurerName) || d.maxHP < 1 || d.attackRange <= 0 || d.attackSpeed <= 0 || d.moveSpeed <= 0)
                { error = "Adventurer Database contains a missing, duplicate, or invalid entry."; return false; }
            }
            if (entries.Count == 0) { error = "Adventurer Database has no entries."; return false; }
            error = string.Empty; return true;
        }
        public void SetEntries(List<AdventurerData> value) => entries = value ?? new List<AdventurerData>();
    }
}
