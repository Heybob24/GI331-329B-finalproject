using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class QuestCardUI : MonoBehaviour
    {
        private string questId;
        private Image cardBackground;
        private Image iconImage;
        private Text titleText;
        private Text descText;
        private Text progressText;
        private Image progressBarFill;
        private Text rewardText;
        private Text statusText;
        private Button claimButton;
        private Text claimButtonLabel;
        private Action<string> onClaimClicked;

        public string QuestId => questId;
        public Button ClaimButton => claimButton;

        public static QuestCardUI Create(Transform parent, QuestData data, QuestProgressEntry entry, Action<string> claimCallback)
        {
            GameObject cardObj = RoomUIFactory.CreateCard(parent, $"QuestCard_{data.questId}", 245f);
            QuestCardUI card = cardObj.AddComponent<QuestCardUI>();
            card.BuildCard(data, entry, claimCallback);
            return card;
        }

        private void BuildCard(QuestData data, QuestProgressEntry entry, Action<string> claimCallback)
        {
            questId = data.questId;
            onClaimClicked = claimCallback;

            Transform t = transform;
            cardBackground = GetComponent<Image>();

            RectTransform iconRt = RoomUIFactory.CreatePanel(t, "QuestIcon",
                data.icon != null ? Color.white : QuestTypeColor(data.questType),
                new Vector2(-365f, 35f), new Vector2(88f, 88f));
            iconImage = iconRt.GetComponent<Image>();
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.preserveAspect = true;
            }

            string headerPrefix = data.questType == QuestType.Main && !string.IsNullOrEmpty(data.chapterTitle)
                ? $"[Ch.{data.chapterIndex}] "
                : "";
            titleText = RoomUIFactory.CreateText(t, "QuestName", headerPrefix + data.questName, 24,
                new Vector2(-95f, 76f), new Vector2(420f, 38f), Color.white, TextAnchor.MiddleLeft);

            descText = RoomUIFactory.CreateText(t, "QuestDescription", data.description, 17,
                new Vector2(-95f, 34f), new Vector2(420f, 44f), new Color(0.80f, 0.84f, 0.90f), TextAnchor.MiddleLeft);

            // Progress Bar
            RectTransform barBg = RoomUIFactory.CreatePanel(t, "ProgressBarBg",
                new Color(0.08f, 0.10f, 0.14f, 1f), new Vector2(-95f, -18f), new Vector2(420f, 32f));
            var fillObj = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBg, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            progressBarFill = fillObj.GetComponent<Image>();
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.color = new Color(0.28f, 0.78f, 0.45f);

            progressText = RoomUIFactory.CreateText(barBg, "ProgressText", "0 / 1", 16,
                Vector2.zero, new Vector2(400f, 32f), Color.white);

            string rewardStr = $"REWARD:  +{data.rewardGold} GOLD   +{data.rewardSoul} SOUL   +{data.rewardDungeonEXP} EXP";
            rewardText = RoomUIFactory.CreateText(t, "RewardText", rewardStr, 16,
                new Vector2(-95f, -72f), new Vector2(440f, 32f), new Color(0.96f, 0.80f, 0.38f), TextAnchor.MiddleLeft);

            statusText = RoomUIFactory.CreateText(t, "QuestStatus", "IN PROGRESS", 17,
                new Vector2(290f, 52f), new Vector2(220f, 36f), Color.white);

            claimButton = RoomUIFactory.CreateButton(t, "ClaimQuestButton", "CLAIM",
                new Vector2(290f, -35f), new Vector2(200f, 70f), new Color(0.22f, 0.62f, 0.34f), 20);
            claimButtonLabel = claimButton.GetComponentInChildren<Text>();
            claimButton.onClick.AddListener(HandleClaim);

            Refresh(data, entry);
        }

        public void Refresh(QuestData data, QuestProgressEntry entry)
        {
            if (data == null) return;
            int current = entry != null ? Mathf.Clamp(entry.currentProgress, 0, data.targetAmount) : 0;
            int target = Mathf.Max(1, data.targetAmount);
            QuestStatus status = entry != null ? entry.status : QuestStatus.InProgress;

            if (progressText != null) progressText.text = $"{current} / {target}";
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01((float)current / target);
                progressBarFill.color = status == QuestStatus.Claimed
                    ? new Color(0.35f, 0.50f, 0.65f)
                    : (status == QuestStatus.Completed ? new Color(0.32f, 0.88f, 0.48f) : new Color(0.32f, 0.68f, 0.95f));
            }

            if (statusText != null)
            {
                switch (status)
                {
                    case QuestStatus.Locked:
                        statusText.text = "LOCKED";
                        statusText.color = new Color(0.92f, 0.45f, 0.40f);
                        break;
                    case QuestStatus.Completed:
                        statusText.text = "COMPLETED";
                        statusText.color = new Color(0.38f, 0.95f, 0.52f);
                        break;
                    case QuestStatus.Claimed:
                        statusText.text = "CLAIMED";
                        statusText.color = new Color(0.62f, 0.72f, 0.82f);
                        break;
                    default:
                        statusText.text = "IN PROGRESS";
                        statusText.color = new Color(0.95f, 0.82f, 0.42f);
                        break;
                }
            }

            if (cardBackground != null)
            {
                cardBackground.color = status switch
                {
                    QuestStatus.Locked => new Color(0.11f, 0.11f, 0.14f, 1f),
                    QuestStatus.Completed => new Color(0.14f, 0.21f, 0.17f, 1f),
                    QuestStatus.Claimed => new Color(0.12f, 0.14f, 0.18f, 1f),
                    _ => new Color(0.14f, 0.15f, 0.20f, 1f)
                };
            }

            if (claimButton != null)
            {
                bool canClaim = status == QuestStatus.Completed && (entry == null || !entry.isRewardClaimed);
                claimButton.interactable = canClaim;
                Image btnImg = claimButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = canClaim
                        ? new Color(0.24f, 0.68f, 0.36f)
                        : new Color(0.24f, 0.25f, 0.30f);
                }
            }

            if (claimButtonLabel != null)
            {
                claimButtonLabel.text = status switch
                {
                    QuestStatus.Locked => "LOCKED",
                    QuestStatus.Completed => "CLAIM",
                    QuestStatus.Claimed => "CLAIMED",
                    _ => "IN PROGRESS"
                };
            }
        }

        private void HandleClaim()
        {
            onClaimClicked?.Invoke(questId);
        }

        private static Color QuestTypeColor(QuestType type) => type switch
        {
            QuestType.Main => new Color(0.92f, 0.68f, 0.24f),
            QuestType.Daily => new Color(0.28f, 0.68f, 0.92f),
            QuestType.Weekly => new Color(0.68f, 0.38f, 0.92f),
            _ => new Color(0.32f, 0.82f, 0.58f)
        };
    }
}
