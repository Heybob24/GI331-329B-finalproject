using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class BossSkillController : MonoBehaviour
    {
        private BossData _bossData;
        private BossPhaseController _phaseController;
        private BossBattleActor _actor;

        private readonly Dictionary<BossSkillData, float> _cooldownTimers = new Dictionary<BossSkillData, float>();
        private bool _isTelegraphing = false;
        private GameObject _activeTelegraphObject;

        public bool IsCasting => _isTelegraphing;

        public event Action<BossSkillData> OnSkillTelegraphStarted;
        public event Action<BossSkillData> OnSkillExecuted;

        public void Initialize(BossData data, BossPhaseController phaseCtrl, BossBattleActor actor)
        {
            _bossData = data;
            _phaseController = phaseCtrl;
            _actor = actor;
            _cooldownTimers.Clear();

            if (_bossData != null && _bossData.skills != null)
            {
                foreach (var skill in _bossData.skills)
                {
                    if (skill != null)
                    {
                        // Initial small delay before first skill use
                        _cooldownTimers[skill] = 3.0f;
                    }
                }
            }
        }

        public void TickCooldowns(float deltaTime)
        {
            if (_cooldownTimers.Count == 0) return;

            var keys = new List<BossSkillData>(_cooldownTimers.Keys);
            foreach (var skill in keys)
            {
                if (_cooldownTimers[skill] > 0f)
                {
                    _cooldownTimers[skill] -= deltaTime;
                }
            }
        }

        public bool CanCastSkill(out BossSkillData readySkill)
        {
            readySkill = null;
            if (_isTelegraphing || _bossData == null || _bossData.skills == null) return false;

            foreach (var skill in _bossData.skills)
            {
                if (skill != null && _cooldownTimers.TryGetValue(skill, out float cd) && cd <= 0f)
                {
                    readySkill = skill;
                    return true;
                }
            }
            return false;
        }

        public void CastSkill(BossSkillData skill, Vector3 targetPosition)
        {
            if (skill == null || _isTelegraphing) return;
            StartCoroutine(ExecuteSkillRoutine(skill, targetPosition));
        }

        private IEnumerator ExecuteSkillRoutine(BossSkillData skill, Vector3 targetPosition)
        {
            _isTelegraphing = true;
            OnSkillTelegraphStarted?.Invoke(skill);

            // Create telegraph visual indicator
            CreateTelegraphVisual(skill, targetPosition);

            yield return new WaitForSeconds(skill.telegraphDuration);

            // Destroy telegraph visual
            DestroyTelegraphVisual();

            // Execute AoE damage
            ExecuteSkillDamage(skill, targetPosition);

            // Reset cooldown
            float cdMult = _phaseController != null ? _phaseController.CooldownMultiplier : 1.0f;
            _cooldownTimers[skill] = skill.cooldown * cdMult;

            _isTelegraphing = false;
            OnSkillExecuted?.Invoke(skill);
        }

        private void ExecuteSkillDamage(BossSkillData skill, Vector3 targetPosition)
        {
            int baseAtk = _actor != null ? _actor.CurrentAttack : (_bossData != null ? _bossData.attack : 50);
            int skillAtk = Mathf.RoundToInt(baseAtk * skill.damageMultiplier);

            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, skill.radius);
            if (hits != null)
            {
                foreach (var hit in hits)
                {
                    var target = hit.GetComponentInParent<ICombatTarget>();
                    if (target != null && target.Team == CombatTeam.Adventurer && target.IsAlive)
                    {
                        int dmg = DamageCalculator.Calculate(skillAtk, target.Defense);
                        target.TakeDamage(new DamageInfo(dmg, _actor, CombatTeam.Monster));
                    }
                }
            }
        }

        private void CreateTelegraphVisual(BossSkillData skill, Vector3 position)
        {
            DestroyTelegraphVisual();

            _activeTelegraphObject = new GameObject("BossSkillTelegraph");
            _activeTelegraphObject.transform.position = position;

            var renderer = _activeTelegraphObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite();
            renderer.color = skill.telegraphColor;
            renderer.sortingOrder = 5;

            float diameter = skill.radius * 2f;
            _activeTelegraphObject.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private void DestroyTelegraphVisual()
        {
            if (_activeTelegraphObject != null)
            {
                Destroy(_activeTelegraphObject);
                _activeTelegraphObject = null;
            }
        }

        private Sprite CreateCircleSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 2f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.7f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void OnDestroy()
        {
            DestroyTelegraphVisual();
        }
    }
}
