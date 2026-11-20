using System;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class MonsterInstanceData
    {
        public string instanceId;
        public string monsterId;
        public string roomInstanceId;
        public Vector2 localPosition;
        public int level = 1;

        public int MaxHP(MonsterData definition) => definition != null ? definition.MaxHPAtLevel(level) : 0;
        public int CurrentHP(MonsterData definition) => MaxHP(definition);
        public int Attack(MonsterData definition) => definition != null ? definition.AttackAtLevel(level) : 0;
        public int Defense(MonsterData definition) => definition != null ? definition.DefenseAtLevel(level) : 0;
        public float AttackRange(MonsterData definition) => definition != null ? definition.attackRange : 0f;
        public float AttackSpeed(MonsterData definition) => definition != null ? definition.attackSpeed : 0f;
        public float MoveSpeed(MonsterData definition) => definition != null ? definition.moveSpeed : 0f;
    }
}
