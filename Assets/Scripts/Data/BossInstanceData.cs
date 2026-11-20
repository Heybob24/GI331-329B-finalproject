using System;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class BossInstanceData
    {
        public string instanceId;
        public string bossId;
        public string roomInstanceId;
        public Vector2 localPosition;
        public int level = 1;

        public int MaxHP(BossData definition) => definition != null ? definition.maxHP : 0;
        public int Attack(BossData definition) => definition != null ? definition.attack : 0;
        public int Defense(BossData definition) => definition != null ? definition.defense : 0;
        public float AttackRange(BossData definition) => definition != null ? definition.attackRange : 0f;
        public float AttackSpeed(BossData definition) => definition != null ? definition.attackSpeed : 0f;
        public float MoveSpeed(BossData definition) => definition != null ? definition.moveSpeed : 0f;
    }
}
