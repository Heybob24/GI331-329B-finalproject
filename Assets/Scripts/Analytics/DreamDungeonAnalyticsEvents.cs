using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamDungeon
{
    public enum AnalyticsInitStatus
    {
        Uninitialized,
        Initializing,
        Initialized,
        InitializedOffline,
        SdkNotEnabled,
        Failed
    }

    public enum AnalyticsEventRecordStatus
    {
        Recorded,
        SkippedDuplicate,
        ValidationFailed,
        NotInitialized,
        SdkNotEnabled,
        Error
    }

    public enum AnalyticsEventSource
    {
        Gameplay,
        TestPanel
    }

    [Serializable]
    public sealed class AnalyticsEventLogEntry
    {
        public string timestampUtc;
        public string eventName;
        public AnalyticsEventSource source;
        public AnalyticsEventRecordStatus status;
        public string payloadPreview;
        public string note;
    }

    /// <summary>
    /// Exact Custom Event names and parameter keys matching Unity Dashboard -> Analytics -> Event Manager schema.
    /// </summary>
    public static class DreamDungeonAnalyticsSchema
    {
        // Event Names
        public const string EventDungeonCreated = "dungeon_created";
        public const string EventRaidStarted = "raid_started";
        public const string EventRaidCompleted = "raid_completed";
        public const string EventRaidFailed = "raid_failed";
        public const string EventCrystalSpent = "crystal_spent";

        // dungeon_created parameters
        public const string ParamDungeonLevel = "dungeon_level";
        public const string ParamRoomCount = "room_count";

        // raid_started parameters
        public const string ParamMonsterCount = "monster_count";
        public const string ParamTrapCount = "trap_count";

        // raid_completed & raid_failed parameters
        public const string ParamWave = "wave";
        public const string ParamScore = "score";
        public const string ParamCoreHp = "core_hp";
        public const string ParamDuration = "duration";

        // crystal_spent parameters
        public const string ParamAmount = "amount";
        public const string ParamItemId = "item_id";
        public const string ParamBalanceAfter = "balance_after";

        public static string FormatParametersJson(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return "{}";

            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kv in parameters)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append('"').Append(kv.Key).Append("\": ");
                if (kv.Value == null)
                {
                    sb.Append("null");
                }
                else if (kv.Value is string s)
                {
                    sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
                }
                else if (kv.Value is float f)
                {
                    sb.Append(f.ToString("0.##", CultureInfo.InvariantCulture));
                }
                else if (kv.Value is double d)
                {
                    sb.Append(d.ToString("0.##", CultureInfo.InvariantCulture));
                }
                else if (kv.Value is bool b)
                {
                    sb.Append(b ? "true" : "false");
                }
                else
                {
                    sb.Append(Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
                }
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
