using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class QuestNotificationUI : MonoBehaviour
    {
        public static QuestNotificationUI Instance { get; private set; }

        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField, Min(0.5f)] private float displayDuration = 3.2f;

        private struct NotificationItem
        {
            public string Key;
            public string Title;
            public string Subtitle;
            public Color AccentColor;
            public Sprite Icon;
        }

        private readonly Queue<NotificationItem> queue = new Queue<NotificationItem>();
        private readonly HashSet<string> shownNotificationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private RectTransform bannerRoot;
        private Image iconImage;
        private Image accentBadge;
        private Text titleText;
        private Text subtitleText;
        private float activeTimer;
        private bool isShowing;
        private bool subscribed;

        public bool IsShowing => isShowing;
        public int QueuedCount => queue.Count;
        public string LastShownTitle { get; private set; } = string.Empty;
        public string LastShownSubtitle { get; private set; } = string.Empty;

        public void Configure(RectTransform safeArea)
        {
            safeAreaRoot = safeArea;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (isShowing)
            {
                activeTimer -= Time.unscaledDeltaTime;
                if (activeTimer <= 0f)
                {
                    HideBanner();
                }
            }

            if (!isShowing && queue.Count > 0)
            {
                ShowNext();
            }
        }

        private void Build()
        {
            if (bannerRoot != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;

            bannerRoot = RoomUIFactory.CreatePanel(parent, "QuestNotificationBanner",
                new Color(0.06f, 0.08f, 0.13f, 0.96f), new Vector2(0f, 360f), new Vector2(880f, 115f));
            bannerRoot.GetComponent<Image>().raycastTarget = false;

            accentBadge = RoomUIFactory.CreatePanel(bannerRoot, "AccentBar",
                new Color(0.96f, 0.78f, 0.35f, 1f), new Vector2(-432f, 0f), new Vector2(12f, 115f)).GetComponent<Image>();
            accentBadge.raycastTarget = false;

            RectTransform iconPanel = RoomUIFactory.CreatePanel(bannerRoot, "NotifIcon",
                new Color(0.22f, 0.28f, 0.38f, 1f), new Vector2(-365f, 0f), new Vector2(76f, 76f));
            iconImage = iconPanel.GetComponent<Image>();
            iconImage.raycastTarget = false;

            titleText = RoomUIFactory.CreateText(bannerRoot, "NotifTitle", "QUEST COMPLETED", 23,
                new Vector2(35f, 22f), new Vector2(680f, 40f), new Color(0.98f, 0.84f, 0.40f), TextAnchor.MiddleLeft);

            subtitleText = RoomUIFactory.CreateText(bannerRoot, "NotifSubtitle", "", 19,
                new Vector2(35f, -20f), new Vector2(680f, 44f), Color.white, TextAnchor.MiddleLeft);

            bannerRoot.gameObject.SetActive(false);
        }

        public bool ShowNotification(string uniqueKey, string title, string subtitle, Color accentColor, Sprite icon = null)
        {
            if (!string.IsNullOrWhiteSpace(uniqueKey) && !shownNotificationKeys.Add(uniqueKey.Trim()))
            {
                return false;
            }

            Build();
            queue.Enqueue(new NotificationItem
            {
                Key = uniqueKey,
                Title = title,
                Subtitle = subtitle,
                AccentColor = accentColor,
                Icon = icon
            });

            LastShownTitle = title;
            LastShownSubtitle = subtitle;

            if (!isShowing)
            {
                ShowNext();
            }
            return true;
        }

        private void ShowNext()
        {
            if (queue.Count == 0) return;
            Build();
            NotificationItem item = queue.Dequeue();

            if (titleText != null)
            {
                titleText.text = item.Title;
                titleText.color = item.AccentColor;
            }
            if (subtitleText != null) subtitleText.text = item.Subtitle;
            if (accentBadge != null) accentBadge.color = item.AccentColor;
            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.color = item.Icon != null ? Color.white : item.AccentColor;
            }

            if (bannerRoot != null)
            {
                bannerRoot.SetAsLastSibling();
                bannerRoot.gameObject.SetActive(true);
            }

            isShowing = true;
            activeTimer = displayDuration;
        }

        public void HideBanner()
        {
            isShowing = false;
            if (bannerRoot != null) bannerRoot.gameObject.SetActive(false);
        }

        public void ClearNotificationHistory()
        {
            shownNotificationKeys.Clear();
            queue.Clear();
            HideBanner();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
                QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
                QuestManager.Instance.OnQuestRewardClaimed -= OnQuestRewardClaimed;
                QuestManager.Instance.OnQuestRewardClaimed += OnQuestRewardClaimed;
                QuestManager.Instance.OnDailyQuestsRefreshed -= OnDailyQuestsRefreshed;
                QuestManager.Instance.OnDailyQuestsRefreshed += OnDailyQuestsRefreshed;
                QuestManager.Instance.OnWeeklyQuestsRefreshed -= OnWeeklyQuestsRefreshed;
                QuestManager.Instance.OnWeeklyQuestsRefreshed += OnWeeklyQuestsRefreshed;
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
                AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
                AchievementManager.Instance.OnAchievementRewardClaimed -= OnAchievementRewardClaimed;
                AchievementManager.Instance.OnAchievementRewardClaimed += OnAchievementRewardClaimed;
            }
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            subscribed = false;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
                QuestManager.Instance.OnQuestRewardClaimed -= OnQuestRewardClaimed;
                QuestManager.Instance.OnDailyQuestsRefreshed -= OnDailyQuestsRefreshed;
                QuestManager.Instance.OnWeeklyQuestsRefreshed -= OnWeeklyQuestsRefreshed;
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
                AchievementManager.Instance.OnAchievementRewardClaimed -= OnAchievementRewardClaimed;
            }
        }

        private void OnQuestCompleted(QuestData quest)
        {
            if (quest == null) return;
            string cycleTag = quest.questType == QuestType.Daily
                ? (QuestManager.Instance?.DailyQuests?.LastResetDateKey ?? "")
                : (quest.questType == QuestType.Weekly ? (QuestManager.Instance?.WeeklyQuests?.LastResetWeekKey ?? "") : "");
            string key = $"NOTIF_Q_DONE_{quest.questId}_{cycleTag}";
            ShowNotification(key, "QUEST COMPLETED!", $"{quest.questName} — Tap Quests to claim reward!",
                new Color(0.35f, 0.92f, 0.52f), quest.icon);
        }

        private void OnQuestRewardClaimed(QuestData quest, int gold, int soul, int exp)
        {
            if (quest == null) return;
            string cycleTag = quest.questType == QuestType.Daily
                ? (QuestManager.Instance?.DailyQuests?.LastResetDateKey ?? "")
                : (quest.questType == QuestType.Weekly ? (QuestManager.Instance?.WeeklyQuests?.LastResetWeekKey ?? "") : "");
            string key = $"NOTIF_Q_CLAIM_{quest.questId}_{cycleTag}";
            ShowNotification(key, "REWARD CLAIMED!", $"{quest.questName}: +{gold}G  +{soul}S  +{exp} EXP",
                new Color(0.98f, 0.82f, 0.35f), quest.icon);
        }

        private void OnDailyQuestsRefreshed()
        {
            string dayKey = QuestManager.Instance?.DailyQuests?.LastResetDateKey ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            ShowNotification($"NOTIF_DAILY_REFRESH_{dayKey}", "DAILY QUESTS REFRESHED",
                "New daily dungeon missions are now available!", new Color(0.40f, 0.80f, 1.0f));
        }

        private void OnWeeklyQuestsRefreshed()
        {
            string weekKey = QuestManager.Instance?.WeeklyQuests?.LastResetWeekKey ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            ShowNotification($"NOTIF_WEEKLY_REFRESH_{weekKey}", "WEEKLY QUESTS REFRESHED",
                "New weekly challenges have begun!", new Color(0.75f, 0.55f, 1.0f));
        }

        private void OnAchievementUnlocked(AchievementData achievement)
        {
            if (achievement == null) return;
            string key = $"NOTIF_ACH_UNLOCK_{achievement.achievementId}";
            ShowNotification(key, "ACHIEVEMENT UNLOCKED!", $"{achievement.achievementName} — {achievement.description}",
                new Color(1.0f, 0.78f, 0.28f), achievement.icon);
        }

        private void OnAchievementRewardClaimed(AchievementData achievement, int gold, int soul, int exp)
        {
            if (achievement == null) return;
            string key = $"NOTIF_ACH_CLAIM_{achievement.achievementId}";
            ShowNotification(key, "REWARD CLAIMED!", $"{achievement.achievementName}: +{gold}G  +{soul}S  +{exp} EXP",
                new Color(0.98f, 0.82f, 0.35f), achievement.icon);
        }
    }
}
