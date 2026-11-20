using UnityEngine;

namespace DreamDungeon
{
    public static class DamageCalculator
    {
        public static int Calculate(int attack, int defense, float multiplier = 1f)
        {
            int baseDamage = Mathf.Max(1, attack - defense);
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, multiplier)));
        }
    }
}
