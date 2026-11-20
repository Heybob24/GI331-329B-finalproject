using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class StageCardUI : MonoBehaviour
    {
        private string stageId;
        private Image cardBackground;
        private Text titleText;
        private Text descText;
        private Text infoText;
        private Text bestResultText;
        private Text firstClearText;
        private Text statusText;
        private Button selectButton;
        private Text selectButtonLabel;
        private Action<string> onSelectClicked;

        public string StageId => stageId;
        public Button SelectButton => selectButton;

        public static StageCardUI Create(Transform parent, CampaignStageData data, CampaignManager manager, Action<string> selectCallback)
        {
            GameObject cardObj = RoomUIFactory.CreateCard(parent, $"StageCard_{data.stageId}", 245f);
            StageCardUI card = cardObj.AddComponent<StageCardUI>();
            card.BuildCard(data, manager, selectCallback);
            return card;
        }

        private void BuildCard(CampaignStageData data, CampaignManager manager, Action<string> selectCallback)
        {
            stageId = data.stageId;
            onSelectClicked = selectCallback;

            Transform t = transform;
            cardBackground = GetComponent<Image>();

            RectTransform badge = RoomUIFactory.CreatePanel(t, "StageBadge",
                new Color(0.24f, 0.28f, 0.38f, 1f), new Vector2(-365f, 35f), new Vector2(88f, 88f));
            RoomUIFactory.CreateText(badge, "StageNumberLabel", $"STAGE\n{data.stageNumber}", 20,
                Vector2.zero, new Vector2(82f, 82f), new Color(0.96f, 0.80f, 0.38f));

            titleText = RoomUIFactory.CreateText(t, "StageName", data.stageName, 24,
                new Vector2(-95f, 78f), new Vector2(420f, 38f), Color.white, TextAnchor.MiddleLeft);

            descText = RoomUIFactory.CreateText(t, "StageDescription", data.description, 17,
                new Vector2(-95f, 38f), new Vector2(420f, 42f), new Color(0.80f, 0.84f, 0.90f), TextAnchor.MiddleLeft);

            int waveCount = data.waveConfiguration != null ? data.waveConfiguration.Count : 0;
            string bossTag = !string.IsNullOrWhiteSpace(data.bossId) ? $"  •  BOSS: {data.bossId}" : "";
            infoText = RoomUIFactory.CreateText(t, "StageInfo",
                $"Rec. Lv.{data.recommendedDungeonLevel}  •  Base: {data.difficulty}  •  Waves: {waveCount}{bossTag}", 16,
                new Vector2(-95f, -4f), new Vector2(430f, 32f), new Color(0.75f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

            bestResultText = RoomUIFactory.CreateText(t, "BestResult", "Best: None", 15,
                new Vector2(-95f, -38f), new Vector2(430f, 30f), new Color(0.76f, 0.84f, 0.76f), TextAnchor.MiddleLeft);

            int fcG = data.firstClearReward != null ? data.firstClearReward.gold : 0;
            int fcS = data.firstClearReward != null ? data.firstClearReward.soul : 0;
            firstClearText = RoomUIFactory.CreateText(t, "FirstClearReward",
                $"First Clear Bonus: +{fcG}G  +{fcS}S   |   Clear: +{data.rewardGold}G  +{data.rewardSoul}S", 15,
                new Vector2(-95f, -76f), new Vector2(440f, 30f), new Color(0.96f, 0.80f, 0.38f), TextAnchor.MiddleLeft);

            statusText = RoomUIFactory.CreateText(t, "StageStatus", "LOCKED", 16,
                new Vector2(290f, 52f), new Vector2(220f, 42f), Color.white);

            selectButton = RoomUIFactory.CreateButton(t, "SelectStageButton", "DETAILS",
                new Vector2(290f, -38f), new Vector2(200f, 70f), new Color(0.22f, 0.58f, 0.36f), 19);
            selectButtonLabel = selectButton.GetComponentInChildren<Text>();
            selectButton.onClick.AddListener(HandleSelect);

            Refresh(data, manager);
        }

        public void Refresh(CampaignStageData data, CampaignManager manager)
        {
            if (data == null || manager == null) return;
            StageProgressRecord rec = manager.GetStageRecord(data.stageId);
            bool unlocked = manager.IsStageUnlocked(data.stageId);
            CampaignStageStatus status = rec != null ? rec.status : (unlocked ? CampaignStageStatus.Available : CampaignStageStatus.Locked);

            if (bestResultText != null)
            {
                if (rec != null && rec.totalVictories > 0)
                {
                    string perf = rec.perfectClearAchieved ? " (PERFECT CLEAR)" : "";
                    bestResultText.text = $"Best: {rec.bestDifficulty}{perf}  •  Wins: {rec.totalVictories}  •  Attempts: {rec.totalAttempts}";
                }
                else if (rec != null && rec.totalAttempts > 0)
                {
                    bestResultText.text = $"Best Wave: {rec.bestWave}  •  Attempts: {rec.totalAttempts}";
                }
                else
                {
                    bestResultText.text = "Best: Not attempted yet";
                }
            }

            if (firstClearText != null)
            {
                bool fcClaimed = manager.IsFirstClearClaimed(data.stageId);
                int fcG = data.firstClearReward != null ? data.firstClearReward.gold : 0;
                int fcS = data.firstClearReward != null ? data.firstClearReward.soul : 0;
                firstClearText.text = fcClaimed
                    ? $"First Clear: CLAIMED   |   Replay Reward: +{data.rewardGold}G  +{data.rewardSoul}S"
                    : $"First Clear Bonus: +{fcG}G  +{fcS}S   |   Clear: +{data.rewardGold}G  +{data.rewardSoul}S";
            }

            if (statusText != null)
            {
                switch (status)
                {
                    case CampaignStageStatus.PerfectClear:
                        statusText.text = "PERFECT CLEAR";
                        statusText.color = new Color(1.0f, 0.85f, 0.32f);
                        break;
                    case CampaignStageStatus.Completed:
                        statusText.text = "COMPLETED";
                        statusText.color = new Color(0.38f, 0.95f, 0.52f);
                        break;
                    case CampaignStageStatus.InProgress:
                        statusText.text = "IN PROGRESS";
                        statusText.color = new Color(0.45f, 0.82f, 1.0f);
                        break;
                    case CampaignStageStatus.Available:
                        statusText.text = "AVAILABLE";
                        statusText.color = new Color(0.95f, 0.84f, 0.42f);
                        break;
                    default:
                        statusText.text = "LOCKED";
                        statusText.color = new Color(0.92f, 0.45f, 0.40f);
                        break;
                }
            }

            if (cardBackground != null)
            {
                cardBackground.color = !unlocked
                    ? new Color(0.11f, 0.11f, 0.14f, 1f)
                    : (status == CampaignStageStatus.PerfectClear || status == CampaignStageStatus.Completed
                        ? new Color(0.14f, 0.21f, 0.17f, 1f)
                        : new Color(0.14f, 0.16f, 0.22f, 1f));
            }

            if (selectButton != null)
            {
                selectButton.interactable = unlocked;
                Image btnImg = selectButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = unlocked
                        ? new Color(0.24f, 0.64f, 0.38f)
                        : new Color(0.24f, 0.25f, 0.30f);
                }
            }

            if (selectButtonLabel != null)
            {
                selectButtonLabel.text = !unlocked
                    ? "LOCKED"
                    : (status == CampaignStageStatus.Completed || status == CampaignStageStatus.PerfectClear ? "REPLAY" : "SELECT");
            }
        }

        private void HandleSelect()
        {
            onSelectClicked?.Invoke(stageId);
        }
    }
}
