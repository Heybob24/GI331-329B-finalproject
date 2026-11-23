using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class SelectGameModeUI : MonoBehaviour
    {
        private sealed class ModeCardView
        {
            public SelectedGameMode Mode;
            public Image HighlightBorder;
            public Image CardImage;
            public TextMeshProUGUI ProgressText;
            public Button CardButton;
            public Button PlayButton;
        }

        private RectTransform safeAreaRoot;
        private DreamDungeonUIAssetDatabase uiAssets;
        private GameObject rootOverlay;

        private TextMeshProUGUI goldValueText;
        private TextMeshProUGUI crystalValueText;
        private TextMeshProUGUI selectedModeTitleText;
        private TextMeshProUGUI selectedModeDescText;

        private readonly List<ModeCardView> cardViews = new List<ModeCardView>();
        private Action onSettingsClicked;
        private Action onBackClicked;
        private bool isTransitioning;

        public bool IsOpen => rootOverlay != null && rootOverlay.activeSelf;

        public void Configure(RectTransform safeRoot, Action onSettings = null, Action onBack = null)
        {
            safeAreaRoot = safeRoot;
            onSettingsClicked = onSettings;
            onBackClicked = onBack;
        }

        public void Open()
        {
            SelectedGameModeManager.EnsureInstance();
            uiAssets = DreamDungeonUIAssetDatabase.Load();

            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }

            if (rootOverlay == null)
            {
                BuildUI();
            }

            if (rootOverlay != null)
            {
                isTransitioning = false;
                rootOverlay.SetActive(true);
                rootOverlay.transform.SetAsLastSibling();
                RefreshAll();
            }
        }

        public void Close()
        {
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            SelectedGameModeManager mgr = SelectedGameModeManager.EnsureInstance();

            // Live Currency values (never hardcoded)
            int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
            int crystal = CrystalManager.Instance != null
                ? CrystalManager.Instance.CrystalBalance
                : (CurrencyManager.Instance != null ? CurrencyManager.Instance.Crystal : 0);

            if (goldValueText != null) goldValueText.text = gold.ToString("N0");
            if (crystalValueText != null) crystalValueText.text = crystal.ToString("N0");

            for (int i = 0; i < cardViews.Count; i++)
            {
                ModeCardView view = cardViews[i];
                bool isSelected = view.Mode == mgr.CurrentMode;

                if (view.HighlightBorder != null)
                {
                    view.HighlightBorder.color = isSelected
                        ? new Color(1f, 0.82f, 0.25f, 1f)
                        : new Color(0.28f, 0.20f, 0.45f, 0.45f);
                }

                if (view.CardImage != null)
                {
                    view.CardImage.color = isSelected ? Color.white : new Color(0.86f, 0.86f, 0.92f, 1f);
                }

                if (view.ProgressText != null)
                {
                    view.ProgressText.text = mgr.GetModeLiveProgressSummary(view.Mode);
                }
            }

            if (selectedModeTitleText != null)
            {
                selectedModeTitleText.text = $"SELECTED: {mgr.GetModeDisplayName(mgr.CurrentMode)}";
            }

            if (selectedModeDescText != null)
            {
                selectedModeDescText.text = $"{mgr.GetModeDescription(mgr.CurrentMode)}\n{mgr.GetModeLiveProgressSummary(mgr.CurrentMode)}";
            }
        }

        public void SelectModeCard(SelectedGameMode mode)
        {
            SelectedGameModeManager.EnsureInstance().SelectMode(mode);
            RefreshAll();
        }

        public void PlaySelectedMode(SelectedGameMode mode)
        {
            if (isTransitioning) return;
            isTransitioning = true;

            SelectedGameModeManager mgr = SelectedGameModeManager.EnsureInstance();
            mgr.SelectMode(mode);

            bool hasExistingCloudDungeon = PlayerDatabaseManager.Instance?.CurrentData?.dungeonData != null &&
                                           !string.IsNullOrEmpty(PlayerDatabaseManager.Instance.CurrentData.dungeonData.dungeonLayoutJson);
            bool hasLocalSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();

            if (!hasExistingCloudDungeon && !hasLocalSave)
            {
                GameManager.RequestNewGame();
                BattleSession.Reset();
                mgr.ApplyModeToBattleSession();
            }

            SceneLoader.Load("DungeonEditor");
        }

        private void OnBackPressed()
        {
            Close();
            onBackClicked?.Invoke();
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null) return;
            cardViews.Clear();

            // Full-screen dark fantasy backdrop inside SafeAreaRoot
            GameObject overlayObj = new GameObject("SelectGameModeOverlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(safeAreaRoot, false);
            RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.09f, 0.99f);
            rootOverlay = overlayObj;

            // Decorative Purple Banners (Left & Right)
            if (uiAssets != null && uiAssets.decorBannerPurple != null)
            {
                CreateDecorImage(overlayRt, "LeftBanner", uiAssets.decorBannerPurple,
                    new Vector2(0f, 0.80f), new Vector2(0.11f, 0.99f), new Color(1f, 1f, 1f, 0.85f));
                CreateDecorImage(overlayRt, "RightBanner", uiAssets.decorBannerPurple,
                    new Vector2(0.89f, 0.80f), new Vector2(1f, 0.99f), new Color(1f, 1f, 1f, 0.85f));
            }

            // Main vertical layout container
            GameObject layoutObj = new GameObject("SelectModeLayout", typeof(RectTransform), typeof(VerticalLayoutGroup));
            layoutObj.transform.SetParent(overlayRt, false);
            RectTransform layoutRt = layoutObj.GetComponent<RectTransform>();
            layoutRt.anchorMin = Vector2.zero;
            layoutRt.anchorMax = Vector2.one;
            layoutRt.offsetMin = new Vector2(18f, 18f);
            layoutRt.offsetMax = new Vector2(-18f, -14f);

            VerticalLayoutGroup vlg = layoutObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. TOP BAR: Logo + Currencies + Settings
            BuildTopBar(layoutRt);

            // 2. TITLE HEADER: Skull Emblem + SELECT GAME MODE
            BuildTitleHeader(layoutRt);

            // 3. CENTER: 4 Game Mode Cards (2x2 GridLayoutGroup)
            BuildModeCardsGrid(layoutRt);

            // 4. BOTTOM BAR: Back Button + Selected Mode Description
            BuildBottomBar(layoutRt);
        }

        private void BuildTopBar(RectTransform parent)
        {
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            topBar.transform.SetParent(parent, false);
            topBar.GetComponent<LayoutElement>().preferredHeight = 126f;

            HorizontalLayoutGroup hlg = topBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Logo
            GameObject logoObj = new GameObject("Logo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            logoObj.transform.SetParent(topBar.transform, false);
            LayoutElement logoLe = logoObj.GetComponent<LayoutElement>();
            logoLe.preferredWidth = 240f;
            logoLe.preferredHeight = 118f;
            Image logoImg = logoObj.GetComponent<Image>();
            if (uiAssets != null && uiAssets.logoDreamDungeon != null)
            {
                logoImg.sprite = uiAssets.logoDreamDungeon;
                logoImg.preserveAspect = true;
                logoImg.color = Color.white;
            }
            else
            {
                logoImg.color = new Color(0.14f, 0.10f, 0.24f, 1f);
            }

            // Spacer
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(topBar.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

            // Crystal Counter
            crystalValueText = CreateCurrencyCounter(topBar.transform, "CrystalCounter",
                uiAssets != null ? uiAssets.currencyCrystalCounter : null, new Color(0.84f, 0.56f, 1f));

            // Gold Counter
            goldValueText = CreateCurrencyCounter(topBar.transform, "GoldCounter",
                uiAssets != null ? uiAssets.currencyGoldCounter : null, new Color(1f, 0.88f, 0.40f));

            // Settings Button
            GameObject settingsObj = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            settingsObj.transform.SetParent(topBar.transform, false);
            LayoutElement setLe = settingsObj.GetComponent<LayoutElement>();
            setLe.preferredWidth = 72f;
            setLe.preferredHeight = 72f;
            Image setImg = settingsObj.GetComponent<Image>();
            if (uiAssets != null && uiAssets.iconSettings != null)
            {
                setImg.sprite = uiAssets.iconSettings;
                setImg.preserveAspect = true;
            }
            else
            {
                setImg.color = new Color(0.24f, 0.20f, 0.34f, 1f);
            }
            settingsObj.GetComponent<Button>().onClick.AddListener(() => onSettingsClicked?.Invoke());
        }

        private void BuildTitleHeader(RectTransform parent)
        {
            GameObject header = new GameObject("TitleHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(parent, false);
            header.GetComponent<LayoutElement>().preferredHeight = 72f;

            HorizontalLayoutGroup hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            if (uiAssets != null && uiAssets.decorSkullEmblem != null)
            {
                GameObject skull = new GameObject("SkullEmblem", typeof(RectTransform), typeof(Image));
                skull.transform.SetParent(header.transform, false);
                skull.GetComponent<RectTransform>().sizeDelta = new Vector2(56f, 64f);
                Image skullImg = skull.GetComponent<Image>();
                skullImg.sprite = uiAssets.decorSkullEmblem;
                skullImg.preserveAspect = true;
            }

            GameObject titleObj = new GameObject("SelectGameModeTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 62f);
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "SELECT GAME MODE";
            titleTmp.fontSize = 42;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.84f, 0.42f);
            titleTmp.alignment = TextAlignmentOptions.Center;
        }

        private void BuildModeCardsGrid(RectTransform parent)
        {
            GameObject gridContainer = new GameObject("ModeCardsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridContainer.transform.SetParent(parent, false);
            LayoutElement le = gridContainer.GetComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.minHeight = 960f;

            GridLayoutGroup glg = gridContainer.GetComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.cellSize = new Vector2(468f, 590f);
            glg.spacing = new Vector2(22f, 22f);
            glg.childAlignment = TextAnchor.MiddleCenter;

            CreateModeCard(gridContainer.transform, SelectedGameMode.Campaign, "CAMPAIGN");
            CreateModeCard(gridContainer.transform, SelectedGameMode.Endless, "ENDLESS");
            CreateModeCard(gridContainer.transform, SelectedGameMode.Challenge, "CHALLENGE");
            CreateModeCard(gridContainer.transform, SelectedGameMode.Custom, "CUSTOM");
        }

        private void CreateModeCard(Transform parent, SelectedGameMode mode, string label)
        {
            GameObject cardRoot = new GameObject($"ModeCard_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardRoot.transform.SetParent(parent, false);
            Image highlightBorder = cardRoot.GetComponent<Image>();
            highlightBorder.color = new Color(0.28f, 0.20f, 0.45f, 0.45f);

            // Card Art Image (uses exact matching ModeCard_*.png)
            GameObject artObj = new GameObject("CardArt", typeof(RectTransform), typeof(Image));
            artObj.transform.SetParent(cardRoot.transform, false);
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = new Vector2(8f, 8f);
            artRt.offsetMax = new Vector2(-8f, -8f);
            Image artImg = artObj.GetComponent<Image>();
            Sprite cardSprite = uiAssets != null ? uiAssets.GetModeCard(mode) : null;
            if (cardSprite != null)
            {
                artImg.sprite = cardSprite;
                artImg.preserveAspect = true;
            }
            else
            {
                artImg.color = new Color(0.14f, 0.12f, 0.22f, 1f);
            }

            // Live Progress Strip (overlays cleanly above the Play button area)
            GameObject progObj = new GameObject("ProgressStrip", typeof(RectTransform), typeof(Image));
            progObj.transform.SetParent(cardRoot.transform, false);
            RectTransform progRt = progObj.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0.08f, 0.19f);
            progRt.anchorMax = new Vector2(0.92f, 0.265f);
            progRt.offsetMin = Vector2.zero;
            progRt.offsetMax = Vector2.zero;
            progObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.11f, 0.90f);

            GameObject progTxtObj = new GameObject("ProgressText", typeof(RectTransform), typeof(TextMeshProUGUI));
            progTxtObj.transform.SetParent(progObj.transform, false);
            Stretch(progTxtObj, 8f, 2f, 8f, 2f);
            TextMeshProUGUI progTmp = progTxtObj.GetComponent<TextMeshProUGUI>();
            progTmp.fontSize = 18;
            progTmp.fontStyle = FontStyles.Bold;
            progTmp.color = new Color(0.98f, 0.86f, 0.50f);
            progTmp.alignment = TextAlignmentOptions.Center;
            progTmp.enableAutoSizing = true;
            progTmp.fontSizeMin = 13;
            progTmp.fontSizeMax = 18;

            // Play Button (uses real Button_Play.png)
            GameObject playObj = new GameObject($"PlayButton_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            playObj.transform.SetParent(cardRoot.transform, false);
            RectTransform playRt = playObj.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.18f, 0.045f);
            playRt.anchorMax = new Vector2(0.82f, 0.175f);
            playRt.offsetMin = Vector2.zero;
            playRt.offsetMax = Vector2.zero;
            Image playImg = playObj.GetComponent<Image>();
            if (uiAssets != null && uiAssets.buttonPlay != null)
            {
                playImg.sprite = uiAssets.buttonPlay;
                playImg.preserveAspect = true;
            }
            else
            {
                playImg.color = new Color(0.85f, 0.55f, 0.12f, 1f);
            }

            Button cardBtn = cardRoot.GetComponent<Button>();
            cardBtn.targetGraphic = artImg;
            cardBtn.onClick.AddListener(() => SelectModeCard(mode));

            Button playBtn = playObj.GetComponent<Button>();
            playBtn.targetGraphic = playImg;
            playBtn.onClick.AddListener(() => PlaySelectedMode(mode));

            cardViews.Add(new ModeCardView
            {
                Mode = mode,
                HighlightBorder = highlightBorder,
                CardImage = artImg,
                ProgressText = progTmp,
                CardButton = cardBtn,
                PlayButton = playBtn
            });
        }

        private void BuildBottomBar(RectTransform parent)
        {
            GameObject bottom = new GameObject("BottomBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bottom.transform.SetParent(parent, false);
            bottom.GetComponent<LayoutElement>().preferredHeight = 148f;

            Image bg = bottom.GetComponent<Image>();
            if (uiAssets != null && uiAssets.panelEmptyFrame != null)
            {
                bg.sprite = uiAssets.panelEmptyFrame;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.09f, 0.08f, 0.16f, 0.96f);
            }

            HorizontalLayoutGroup hlg = bottom.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(22, 22, 18, 18);
            hlg.spacing = 18f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Back Button (uses real Button_Back.png)
            GameObject backObj = new GameObject("BackToMainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            backObj.transform.SetParent(bottom.transform, false);
            LayoutElement backLe = backObj.GetComponent<LayoutElement>();
            backLe.preferredWidth = 210f;
            backLe.preferredHeight = 82f;
            Image backImg = backObj.GetComponent<Image>();
            if (uiAssets != null && uiAssets.buttonBack != null)
            {
                backImg.sprite = uiAssets.buttonBack;
                backImg.preserveAspect = true;
            }
            else
            {
                backImg.color = new Color(0.32f, 0.20f, 0.44f, 1f);
            }
            backObj.GetComponent<Button>().onClick.AddListener(OnBackPressed);

            // Selected Mode Description Area
            GameObject infoCol = new GameObject("SelectedModeInfo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            infoCol.transform.SetParent(bottom.transform, false);
            infoCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup colVlg = infoCol.GetComponent<VerticalLayoutGroup>();
            colVlg.spacing = 4f;
            colVlg.childControlWidth = true;
            colVlg.childControlHeight = true;

            GameObject titleObj = new GameObject("SelectedTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleObj.transform.SetParent(infoCol.transform, false);
            titleObj.GetComponent<LayoutElement>().preferredHeight = 32f;
            selectedModeTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            selectedModeTitleText.fontSize = 24;
            selectedModeTitleText.fontStyle = FontStyles.Bold;
            selectedModeTitleText.color = new Color(1f, 0.84f, 0.42f);
            selectedModeTitleText.alignment = TextAlignmentOptions.Left;

            GameObject descObj = new GameObject("SelectedDesc", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            descObj.transform.SetParent(infoCol.transform, false);
            descObj.GetComponent<LayoutElement>().preferredHeight = 68f;
            selectedModeDescText = descObj.GetComponent<TextMeshProUGUI>();
            selectedModeDescText.fontSize = 18;
            selectedModeDescText.color = new Color(0.86f, 0.90f, 0.96f);
            selectedModeDescText.alignment = TextAlignmentOptions.TopLeft;
            selectedModeDescText.textWrappingMode = TextWrappingModes.Normal;
        }

        private TextMeshProUGUI CreateCurrencyCounter(Transform parent, string name, Sprite counterSprite, Color textColor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.preferredWidth = 228f;
            le.preferredHeight = 72f;

            Image bg = obj.GetComponent<Image>();
            if (counterSprite != null)
            {
                bg.sprite = counterSprite;
                bg.preserveAspect = true;
            }
            else
            {
                bg.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);
            }

            // Cover baked sample number cleanly with dark backdrop, then draw LIVE value
            GameObject mask = new GameObject("LiveValueBackdrop", typeof(RectTransform), typeof(Image));
            mask.transform.SetParent(obj.transform, false);
            RectTransform mRt = mask.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.27f, 0.16f);
            mRt.anchorMax = new Vector2(0.80f, 0.84f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            mask.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 1f);

            GameObject txtObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(mask.transform, false);
            Stretch(txtObj, 2f, 0f, 2f, 0f);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "0";
            tmp.fontSize = 23;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 15;
            tmp.fontSizeMax = 24;
            return tmp;
        }

        private static void CreateDecorImage(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
        }

        private static void Stretch(GameObject go, float left, float bottom, float right, float top)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
