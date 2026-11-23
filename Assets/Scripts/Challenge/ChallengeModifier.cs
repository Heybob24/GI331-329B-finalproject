using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum ChallengeModifierType
    {
        ReducedCoreHP,
        IncreasedEnemyHP,
        IncreasedEnemyDamage,
        ReducedMonsterCapacity,
        LimitedTrapPlacement,
        ReducedStartingGold,
        IncreasedBossDamage,
        NoHealing,
        DoubleEnemyCount,
        IncreasedReward
    }

    [Serializable]
    public sealed class ChallengeModifierConfig
    {
        public ChallengeModifierType modifierType = ChallengeModifierType.IncreasedEnemyHP;
        public string displayName = "Increased Enemy HP";
        [TextArea(1, 2)]
        public string description = "Invading enemies have +50% Max HP.";
        public float multiplierValue = 1.5f;
        public int flatLimitValue = 0;

        public string GetFormattedSummary()
        {
            return modifierType switch
            {
                ChallengeModifierType.ReducedCoreHP => $"Reduced Core HP ({Mathf.RoundToInt(multiplierValue * 100f)}%)",
                ChallengeModifierType.IncreasedEnemyHP => $"Enemy HP x{multiplierValue:0.00}",
                ChallengeModifierType.IncreasedEnemyDamage => $"Enemy Damage x{multiplierValue:0.00}",
                ChallengeModifierType.ReducedMonsterCapacity => $"Max Monsters Allowed: {flatLimitValue}",
                ChallengeModifierType.LimitedTrapPlacement => $"Max Traps Allowed: {flatLimitValue}",
                ChallengeModifierType.ReducedStartingGold => $"Budget Cap: {flatLimitValue} Gold",
                ChallengeModifierType.IncreasedBossDamage => $"Boss Damage x{multiplierValue:0.00}",
                ChallengeModifierType.NoHealing => "No Healing / Regen Disabled",
                ChallengeModifierType.DoubleEnemyCount => $"Enemy Count x{multiplierValue:0.00}",
                ChallengeModifierType.IncreasedReward => $"Rewards x{multiplierValue:0.00}",
                _ => displayName
            };
        }
    }

    public sealed class ChallengeRuntimeModifierState
    {
        public float CoreHpMultiplier { get; private set; } = 1.0f;
        public float EnemyHpMultiplier { get; private set; } = 1.0f;
        public float EnemyDamageMultiplier { get; private set; } = 1.0f;
        public float BossDamageMultiplier { get; private set; } = 1.0f;
        public float EnemyCountMultiplier { get; private set; } = 1.0f;
        public float RewardMultiplier { get; private set; } = 1.0f;
        public int MaxAllowedMonsters { get; private set; } = -1;
        public int MaxAllowedTraps { get; private set; } = -1;
        public int BudgetGoldCap { get; private set; } = -1;
        public bool DisableHealing { get; private set; } = false;
        public bool IsActive { get; private set; } = false;

        public void Reset()
        {
            CoreHpMultiplier = 1.0f;
            EnemyHpMultiplier = 1.0f;
            EnemyDamageMultiplier = 1.0f;
            BossDamageMultiplier = 1.0f;
            EnemyCountMultiplier = 1.0f;
            RewardMultiplier = 1.0f;
            MaxAllowedMonsters = -1;
            MaxAllowedTraps = -1;
            BudgetGoldCap = -1;
            DisableHealing = false;
            IsActive = false;
        }

        public void ApplyModifiers(IReadOnlyList<ChallengeModifierConfig> modifiers)
        {
            // Always reset first so modifiers never stack across retries or consecutive runs
            Reset();
            if (modifiers == null || modifiers.Count == 0) return;

            IsActive = true;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                if (m == null) continue;

                switch (m.modifierType)
                {
                    case ChallengeModifierType.ReducedCoreHP:
                        CoreHpMultiplier = Mathf.Clamp(m.multiplierValue, 0.1f, 1.0f);
                        break;
                    case ChallengeModifierType.IncreasedEnemyHP:
                        EnemyHpMultiplier = Mathf.Clamp(m.multiplierValue, 1.0f, 10.0f);
                        break;
                    case ChallengeModifierType.IncreasedEnemyDamage:
                        EnemyDamageMultiplier = Mathf.Clamp(m.multiplierValue, 1.0f, 10.0f);
                        break;
                    case ChallengeModifierType.ReducedMonsterCapacity:
                        MaxAllowedMonsters = Mathf.Max(0, m.flatLimitValue);
                        break;
                    case ChallengeModifierType.LimitedTrapPlacement:
                        MaxAllowedTraps = Mathf.Max(0, m.flatLimitValue);
                        break;
                    case ChallengeModifierType.ReducedStartingGold:
                        BudgetGoldCap = Mathf.Max(0, m.flatLimitValue);
                        break;
                    case ChallengeModifierType.IncreasedBossDamage:
                        BossDamageMultiplier = Mathf.Clamp(m.multiplierValue, 1.0f, 10.0f);
                        break;
                    case ChallengeModifierType.NoHealing:
                        DisableHealing = true;
                        break;
                    case ChallengeModifierType.DoubleEnemyCount:
                        EnemyCountMultiplier = Mathf.Clamp(m.multiplierValue, 1.0f, 5.0f);
                        break;
                    case ChallengeModifierType.IncreasedReward:
                        RewardMultiplier = Mathf.Clamp(m.multiplierValue, 1.0f, 5.0f);
                        break;
                }
            }
        }
    }
}
