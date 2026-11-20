using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(fileName = "BossDatabase", menuName = "Dream Dungeon/Boss/Boss Database")]
    public sealed class BossDatabase : ScriptableObject
    {
        public List<BossData> bosses = new List<BossData>();

        public BossData GetBoss(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return null;
            return bosses.Find(b => b != null && string.Equals(b.bossId, bossId, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<BossData> GetAllBosses() => bosses;
    }
}
