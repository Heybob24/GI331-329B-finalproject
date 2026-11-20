using System;
using UnityEngine;

namespace DreamDungeon
{
    public enum BossSkillType
    {
        FireBreath,
        HeavySmash,
        DarkExplosion
    }

    [Serializable]
    public sealed class BossSkillData
    {
        public string skillName = "Fire Breath";
        public BossSkillType skillType = BossSkillType.FireBreath;
        [Tooltip("Damage multiplier based on Boss attack power")]
        public float damageMultiplier = 1.5f;
        public float cooldown = 8f;
        public float radius = 3.5f;
        public float telegraphDuration = 1.2f;
        [TextArea(2, 4)]
        public string description = "Breaths scorching fire in an area.";
        public Color telegraphColor = new Color(1f, 0.3f, 0.1f, 0.4f);
    }
}
