using System;
using UnityEngine;

namespace DreamDungeon
{
    [SelectionBase]
    public sealed class DungeonCoreActor : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private int maxHP = 500;
        [SerializeField] private int defense = 1;

        private int currentHP;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Vector3 initialScale;

        // ICombatTarget
        public CombatTeam Team => CombatTeam.DungeonCore;
        public string Name => "Dungeon Core";
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int Defense => defense;
        public bool IsAlive => currentHP > 0;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;

        public event Action<int, int> HPChanged;
        public event Action CoreDestroyed;

        public void Initialize(int baseCoreHP, int coreDefense, int coreRoomLevel, Sprite whiteSprite, float cellSize)
        {
            defense = Mathf.Max(0, coreDefense);
            float levelMultiplier = 1f + 0.1f * Mathf.Max(0, coreRoomLevel - 1);
            maxHP = currentHP = Mathf.RoundToInt(Mathf.Max(1, baseCoreHP) * levelMultiplier);

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (whiteSprite != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = whiteSprite;
            }

            originalColor = new Color(0.66f, 0.31f, 0.82f);
            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = 4;
            transform.localScale = Vector3.one * (cellSize * 0.75f);
            initialScale = transform.localScale;

            HPChanged?.Invoke(currentHP, maxHP);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            currentHP = Mathf.Max(0, currentHP - damageInfo.Amount);
            DamageNumber.Spawn(transform.position, damageInfo.Amount.ToString(), new Color(1f, 0.2f, 0.2f), 0.045f);
            HPChanged?.Invoke(currentHP, maxHP);

            if (currentHP == 0)
            {
                CoreDestroyed?.Invoke();
            }
            else
            {
                FlashHit();
            }
        }

        private void FlashHit()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                CancelInvoke(nameof(RestoreColor));
                Invoke(nameof(RestoreColor), 0.09f);
            }
            transform.localScale = initialScale * 1.12f;
            CancelInvoke(nameof(RestoreScale));
            Invoke(nameof(RestoreScale), 0.09f);
        }

        private void RestoreColor()
        {
            if (spriteRenderer != null && IsAlive)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void RestoreScale()
        {
            if (IsAlive)
            {
                transform.localScale = initialScale;
            }
        }
    }
}
