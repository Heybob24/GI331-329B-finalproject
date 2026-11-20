using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonCoreUpgradeManager : MonoBehaviour
    {
        public static DungeonCoreUpgradeManager Instance { get; private set; }

        public const int MaxCoreLevel = 10;

        [SerializeField] private int _coreLevel = 1;

        public int CoreLevel => _coreLevel;
        public int MaxHP => GetMaxHPForLevel(_coreLevel);
        public int Defense => GetDefenseForLevel(_coreLevel);

        public event Action<int, int, int> OnCoreUpgraded; // level, maxHP, defense

        private readonly int[] _hpTable = new int[]
        {
            500,  // Lv 1
            750,  // Lv 2
            1000, // Lv 3
            1250, // Lv 4
            1550, // Lv 5
            1850, // Lv 6
            2200, // Lv 7
            2550, // Lv 8
            2800, // Lv 9
            3000  // Lv 10
        };

        private readonly int[] _defTable = new int[]
        {
            0,  // Lv 1
            5,  // Lv 2
            10, // Lv 3
            15, // Lv 4
            20, // Lv 5
            25, // Lv 6
            30, // Lv 7
            35, // Lv 8
            42, // Lv 9
            50  // Lv 10
        };

        private readonly (int gold, int soul)[] _upgradeCostTable = new (int, int)[]
        {
            (200, 50),   // 1 -> 2
            (400, 100),  // 2 -> 3
            (700, 175),  // 3 -> 4
            (1100, 275), // 4 -> 5
            (1600, 400), // 5 -> 6
            (2300, 600), // 6 -> 7
            (3200, 850), // 7 -> 8
            (4300, 1200),// 8 -> 9
            (6000, 1800) // 9 -> 10
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int GetMaxHPForLevel(int level)
        {
            int idx = Mathf.Clamp(level, 1, MaxCoreLevel) - 1;
            return _hpTable[idx];
        }

        public int GetDefenseForLevel(int level)
        {
            int idx = Mathf.Clamp(level, 1, MaxCoreLevel) - 1;
            return _defTable[idx];
        }

        public bool GetUpgradeCost(int currentLevel, out int goldCost, out int soulCost)
        {
            goldCost = 0;
            soulCost = 0;
            if (currentLevel < 1 || currentLevel >= MaxCoreLevel) return false;

            var (g, s) = _upgradeCostTable[currentLevel - 1];
            goldCost = g;
            soulCost = s;
            return true;
        }

        public bool CanUpgrade(out string error)
        {
            if (_coreLevel >= MaxCoreLevel)
            {
                error = "Dungeon Core is already at maximum level.";
                return false;
            }

            GetUpgradeCost(_coreLevel, out int goldReq, out int soulReq);

            if (CurrencyManager.Instance != null)
            {
                if (CurrencyManager.Instance.Gold < goldReq)
                {
                    error = $"Not enough Gold. Need {goldReq} Gold.";
                    return false;
                }
                if (CurrencyManager.Instance.Soul < soulReq)
                {
                    error = $"Not enough Soul. Need {soulReq} Soul.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public bool TryUpgrade(out string error)
        {
            if (!CanUpgrade(out error)) return false;

            GetUpgradeCost(_coreLevel, out int goldReq, out int soulReq);

            if (CurrencyManager.Instance != null)
            {
                if (!CurrencyManager.Instance.TrySpendGold(goldReq))
                {
                    error = "Failed to spend Gold.";
                    return false;
                }
                if (!CurrencyManager.Instance.TrySpendSoul(soulReq))
                {
                    // Refund gold if soul spend fails
                    CurrencyManager.Instance.AddGold(goldReq);
                    error = "Failed to spend Soul.";
                    return false;
                }
            }

            _coreLevel++;
            OnCoreUpgraded?.Invoke(_coreLevel, MaxHP, Defense);
            SaveManager.Instance?.SaveGame();

            error = $"Dungeon Core upgraded to Level {_coreLevel}!";
            return true;
        }

        public void SetLevel(int level)
        {
            _coreLevel = Mathf.Clamp(level, 1, MaxCoreLevel);
            OnCoreUpgraded?.Invoke(_coreLevel, MaxHP, Defense);
        }

        public void ResetUpgrade()
        {
            _coreLevel = 1;
            OnCoreUpgraded?.Invoke(_coreLevel, MaxHP, Defense);
        }
    }
}
