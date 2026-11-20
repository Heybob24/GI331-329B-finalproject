using UnityEngine;

namespace DreamDungeon
{
    public struct DamageInfo
    {
        public int Amount;
        public ICombatTarget Source;
        public CombatTeam SourceTeam;
        public AdventurerAttackType AttackType;
        public bool IsCritical;
        public string EffectLabel;

        public DamageInfo(int amount, ICombatTarget source, CombatTeam sourceTeam, AdventurerAttackType attackType = AdventurerAttackType.Melee, bool isCritical = false, string effectLabel = null)
        {
            Amount = Mathf.Max(1, amount);
            Source = source;
            SourceTeam = sourceTeam;
            AttackType = attackType;
            IsCritical = isCritical;
            EffectLabel = effectLabel;
        }
    }
}
