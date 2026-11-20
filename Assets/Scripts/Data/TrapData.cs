using UnityEngine;

namespace DreamDungeon
{
    public enum TrapType { Spike, Fire, Poison, Slow, Magic }

    [CreateAssetMenu(menuName = "Dream Dungeon/Trap Data", fileName = "TrapData")]
    public sealed class TrapData : ScriptableObject
    {
        public string trapId;
        public string trapName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject trapPrefab;
        public TrapType trapType;
        [Min(0)] public int damage;
        [Min(0)] public float damagePerSecond;
        [Min(0)] public float duration;
        [Min(0)] public float cooldown;
        [Min(0)] public float triggerRadius = 1f;
        [Range(0f, 1f)] public float slowPercentage;
        [Min(0)] public int goldCost;
        [Min(1)] public int unlockDungeonLevel = 1;
        [Min(1)] public int maxLevel = 5;
        [Min(0)] public int baseUpgradeGoldCost = 100;
        [Min(0)] public int baseUpgradeSoulCost;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(trapId)) Debug.LogError($"TrapData '{name}' needs a stable trapId.", this);
            if (string.IsNullOrWhiteSpace(trapName)) Debug.LogError($"TrapData '{name}' needs a name.", this);
            damage = Mathf.Max(0, damage); damagePerSecond = Mathf.Max(0, damagePerSecond); duration = Mathf.Max(0, duration);
            cooldown = Mathf.Max(0, cooldown); triggerRadius = Mathf.Max(0, triggerRadius); slowPercentage = Mathf.Clamp01(slowPercentage);
            goldCost = Mathf.Max(0, goldCost); unlockDungeonLevel = Mathf.Max(1, unlockDungeonLevel); maxLevel = Mathf.Max(1, maxLevel);
            baseUpgradeGoldCost = Mathf.Max(0, baseUpgradeGoldCost); baseUpgradeSoulCost = Mathf.Max(0, baseUpgradeSoulCost);
        }
        public int DamageAtLevel(int level) => Mathf.RoundToInt(damage * (1f + .20f * Mathf.Max(0, level - 1)));
        public float DamagePerSecondAtLevel(int level) => damagePerSecond * (1f + .20f * Mathf.Max(0, level - 1));
        public float SlowAtLevel(int level) => slowPercentage;
    }
}
