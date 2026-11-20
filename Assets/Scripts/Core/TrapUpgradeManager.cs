using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class TrapUpgradeManager : MonoBehaviour
    {
        [SerializeField] private TrapDatabase database;
        [SerializeField] private CurrencyManager currency;
        [SerializeField] private RoomManager roomManager;
        public event Action<TrapInstanceData> TrapUpgraded;
        public void Configure(TrapDatabase db, CurrencyManager money, RoomManager rooms) { database = db; currency = money; roomManager = rooms; }
        public bool CanUpgrade(string instanceId, out int gold, out int soul, out string error)
        {
            gold = soul = 0; TrapInstanceData unit = roomManager != null ? roomManager.FindTrap(instanceId) : null;
            TrapData def = unit != null && database != null ? database.FindById(unit.trapId) : null;
            if (def == null) { error = "Placed trap or its definition was not found."; return false; }
            if (unit.level >= def.maxLevel) { error = $"Maximum trap level {def.maxLevel} reached."; return false; }
            long g = (long)def.baseUpgradeGoldCost * unit.level, s = (long)def.baseUpgradeSoulCost * unit.level;
            if (g > int.MaxValue || s > int.MaxValue) { error = "Upgrade cost exceeds the currency limit."; return false; }
            gold = (int)g; soul = (int)s;
            if (currency == null || !currency.CanAfford(gold, soul)) { error = $"Upgrade costs {gold} Gold and {soul} Soul."; return false; }
            error = string.Empty; return true;
        }
        public bool TryUpgrade(string id, out string message)
        {
            if (!CanUpgrade(id, out int gold, out int soul, out message)) return false;
            TrapInstanceData unit = roomManager.FindTrap(id); TrapData def = database.FindById(unit.trapId);
            if (!currency.TrySpend(gold, soul, 0, out message)) return false;
            if (!roomManager.TrySetTrapLevel(id, unit.level + 1, def.maxLevel, gold, soul, out message)) { currency.TryAdd(gold, soul, 0, out _); return false; }
            unit = roomManager.FindTrap(id); message = $"{def.trapName} upgraded to Level {unit.level}."; TrapUpgraded?.Invoke(unit); return true;
        }
    }
}
