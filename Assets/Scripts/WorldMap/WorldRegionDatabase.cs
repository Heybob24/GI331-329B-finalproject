using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/World Region Database", fileName = "WorldRegionDatabase")]
    public sealed class WorldRegionDatabase : ScriptableObject
    {
        [SerializeField] private List<WorldRegionData> regions = new List<WorldRegionData>();
        private Dictionary<string, WorldRegionData> byId;

        public IReadOnlyList<WorldRegionData> AllRegions
        {
            get
            {
                EnsureCache();
                return regions;
            }
        }

        public void SetRegions(IEnumerable<WorldRegionData> entries)
        {
            regions = entries != null ? new List<WorldRegionData>(entries) : new List<WorldRegionData>();
            RebuildCache();
        }

        public WorldRegionData FindById(string regionId)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(regionId)) return null;
            return byId.TryGetValue(regionId.Trim(), out WorldRegionData data) ? data : null;
        }

        public bool ValidateDatabase(out string error)
        {
            EnsureCache();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < regions.Count; i++)
            {
                WorldRegionData r = regions[i];
                if (r == null)
                {
                    error = $"WorldRegionDatabase entry {i} is null.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(r.regionId))
                {
                    error = $"WorldRegionData '{r.name}' has an empty regionId.";
                    return false;
                }
                if (!ids.Add(r.regionId.Trim()))
                {
                    error = $"Duplicate regionId '{r.regionId}' in WorldRegionDatabase.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        private void OnEnable() => RebuildCache();
        private void OnValidate() => RebuildCache();

        private void EnsureCache()
        {
            if (byId == null) RebuildCache();
        }

        public void RebuildCache()
        {
            if (regions == null) regions = new List<WorldRegionData>();
            byId = new Dictionary<string, WorldRegionData>(StringComparer.OrdinalIgnoreCase);
            foreach (WorldRegionData r in regions)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.regionId)) continue;
                string id = r.regionId.Trim();
                if (!byId.ContainsKey(id))
                    byId[id] = r;
            }
            regions.Sort((a, b) => (a != null ? a.regionOrder : 0).CompareTo(b != null ? b.regionOrder : 0));
        }

        public static WorldRegionDatabase CreateRuntimeDefaultDatabase()
        {
            WorldRegionDatabase db = CreateInstance<WorldRegionDatabase>();
            db.name = "DefaultWorldRegionDatabase";

            var list = new List<WorldRegionData>
            {
                MakeRegion("forgotten_forest", "Forgotten Forest",
                    "Starting borderland woodland patrolled by rookie Adventurer Guild scouts.",
                    order: 1, recLvl: 1, reqCampaignLvl: 0, diff: StageDifficulty.Easy,
                    factionId: "adventurer_guild",
                    stages: new[] { "forest_stage_1", "forest_stage_2", "forest_stage_3" },
                    prevRegion: "", reqCampaign: "", reqDungeonLvl: 1, reqBoss: "", reqQuest: "",
                    rewardGold: 400, rewardSoul: 80, isStarting: true),

                MakeRegion("cursed_cave", "Cursed Cave",
                    "Subterranean caverns purged by the Holy Order and Undead inquisitors.",
                    order: 2, recLvl: 2, reqCampaignLvl: 1, diff: StageDifficulty.Normal,
                    factionId: "holy_order",
                    stages: new[] { "cave_stage_1", "cave_stage_2", "cave_stage_3" },
                    prevRegion: "forgotten_forest", reqCampaign: "campaign_forgotten_forest", reqDungeonLvl: 2, reqBoss: "", reqQuest: "",
                    rewardGold: 700, rewardSoul: 150, isStarting: false),

                MakeRegion("volcanic_land", "Volcanic Land",
                    "Scorched magma fields infiltrated by swift Shadow Assassins.",
                    order: 3, recLvl: 4, reqCampaignLvl: 2, diff: StageDifficulty.Hard,
                    factionId: "shadow_assassins",
                    stages: new[] { "volcano_stage_1", "volcano_stage_2", "volcano_stage_3" },
                    prevRegion: "cursed_cave", reqCampaign: "campaign_cursed_cave", reqDungeonLvl: 3, reqBoss: "", reqQuest: "",
                    rewardGold: 1100, rewardSoul: 250, isStarting: false),

                MakeRegion("frozen_wasteland", "Frozen Wasteland",
                    "Glacial tundra besieged by long-range Mages of the Arcane Council.",
                    order: 4, recLvl: 5, reqCampaignLvl: 3, diff: StageDifficulty.Hard,
                    factionId: "arcane_council",
                    stages: new[] { "frozen_stage_1", "frozen_stage_2", "frozen_stage_3" },
                    prevRegion: "volcanic_land", reqCampaign: "campaign_volcanic_land", reqDungeonLvl: 4, reqBoss: "", reqQuest: "",
                    rewardGold: 1500, rewardSoul: 350, isStarting: false),

                MakeRegion("demon_territory", "Demon Territory",
                    "Ashen stronghold targeted by elite Dragon Hunters with high boss lethality.",
                    order: 5, recLvl: 7, reqCampaignLvl: 4, diff: StageDifficulty.VeryHard,
                    factionId: "dragon_hunters",
                    stages: new[] { "demon_stage_1", "demon_stage_2", "demon_stage_3" },
                    prevRegion: "frozen_wasteland", reqCampaign: "campaign_frozen_wasteland", reqDungeonLvl: 6, reqBoss: "cave_dragon", reqQuest: "",
                    rewardGold: 2200, rewardSoul: 500, isStarting: false),

                MakeRegion("abyssal_realm", "Abyssal Realm",
                    "The deepest abyss assaulted by legendary Demon Hunters and High Champions.",
                    order: 6, recLvl: 9, reqCampaignLvl: 5, diff: StageDifficulty.Nightmare,
                    factionId: "demon_hunters",
                    stages: new[] { "abyss_stage_1", "abyss_stage_2", "abyss_stage_3" },
                    prevRegion: "demon_territory", reqCampaign: "campaign_demon_territory", reqDungeonLvl: 8, reqBoss: "dark_golem", reqQuest: "",
                    rewardGold: 3500, rewardSoul: 900, isStarting: false)
            };

            db.SetRegions(list);
            return db;
        }

        private static WorldRegionData MakeRegion(string id, string title, string desc,
            int order, int recLvl, int reqCampaignLvl, StageDifficulty diff, string factionId,
            string[] stages, string prevRegion, string reqCampaign, int reqDungeonLvl, string reqBoss, string reqQuest,
            int rewardGold, int rewardSoul, bool isStarting)
        {
            WorldRegionData r = CreateInstance<WorldRegionData>();
            r.name = id;
            r.regionId = id;
            r.regionName = title;
            r.description = desc;
            r.regionOrder = order;
            r.recommendedDungeonLevel = recLvl;
            r.requiredCampaignLevel = reqCampaignLvl;
            r.baseDifficulty = diff;
            r.primaryFactionId = factionId;
            r.availableStageIds = new List<string>(stages);
            r.unlockRequirements = new RegionUnlockRequirement
            {
                requiredPreviousRegionId = prevRegion,
                requiredCampaignId = reqCampaign,
                requiredDungeonLevel = reqDungeonLvl,
                requiredBossId = reqBoss,
                requiredQuestId = reqQuest
            };
            r.regionRewardGold = rewardGold;
            r.regionRewardSoul = rewardSoul;
            r.isStartingRegion = isStarting;
            return r;
        }
    }
}
