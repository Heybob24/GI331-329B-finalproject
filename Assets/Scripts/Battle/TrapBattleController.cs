using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class TrapBattleController : MonoBehaviour
    {
        public sealed class TrapRuntime
        {
            public TrapInstanceData Instance;
            public TrapData Definition;
            public Vector3 Position;
            public float CooldownRemaining;
        }

        private sealed class ActiveDamageEffect
        {
            public AdventurerActor Target;
            public TrapRuntime Trap;
            public float Dps;
            public float DurationRemaining;
            public float NextTickTimer;
        }

        private readonly List<TrapRuntime> traps = new List<TrapRuntime>();
        private readonly List<ActiveDamageEffect> activeEffects = new List<ActiveDamageEffect>();

        public IReadOnlyList<TrapRuntime> Traps => traps;

        public void RegisterTrap(TrapInstanceData instance, TrapData definition, Vector3 worldPos)
        {
            if (instance == null || definition == null) return;
            traps.Add(new TrapRuntime
            {
                Instance = instance,
                Definition = definition,
                Position = worldPos,
                CooldownRemaining = 0f
            });
        }

        public void ClearTraps()
        {
            traps.Clear();
            activeEffects.Clear();
        }

        public void TickTraps(float deltaTime, IReadOnlyList<AdventurerActor> adventurers)
        {
            // 1. Tick trap cooldowns & trigger detection
            for (int t = 0; t < traps.Count; t++)
            {
                TrapRuntime trap = traps[t];
                if (trap.CooldownRemaining > 0f)
                {
                    trap.CooldownRemaining = Mathf.Max(0f, trap.CooldownRemaining - deltaTime);
                }

                if (trap.CooldownRemaining > 0f) continue;

                float radius = trap.Instance.TriggerRadius(trap.Definition);
                for (int a = 0; a < adventurers.Count; a++)
                {
                    AdventurerActor raider = adventurers[a];
                    if (raider == null || !raider.IsAlive) continue;

                    if (Vector2.Distance(trap.Position, raider.Position) <= radius)
                    {
                        TriggerTrap(trap, raider);
                        break; // Trigger once per cycle for this trap
                    }
                }
            }

            // 2. Tick DoT effects (Fire, Poison)
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveDamageEffect effect = activeEffects[i];
                if (effect.Target == null || !effect.Target.IsAlive)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                effect.DurationRemaining -= deltaTime;
                effect.NextTickTimer -= deltaTime;

                if (effect.NextTickTimer <= 0f)
                {
                    effect.NextTickTimer = 0.5f;
                    int tickDamage = Mathf.Max(1, Mathf.RoundToInt(effect.Dps * 0.5f));
                    var info = new DamageInfo(tickDamage, null, CombatTeam.Monster, AdventurerAttackType.Magic, false, effect.Trap.Definition.trapName);
                    effect.Target.TakeDamage(info);
                }

                if (effect.DurationRemaining <= 0f)
                {
                    activeEffects.RemoveAt(i);
                }
            }
        }

        private void TriggerTrap(TrapRuntime trap, AdventurerActor target)
        {
            trap.CooldownRemaining = Mathf.Max(0.2f, trap.Instance.Cooldown(trap.Definition));
            float duration = trap.Instance.Duration(trap.Definition);
            Color trapColor = GetTrapColor(trap.Definition.trapType);

            DamageNumber.Spawn(target.Position, $"{trap.Definition.trapName}!", trapColor);

            switch (trap.Definition.trapType)
            {
                case TrapType.Spike:
                case TrapType.Magic:
                    int directDamage = Mathf.Max(1, trap.Instance.Damage(trap.Definition));
                    var info = new DamageInfo(directDamage, null, CombatTeam.Monster, AdventurerAttackType.Magic, false, trap.Definition.trapName);
                    target.TakeDamage(info);
                    break;

                case TrapType.Fire:
                case TrapType.Poison:
                    activeEffects.Add(new ActiveDamageEffect
                    {
                        Target = target,
                        Trap = trap,
                        Dps = trap.Instance.DamagePerSecond(trap.Definition),
                        DurationRemaining = Mathf.Max(0.5f, duration),
                        NextTickTimer = 0.25f
                    });
                    break;

                case TrapType.Slow:
                    float slowPercent = trap.Instance.SlowPercentage(trap.Definition);
                    target.ApplySlow(slowPercent, Mathf.Max(0.5f, duration));
                    break;
            }
        }

        public static Color GetTrapColor(TrapType type) => type switch
        {
            TrapType.Spike => new Color(0.65f, 0.65f, 0.65f),
            TrapType.Fire => new Color(1f, 0.35f, 0.1f),
            TrapType.Poison => new Color(0.35f, 0.85f, 0.25f),
            TrapType.Slow => new Color(0.25f, 0.75f, 1f),
            _ => new Color(0.75f, 0.4f, 1f)
        };
    }
}
