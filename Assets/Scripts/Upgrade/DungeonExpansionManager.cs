using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonExpansionManager : MonoBehaviour
    {
        public static DungeonExpansionManager Instance { get; private set; }

        public const int MaxGridDimension = 17;

        [SerializeField] private int _currentDimension = 9;

        public int CurrentDimension => _currentDimension;

        public event Action<int, int> OnGridExpanded; // width, height

        private readonly struct ExpansionTier
        {
            public readonly int Dimension;
            public readonly int RequiredDungeonLevel;
            public readonly int GoldCost;
            public readonly int SoulCost;

            public ExpansionTier(int dimension, int reqLevel, int gold, int soul)
            {
                Dimension = dimension;
                RequiredDungeonLevel = reqLevel;
                GoldCost = gold;
                SoulCost = soul;
            }
        }

        private readonly ExpansionTier[] _expansionTiers = new ExpansionTier[]
        {
            new ExpansionTier(11, 3, 500, 100),
            new ExpansionTier(13, 5, 1200, 250),
            new ExpansionTier(15, 7, 2500, 600),
            new ExpansionTier(17, 9, 5000, 1200)
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

        public bool GetNextExpansionTier(out int nextDimension, out int requiredLevel, out int goldCost, out int soulCost)
        {
            nextDimension = _currentDimension;
            requiredLevel = 1;
            goldCost = 0;
            soulCost = 0;

            foreach (var tier in _expansionTiers)
            {
                if (tier.Dimension > _currentDimension)
                {
                    nextDimension = tier.Dimension;
                    requiredLevel = tier.RequiredDungeonLevel;
                    goldCost = tier.GoldCost;
                    soulCost = tier.SoulCost;
                    return true;
                }
            }

            return false;
        }

        public bool CanExpand(out string error)
        {
            if (!GetNextExpansionTier(out int nextDim, out int reqLevel, out int goldCost, out int soulCost))
            {
                error = "Dungeon grid is already at maximum expansion (17x17).";
                return false;
            }

            int currentDungeonLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            if (currentDungeonLevel < reqLevel)
            {
                error = $"Requires Dungeon Level {reqLevel} to expand to {nextDim}x{nextDim}.";
                return false;
            }

            if (CurrencyManager.Instance != null)
            {
                if (CurrencyManager.Instance.Gold < goldCost)
                {
                    error = $"Not enough Gold. Need {goldCost} Gold.";
                    return false;
                }
                if (CurrencyManager.Instance.Soul < soulCost)
                {
                    error = $"Not enough Soul. Need {soulCost} Soul.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public bool TryExpand(out string error)
        {
            if (!CanExpand(out error)) return false;

            GetNextExpansionTier(out int nextDim, out _, out int goldCost, out int soulCost);

            // Deduct currency
            if (CurrencyManager.Instance != null)
            {
                if (!CurrencyManager.Instance.TrySpendGold(goldCost))
                {
                    error = "Failed to spend Gold.";
                    return false;
                }
                if (!CurrencyManager.Instance.TrySpendSoul(soulCost))
                {
                    CurrencyManager.Instance.AddGold(goldCost);
                    error = "Failed to spend Soul.";
                    return false;
                }
            }

            // Apply grid expansion
            if (RoomManager.Instance != null)
            {
                if (!RoomManager.Instance.SetGridDimensions(nextDim, nextDim, out string resizeErr))
                {
                    // Refund if failed
                    if (CurrencyManager.Instance != null)
                    {
                        CurrencyManager.Instance.AddGold(goldCost);
                        CurrencyManager.Instance.AddSoul(soulCost);
                    }
                    error = resizeErr;
                    return false;
                }
            }

            _currentDimension = nextDim;
            OnGridExpanded?.Invoke(_currentDimension, _currentDimension);
            SaveManager.Instance?.SaveGame();

            error = $"Dungeon grid expanded to {_currentDimension}x{_currentDimension}!";
            return true;
        }

        public void SetDimension(int dimension)
        {
            _currentDimension = Mathf.Clamp(dimension, 9, MaxGridDimension);
            OnGridExpanded?.Invoke(_currentDimension, _currentDimension);
        }

        public void ResetExpansion()
        {
            _currentDimension = 9;
            OnGridExpanded?.Invoke(_currentDimension, _currentDimension);
        }
    }
}
