using System;
using UnityEngine;

namespace DreamDungeon
{
    public enum ElitePassiveType
    {
        None,
        Regeneration,
        DamageResistance,
        FrenzySpeed,
        CoreBreaker
    }

    [CreateAssetMenu(fileName = "EliteEnemyData", menuName = "Dream Dungeon/Endless/Elite Enemy Data")]
    public sealed class EliteEnemyData : ScriptableObject
    {
        [Header("Identification")]
        public string eliteId = "elite_knight";
        public string eliteName = "Elite Knight";
        public string baseAdventurerId = "knight";
        [TextArea(2, 3)]
        public string description = "Veteran armored champion with reinforced defenses.";

        [Header("Stat Multipliers")]
        [Min(1f)] public float hpMultiplier = 1.8f;
        [Min(1f)] public float damageMultiplier = 1.4f;
        [Min(1f)] public float defenseMultiplier = 1.5f;
        [Min(0.5f)] public float moveSpeedMultiplier = 1.1f;
        [Min(0.5f)] public float attackSpeedMultiplier = 1.15f;
        [Range(0f, 0.75f)] public float damageResistance = 0.20f;

        [Header("Passive Ability")]
        public ElitePassiveType passiveType = ElitePassiveType.DamageResistance;
        [Min(0)] public int passiveHealPerSecond = 0;

        [Header("Visual Presentation")]
        public Color eliteTint = new Color(1.0f, 0.82f, 0.28f, 1f);
        [Range(1.0f, 1.5f)] public float scaleMultiplier = 1.18f;

        public AdventurerData CreateRuntimeEliteAdventurerData(AdventurerData baseData, float waveHpMult = 1f, float waveAtkMult = 1f, float waveDefMult = 1f)
        {
            if (baseData == null) return null;

            float safeWaveHp = Mathf.Clamp(waveHpMult, 0.1f, 500f);
            float safeWaveAtk = Mathf.Clamp(waveAtkMult, 0.1f, 500f);
            float safeWaveDef = Mathf.Clamp(waveDefMult, 0.1f, 500f);

            AdventurerData copy = CreateInstance<AdventurerData>();
            copy.name = $"{eliteId}_Runtime";
            copy.adventurerId = eliteId;
            copy.adventurerName = eliteName;
            copy.description = description;
            copy.icon = baseData.icon;
            copy.prefab = baseData.prefab;
            copy.attackType = baseData.attackType;
            copy.targetPriority = baseData.targetPriority;
            copy.attackRange = baseData.attackRange;

            long rawHp = (long)Mathf.RoundToInt(baseData.maxHP * Mathf.Max(1f, hpMultiplier) * safeWaveHp);
            long rawAtk = (long)Mathf.RoundToInt(baseData.attack * Mathf.Max(1f, damageMultiplier) * safeWaveAtk);
            long rawDef = (long)Mathf.RoundToInt(Mathf.Max(1, baseData.defense) * Mathf.Max(1f, defenseMultiplier) * safeWaveDef);

            copy.maxHP = (int)Mathf.Clamp(rawHp, 1, 2000000);
            copy.attack = (int)Mathf.Clamp(rawAtk, 1, 500000);
            copy.defense = (int)Mathf.Clamp(rawDef, 0, 500000);
            copy.moveSpeed = Mathf.Clamp(baseData.moveSpeed * Mathf.Max(0.5f, moveSpeedMultiplier), 0.2f, 6f);
            copy.attackSpeed = Mathf.Clamp(baseData.attackSpeed * Mathf.Max(0.5f, attackSpeedMultiplier), 0.2f, 6f);
            return copy;
        }

        public static EliteEnemyData CreateRuntimePreset(
            string id, string title, string baseAdvId, string desc,
            float hpMult, float atkMult, float defMult, float moveMult, float atkSpdMult,
            float resistance, ElitePassiveType passive, int healPerSec, Color tint, float scaleMult = 1.18f)
        {
            EliteEnemyData data = CreateInstance<EliteEnemyData>();
            data.name = id;
            data.eliteId = id;
            data.eliteName = title;
            data.baseAdventurerId = baseAdvId;
            data.description = desc;
            data.hpMultiplier = hpMult;
            data.damageMultiplier = atkMult;
            data.defenseMultiplier = defMult;
            data.moveSpeedMultiplier = moveMult;
            data.attackSpeedMultiplier = atkSpdMult;
            data.damageResistance = resistance;
            data.passiveType = passive;
            data.passiveHealPerSecond = healPerSec;
            data.eliteTint = tint;
            data.scaleMultiplier = scaleMult;
            return data;
        }
    }
}
