using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum FactionPassiveType
    {
        Balanced,
        HolyRegeneration,
        SwiftStrike,
        ArcaneReach,
        BossSlayer,
        DemonicHunt
    }

    [Serializable]
    public sealed class FactionPassiveBonus
    {
        public FactionPassiveType passiveType = FactionPassiveType.Balanced;
        public string passiveDescription = "Standard guild combat training.";
        [Min(0.5f)] public float healthMultiplier = 1.0f;
        [Min(0.5f)] public float attackMultiplier = 1.0f;
        [Min(0.5f)] public float defenseMultiplier = 1.0f;
        [Min(0.5f)] public float attackSpeedMultiplier = 1.0f;
        [Min(0.5f)] public float moveSpeedMultiplier = 1.0f;
        [Min(1.0f)] public float bonusDamageVsBossMultiplier = 1.0f;
        [Min(0)] public int healPerSecond = 0;
    }

    [Serializable]
    public sealed class FactionSpawnConfiguration
    {
        [Min(0.2f)] public float spawnIntervalMultiplier = 1.0f;
        [Min(0f)] public float initialSpawnDelay = 0.25f;
    }

    [Serializable]
    public sealed class FactionRewardBonus
    {
        [Min(0)] public int bonusGold = 50;
        [Min(0)] public int bonusSoul = 10;
        [Min(0)] public int bonusEXP = 25;
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Adventurer Faction Data", fileName = "AdventurerFactionData")]
    public sealed class AdventurerFactionData : ScriptableObject
    {
        [Header("Identity")]
        public string factionId = "adventurer_guild";
        public string factionName = "Adventurer Guild";
        [TextArea] public string description = "Standard adventurers including swordsmen and archers.";
        public Sprite factionIcon;

        [Header("Roster & Difficulty")]
        public List<string> adventurerTypes = new List<string>();
        public StageDifficulty factionDifficulty = StageDifficulty.Easy;

        [Header("Passive & Spawn & Reward")]
        public FactionPassiveBonus factionPassiveBonus = new FactionPassiveBonus();
        public FactionSpawnConfiguration factionSpawnConfiguration = new FactionSpawnConfiguration();
        public FactionRewardBonus factionRewardBonus = new FactionRewardBonus();

        private static Dictionary<string, AdventurerFactionData> defaultFactions;

        public static IReadOnlyDictionary<string, AdventurerFactionData> GetDefaultFactions()
        {
            if (defaultFactions != null && defaultFactions.Count >= 6) return defaultFactions;

            defaultFactions = new Dictionary<string, AdventurerFactionData>(StringComparer.OrdinalIgnoreCase)
            {
                ["adventurer_guild"] = MakeFaction("adventurer_guild", "Adventurer Guild",
                    "Standard frontier raiders composed of Swordsmen and Archers.",
                    new[] { "swordsman", "archer" }, StageDifficulty.Easy,
                    FactionPassiveType.Balanced, "Balanced stats (+5% HP & Attack)",
                    hp: 1.05f, atk: 1.05f, def: 1.0f, spd: 1.0f, move: 1.0f, bossMult: 1.0f, heal: 0,
                    goldBonus: 40, soulBonus: 10, expBonus: 20),

                ["holy_order"] = MakeFaction("holy_order", "Holy Order",
                    "Paladins, Priests, and Holy Knights blessed with regenerative wards and high defense.",
                    new[] { "knight", "swordsman", "mage" }, StageDifficulty.Normal,
                    FactionPassiveType.HolyRegeneration, "Holy Ward (+25% HP, +20% DEF, Regeneration)",
                    hp: 1.25f, atk: 1.05f, def: 1.20f, spd: 0.95f, move: 0.95f, bossMult: 1.0f, heal: 4,
                    goldBonus: 75, soulBonus: 20, expBonus: 35),

                ["shadow_assassins"] = MakeFaction("shadow_assassins", "Shadow Assassins",
                    "Assassins and Rogues specializing in rapid movement and high attack speed.",
                    new[] { "swordsman", "archer" }, StageDifficulty.Hard,
                    FactionPassiveType.SwiftStrike, "Swift Strike (+35% Attack Speed, +30% Move Speed)",
                    hp: 0.95f, atk: 1.25f, def: 0.90f, spd: 1.35f, move: 1.30f, bossMult: 1.1f, heal: 0,
                    goldBonus: 110, soulBonus: 30, expBonus: 50),

                ["arcane_council"] = MakeFaction("arcane_council", "Arcane Council",
                    "Mages and Dark Mages wielding devastating long-range arcane spells.",
                    new[] { "mage", "archer" }, StageDifficulty.Hard,
                    FactionPassiveType.ArcaneReach, "Arcane Mastery (+35% Magic Attack, +15% Attack Speed)",
                    hp: 1.05f, atk: 1.35f, def: 1.0f, spd: 1.15f, move: 1.05f, bossMult: 1.15f, heal: 0,
                    goldBonus: 130, soulBonus: 40, expBonus: 60),

                ["dragon_hunters"] = MakeFaction("dragon_hunters", "Dragon Hunters",
                    "Elite Knights, Rangers, and Dragon Slayers trained to bring down dungeon bosses.",
                    new[] { "knight", "archer", "swordsman" }, StageDifficulty.VeryHard,
                    FactionPassiveType.BossSlayer, "Dragon Slayer (+30% HP/ATK, +50% Bonus Damage vs Bosses)",
                    hp: 1.30f, atk: 1.30f, def: 1.25f, spd: 1.15f, move: 1.10f, bossMult: 1.50f, heal: 0,
                    goldBonus: 180, soulBonus: 55, expBonus: 85),

                ["demon_hunters"] = MakeFaction("demon_hunters", "Demon Hunters",
                    "Legendary Demon Hunters, Holy Mages, and Boss Hunters forged for the Abyss.",
                    new[] { "knight", "mage", "archer", "swordsman" }, StageDifficulty.Nightmare,
                    FactionPassiveType.DemonicHunt, "Abyssal Crusade (+45% All Combat Stats, +60% vs Bosses)",
                    hp: 1.45f, atk: 1.45f, def: 1.35f, spd: 1.25f, move: 1.15f, bossMult: 1.60f, heal: 5,
                    goldBonus: 260, soulBonus: 80, expBonus: 120)
            };

            return defaultFactions;
        }

        public static AdventurerFactionData FindFaction(string factionId)
        {
            var dict = GetDefaultFactions();
            if (!string.IsNullOrWhiteSpace(factionId) && dict.TryGetValue(factionId.Trim(), out AdventurerFactionData data))
                return data;
            return dict["adventurer_guild"];
        }

        private static AdventurerFactionData MakeFaction(string id, string name, string desc, string[] units,
            StageDifficulty diff, FactionPassiveType passiveType, string passiveDesc,
            float hp, float atk, float def, float spd, float move, float bossMult, int heal,
            int goldBonus, int soulBonus, int expBonus)
        {
            AdventurerFactionData f = CreateInstance<AdventurerFactionData>();
            f.name = id;
            f.factionId = id;
            f.factionName = name;
            f.description = desc;
            f.adventurerTypes = new List<string>(units);
            f.factionDifficulty = diff;
            f.factionPassiveBonus = new FactionPassiveBonus
            {
                passiveType = passiveType,
                passiveDescription = passiveDesc,
                healthMultiplier = hp,
                attackMultiplier = atk,
                defenseMultiplier = def,
                attackSpeedMultiplier = spd,
                moveSpeedMultiplier = move,
                bonusDamageVsBossMultiplier = bossMult,
                healPerSecond = heal
            };
            f.factionSpawnConfiguration = new FactionSpawnConfiguration
            {
                spawnIntervalMultiplier = 1.0f,
                initialSpawnDelay = 0.25f
            };
            f.factionRewardBonus = new FactionRewardBonus
            {
                bonusGold = goldBonus,
                bonusSoul = soulBonus,
                bonusEXP = expBonus
            };
            return f;
        }
    }
}
