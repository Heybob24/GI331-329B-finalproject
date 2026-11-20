using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonFacilityManager : MonoBehaviour
    {
        public static DungeonFacilityManager Instance { get; private set; }

        public const int MaxFacilityLevel = 10;
        public const int MaxCapacityLevel = 5;

        [SerializeField] private int _goldFacilityLevel = 1;
        [SerializeField] private int _soulFacilityLevel = 1;
        [SerializeField] private int _capacityFacilityLevel = 1;

        public int GoldFacilityLevel => _goldFacilityLevel;
        public int SoulFacilityLevel => _soulFacilityLevel;
        public int CapacityFacilityLevel => _capacityFacilityLevel;

        public event Action<string, int> OnFacilityUpgraded; // facilityName, newLevel

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

        public float GetGoldMultiplier() => 1.0f + (_goldFacilityLevel - 1) * 0.10f;
        public float GetSoulMultiplier() => 1.0f + (_soulFacilityLevel - 1) * 0.10f;
        public int GetBonusMonsterCapacity() => (_capacityFacilityLevel - 1);

        public (int goldCost, int soulCost) GetFacilityUpgradeCost(string facilityType, int currentLevel)
        {
            int baseGold = string.Equals(facilityType, "capacity", StringComparison.OrdinalIgnoreCase) ? 400 : 150;
            int baseSoul = string.Equals(facilityType, "capacity", StringComparison.OrdinalIgnoreCase) ? 100 : 30;

            int gold = baseGold * currentLevel;
            int soul = baseSoul * currentLevel;
            return (gold, soul);
        }

        public bool CanUpgradeFacility(string facilityType, out string error)
        {
            int currentLevel = GetLevel(facilityType);
            int maxLevel = string.Equals(facilityType, "capacity", StringComparison.OrdinalIgnoreCase) ? MaxCapacityLevel : MaxFacilityLevel;

            if (currentLevel >= maxLevel)
            {
                error = $"{facilityType} facility is already at maximum level.";
                return false;
            }

            var (goldReq, soulReq) = GetFacilityUpgradeCost(facilityType, currentLevel);

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

        public bool TryUpgradeFacility(string facilityType, out string error)
        {
            if (!CanUpgradeFacility(facilityType, out error)) return false;

            int currentLevel = GetLevel(facilityType);
            var (goldReq, soulReq) = GetFacilityUpgradeCost(facilityType, currentLevel);

            if (CurrencyManager.Instance != null)
            {
                if (!CurrencyManager.Instance.TrySpendGold(goldReq))
                {
                    error = "Failed to spend Gold.";
                    return false;
                }
                if (!CurrencyManager.Instance.TrySpendSoul(soulReq))
                {
                    CurrencyManager.Instance.AddGold(goldReq);
                    error = "Failed to spend Soul.";
                    return false;
                }
            }

            if (string.Equals(facilityType, "gold", StringComparison.OrdinalIgnoreCase))
            {
                _goldFacilityLevel++;
                OnFacilityUpgraded?.Invoke("Gold Production", _goldFacilityLevel);
            }
            else if (string.Equals(facilityType, "soul", StringComparison.OrdinalIgnoreCase))
            {
                _soulFacilityLevel++;
                OnFacilityUpgraded?.Invoke("Soul Production", _soulFacilityLevel);
            }
            else if (string.Equals(facilityType, "capacity", StringComparison.OrdinalIgnoreCase))
            {
                _capacityFacilityLevel++;
                OnFacilityUpgraded?.Invoke("Monster Capacity", _capacityFacilityLevel);
            }

            SaveManager.Instance?.SaveGame();
            error = $"{facilityType} facility upgraded to Level {GetLevel(facilityType)}!";
            return true;
        }

        public int GetLevel(string facilityType)
        {
            if (string.Equals(facilityType, "gold", StringComparison.OrdinalIgnoreCase)) return _goldFacilityLevel;
            if (string.Equals(facilityType, "soul", StringComparison.OrdinalIgnoreCase)) return _soulFacilityLevel;
            if (string.Equals(facilityType, "capacity", StringComparison.OrdinalIgnoreCase)) return _capacityFacilityLevel;
            return 1;
        }

        public void SetLevels(int goldLvl, int soulLvl, int capLvl)
        {
            _goldFacilityLevel = Mathf.Clamp(goldLvl, 1, MaxFacilityLevel);
            _soulFacilityLevel = Mathf.Clamp(soulLvl, 1, MaxFacilityLevel);
            _capacityFacilityLevel = Mathf.Clamp(capLvl, 1, MaxCapacityLevel);
        }

        public void ResetFacilities()
        {
            _goldFacilityLevel = 1;
            _soulFacilityLevel = 1;
            _capacityFacilityLevel = 1;
        }
    }
}
