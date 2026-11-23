using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonUnlockManager : MonoBehaviour
    {
        public static DungeonUnlockManager Instance { get; private set; }

        private readonly Dictionary<string, int> _roomUnlockLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "empty", 1 },
            { "pathway", 1 },
            { "core", 1 },
            { "entrance", 1 },
            { "spawner", 1 },
            { "monster_room", 1 },
            { "trap_room", 1 },
            { "magic_room", 4 },
            { "boss_room", 5 },
            { "dark_room", 8 }
        };

        private readonly Dictionary<string, int> _monsterUnlockLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "slime", 1 },
            { "goblin", 1 },
            { "skeleton", 1 },
            { "bat", 1 },
            { "skeleton_archer", 2 },
            { "zombie", 2 },
            { "imp", 2 },
            { "wolf", 2 },
            { "spider", 2 },
            { "dark_mage", 3 },
            { "ghost", 3 },
            { "mimic", 3 },
            { "harpy", 3 },
            { "orc", 4 },
            { "demon", 4 },
            { "golem", 4 },
            { "gargoyle", 4 },
            { "baby_dragon", 5 },
            { "crystal_golem", 5 },
            { "fire_elemental", 5 },
            { "ice_elemental", 5 },
            { "shadow_creature", 6 },
            { "treant", 6 },
            { "beholder", 7 }
        };

        private readonly Dictionary<string, int> _trapUnlockLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "spike_trap", 1 },
            { "poison_trap", 2 },
            { "arrow_trap", 3 },
            { "fire_trap", 6 }
        };

        private readonly Dictionary<string, int> _bossUnlockLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "cave_dragon", 5 },
            { "dragon", 5 },
            { "ice_queen", 6 },
            { "dark_golem", 7 },
            { "ancient_guardian", 7 },
            { "lich_king", 8 },
            { "demon_lord", 10 }
        };

        private readonly List<(int size, int level)> _gridUnlockLevels = new List<(int, int)>
        {
            (9, 1),
            (11, 3),
            (13, 5),
            (15, 7),
            (17, 9)
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Destroy(this);
                }
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int GetRequiredLevelForRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return 1;
            return _roomUnlockLevels.TryGetValue(roomId, out int lvl) ? lvl : 1;
        }

        public int GetRequiredLevelForMonster(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return 1;
            return _monsterUnlockLevels.TryGetValue(monsterId, out int lvl) ? lvl : 1;
        }

        public int GetRequiredLevelForTrap(string trapId)
        {
            if (string.IsNullOrEmpty(trapId)) return 1;
            return _trapUnlockLevels.TryGetValue(trapId, out int lvl) ? lvl : 1;
        }

        public int GetRequiredLevelForBoss(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return 1;
            return _bossUnlockLevels.TryGetValue(bossId, out int lvl) ? lvl : 5;
        }

        public int GetRequiredLevelForGridSize(int width, int height)
        {
            int maxDim = Mathf.Max(width, height);
            foreach (var pair in _gridUnlockLevels)
            {
                if (maxDim <= pair.size)
                {
                    return pair.level;
                }
            }
            return 9;
        }

        public bool IsRoomUnlocked(string roomId, int currentLevel)
        {
            return currentLevel >= GetRequiredLevelForRoom(roomId);
        }

        public bool IsMonsterUnlocked(string monsterId, int currentLevel)
        {
            return currentLevel >= GetRequiredLevelForMonster(monsterId);
        }

        public bool IsTrapUnlocked(string trapId, int currentLevel)
        {
            return currentLevel >= GetRequiredLevelForTrap(trapId);
        }

        public bool IsBossUnlocked(string bossId, int currentLevel)
        {
            return currentLevel >= GetRequiredLevelForBoss(bossId);
        }

        public bool IsGridSizeUnlocked(int width, int height, int currentLevel)
        {
            return currentLevel >= GetRequiredLevelForGridSize(width, height);
        }

        public List<string> GetUnlocksForLevel(int level)
        {
            var list = new List<string>();
            foreach (var kvp in _roomUnlockLevels)
            {
                if (kvp.Value == level) list.Add($"Room: {kvp.Key}");
            }
            foreach (var kvp in _monsterUnlockLevels)
            {
                if (kvp.Value == level) list.Add($"Monster: {kvp.Key}");
            }
            foreach (var kvp in _trapUnlockLevels)
            {
                if (kvp.Value == level) list.Add($"Trap: {kvp.Key}");
            }
            foreach (var kvp in _bossUnlockLevels)
            {
                if (kvp.Value == level) list.Add($"Boss: {kvp.Key}");
            }
            foreach (var pair in _gridUnlockLevels)
            {
                if (pair.level == level && pair.size > 9) list.Add($"Grid Expansion: {pair.size}x{pair.size}");
            }
            return list;
        }
    }
}
