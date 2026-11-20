using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DailyQuestManager : MonoBehaviour
    {
        public static DailyQuestManager Instance { get; private set; }

        [SerializeField, Min(1)] private int dailyQuestCount = 3;

        private QuestManager questManager;
        private readonly List<string> activeDailyIds = new List<string>();
        private string lastResetDateKey = string.Empty;

        public IReadOnlyList<string> ActiveDailyQuestIds => activeDailyIds;
        public string LastResetDateKey => lastResetDateKey;
        public int ConfiguredDailyQuestCount => dailyQuestCount;

        public event Action OnDailyQuestsRefreshed;

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

        public void SetDailyQuestCount(int count)
        {
            dailyQuestCount = Mathf.Max(1, count);
        }

        public void Initialize(QuestManager manager, List<string> savedIds, string savedDateKey)
        {
            questManager = manager;
            if (questManager != null && questManager.Database != null)
            {
                dailyQuestCount = Mathf.Max(1, questManager.Database.DailyQuestCount);
            }

            activeDailyIds.Clear();
            if (savedIds != null)
            {
                foreach (string id in savedIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && !activeDailyIds.Contains(id))
                        activeDailyIds.Add(id);
                }
            }

            lastResetDateKey = savedDateKey ?? string.Empty;
            CheckAndResetIfNeeded(notifyIfRefreshed: false);
        }

        public static string GetDateKey(DateTime utcTime)
        {
            return utcTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public TimeSpan GetTimeUntilNextReset()
        {
            DateTime now = questManager != null ? questManager.UtcNow : DateTime.UtcNow;
            DateTime nextMidnight = now.Date.AddDays(1);
            TimeSpan remaining = nextMidnight - now;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        public string GetFormattedCountdown()
        {
            TimeSpan t = GetTimeUntilNextReset();
            return $"{(int)t.TotalHours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";
        }

        public bool CheckAndResetIfNeeded(bool notifyIfRefreshed = true)
        {
            if (questManager == null || questManager.Database == null) return false;

            string todayKey = GetDateKey(questManager.UtcNow);
            bool needsReset = !string.Equals(lastResetDateKey, todayKey, StringComparison.Ordinal) || activeDailyIds.Count == 0;

            if (!needsReset) return false;

            bool hadPreviousDate = !string.IsNullOrEmpty(lastResetDateKey);
            GenerateDailyQuestSet(todayKey);

            if (notifyIfRefreshed && hadPreviousDate)
            {
                OnDailyQuestsRefreshed?.Invoke();
                questManager.NotifyDailyQuestsRefreshed();
            }

            return true;
        }

        public void ForceResetForDate(DateTime simulatedUtcNow, bool notify = true)
        {
            if (questManager == null) return;
            string dateKey = GetDateKey(simulatedUtcNow);
            if (string.Equals(lastResetDateKey, dateKey, StringComparison.Ordinal) && activeDailyIds.Count > 0) return;

            GenerateDailyQuestSet(dateKey);
            if (notify)
            {
                OnDailyQuestsRefreshed?.Invoke();
                questManager.NotifyDailyQuestsRefreshed();
            }
        }

        private void GenerateDailyQuestSet(string dateKey)
        {
            lastResetDateKey = dateKey;
            activeDailyIds.Clear();

            IReadOnlyList<QuestData> pool = questManager.Database.GetByType(QuestType.Daily);
            var availablePool = new List<QuestData>();
            foreach (QuestData q in pool)
            {
                if (q != null && q.isActive) availablePool.Add(q);
            }

            // Reset all daily quest entries
            foreach (QuestData q in pool)
            {
                if (q == null) continue;
                questManager.ResetQuestEntryForCycle(q.questId, dateKey);
            }

            if (availablePool.Count == 0) return;

            // Deterministic seed based on dateKey so multiple checks on same day produce valid selection,
            // while different days rotate/shuffle quests
            int seed = StableHash(dateKey);
            var rng = new System.Random(seed);
            for (int i = availablePool.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                QuestData temp = availablePool[i];
                availablePool[i] = availablePool[swapIdx];
                availablePool[swapIdx] = temp;
            }

            int count = Mathf.Min(Mathf.Max(3, dailyQuestCount), availablePool.Count);
            for (int i = 0; i < count; i++)
            {
                string id = availablePool[i].questId;
                activeDailyIds.Add(id);
                questManager.GetOrCreateEntry(availablePool[i]);
            }
        }

        public List<QuestData> GetActiveDailyQuests()
        {
            var result = new List<QuestData>();
            if (questManager == null || questManager.Database == null) return result;
            foreach (string id in activeDailyIds)
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
                int hash = 23;
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
