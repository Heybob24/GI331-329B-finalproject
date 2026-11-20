using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonProgressionManager : MonoBehaviour
    {
        public static DungeonProgressionManager Instance { get; private set; }

        public const int MaxLevel = 10;

        [Header("Runtime State")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _currentExp = 0;
        [SerializeField] private int _totalExpEarned = 0;

        [Header("Statistics")]
        [SerializeField] private int _totalRaids = 0;
        [SerializeField] private int _totalVictories = 0;
        [SerializeField] private int _totalDefeats = 0;
        [SerializeField] private int _totalBossesDefeated = 0;
        [SerializeField] private int _highestWaveCleared = 0;

        private readonly HashSet<int> _claimedLevelRewards = new HashSet<int>();

        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public int TotalExpEarned => _totalExpEarned;
        public int TotalRaids => _totalRaids;
        public int TotalVictories => _totalVictories;
        public int TotalDefeats => _totalDefeats;
        public int TotalBossesDefeated => _totalBossesDefeated;
        public int HighestWaveCleared => _highestWaveCleared;

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnExpChanged;
        public event Action<int, int, int> OnLevelRewardGranted; // level, gold, soul

        private readonly int[] _requiredExpTable = new int[]
        {
            100,  // Lv 1 -> 2
            250,  // Lv 2 -> 3
            450,  // Lv 3 -> 4
            700,  // Lv 4 -> 5
            1050, // Lv 5 -> 6
            1500, // Lv 6 -> 7
            2100, // Lv 7 -> 8
            2850, // Lv 8 -> 9
            3800, // Lv 9 -> 10
            0     // Lv 10 (Max)
        };

        private readonly (int gold, int soul)[] _levelRewardTable = new (int, int)[]
        {
            (0, 0),        // Lv 1
            (200, 50),     // Lv 2
            (300, 75),     // Lv 3
            (450, 100),    // Lv 4
            (650, 150),    // Lv 5
            (900, 200),    // Lv 6
            (1200, 250),   // Lv 7
            (1600, 350),   // Lv 8
            (2100, 500),   // Lv 9
            (3000, 800)    // Lv 10
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

        public int GetRequiredExpForLevel(int level)
        {
            if (level < 1) return _requiredExpTable[0];
            if (level >= MaxLevel) return 0;
            return _requiredExpTable[level - 1];
        }

        public (int gold, int soul) GetRewardForLevel(int level)
        {
            if (level <= 1 || level > MaxLevel) return (0, 0);
            return _levelRewardTable[level - 1];
        }

        public bool IsRewardClaimed(int level)
        {
            return _claimedLevelRewards.Contains(level);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            _totalExpEarned += amount;

            if (_currentLevel >= MaxLevel)
            {
                _currentExp = 0;
                OnExpChanged?.Invoke(_currentExp, 0);
                return;
            }

            _currentExp += amount;

            while (_currentLevel < MaxLevel && _currentExp >= GetRequiredExpForLevel(_currentLevel))
            {
                int req = GetRequiredExpForLevel(_currentLevel);
                _currentExp -= req;
                _currentLevel++;

                ClaimLevelReward(_currentLevel);
                OnLevelUp?.Invoke(_currentLevel);
            }

            if (_currentLevel >= MaxLevel)
            {
                _currentExp = 0;
            }

            OnExpChanged?.Invoke(_currentExp, GetRequiredExpForLevel(_currentLevel));
            SaveManager.Instance?.SaveGame();
        }

        private void ClaimLevelReward(int level)
        {
            if (_claimedLevelRewards.Contains(level)) return;

            _claimedLevelRewards.Add(level);
            var (gold, soul) = GetRewardForLevel(level);

            if (CurrencyManager.Instance != null)
            {
                if (gold > 0) CurrencyManager.Instance.AddGold(gold);
                if (soul > 0) CurrencyManager.Instance.AddSoul(soul);
            }

            OnLevelRewardGranted?.Invoke(level, gold, soul);
        }

        public void RecordRaidResult(bool victory, int waveCleared, int adventurersDefeated, int bossesDefeated)
        {
            _totalRaids++;
            if (victory)
            {
                _totalVictories++;
            }
            else
            {
                _totalDefeats++;
            }

            _totalBossesDefeated += bossesDefeated;
            if (waveCleared > _highestWaveCleared)
            {
                _highestWaveCleared = waveCleared;
            }

            SaveManager.Instance?.SaveGame();
        }

        public DungeonProgressionData ExportData()
        {
            var data = new DungeonProgressionData
            {
                dungeonLevel = _currentLevel,
                currentExp = _currentExp,
                totalExpEarned = _totalExpEarned,
                totalRaids = _totalRaids,
                totalVictories = _totalVictories,
                totalDefeats = _totalDefeats,
                totalBossesDefeated = _totalBossesDefeated,
                highestWaveCleared = _highestWaveCleared,
                claimedLevelRewards = new List<int>(_claimedLevelRewards)
            };
            return data;
        }

        public void ImportData(DungeonProgressionData data)
        {
            if (data == null) return;

            _currentLevel = Mathf.Clamp(data.dungeonLevel, 1, MaxLevel);
            _currentExp = Mathf.Max(0, data.currentExp);
            _totalExpEarned = Mathf.Max(0, data.totalExpEarned);
            _totalRaids = Mathf.Max(0, data.totalRaids);
            _totalVictories = Mathf.Max(0, data.totalVictories);
            _totalDefeats = Mathf.Max(0, data.totalDefeats);
            _totalBossesDefeated = Mathf.Max(0, data.totalBossesDefeated);
            _highestWaveCleared = Mathf.Max(0, data.highestWaveCleared);

            _claimedLevelRewards.Clear();
            if (data.claimedLevelRewards != null)
            {
                foreach (int lvl in data.claimedLevelRewards)
                {
                    _claimedLevelRewards.Add(lvl);
                }
            }

            OnExpChanged?.Invoke(_currentExp, GetRequiredExpForLevel(_currentLevel));
        }

        public void ResetProgression()
        {
            _currentLevel = 1;
            _currentExp = 0;
            _totalExpEarned = 0;
            _totalRaids = 0;
            _totalVictories = 0;
            _totalDefeats = 0;
            _totalBossesDefeated = 0;
            _highestWaveCleared = 0;
            _claimedLevelRewards.Clear();
            OnExpChanged?.Invoke(_currentExp, GetRequiredExpForLevel(_currentLevel));
        }
    }
}
