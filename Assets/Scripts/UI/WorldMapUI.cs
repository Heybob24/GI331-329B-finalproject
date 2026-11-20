using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class WorldMapUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private WorldMapManager worldMapManager;
        [SerializeField] private CampaignSelectionUI campaignSelectionUI;

        private RectTransform overlay;
        private Transform content;
        private Text statusText;
        private Text summaryText;
        private readonly List<RegionCardUI> spawnedCards = new List<RegionCardUI>();

        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public IReadOnlyList<RegionCardUI> SpawnedCards => spawnedCards;

        public void Configure(RectTransform safeArea, WorldMapManager manager = null, CampaignSelectionUI campaignUI = null)
        {
            safeAreaRoot = safeArea;
            if (manager != null) worldMapManager = manager;
            if (campaignUI != null) campaignSelectionUI = campaignUI;
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
            if (worldMapManager != null) worldMapManager.OnWorldMapUpdated += Refresh;
        }

        private void OnDisable()
        {
            if (worldMapManager != null) worldMapManager.OnWorldMapUpdated -= Refresh;
        }

        private void EnsureReferences()
        {
            if (worldMapManager == null)
                worldMapManager = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            if (campaignSelectionUI == null)
                campaignSelectionUI = FindAnyObjectByType<CampaignSelectionUI>();
        }

        public void Open()
        {
            EnsureReferences();
            if (overlay == null) Build();
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (overlay != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "WorldMapOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "WorldMapWindow",
                new Vector2(1000f, 1680f), new Color(0.075f, 0.085f, 0.12f, 0.98f));

            RoomUIFactory.CreateText(window, "WorldMapTitle", "WORLD MAP", 40,
                new Vector2(-40f, 770f), new Vector2(720f, 68f), new Color(0.96f, 0.78f, 0.43f));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseWorldMapButton", "CLOSE",
                new Vector2(405f, 770f), new Vector2(150f, 68f), new Color(0.34f, 0.19f, 0.19f), 20);
            closeBtn.onClick.AddListener(Close);

            summaryText = RoomUIFactory.CreateText(window, "WorldMapSummary", "REGIONS UNLOCKED: 1 / 6", 20,
                new Vector2(0f, 705f), new Vector2(880f, 44f), new Color(0.68f, 0.88f, 0.96f));

            statusText = RoomUIFactory.CreateText(window, "WorldMapStatus",
                "Select an unlocked World Region to view its Campaign Stages.", 19,
                new Vector2(0f, 645f), new Vector2(920f, 48f), new Color(0.84f, 0.86f, 0.92f));

            content = RoomUIFactory.CreateScrollList(window, "WorldMapScrollList",
                new Vector2(0f, -65f), new Vector2(940f, 1340f));

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (content == null) return;
            EnsureReferences();
            if (worldMapManager == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            spawnedCards.Clear();

            IReadOnlyList<WorldRegionData> regions = worldMapManager.GetAllRegions();
            int unlockedCount = 0;
            int completedCount = 0;

            foreach (WorldRegionData region in regions)
            {
                if (region == null) continue;
                if (worldMapManager.CanEnterRegion(region.regionId, out _)) unlockedCount++;
                if (worldMapManager.IsRegionCompleted(region.regionId)) completedCount++;

                RegionCardUI card = RegionCardUI.Create(content, region, worldMapManager, OnRegionClicked);
                spawnedCards.Add(card);
            }

            if (summaryText != null)
            {
                summaryText.text = $"REGIONS UNLOCKED: {unlockedCount} / {regions.Count}   •   COMPLETED: {completedCount} / {regions.Count}";
            }

            Canvas.ForceUpdateCanvases();
        }

        private void OnRegionClicked(string regionId)
        {
            EnsureReferences();
            if (worldMapManager == null) return;

            if (!worldMapManager.TrySelectRegion(regionId, out string message))
            {
                SetStatus(message);
                return;
            }

            SetStatus(message);
            if (campaignSelectionUI != null)
            {
                Close();
                campaignSelectionUI.OpenForRegion(regionId);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
