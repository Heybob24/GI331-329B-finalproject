using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class GameModeSelectionUI : MonoBehaviour
    {
        private RectTransform safeAreaRoot;
        private WorldMapUI worldMapUI;
        private EndlessModeUI endlessModeUI;
        private ChallengeSelectionUI challengeSelectionUI;

        private GameObject rootOverlay;
        private Text campaignStatsText;
        private Text endlessStatsText;
        private Text challengeStatsText;

        public bool IsOpen => rootOverlay != null && rootOverlay.activeSelf;

        public void Configure(RectTransform safeRoot, WorldMapUI mapUI, EndlessModeUI endlessUI, ChallengeSelectionUI challengeUI)
        {
            safeAreaRoot = safeRoot;
            worldMapUI = mapUI;
            endlessModeUI = endlessUI;
            challengeSelectionUI = challengeUI;
        }

        private void EnsureConfigured()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
            if (worldMapUI == null) worldMapUI = FindAnyObjectByType<WorldMapUI>();
            if (endlessModeUI == null) endlessModeUI = FindAnyObjectByType<EndlessModeUI>();
            if (challengeSelectionUI == null) challengeSelectionUI = FindAnyObjectByType<ChallengeSelectionUI>();
        }

        public void Open()
        {
            EnsureConfigured();
            if (rootOverlay == null)
            {
                BuildUI();
            }
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(true);
                Refresh();
            }
        }

        public void Close()
        {
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(false);
            }
        }

        public void Refresh()
        {
            EnsureConfigured();

            if (campaignStatsText != null)
            {
                int victories = CampaignManager.Instance != null ? CampaignManager.Instance.TotalVictories : 0;
                campaignStatsText.text = $"Conquer 6 World Regions & 18 Stages   •   Victories: {victories}";
            }

            if (endlessStatsText != null)
            {
                int bestWave = EndlessChallengeManager.Instance != null ? EndlessChallengeManager.Instance.HighestEndlessWave : 0;
                int bestScore = EndlessChallengeManager.Instance != null ? EndlessChallengeManager.Instance.HighestEndlessScore : 0;
                endlessStatsText.text = $"Relentless Waves, Elites & Bosses   •   Best Wave: {bestWave}   •   Best Score: {bestScore}";
            }

            if (challengeStatsText != null)
            {
                int cleared = 0;
                int total = 6;
                if (EndlessChallengeManager.Instance != null)
                {
                    var list = EndlessChallengeManager.Instance.Challenges.AllChallenges;
                    total = list.Count;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && EndlessChallengeManager.Instance.IsChallengeCompleted(list[i].challengeId))
                            cleared++;
                    }
                }
                challengeStatsText.text = $"Tactical Rule Encounters & Modifiers   •   Cleared: {cleared} / {total}";
            }
        }

        public void SelectCampaignMode()
        {
            Close();
            worldMapUI?.Open();
        }

        public void SelectEndlessMode()
        {
            Close();
            endlessModeUI?.Open();
        }

        public void SelectChallengeMode()
        {
            Close();
            challengeSelectionUI?.Open();
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null) return;

            RectTransform overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "GameModeSelectionOverlay");
            rootOverlay = overlay.gameObject;

            RectTransform card = RoomUIFactory.CreatePanel(overlay, "GameModeSelectionCard",
                new Color(0.08f, 0.08f, 0.13f, 0.98f), Vector2.zero, new Vector2(940f, 1180f));

            RoomUIFactory.CreateText(card, "Title", "SELECT GAME MODE", 42,
                new Vector2(0f, 495f), new Vector2(860f, 65f), new Color(0.96f, 0.78f, 0.43f));

            // 1. Campaign Mode Button Card
            Button campBtn = RoomUIFactory.CreateButton(card, "SelectCampaignModeButton", "1. CAMPAIGN MODE (WORLD MAP)",
                new Vector2(0f, 305f), new Vector2(820f, 110f), new Color(0.16f, 0.34f, 0.44f, 1f), 26);
            campBtn.onClick.AddListener(SelectCampaignMode);
            campaignStatsText = RoomUIFactory.CreateText(card, "CampaignModeSubText", "", 19,
                new Vector2(0f, 225f), new Vector2(820f, 38f), new Color(0.75f, 0.88f, 0.96f));

            // 2. Endless Dungeon Button Card
            Button endlessBtn = RoomUIFactory.CreateButton(card, "SelectEndlessModeButton", "2. ENDLESS DUNGEON MODE",
                new Vector2(0f, 85f), new Vector2(820f, 110f), new Color(0.46f, 0.20f, 0.16f, 1f), 26);
            endlessBtn.onClick.AddListener(SelectEndlessMode);
            endlessStatsText = RoomUIFactory.CreateText(card, "EndlessModeSubText", "", 19,
                new Vector2(0f, 5f), new Vector2(820f, 38f), new Color(0.96f, 0.82f, 0.72f));

            // 3. Challenge Mode Button Card
            Button challengeBtn = RoomUIFactory.CreateButton(card, "SelectChallengeModeButton", "3. CHALLENGE MODE",
                new Vector2(0f, -135f), new Vector2(820f, 110f), new Color(0.32f, 0.20f, 0.42f, 1f), 26);
            challengeBtn.onClick.AddListener(SelectChallengeMode);
            challengeStatsText = RoomUIFactory.CreateText(card, "ChallengeModeSubText", "", 19,
                new Vector2(0f, -215f), new Vector2(820f, 38f), new Color(0.88f, 0.78f, 0.96f));

            Button closeBtn = RoomUIFactory.CreateButton(card, "CloseGameModeButton", "CLOSE",
                new Vector2(0f, -430f), new Vector2(360f, 84f), new Color(0.22f, 0.24f, 0.32f, 1f), 24);
            closeBtn.onClick.AddListener(Close);
        }
    }
}
