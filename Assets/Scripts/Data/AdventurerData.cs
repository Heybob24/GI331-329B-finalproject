using UnityEngine;

namespace DreamDungeon
{
    public enum AdventurerAttackType { Melee, Ranged, Magic }
    public enum AdventurerTargetPriority { Nearest, Weakest, Strongest }

    [CreateAssetMenu(menuName = "Dream Dungeon/Adventurer Data", fileName = "AdventurerData")]
    public sealed class AdventurerData : ScriptableObject
    {
        public string adventurerId;
        public string adventurerName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject prefab;
        [Min(1)] public int maxHP = 100;
        [Min(0)] public int attack = 10;
        [Min(0)] public int defense;
        [Min(0.1f)] public float attackRange = 1f;
        [Min(0.01f)] public float attackSpeed = 1f;
        [Min(0.1f)] public float moveSpeed = 1f;
        public AdventurerAttackType attackType;
        public AdventurerTargetPriority targetPriority;

        private void OnValidate()
        {
            maxHP = Mathf.Max(1, maxHP); attack = Mathf.Max(0, attack); defense = Mathf.Max(0, defense);
            attackRange = Mathf.Max(.1f, attackRange); attackSpeed = Mathf.Max(.01f, attackSpeed); moveSpeed = Mathf.Max(.1f, moveSpeed);
        }
    }
}
