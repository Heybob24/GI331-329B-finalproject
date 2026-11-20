using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class CurrencyManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int gold = 1000;
        [SerializeField, Min(0)] private int soul;
        [SerializeField, Min(0)] private int crystal;

        public static CurrencyManager Instance { get; private set; }

        public int Gold => gold;
        public int Soul => soul;
        public int Crystal => crystal;
        public event Action<int, int, int> CurrencyChanged;

        public void Configure(int startingGold, int startingSoul, int startingCrystal)
        {
            gold = Mathf.Max(0, startingGold);
            soul = Mathf.Max(0, startingSoul);
            crystal = Mathf.Max(0, startingCrystal);
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            CurrencyChanged?.Invoke(gold, soul, crystal);
        }

        public void AddGold(int amount) => TryAdd(amount, 0, 0, out _);
        public void AddSoul(int amount) => TryAdd(0, amount, 0, out _);
        public bool TrySpendGold(int amount) => TrySpend(amount, 0, 0, out _);
        public bool TrySpendSoul(int amount) => TrySpend(0, amount, 0, out _);
        public void SetGold(int val) { gold = Mathf.Max(0, val); CurrencyChanged?.Invoke(gold, soul, crystal); }
        public void SetSoul(int val) { soul = Mathf.Max(0, val); CurrencyChanged?.Invoke(gold, soul, crystal); }
        public void ResetCurrencies() { gold = 1000; soul = 200; crystal = 0; CurrencyChanged?.Invoke(gold, soul, crystal); }

        public bool CanAfford(int goldCost, int soulCost, int crystalCost = 0)
        {
            return goldCost >= 0 && soulCost >= 0 && crystalCost >= 0 &&
                   gold >= goldCost && soul >= soulCost && crystal >= crystalCost;
        }

        public bool TrySpend(int goldCost, int soulCost, int crystalCost, out string error)
        {
            if (goldCost < 0 || soulCost < 0 || crystalCost < 0)
            {
                error = "Currency costs cannot be negative.";
                return false;
            }
            if (!CanAfford(goldCost, soulCost, crystalCost))
            {
                error = $"Not enough currency. Need {goldCost} Gold, {soulCost} Soul, and {crystalCost} Crystal.";
                return false;
            }

            gold -= goldCost;
            soul -= soulCost;
            crystal -= crystalCost;
            CurrencyChanged?.Invoke(gold, soul, crystal);
            error = string.Empty;
            return true;
        }

        public bool TryAdd(int goldAmount, int soulAmount, int crystalAmount, out string error)
        {
            if (goldAmount < 0 || soulAmount < 0 || crystalAmount < 0)
            {
                error = "Currency amounts cannot be negative.";
                return false;
            }
            gold = (int)Math.Min(int.MaxValue, (long)gold + goldAmount);
            soul = (int)Math.Min(int.MaxValue, (long)soul + soulAmount);
            crystal = (int)Math.Min(int.MaxValue, (long)crystal + crystalAmount);
            CurrencyChanged?.Invoke(gold, soul, crystal);
            error = string.Empty;
            return true;
        }

        public bool TryApplyDelta(int goldDelta, int soulDelta, int crystalDelta, out string error)
        {
            long nextGold = (long)gold + goldDelta;
            long nextSoul = (long)soul + soulDelta;
            long nextCrystal = (long)crystal + crystalDelta;
            if (nextGold < 0 || nextSoul < 0 || nextCrystal < 0)
            { error = "Not enough currency to redo that upgrade."; return false; }
            if (nextGold > int.MaxValue || nextSoul > int.MaxValue || nextCrystal > int.MaxValue)
            { error = "Currency limit reached; the history action cannot be applied."; return false; }
            gold = (int)nextGold; soul = (int)nextSoul; crystal = (int)nextCrystal;
            CurrencyChanged?.Invoke(gold, soul, crystal); error = string.Empty; return true;
        }

        public CurrencySnapshot Capture() => new CurrencySnapshot { gold = gold, soul = soul, crystal = crystal };

        public void RestoreForUndo(CurrencySnapshot snapshot)
        {
            if (snapshot == null) return;
            gold = Mathf.Max(0, snapshot.gold);
            soul = Mathf.Max(0, snapshot.soul);
            crystal = Mathf.Max(0, snapshot.crystal);
            CurrencyChanged?.Invoke(gold, soul, crystal);
        }
    }

    [Serializable]
    public sealed class CurrencySnapshot
    {
        public int gold;
        public int soul;
        public int crystal;
    }
}
