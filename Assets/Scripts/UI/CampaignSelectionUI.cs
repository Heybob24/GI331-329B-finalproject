using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class CampaignSelectionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private CampaignManager campaignManager;
        [SerializeField] private WorldMapManager worldMapManager;
        [SerializeField] private WorldMapUI worldMapUI;
        [SerializeField] private StageDetailsUI stageDetailsUI;

        private RectTransform overlay;
        private Transform content;
        private Text campaignTitleText;
        private Text campaignDescText;
        private Text statusText;
        private string currentRegionId = "forgotten_forest";
        private readonly List<StageCardUI> spawnedCards = new List<StageCardUI>();

        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public string CurrentRegionId => currentRegionId;
        public IReadOnlyList<StageCardUI> SpawnedCards => spawnedCards;

        public void Configure(RectTransform safeArea, CampaignManager campaign = null, WorldMapManager worldMap = null,
            WorldMapUI mapUI = null, StageDetailsUI detailsUI = null)
        {
            safeAreaRoot = safeArea;
            if (campaign != null) campaignManager = campaign;
            if (worldMap != null) worldMapManager = worldMap;
            if (mapUI != null) worldMapUI = mapUI;
            if (detailsUI != null) stageDetailsUI = detailsUI;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            EnsureReferences();
            Build();
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (campaignManager != null) campaignManager.OnCampaignUpdated += Refresh;
        }

        private void OnDisable()
        {
            if (campaignManager != null) campaignManager.OnCampaignUpdated -= Refresh;
        }

        private void EnsureReferences()
        {
            if (campaignManager == null)
                campaignManager = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>();
            if (worldMapManager == null)
                worldMapManager = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            if (worldMapUI == null)
                worldMapUI = FindAnyObjectByType<WorldMapUI>();
            if (stageDetailsUI == null)
                stageDetailsUI = FindAnyObjectByType<StageDetailsUI>();
        }

        public void OpenForRegion(string regionId)
        {
            EnsureReferences();
            if (!string.IsNullOrWhiteSpace(regionId))
                currentRegionId = regionId.Trim();
            else if (worldMapManager != null && !string.IsNullOrWhiteSpace(worldMapManager.SelectedRegionId))
                currentRegionId = worldMapManager.SelectedRegionId;

            if (overlay == null) Build();
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void BackToWorldMap()
        {
            Close();
            worldMapUI?.Open();
        }

        private void Build()
        {
            if (overlay != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "CampaignSelectionOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "CampaignSelectionWindow",
                new Vector2(1000f, 1680f), new Color(0.075f, 0.085f, 0.12f, 0.98f));

            Button backBtn = RoomUIFactory.CreateButton(window, "BackToWorldMapButton", "< MAP",
                new Vector2(-400f, 770f), new Vector2(160f, 68f), new Color(0.20f, 0.26f, 0.36f), 20);
            backBtn.onClick.AddListener(BackToWorldMap);

            campaignTitleText = RoomUIFactory.CreateText(window, "CampaignTitle", "CAMPAIGN STAGES", 36,
                new Vector2(0f, 770f), new Vector2(620f, 68f), new Color(0.96f, 0.78f, 0.43f));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseCampaignButton", "CLOSE",
                new Vector2(405f, 770f), new Vector2(150f, 68f), new Color(0.34f, 0.19f, 0.19f), 20);
            closeBtn.onClick.AddListener(Close);

            campaignDescText = RoomUIFactory.CreateText(window, "CampaignDescription", "", 19,
                new Vector2(0f, 700f), new Vector2(900f, 54f), new Color(0.80f, 0.85f, 0.92f));

            statusText = RoomUIFactory.CreateText(window, "CampaignStatus",
                "Select an unlocked stage to review enemy factions, difficulty, and rewards.", 18,
                new Vector2(0f, 640f), new Vector2(900f, 44f), new Color(0.70f, 0.88f, 0.95f));

            content = RoomUIFactory.CreateScrollList(window, "CampaignStageScrollList",
                new Vector2(0f, -65f), new Vector2(940f, 1340f));

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (content == null) return;
            EnsureReferences();
            if (campaignManager == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            spawnedCards.Clear();

            CampaignData campaign = campaignManager.GetCampaignForRegion(currentRegionId);
            if (campaignTitleText != null)
            {
                campaignTitleText.text = campaign != null ? campaign.campaignName.ToUpperInvariant() : "CAMPAIGN STAGES";
            }
            if (campaignDescText != null)
            {
                campaignDescText.text = campaign != null ? campaign.description : "";
            }

            List<CampaignStageData> stageList = campaignManager.GetStagesForRegion(currentRegionId);
            foreach (CampaignStageData stage in stageList)
            {
                if (stage == null) continue;
                StageCardUI card = StageCardUI.Create(content, stage, campaignManager, OnStageClicked);
                spawnedCards.Add(card);
            }

            Canvas.ForceUpdateCanvases();
        }

        private void OnStageClicked(string stageId)
        {
            EnsureReferences();
            if (campaignManager == null) return;

            if (!campaignManager.TrySelectStage(stageId, out string error))
            {
                SetStatus(error);
                return;
            }

            CampaignStageData stage = campaignManager.GetStage(stageId);
            if (stageDetailsUI != null && stage != null)
            {
                Close();
                stageDetailsUI.OpenForStage(stage);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
