using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class ChallengeSelectionUI : MonoBehaviour
    {
        private RectTransform safeAreaRoot;
        private EndlessChallengeManager manager;
        private RoomManager roomManager;

        private GameObject rootOverlay;
        private RectTransform listContent;
        private Text selectedTitleText;
        private Text selectedDifficultyUnlockText;
        private Text selectedDescriptionText;
        private Text selectedModifiersText;
        private Text selectedRewardsAndRecordText;
        private Text validationStatusText;
        private Button startChallengeButton;

        private readonly List<GameObject> spawnedCards = new List<GameObject>();
        private string selectedChallengeId = "challenge_monster_survival";

        public bool IsOpen => rootOverlay != null && rootOverlay.activeSelf;
        public IReadOnlyList<GameObject> SpawnedCards => spawnedCards;

        public void Configure(RectTransform safeRoot, EndlessChallengeManager ecMgr = null, RoomManager rMgr = null)
        {
            safeAreaRoot = safeRoot;
            if (ecMgr != null) manager = ecMgr;
            if (rMgr != null) roomManager = rMgr;
        }

        private void EnsureConfigured()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
            if (manager == null)
            {
                manager = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>();
                if (manager == null)
                {
                    manager = gameObject.GetComponent<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
                }
            }
            if (roomManager == null)
            {
                roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
            }
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
                RebuildChallengeList();
                SelectChallenge(selectedChallengeId);
            }
        }

        public void Close()
        {
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(false);
            }
        }

        public void SelectChallenge(string challengeId)
        {
            EnsureConfigured();
            if (manager == null) return;

            ChallengeData ch = manager.GetChallenge(challengeId);
            if (ch == null && manager.Challenges.AllChallenges.Count > 0)
            {
                ch = manager.Challenges.AllChallenges[0];
            }
            if (ch == null) return;

            selectedChallengeId = ch.challengeId;
            ChallengeRecordEntry rec = manager.GetOrCreateChallengeRecord(ch.challengeId);
            bool unlocked = manager.IsChallengeUnlocked(ch.challengeId, out string lockReason);
            bool dungeonValid = manager.ValidateDungeonForRun(roomManager, ch, out string dungeonErr);

            if (selectedTitleText != null)
            {
                string clearTag = rec != null && rec.isCompleted ? " <color=#66F088>[COMPLETED ★]</color>" : "";
                selectedTitleText.text = $"{ch.challengeName}{clearTag}";
            }

            if (selectedDifficultyUnlockText != null)
            {
                string statusStr = unlocked ? "<color=#66F088>UNLOCKED</color>" : $"<color=#FF7777>LOCKED ({lockReason})</color>";
                selectedDifficultyUnlockText.text = $"Difficulty: {ch.difficulty}   •   Waves: {ch.waveConfiguration.Count}   •   Status: {statusStr}";
            }

            if (selectedDescriptionText != null)
            {
                selectedDescriptionText.text = $"{ch.description}\nObjective: {ch.GetObjectivesText()}";
            }

            if (selectedModifiersText != null)
            {
                selectedModifiersText.text = $"<b>ACTIVE MODIFIERS:</b>  {ch.GetModifiersSummaryText()}";
            }

            if (selectedRewardsAndRecordText != null)
            {
                bool fcClaimed = rec != null && rec.firstClearClaimed;
                string rewardLine = fcClaimed
                    ? $"Replay Reward: +{ch.replayGold} Gold, +{ch.replaySoul} Soul, +{ch.replayExp} EXP (First Clear Claimed)"
                    : $"First-Clear Reward: +{ch.firstClearGold} Gold, +{ch.firstClearSoul} Soul, +{ch.firstClearExp} EXP";
                string bestTimeStr = rec != null && rec.bestClearTimeSeconds > 0f ? $"{rec.bestClearTimeSeconds:0.0}s" : "--";
                int bestScore = rec != null ? rec.bestScore : 0;

                selectedRewardsAndRecordText.text =
                    $"{rewardLine}\nBest Score: <color=#FFD966>{bestScore}</color>   •   Best Clear Time: {bestTimeStr}   •   Clears: {(rec != null ? rec.totalClears : 0)}";
            }

            if (validationStatusText != null)
            {
                if (!unlocked)
                    validationStatusText.text = $"<color=#FF7777>Locked: {lockReason}</color>";
                else if (!dungeonValid)
                    validationStatusText.text = $"<color=#FF7777>⚠ {dungeonErr}</color>";
                else
                    validationStatusText.text = "<color=#66F088>✓ Dungeon Ready for Challenge!</color>";
            }

            if (startChallengeButton != null)
            {
                startChallengeButton.interactable = unlocked && dungeonValid;
            }
        }

        private void OnStartChallengeClicked()
        {
            EnsureConfigured();
            if (manager == null) return;

            if (!manager.TryStartChallengeRun(selectedChallengeId, roomManager, out string error))
            {
                if (validationStatusText != null)
                {
                    validationStatusText.text = $"<color=#FF7777>⚠ {error}</color>";
                }
            }
        }

        private void RebuildChallengeList()
        {
            if (listContent == null || manager == null) return;

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null) Destroy(spawnedCards[i]);
            }
            spawnedCards.Clear();

            var challenges = manager.Challenges.AllChallenges;
            for (int i = 0; i < challenges.Count; i++)
            {
                ChallengeData ch = challenges[i];
                if (ch == null) continue;

                bool unlocked = manager.IsChallengeUnlocked(ch.challengeId, out _);
                bool completed = manager.IsChallengeCompleted(ch.challengeId);

                Color cardColor = completed
                    ? new Color(0.16f, 0.30f, 0.22f, 1f)
                    : (unlocked ? new Color(0.20f, 0.22f, 0.32f, 1f) : new Color(0.14f, 0.14f, 0.18f, 0.85f));

                RectTransform cardRt = RoomUIFactory.CreatePanel(listContent, $"ChallengeCard_{ch.challengeId}",
                    cardColor, Vector2.zero, new Vector2(840f, 88f));
                LayoutElement le = cardRt.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 88f;
                le.minHeight = 88f;

                Button btn = cardRt.gameObject.AddComponent<Button>();
                string cId = ch.challengeId;
                btn.onClick.AddListener(() => SelectChallenge(cId));

                string badge = completed ? "[CLEARED ★]" : (unlocked ? "[AVAILABLE]" : "[LOCKED]");
                RoomUIFactory.CreateText(cardRt, "CardLabel",
                    $"{ch.challengeName}   •   {ch.difficulty}   •   {badge}",
                    21, Vector2.zero, new Vector2(800f, 72f), Color.white);

                spawnedCards.Add(cardRt.gameObject);
            }
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null) return;

            RectTransform overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "ChallengeSelectionOverlay");
            rootOverlay = overlay.gameObject;

            RectTransform card = RoomUIFactory.CreatePanel(overlay, "ChallengeSelectionCard",
                new Color(0.08f, 0.08f, 0.13f, 0.98f), Vector2.zero, new Vector2(980f, 1560f));

            RoomUIFactory.CreateText(card, "Title", "CHALLENGE MODE", 42,
                new Vector2(0f, 705f), new Vector2(900f, 65f), new Color(0.96f, 0.78f, 0.43f));

            Transform contentTr = RoomUIFactory.CreateScrollList(card, "ChallengeScrollView",
                new Vector2(0f, 345f), new Vector2(880f, 580f));
            listContent = contentTr as RectTransform;

            RectTransform detailsBox = RoomUIFactory.CreatePanel(card, "ChallengeDetailsBox",
                new Color(0.12f, 0.13f, 0.20f, 1f), new Vector2(0f, -200f), new Vector2(880f, 440f));

            selectedTitleText = RoomUIFactory.CreateText(detailsBox, "SelectedChallengeTitle", "", 28,
                new Vector2(0f, 175f), new Vector2(840f, 46f), new Color(0.98f, 0.85f, 0.45f));

            selectedDifficultyUnlockText = RoomUIFactory.CreateText(detailsBox, "SelectedChallengeDiff", "", 20,
                new Vector2(0f, 130f), new Vector2(840f, 36f), Color.white);

            selectedDescriptionText = RoomUIFactory.CreateText(detailsBox, "SelectedChallengeDesc", "", 20,
                new Vector2(0f, 55f), new Vector2(840f, 95f), new Color(0.86f, 0.88f, 0.94f));

            selectedModifiersText = RoomUIFactory.CreateText(detailsBox, "SelectedChallengeMods", "", 19,
                new Vector2(0f, -35f), new Vector2(840f, 75f), new Color(1.0f, 0.75f, 0.45f));

            selectedRewardsAndRecordText = RoomUIFactory.CreateText(detailsBox, "SelectedChallengeRewards", "", 19,
                new Vector2(0f, -125f), new Vector2(840f, 80f), new Color(0.70f, 0.95f, 0.78f));

            validationStatusText = RoomUIFactory.CreateText(card, "ValidationStatusText", "", 21,
                new Vector2(0f, -465f), new Vector2(880f, 48f), Color.white);

            startChallengeButton = RoomUIFactory.CreateButton(card, "StartChallengeButton", "START CHALLENGE",
                new Vector2(-190f, -565f), new Vector2(440f, 88f), new Color(0.50f, 0.22f, 0.14f, 1f), 26);
            startChallengeButton.onClick.AddListener(OnStartChallengeClicked);

            Button closeBtn = RoomUIFactory.CreateButton(card, "CloseChallengeButton", "BACK",
                new Vector2(245f, -565f), new Vector2(340f, 88f), new Color(0.22f, 0.24f, 0.32f, 1f), 24);
            closeBtn.onClick.AddListener(Close);
        }
    }
}
