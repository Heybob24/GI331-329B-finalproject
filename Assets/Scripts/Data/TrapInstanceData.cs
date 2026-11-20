using System;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class TrapInstanceData
    {
        public string instanceId;
        public string trapId;
        public string roomInstanceId;
        public Vector2 localPosition;
        public int level = 1;
        public int Damage(TrapData definition) => definition != null ? definition.DamageAtLevel(level) : 0;
        public float DamagePerSecond(TrapData definition) => definition != null ? definition.DamagePerSecondAtLevel(level) : 0f;
        public float Duration(TrapData definition) => definition != null ? definition.duration : 0f;
        public float Cooldown(TrapData definition) => definition != null ? definition.cooldown : 0f;
        public float TriggerRadius(TrapData definition) => definition != null ? definition.triggerRadius : 0f;
        public float SlowPercentage(TrapData definition) => definition != null ? definition.SlowAtLevel(level) : 0f;
    }
}
