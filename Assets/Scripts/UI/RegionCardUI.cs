using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class RegionCardUI : MonoBehaviour
    {
        private string regionId;
        private Image cardBackground;
        private Image iconImage;
        private Text titleText;
        private Text descText;
        private Text metaText;
        private Text completionText;
        private Image completionBarFill;
        private Text rewardText;
        private Text statusText;
        private Button selectButton;
        private Text selectButtonLabel;
        private Action<string> onSelectClicked;

        public string RegionId => regionId;
        public Button SelectButton => selectButton;

        public static RegionCardUI Create(Transform parent, WorldRegionData data, WorldMapManager manager, Action<string> selectCallback)
        {
            GameObject cardObj = RoomUIFactory.CreateCard(parent, $"RegionCard_{data.regionId}", 255f);
            RegionCardUI card = cardObj.AddComponent<RegionCardUI>();
            card.BuildCard(data, manager, selectCallback);
            return card;
        }

        private void BuildCard(WorldRegionData data, WorldMapManager manager, Action<string> selectCallback)
        {
            regionId = data.regionId;
            onSelectClicked = selectCallback;

            Transform t = transform;
            cardBackground = GetComponent<Image>();

            RectTransform iconRt = RoomUIFactory.CreatePanel(t, "RegionIcon",
                data.regionIcon != null ? Color.white : RegionThemeColor(data.regionOrder),
                new Vector2(-365f, 35f), new Vector2(92f, 92f));
            iconImage = iconRt.GetComponent<Image>();
            if (data.regionIcon != null)
            {
                iconImage.sprite = data.regionIcon;
                iconImage.preserveAspect = true;
            }

            titleText = RoomUIFactory.CreateText(t, "RegionName", $"{data.regionOrder}. {data.regionName}", 25,
                new Vector2(-90f, 82f), new Vector2(430f, 38f), Color.white, TextAnchor.MiddleLeft);

            descText = RoomUIFactory.CreateText(t, "RegionDescription", data.description, 17,
                new Vector2(-90f, 40f), new Vector2(430f, 44f), new Color(0.80f, 0.84f, 0.90f), TextAnchor.MiddleLeft);

            metaText = RoomUIFactory.CreateText(t, "RegionMeta",
                $"Rec. Dungeon Lv.{data.recommendedDungeonLevel}   •   Difficulty: {data.baseDifficulty}", 16,
                new Vector2(-90f, -2f), new Vector2(430f, 32f), new Color(0.75f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

            // Completion Bar
            RectTransform barBg = RoomUIFactory.CreatePanel(t, "CompletionBarBg",
                new Color(0.08f, 0.10f, 0.14f, 1f), new Vector2(-95f, -38f), new Vector2(420f, 28f));
            var fillObj = new GameObject("CompletionBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBg, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            completionBarFill = fillObj.GetComponent<Image>();
            completionBarFill.type = Image.Type.Filled;
            completionBarFill.fillMethod = Image.FillMethod.Horizontal;
            completionBarFill.color = new Color(0.32f, 0.82f, 0.50f);

            completionText = RoomUIFactory.CreateText(barBg, "CompletionText", "0% COMPLETED", 15,
                Vector2.zero, new Vector2(400f, 28f), Color.white);

            rewardText = RoomUIFactory.CreateText(t, "RegionReward",
                $"REGION REWARD: +{data.regionRewardGold} GOLD   +{data.regionRewardSoul} SOUL", 16,
                new Vector2(-90f, -82f), new Vector2(440f, 32f), new Color(0.96f, 0.80f, 0.38f), TextAnchor.MiddleLeft);

            statusText = RoomUIFactory.CreateText(t, "RegionStatus", "UNLOCKED", 16,
                new Vector2(290f, 52f), new Vector2(225f, 44f), Color.white);

            selectButton = RoomUIFactory.CreateButton(t, "SelectRegionButton", "ENTER REGION",
                new Vector2(290f, -38f), new Vector2(205f, 70f), new Color(0.22f, 0.58f, 0.36f), 18);
            selectButtonLabel = selectButton.GetComponentInChildren<Text>();
            selectButton.onClick.AddListener(HandleSelect);

            Refresh(data, manager);
        }

        public void Refresh(WorldRegionData data, WorldMapManager manager)
        {
            if (data == null || manager == null) return;

            bool unlocked = manager.CanEnterRegion(data.regionId, out string lockReason);
            bool completed = manager.IsRegionCompleted(data.regionId);
            float ratio = manager.GetRegionCompletionRatio(data.regionId);

            if (completionBarFill != null)
            {
                completionBarFill.fillAmount = ratio;
                completionBarFill.color = completed
                    ? new Color(0.35f, 0.92f, 0.52f)
                    : new Color(0.32f, 0.72f, 0.96f);
            }
            if (completionText != null)
            {
                completionText.text = $"COMPLETION: {Mathf.RoundToInt(ratio * 100f)}%";
            }

            if (statusText != null)
            {
                if (completed)
                {
                    statusText.text = "COMPLETED";
                    statusText.color = new Color(0.38f, 0.95f, 0.52f);
                }
                else if (unlocked)
                {
                    statusText.text = "UNLOCKED";
                    statusText.color = new Color(0.95f, 0.84f, 0.42f);
                }
                else
                {
                    statusText.text = $"LOCKED\n{lockReason}";
                    statusText.color = new Color(0.92f, 0.45f, 0.40f);
                }
            }

            if (cardBackground != null)
            {
                cardBackground.color = !unlocked
                    ? new Color(0.11f, 0.11f, 0.14f, 1f)
                    : (completed ? new Color(0.14f, 0.21f, 0.17f, 1f) : new Color(0.14f, 0.16f, 0.22f, 1f));
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
                selectButtonLabel.text = !unlocked ? "LOCKED" : (completed ? "REPLAY" : "ENTER");
            }
        }

        private void HandleSelect()
        {
            onSelectClicked?.Invoke(regionId);
        }

        private static Color RegionThemeColor(int order) => order switch
        {
            1 => new Color(0.24f, 0.65f, 0.35f),
            2 => new Color(0.45f, 0.38f, 0.62f),
            3 => new Color(0.88f, 0.34f, 0.18f),
            4 => new Color(0.30f, 0.72f, 0.92f),
            5 => new Color(0.68f, 0.18f, 0.25f),
            _ => new Color(0.52f, 0.20f, 0.75f)
        };
    }
}
