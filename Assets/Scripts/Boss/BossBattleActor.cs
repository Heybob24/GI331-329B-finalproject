using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [SelectionBase]
    public sealed class BossBattleActor : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private BossData definition;
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

        private BossPhaseController phaseController;
        private BossSkillController skillController;

        // ICombatTarget
        public CombatTeam Team => CombatTeam.Monster;
        public string Name => definition != null ? definition.bossName : "Boss";
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int Defense => defenseStat;
        public bool IsAlive => currentHP > 0;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;

        public BossData Definition => definition;
        public int Level => level;
        public string InstanceId => instanceId;
        public float AttackRange => attackRange;
        public int CurrentAttack => Mathf.RoundToInt(attackStat * (phaseController != null ? phaseController.AttackMultiplier : 1.0f));
        public BossPhaseController PhaseController => phaseController;
        public BossSkillController SkillController => skillController;

        public event Action<BossBattleActor> Died;
        public event Action<BossBattleActor, int, int> HealthChanged; // actor, currentHP, maxHP

        public void Initialize(BossData data, int bossLevel, string instId, Sprite whiteSprite)
        {
            definition = data;
            level = Mathf.Max(1, bossLevel);
            instanceId = instId;

            maxHP = currentHP = data != null ? data.maxHP : 1500;
            attackStat = data != null ? data.attack : 60;
            defenseStat = data != null ? data.defense : 25;
            attackRange = data != null ? Mathf.Max(0.1f, data.attackRange) : 2.5f;
            attackSpeed = data != null ? Mathf.Max(0.01f, data.attackSpeed) : 1f;
            attackCooldown = 0f;

            phaseController = GetComponent<BossPhaseController>();
            if (phaseController == null)
            {
                phaseController = gameObject.AddComponent<BossPhaseController>();
            }
            phaseController.Initialize();
            phaseController.OnPhaseChanged += HandlePhaseChanged;

            skillController = GetComponent<BossSkillController>();
            if (skillController == null)
            {
                skillController = gameObject.AddComponent<BossSkillController>();
            }
            skillController.Initialize(definition, phaseController, this);

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            animator = GetComponentInChildren<Animator>();

            if (data != null && data.bossIcon != null)
            {
                spriteRenderer.sprite = data.bossIcon;
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
                originalColor = data != null ? data.bossColor : new Color(0.85f, 0.2f, 0.2f);
            }

            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = 6;
            initialScale = transform.localScale;

            CreateHealthBar(whiteSprite);
        }

        private void HandlePhaseChanged(int phase)
        {
            if (phase == 2)
            {
                // Enrage visual indicator
                originalColor = new Color(1f, 0.2f, 0.2f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                DamageNumber.Spawn(transform.position + Vector3.up * 1.5f, "ENRAGED!", Color.red);
            }
        }

        public void Tick(float deltaTime, IReadOnlyList<ICombatTarget> adventurerTargets)
        {
            if (!IsAlive) return;

            skillController?.TickCooldowns(deltaTime);

            if (attackCooldown > 0f)
            {
                attackCooldown = Mathf.Max(0f, attackCooldown - deltaTime);
            }

            // Target scanning
            if (currentTarget == null || !currentTarget.IsAlive || Vector2.Distance(transform.position, currentTarget.Position) > attackRange)
            {
                currentTarget = ScanNearestAdventurer(adventurerTargets);
            }

            if (currentTarget == null || !currentTarget.IsAlive) return;

            // Check if skill can be cast
            if (skillController != null && skillController.CanCastSkill(out var readySkill))
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Attack");
                }
                skillController.CastSkill(readySkill, currentTarget.Position);
                return;
            }

            // Normal Attack
            if (attackCooldown <= 0f && (skillController == null || !skillController.IsCasting))
            {
                PerformAttack(currentTarget);
            }
        }

        private ICombatTarget ScanNearestAdventurer(IReadOnlyList<ICombatTarget> targets)
        {
            if (targets == null || targets.Count == 0) return null;

            ICombatTarget nearest = null;
            float bestDistSq = float.MaxValue;
            Vector2 selfPos = transform.position;

            for (int i = 0; i < targets.Count; i++)
            {
                var candidate = targets[i];
                if (candidate == null || !candidate.IsAlive) continue;

                float distSq = ((Vector2)candidate.Position - selfPos).sqrMagnitude;
                if (distSq <= attackRange * attackRange && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void PerformAttack(ICombatTarget target)
        {
            if (target == null || !target.IsAlive) return;

            float currentAtkSpeed = attackSpeed * (phaseController != null ? phaseController.AttackSpeedMultiplier : 1.0f);
            attackCooldown = 1f / Mathf.Max(0.1f, currentAtkSpeed);

            int damage = DamageCalculator.Calculate(CurrentAttack, target.Defense);
            target.TakeDamage(new DamageInfo(damage, this, CombatTeam.Monster));

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
            DamageNumber.Spawn(transform.position, damageInfo.Amount.ToString(), new Color(1f, 0.4f, 0.4f));
            RefreshHealthBar();

            HealthChanged?.Invoke(this, currentHP, maxHP);
            phaseController?.CheckHealth(currentHP, maxHP);

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
            transform.localScale = initialScale;
        }

        private void CreateHealthBar(Sprite whiteSprite)
        {
            if (whiteSprite == null) return;

            var barRoot = new GameObject("BossHealthBar");
            barRoot.transform.SetParent(transform, false);
            float barY = spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite != whiteSprite
                ? Mathf.Max(1.4f, spriteRenderer.sprite.bounds.max.y + 0.16f)
                : 1.4f;
            barRoot.transform.localPosition = new Vector3(0f, barY, 0f);

            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(barRoot.transform, false);
            var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = whiteSprite;
            bgRenderer.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            bgRenderer.sortingOrder = 9;
            bgObj.transform.localScale = new Vector3(1.4f, 0.18f, 1f);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barRoot.transform, false);
            healthBarFill = fillObj.AddComponent<SpriteRenderer>();
            healthBarFill.sprite = whiteSprite;
            healthBarFill.color = new Color(0.85f, 0.1f, 0.1f);
            healthBarFill.sortingOrder = 10;
            fillObj.transform.localScale = new Vector3(1.36f, 0.14f, 1f);
        }

        private void RefreshHealthBar()
        {
            if (healthBarFill == null) return;
            float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
            healthBarFill.transform.localScale = new Vector3(1.36f * ratio, 0.14f, 1f);
        }

        private void OnDestroy()
        {
            if (phaseController != null)
            {
                phaseController.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
    }
}
