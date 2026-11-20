using UnityEngine;

namespace DreamDungeon
{
    public enum CombatTeam
    {
        Adventurer,
        Monster,
        DungeonCore
    }

    public interface ICombatTarget
    {
        CombatTeam Team { get; }
        string Name { get; }
        int CurrentHP { get; }
        int MaxHP { get; }
        int Defense { get; }
        bool IsAlive { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
        void TakeDamage(DamageInfo damageInfo);
    }
}
