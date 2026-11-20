using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class WeeklyQuestManager : MonoBehaviour
    {
        public static WeeklyQuestManager Instance { get; private set; }

        [SerializeField, Min(1)] private int weeklyQuestCount = 3;

        private QuestManager questManager;
        private readonly List<string> activeWeeklyIds = new List<string>();
        private string lastResetWeekKey = string.Empty;

        public IReadOnlyList<string> ActiveWeeklyQuestIds => activeWeeklyIds;
        public string LastResetWeekKey => lastResetWeekKey;
        public int ConfiguredWeeklyQuestCount => weeklyQuestCount;

        public event Action OnWeeklyQuestsRefreshed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetWeeklyQuestCount(int count)
        {
            weeklyQuestCount = Mathf.Max(1, count);
        }

        public void Initialize(QuestManager manager, List<string> savedIds, string savedWeekKey)
        {
            questManager = manager;
            if (questManager != null && questManager.Database != null)
            {
                weeklyQuestCount = Mathf.Max(1, questManager.Database.WeeklyQuestCount);
            }

            activeWeeklyIds.Clear();
            if (savedIds != null)
            {
                foreach (string id in savedIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && !activeWeeklyIds.Contains(id))
                        activeWeeklyIds.Add(id);
                }
            }

            lastResetWeekKey = savedWeekKey ?? string.Empty;
            CheckAndResetIfNeeded(notifyIfRefreshed: false);
        }

        public static DateTime GetStartOfWeekUtc(DateTime utcTime)
        {
            int diff = (7 + (utcTime.DayOfWeek - DayOfWeek.Monday)) % 7;
            return utcTime.Date.AddDays(-diff);
        }

        public static string GetWeekKey(DateTime utcTime)
        {
            DateTime monday = GetStartOfWeekUtc(utcTime);
            return monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public TimeSpan GetTimeUntilNextReset()
        {
            DateTime now = questManager != null ? questManager.UtcNow : DateTime.UtcNow;
            DateTime nextMonday = GetStartOfWeekUtc(now).AddDays(7);
            TimeSpan remaining = nextMonday - now;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        public string GetFormattedCountdown()
        {
            TimeSpan t = GetTimeUntilNextReset();
            return $"{t.Days}d {t.Hours:D2}h {t.Minutes:D2}m";
        }

        public bool CheckAndResetIfNeeded(bool notifyIfRefreshed = true)
        {
            if (questManager == null || questManager.Database == null) return false;

            string currentWeekKey = GetWeekKey(questManager.UtcNow);
            bool needsReset = !string.Equals(lastResetWeekKey, currentWeekKey, StringComparison.Ordinal) || activeWeeklyIds.Count == 0;

            if (!needsReset) return false;

            bool hadPreviousWeek = !string.IsNullOrEmpty(lastResetWeekKey);
            GenerateWeeklyQuestSet(currentWeekKey);

            if (notifyIfRefreshed && hadPreviousWeek)
            {
                OnWeeklyQuestsRefreshed?.Invoke();
                questManager.NotifyWeeklyQuestsRefreshed();
            }

            return true;
        }

        public void ForceResetForDate(DateTime simulatedUtcNow, bool notify = true)
        {
            if (questManager == null) return;
            string weekKey = GetWeekKey(simulatedUtcNow);
            if (string.Equals(lastResetWeekKey, weekKey, StringComparison.Ordinal) && activeWeeklyIds.Count > 0) return;

            GenerateWeeklyQuestSet(weekKey);
            if (notify)
            {
                OnWeeklyQuestsRefreshed?.Invoke();
                questManager.NotifyWeeklyQuestsRefreshed();
            }
        }

        private void GenerateWeeklyQuestSet(string weekKey)
        {
            lastResetWeekKey = weekKey;
            activeWeeklyIds.Clear();

            IReadOnlyList<QuestData> pool = questManager.Database.GetByType(QuestType.Weekly);
            var availablePool = new List<QuestData>();
            foreach (QuestData q in pool)
            {
                if (q != null && q.isActive) availablePool.Add(q);
            }

            // Reset all weekly quest entries
            foreach (QuestData q in pool)
            {
                if (q == null) continue;
                questManager.ResetQuestEntryForCycle(q.questId, weekKey);
            }

            if (availablePool.Count == 0) return;

            int seed = StableHash("WEEK_" + weekKey);
            var rng = new System.Random(seed);
            for (int i = availablePool.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                QuestData temp = availablePool[i];
                availablePool[i] = availablePool[swapIdx];
                availablePool[swapIdx] = temp;
            }

            int count = Mathf.Min(Mathf.Max(3, weeklyQuestCount), availablePool.Count);
            for (int i = 0; i < count; i++)
            {
                string id = availablePool[i].questId;
                activeWeeklyIds.Add(id);
                questManager.GetOrCreateEntry(availablePool[i]);
            }
        }

        public List<QuestData> GetActiveWeeklyQuests()
        {
            var result = new List<QuestData>();
            if (questManager == null || questManager.Database == null) return result;
            foreach (string id in activeWeeklyIds)
            {
                QuestData q = questManager.Database.FindById(id);
                if (q != null) result.Add(q);
            }
            return result;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 29;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                        hash = hash * 31 + text[i];
                }
                return hash & 0x7FFFFFFF;
            }
        }
    }
}
