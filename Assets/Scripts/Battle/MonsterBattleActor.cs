using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [SelectionBase]
    public sealed class MonsterBattleActor : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private MonsterData definition;
        [SerializeField] private int level = 1;

        private string instanceId;
        private int currentHP;
        private int maxHP;
        private int attackStat;
        private int defenseStat;
        private float attackRange;
        private float attackSpeed;
        private float attackCooldown;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Color originalColor;
        private SpriteRenderer healthBarFill;
        private Vector3 initialScale;
        private ICombatTarget currentTarget;

        // ICombatTarget
        public CombatTeam Team => CombatTeam.Monster;
        public string Name => definition != null ? definition.monsterName : "Monster";
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int Defense => defenseStat;
        public bool IsAlive => currentHP > 0;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;

        public MonsterData Definition => definition;
        public int Level => level;
        public string InstanceId => instanceId;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;

        public event Action<MonsterBattleActor> Died;

        public void Initialize(MonsterData data, int monsterLevel, string instId, Sprite whiteSprite)
        {
            definition = data;
            level = Mathf.Max(1, monsterLevel);
            instanceId = instId;

            maxHP = currentHP = data != null ? data.MaxHPAtLevel(level) : 100;
            attackStat = data != null ? data.AttackAtLevel(level) : 10;
            defenseStat = data != null ? data.DefenseAtLevel(level) : 5;
            attackRange = data != null ? Mathf.Max(0.1f, data.attackRange) : 1f;
            attackSpeed = data != null ? Mathf.Max(0.01f, data.attackSpeed) : 1f;
            attackCooldown = 0f;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            animator = GetComponentInChildren<Animator>();

            if (data != null && data.icon != null)
            {
                spriteRenderer.sprite = data.icon;
                originalColor = Color.white;
            }
            else if (spriteRenderer.sprite != null && spriteRenderer.sprite != whiteSprite)
            {
                originalColor = Color.white;
            }
            else
            {
                if (whiteSprite != null && spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = whiteSprite;
                }
                originalColor = new Color(0.42f, 0.85f, 0.42f);
            }

            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = 5;
            initialScale = transform.localScale;

            CreateHealthBar(whiteSprite);
        }

        public void Tick(float deltaTime, IReadOnlyList<ICombatTarget> adventurerTargets)
        {
            if (!IsAlive) return;

            if (attackCooldown > 0f)
            {
                attackCooldown = Mathf.Max(0f, attackCooldown - deltaTime);
            }

            // Target scanning
            if (currentTarget == null || !currentTarget.IsAlive || Vector2.Distance(transform.position, currentTarget.Position) > attackRange)
            {
                currentTarget = ScanNearestAdventurer(adventurerTargets);
            }

            if (currentTarget != null && currentTarget.IsAlive)
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetBool("IsMoving", false);
                }
                if (attackCooldown <= 0f)
                {
                    PerformAttack(currentTarget);
                }
            }
            else if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsMoving", false);
            }
        }

        private ICombatTarget ScanNearestAdventurer(IReadOnlyList<ICombatTarget> targets)
        {
            ICombatTarget best = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                ICombatTarget candidate = targets[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team != CombatTeam.Adventurer) continue;

                float dist = Vector2.Distance(transform.position, candidate.Position);
                if (dist <= attackRange && dist < nearestDistance)
                {
                    best = candidate;
                    nearestDistance = dist;
                }
            }

            return best;
        }

        private void PerformAttack(ICombatTarget target)
        {
            attackCooldown = 1f / Mathf.Max(0.01f, attackSpeed);
            int damage = DamageCalculator.Calculate(attackStat, target.Defense);
            var info = new DamageInfo(damage, this, CombatTeam.Monster);
            target.TakeDamage(info);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Attack");
            }
            PulseVisual();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            currentHP = Mathf.Max(0, currentHP - damageInfo.Amount);
            DamageNumber.Spawn(transform.position, damageInfo.Amount.ToString(), new Color(1f, 0.9f, 0.2f));
            RefreshHealthBar();

            if (currentHP == 0)
            {
                Die();
            }
            else
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Hit");
                }
                FlashHit();
            }
        }

        private void Die()
        {
            Died?.Invoke(this);
            if (animator != null && animator.runtimeAnimatorController != null && gameObject.activeInHierarchy)
            {
                animator.ResetTrigger("Attack");
                animator.ResetTrigger("Hit");
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("Die");
                Invoke(nameof(DeactivateAfterDeath), 0.75f);
            }
            else
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                }
                gameObject.SetActive(false);
            }
        }

        private void DeactivateAfterDeath()
        {
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }

        private void FlashHit()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                CancelInvoke(nameof(RestoreColor));
                Invoke(nameof(RestoreColor), 0.08f);
            }
        }

        private void RestoreColor()
        {
            if (spriteRenderer != null && IsAlive)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void PulseVisual()
        {
            transform.localScale = initialScale * 1.15f;
            CancelInvoke(nameof(RestoreScale));
            Invoke(nameof(RestoreScale), 0.08f);
        }

        private void RestoreScale()
        {
            if (IsAlive)
            {
                transform.localScale = initialScale;
            }
        }

        private void CreateHealthBar(Sprite whiteSprite)
        {
            if (whiteSprite == null) return;
            GameObject bgObj = new GameObject("HealthBar_BG");
            bgObj.transform.SetParent(transform, false);
            float barY = spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite != whiteSprite
                ? Mathf.Max(0.65f, spriteRenderer.sprite.bounds.max.y + 0.12f)
                : 0.65f;
            bgObj.transform.localPosition = new Vector3(0f, barY, -0.02f);
            bgObj.transform.localScale = new Vector3(1.15f, 0.12f, 1f);

            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = whiteSprite;
            bgSr.color = new Color(0.08f, 0.06f, 0.07f, 0.85f);
            bgSr.sortingOrder = 10;

            GameObject fillObj = new GameObject("HealthBar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            fillObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fillObj.transform.localScale = Vector3.one;

            healthBarFill = fillObj.AddComponent<SpriteRenderer>();
            healthBarFill.sprite = whiteSprite;
            healthBarFill.color = new Color(0.28f, 0.90f, 0.38f);
            healthBarFill.sortingOrder = 11;

            RefreshHealthBar();
        }

        private void RefreshHealthBar()
        {
            if (healthBarFill != null)
            {
                float ratio = maxHP > 0 ? (float)currentHP / maxHP : 0f;
                healthBarFill.transform.localScale = new Vector3(ratio, 1f, 1f);
                healthBarFill.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.01f);
                healthBarFill.color = ratio > 0.5f ? new Color(0.28f, 0.90f, 0.38f) : (ratio > 0.25f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.92f, 0.25f, 0.25f));
            }
        }
    }
}
