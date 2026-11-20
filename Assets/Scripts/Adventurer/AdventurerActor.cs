using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [SelectionBase]
    public sealed class AdventurerActor : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private AdventurerData definition;
        [SerializeField] private AdventurerState currentState = AdventurerState.Spawning;

        private string instanceId;
        private int currentHP;
        private int maxHP;
        private int attackStat;
        private int defenseStat;
        private float attackRange;
        private float attackSpeed;
        private float moveSpeed;
        private int waveIndex;
        private int passiveHealPerSecond;
        private float passiveHealTimer;
        private string factionId = string.Empty;

        private AdventurerMovement movement;
        private AdventurerAttack attack;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private SpriteRenderer healthBarFill;
        private Vector3 initialScale;

        // ICombatTarget
        public CombatTeam Team => CombatTeam.Adventurer;
        public string Name => definition != null ? definition.adventurerName : "Adventurer";
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int Defense => defenseStat;
        public bool IsAlive => currentHP > 0 && currentState != AdventurerState.Dead;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;

        public AdventurerData Definition => definition;
        public AdventurerState CurrentState => currentState;
        public int AttackStat => attackStat;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public float MoveSpeed => moveSpeed;
        public int WaveIndex => waveIndex;
        public string InstanceId => instanceId;
        public string FactionId => factionId;
        public int PassiveHealPerSecond => passiveHealPerSecond;
        public AdventurerMovement Movement => movement;
        public AdventurerAttack Attack => attack;

        public event Action<AdventurerActor> Died;
        public event Action<AdventurerState> StateChanged;

        public void ApplyFactionPassive(AdventurerFactionData faction)
        {
            if (faction == null || faction.factionPassiveBonus == null) return;
            factionId = faction.factionId;
            passiveHealPerSecond = Mathf.Max(0, faction.factionPassiveBonus.healPerSecond);
            if (attack != null && definition != null)
            {
                attack.Configure(attackStat, attackRange, attackSpeed, definition.attackType, definition.targetPriority,
                    faction.factionPassiveBonus.bonusDamageVsBossMultiplier);
            }
        }

        public void Initialize(AdventurerData data, int wave, List<Vector2Int> path, Func<Vector2Int, Vector3> worldConverter, Vector3 spawnOffset, Sprite whiteSprite, string id)
        {
            definition = data;
            waveIndex = wave;
            instanceId = id;

            maxHP = currentHP = Mathf.Max(1, data.maxHP);
            attackStat = Mathf.Max(0, data.attack);
            defenseStat = Mathf.Max(0, data.defense);
            attackRange = Mathf.Max(0.1f, data.attackRange);
            attackSpeed = Mathf.Max(0.01f, data.attackSpeed);
            moveSpeed = Mathf.Max(0.1f, data.moveSpeed);

            movement = gameObject.GetComponent<AdventurerMovement>() ?? gameObject.AddComponent<AdventurerMovement>();
            movement.Configure(path, worldConverter, moveSpeed, spawnOffset);
            movement.ReachedCore += OnReachedCore;

            attack = gameObject.GetComponent<AdventurerAttack>() ?? gameObject.AddComponent<AdventurerAttack>();
            attack.Configure(attackStat, attackRange, attackSpeed, data.attackType, data.targetPriority);
            attack.Attacked += OnAttacked;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (whiteSprite != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = whiteSprite;
            }

            originalColor = data.attackType switch
            {
                AdventurerAttackType.Melee => new Color(0.35f, 0.68f, 1f),
                AdventurerAttackType.Ranged => new Color(0.35f, 0.92f, 0.78f),
                _ => new Color(0.85f, 0.50f, 1f)
            };
            if (data.adventurerId == "knight")
            {
                originalColor = new Color(0.88f, 0.75f, 0.35f);
            }

            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = 6;
            initialScale = transform.localScale;

            CreateHealthBar(whiteSprite);
            SetState(AdventurerState.Moving);
        }

        public void SetState(AdventurerState nextState)
        {
            if (currentState == AdventurerState.Dead && nextState != AdventurerState.Dead) return;
            currentState = nextState;
            StateChanged?.Invoke(currentState);
        }

        public void Tick(float deltaTime, IReadOnlyList<ICombatTarget> monsterTargets, ICombatTarget coreTarget)
        {
            if (!IsAlive) return;

            if (passiveHealPerSecond > 0 && currentHP < maxHP)
            {
                passiveHealTimer += deltaTime;
                if (passiveHealTimer >= 1f)
                {
                    passiveHealTimer -= 1f;
                    currentHP = Mathf.Min(maxHP, currentHP + passiveHealPerSecond);
                    RefreshHealthBar();
                }
            }

            attack.TickCooldown(deltaTime);

            // 1. If reaching core or already at core, focus on attacking core
            if (movement.HasReachedEnd || currentState == AdventurerState.ReachingCore || currentState == AdventurerState.AttackingCore)
            {
                if (coreTarget != null && coreTarget.IsAlive)
                {
                    SetState(AdventurerState.AttackingCore);
                    attack.TryAttackTarget(coreTarget, this);
                }
                return;
            }

            // 2. Scan for Monster defenders in attack range
            ICombatTarget target = attack.ScanForTarget(monsterTargets, transform.position);
            if (target != null && target.IsAlive)
            {
                SetState(AdventurerState.Attacking);
                attack.TryAttackTarget(target, this);
                return;
            }

            // 3. If no target in range, resume movement towards Core
            attack.ClearTarget();
            SetState(AdventurerState.Moving);
            movement.TickMovement(deltaTime);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            currentHP = Mathf.Max(0, currentHP - damageInfo.Amount);
            DamageNumber.Spawn(transform.position, damageInfo.Amount.ToString(), new Color(1f, 0.35f, 0.35f));
            RefreshHealthBar();

            if (currentHP == 0)
            {
                Die();
            }
            else
            {
                SetState(AdventurerState.TakingDamage);
                FlashHit();
            }
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            if (movement != null)
            {
                movement.ApplySlow(slowPercent, duration);
            }
        }

        private void OnReachedCore()
        {
            if (!IsAlive) return;
            SetState(AdventurerState.ReachingCore);
        }

        private void OnAttacked(ICombatTarget target, int damage)
        {
            PulseVisual();
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

        private void Die()
        {
            SetState(AdventurerState.Dead);
            if (movement != null) movement.enabled = false;
            if (attack != null) attack.enabled = false;
            Died?.Invoke(this);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }
            gameObject.SetActive(false);
        }

        private void CreateHealthBar(Sprite whiteSprite)
        {
            if (whiteSprite == null) return;
            GameObject bgObj = new GameObject("HealthBar_BG");
            bgObj.transform.SetParent(transform, false);
            bgObj.transform.localPosition = new Vector3(0f, 0.65f, -0.02f);
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

        private void OnDestroy()
        {
            if (movement != null) movement.ReachedCore -= OnReachedCore;
            if (attack != null) attack.Attacked -= OnAttacked;
        }
    }
}
