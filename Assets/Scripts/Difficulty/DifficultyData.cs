using System;
using UnityEngine;

namespace DreamDungeon
{
    public enum StageDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        VeryHard = 3,
        Nightmare = 4
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Difficulty Data", fileName = "DifficultyData")]
    public sealed class DifficultyData : ScriptableObject
    {
        [Header("Identity")]
        public string difficultyId = "easy";
        public string difficultyName = "Easy";
        public StageDifficulty difficultyLevel = StageDifficulty.Easy;

        [Header("Enemy Multipliers")]
        [Min(0.1f)] public float enemyHealthMultiplier = 1.0f;
        [Min(0.1f)] public float enemyDamageMultiplier = 1.0f;
        [Min(0.1f)] public float enemyDefenseMultiplier = 1.0f;
        [Min(0.1f)] public float enemyCountMultiplier = 1.0f;

        [Header("Reward Multipliers")]
        [Min(0.1f)] public float rewardGoldMultiplier = 1.0f;
        [Min(0.1f)] public float rewardSoulMultiplier = 1.0f;
        [Min(0.1f)] public float rewardEXPMultiplier = 1.0f;

        [Header("Unlock Requirement")]
        public string unlockRequirement = string.Empty;
        [Min(1)] public int requiredDungeonLevel = 1;

        private void OnValidate()
        {
            enemyHealthMultiplier = Mathf.Max(0.1f, enemyHealthMultiplier);
            enemyDamageMultiplier = Mathf.Max(0.1f, enemyDamageMultiplier);
            enemyDefenseMultiplier = Mathf.Max(0.1f, enemyDefenseMultiplier);
            enemyCountMultiplier = Mathf.Max(0.1f, enemyCountMultiplier);
            rewardGoldMultiplier = Mathf.Max(0.1f, rewardGoldMultiplier);
            rewardSoulMultiplier = Mathf.Max(0.1f, rewardSoulMultiplier);
            rewardEXPMultiplier = Mathf.Max(0.1f, rewardEXPMultiplier);
            requiredDungeonLevel = Mathf.Max(1, requiredDungeonLevel);
        }

        public AdventurerData CreateRuntimeScaledCopy(AdventurerData baseData, float factionHpMult = 1f, float factionAtkMult = 1f, float factionDefMult = 1f, float factionSpdMult = 1f, float factionMoveMult = 1f)
        {
            if (baseData == null) return null;
            AdventurerData runtime = CreateInstance<AdventurerData>();
            runtime.name = baseData.name + "_Runtime_" + difficultyId;
            runtime.adventurerId = baseData.adventurerId;
            runtime.adventurerName = baseData.adventurerName;
            runtime.description = baseData.description;
            runtime.icon = baseData.icon;
            runtime.prefab = baseData.prefab;
            runtime.attackType = baseData.attackType;
            runtime.targetPriority = baseData.targetPriority;

            runtime.maxHP = Mathf.Max(1, Mathf.RoundToInt(baseData.maxHP * enemyHealthMultiplier * Mathf.Max(0.1f, factionHpMult)));
            runtime.attack = Mathf.Max(1, Mathf.RoundToInt(baseData.attack * enemyDamageMultiplier * Mathf.Max(0.1f, factionAtkMult)));
            runtime.defense = Mathf.Max(0, Mathf.RoundToInt(baseData.defense * enemyDefenseMultiplier * Mathf.Max(0.1f, factionDefMult)));
            runtime.attackRange = baseData.attackRange;
            runtime.attackSpeed = Mathf.Max(0.1f, baseData.attackSpeed * Mathf.Max(0.1f, factionSpdMult));
            runtime.moveSpeed = Mathf.Max(0.1f, baseData.moveSpeed * Mathf.Max(0.1f, factionMoveMult));
            return runtime;
        }
    }
}
