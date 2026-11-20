using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(fileName = "BossData", menuName = "Dream Dungeon/Boss/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        [Header("Identification")]
        public string bossId = "cave_dragon";
        public string bossName = "Cave Dragon";
        public Sprite bossIcon;
        [TextArea(2, 4)]
        public string description = "A fearsome dragon residing in deep caverns.";
        public Color bossColor = new Color(0.85f, 0.2f, 0.2f, 1f);

        [Header("Base Combat Stats")]
        public int maxHP = 1500;
        public int attack = 60;
        public int defense = 25;
        public float attackRange = 2.5f;
        public float attackSpeed = 1.0f;
        public float moveSpeed = 1.8f;

        [Header("Progression & Requirements")]
        public int unlockDungeonLevel = 5;

        [Header("Skills")]
        public List<BossSkillData> skills = new List<BossSkillData>();

        [Header("Defeat Rewards")]
        public int defeatExpReward = 300;
        public int defeatGoldReward = 500;
        public int defeatSoulReward = 150;
    }
}
