using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class CoreLevelTier
    {
        public int level = 1;
        public int maxHP = 500;
        public int defense = 0;
        public int upgradeCostGold = 200;
        public int upgradeCostSoul = 50;
    }

    [Serializable]
    public sealed class GridExpansionTier
    {
        public int dimension = 11;
        public int requiredDungeonLevel = 3;
        public int costGold = 500;
        public int costSoul = 100;
    }

    [Serializable]
    public sealed class FacilityLevelTier
    {
        public int level = 1;
        public int bonusPercent = 0;
        public int costGold = 150;
        public int costSoul = 30;
    }

    [CreateAssetMenu(fileName = "UpgradeConfiguration", menuName = "Dream Dungeon/Upgrade/Upgrade Configuration")]
    public sealed class UpgradeConfiguration : ScriptableObject
    {
        [Header("Core Upgrade Tiers (Levels 1 to 10)")]
        public List<CoreLevelTier> coreTiers = new List<CoreLevelTier>();

        [Header("Grid Expansion Tiers (11x11, 13x13, 15x15, 17x17)")]
        public List<GridExpansionTier> expansionTiers = new List<GridExpansionTier>();

        [Header("Facility Upgrade Tiers")]
        public List<FacilityLevelTier> goldFacilityTiers = new List<FacilityLevelTier>();
        public List<FacilityLevelTier> soulFacilityTiers = new List<FacilityLevelTier>();
        public List<FacilityLevelTier> capacityFacilityTiers = new List<FacilityLevelTier>();
    }
}
