using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class AdventurerAttack : MonoBehaviour
    {
        private float attackRange = 1f;
        private float attackSpeed = 1f;
        private int attackDamage = 10;
        private AdventurerAttackType attackType;
        private AdventurerTargetPriority targetPriority;
        private float attackCooldown;
        private float bonusDamageVsBossMultiplier = 1f;
        private ICombatTarget currentTarget;

        public ICombatTarget CurrentTarget => currentTarget;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public int AttackDamage => attackDamage;
        public float BonusDamageVsBossMultiplier => bonusDamageVsBossMultiplier;
        public bool IsOnCooldown => attackCooldown > 0f;

        public event Action<ICombatTarget, int> Attacked;

        public void Configure(int damage, float range, float speed, AdventurerAttackType type, AdventurerTargetPriority priority, float bossDamageMult = 1f)
        {
            attackDamage = Mathf.Max(1, damage);
            attackRange = Mathf.Max(0.1f, range);
            attackSpeed = Mathf.Max(0.01f, speed);
            attackType = type;
            targetPriority = priority;
            bonusDamageVsBossMultiplier = Mathf.Max(1f, bossDamageMult);
            attackCooldown = 0f;
            currentTarget = null;
        }

        public void TickCooldown(float deltaTime)
        {
            if (attackCooldown > 0f)
            {
                attackCooldown = Mathf.Max(0f, attackCooldown - deltaTime);
            }
        }

        public ICombatTarget ScanForTarget(IReadOnlyList<ICombatTarget> candidates, Vector3 currentPosition)
        {
            ICombatTarget best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatTarget candidate = candidates[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team != CombatTeam.Monster) continue;

                float distance = Vector2.Distance(currentPosition, candidate.Position);
                if (distance > attackRange) continue;

                float score = targetPriority switch
                {
                    AdventurerTargetPriority.Weakest => candidate.CurrentHP,
                    AdventurerTargetPriority.Strongest => -candidate.CurrentHP,
                    _ => distance
                };

                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            currentTarget = best;
            return best;
        }

        public bool TryAttackTarget(ICombatTarget target, ICombatTarget attacker)
        {
            if (attackCooldown > 0f || target == null || !target.IsAlive) return false;

            attackCooldown = 1f / Mathf.Max(0.01f, attackSpeed);
            float mult = target is BossBattleActor ? bonusDamageVsBossMultiplier : 1f;
            int damage = DamageCalculator.Calculate(attackDamage, target.Defense, mult);
            var info = new DamageInfo(damage, attacker, CombatTeam.Adventurer, attackType);
            target.TakeDamage(info);
            Attacked?.Invoke(target, damage);
            return true;
        }

        public void ClearTarget()
        {
            currentTarget = null;
        }
    }
}
