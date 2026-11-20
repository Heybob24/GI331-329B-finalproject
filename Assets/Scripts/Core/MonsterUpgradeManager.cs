using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class MonsterUpgradeManager : MonoBehaviour
    {
        [SerializeField] private MonsterDatabase database;
        [SerializeField] private CurrencyManager currency;
        [SerializeField] private RoomManager roomManager;
        public event Action<MonsterInstanceData> MonsterUpgraded;
        public void Configure(MonsterDatabase db, CurrencyManager money, RoomManager rooms) { database = db; currency = money; roomManager = rooms; }
        public bool CanUpgrade(string instanceId, out int gold, out int soul, out string error)
        {
            gold = soul = 0; MonsterInstanceData unit = roomManager != null ? roomManager.FindMonster(instanceId) : null;
            MonsterData def = unit != null && database != null ? database.FindById(unit.monsterId) : null;
            if (def == null) { error = "Placed monster or its definition was not found."; return false; }
            if (unit.level >= def.maxLevel) { error = $"Maximum monster level {def.maxLevel} reached."; return false; }
            long g = (long)def.baseUpgradeGoldCost * unit.level, s = (long)def.baseUpgradeSoulCost * unit.level;
            if (g > int.MaxValue || s > int.MaxValue) { error = "Upgrade cost exceeds the currency limit."; return false; }
            gold = (int)g; soul = (int)s;
            if (currency == null || !currency.CanAfford(gold, soul)) { error = $"Upgrade costs {gold} Gold and {soul} Soul."; return false; }
            error = string.Empty; return true;
        }
        public bool TryUpgrade(string id, out string message)
        {
            if (!CanUpgrade(id, out int gold, out int soul, out message)) return false;
            MonsterInstanceData unit = roomManager.FindMonster(id); MonsterData def = database.FindById(unit.monsterId);
            if (!currency.TrySpend(gold, soul, 0, out message)) return false;
            if (!roomManager.TrySetMonsterLevel(id, unit.level + 1, def.maxLevel, gold, soul, out message)) { currency.TryAdd(gold, soul, 0, out _); return false; }
            unit = roomManager.FindMonster(id); message = $"{def.monsterName} upgraded to Level {unit.level}."; MonsterUpgraded?.Invoke(unit); return true;
        }
    }
}
