using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class AccountUIManager : MonoBehaviour
    {
        public static AccountUIManager Instance { get; private set; }

        private RectTransform safeAreaRoot;
        private GameObject loginOverlay;
        private TextMeshProUGUI loginStatusText;
        private TextMeshProUGUI loginErrorText;
        private TextMeshProUGUI loadingIndicatorText;
        private TextMeshProUGUI accountHeaderBannerText;
        private Button guestLoginButton;
        private Button googleLoginButton;
        private Button retryButton;

        // Crystal IAP Shop Overlay (5 Google Play SKUs + Real Badges + Purchase Popups)
        private GameObject crystalShopOverlay;
        private TextMeshProUGUI crystalShopBalanceText;
        private TextMeshProUGUI crystalShopStatusText;
        private GameObject purchaseResultPopup;
        private Image purchasePopupGraphic;
        private TextMeshProUGUI purchasePopupMessage;

        private bool lastAttemptWasGoogle;
        private bool uiBuilt;

        public bool IsLoginOverlayOpen => loginOverlay != null && loginOverlay.activeSelf;
        public bool IsCrystalShopOpen => crystalShopOverlay != null && crystalShopOverlay.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (AuthenticationManager.Instance != null)
            {
                AuthenticationManager.Instance.OnSignedIn -= HandleSignedIn;
                AuthenticationManager.Instance.OnSignedOut -= HandleSignedOut;
                AuthenticationManager.Instance.OnProfileUpdated -= HandleProfileUpdated;
            }
            if (CrystalManager.Instance != null)
            {
                CrystalManager.Instance.OnCrystalBalanceChanged -= HandleCrystalChanged;
            }
            if (Instance == this) Instance = null;
        }

        public void Configure(RectTransform safeRoot)
        {
            safeAreaRoot = safeRoot;
        }

        public void InitializeAndShowIfNeeded(RectTransform safeRoot = null)
        {
            if (safeRoot != null) safeAreaRoot = safeRoot;
            EnsureSafeAreaAndAuth();
            if (!uiBuilt) BuildUI();

            if (AuthenticationManager.Instance != null)
            {
                AuthenticationManager.Instance.OnSignedIn -= HandleSignedIn;
                AuthenticationManager.Instance.OnSignedIn += HandleSignedIn;
                AuthenticationManager.Instance.OnSignedOut -= HandleSignedOut;
                AuthenticationManager.Instance.OnSignedOut += HandleSignedOut;
                AuthenticationManager.Instance.OnProfileUpdated -= HandleProfileUpdated;
                AuthenticationManager.Instance.OnProfileUpdated += HandleProfileUpdated;
            }

            if (CrystalManager.Instance != null)
            {
                CrystalManager.Instance.OnCrystalBalanceChanged -= HandleCrystalChanged;
                CrystalManager.Instance.OnCrystalBalanceChanged += HandleCrystalChanged;
            }

            if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsSignedIn)
            {
                HideLoginOverlay();
                RefreshAccountBanner();
            }
            else if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.HasExistingSessionToken())
            {
                ShowLoginOverlay();
                SetLoadingState(true, "Connecting to Server...");
                _ = ContinueAsGuestFlowAsync();
            }
            else
            {
                ShowLoginOverlay();
                SetLoadingState(false, "Build Your Lair. Defend Your Dreams.");
            }
        }

        public void ShowLoginOverlay()
        {
            EnsureSafeAreaAndAuth();
            if (!uiBuilt) BuildUI();
            if (loginOverlay != null)
            {
                loginOverlay.SetActive(true);
                loginOverlay.transform.SetAsLastSibling();
            }
            if (loginErrorText != null) loginErrorText.text = string.Empty;
            if (retryButton != null) retryButton.gameObject.SetActive(false);
        }

        public void HideLoginOverlay()
        {
            if (loginOverlay != null) loginOverlay.SetActive(false);
        }

        public void OpenCrystalShop()
        {
            EnsureSafeAreaAndAuth();
            if (!uiBuilt) BuildUI();
            if (crystalShopOverlay != null)
            {
                crystalShopOverlay.SetActive(true);
                crystalShopOverlay.transform.SetAsLastSibling();
                if (purchaseResultPopup != null) purchaseResultPopup.SetActive(false);
                RefreshCrystalShopUI("Select a Crystal pack (Google Play IAP).");
            }
        }

        public void CloseCrystalShop()
        {
            if (crystalShopOverlay != null) crystalShopOverlay.SetActive(false);
        }

        public async void OnContinueAsGuestClicked()
        {
            lastAttemptWasGoogle = false;
            await ContinueAsGuestFlowAsync();
        }

        public async void OnSignInWithGoogleClicked()
        {
            lastAttemptWasGoogle = true;
            EnsureSafeAreaAndAuth();
            SetLoadingState(true, "Signing In...");

            GoogleLoginManager googleMgr = GoogleLoginManager.Instance ?? FindAnyObjectByType<GoogleLoginManager>();
            AuthOperationResult res = googleMgr != null
                ? await googleMgr.SignInWithGoogleAsync()
                : new AuthOperationResult { Success = false, ErrorCode = AuthErrorCode.AuthenticationFailed, Message = "GoogleLoginManager unavailable." };

            HandleAuthResult(res);
        }

        public void OnRetryClicked()
        {
            if (lastAttemptWasGoogle) OnSignInWithGoogleClicked();
            else OnContinueAsGuestClicked();
        }

        private async Task ContinueAsGuestFlowAsync()
        {
            EnsureSafeAreaAndAuth();
            SetLoadingState(true, "Connecting to Server...");
            await Task.Yield();
            SetLoadingState(true, "Loading Player Data...");

            GuestLoginManager guestMgr = GuestLoginManager.Instance ?? FindAnyObjectByType<GuestLoginManager>();
            AuthOperationResult res = guestMgr != null
                ? await guestMgr.ContinueAsGuestAsync()
                : await AuthenticationManager.Instance.SignInOrRestoreGuestAsync();

            if (res.Success)
            {
                SetLoadingState(true, "Synchronizing Cloud Save...");
            }

            HandleAuthResult(res);
        }

        private void HandleAuthResult(AuthOperationResult res)
        {
            SetLoadingState(false, res.Success ? "Welcome back, Dungeon Master!" : "Sign-in paused.");
            if (res.Success)
            {
                if (loginErrorText != null) loginErrorText.text = string.Empty;
                if (retryButton != null) retryButton.gameObject.SetActive(false);
                HideLoginOverlay();
                RefreshAccountBanner();
            }
            else
            {
                if (loginErrorText != null) loginErrorText.text = $"<color=#FF7777>⚠ {res.Message}</color>";
                if (retryButton != null) retryButton.gameObject.SetActive(true);
            }
        }

        private async void OnBuyCrystalPackClicked(string productId)
        {
            if (CrystalTransactionManager.Instance == null) return;
            RefreshCrystalShopUI($"Verifying Google Play purchase ({productId})...");

            string txId = $"GPA.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.{UnityEngine.Random.Range(1000, 9999)}";
            string receipt = $"{{\"Store\":\"GooglePlay\",\"TransactionID\":\"{txId}\",\"ProductId\":\"{productId}\"}}";

            PurchaseTransactionResult res = await CrystalTransactionManager.Instance.ProcessGooglePlayPurchaseAsync(productId, txId, receipt, "GooglePlay");
            RefreshCrystalShopUI(res.Success
                ? $"<color=#66F088>✓ {res.Message}</color>"
                : $"<color=#FF7777>⚠ {res.Message}</color>");
            ShowPurchasePopup(res.Success, res.Message);
            RefreshAccountBanner();
        }

        private void ShowPurchasePopup(bool success, string message)
        {
            if (purchaseResultPopup == null) return;
            var assets = UIThemeManager.Assets;
            if (purchasePopupGraphic != null && assets != null)
            {
                purchasePopupGraphic.sprite = success ? assets.popupPurchaseSuccess : assets.popupPurchaseFailed;
                purchasePopupGraphic.preserveAspect = true;
            }
            if (purchasePopupMessage != null)
            {
                purchasePopupMessage.text = message;
                purchasePopupMessage.color = success ? UIThemeManager.Theme.success : new Color(1f, 0.55f, 0.55f);
            }
            purchaseResultPopup.SetActive(true);
            purchaseResultPopup.transform.SetAsLastSibling();
        }

        private void HandleSignedIn(PlayerProfile profile)
        {
            HideLoginOverlay();
            RefreshAccountBanner();
        }

        private void HandleSignedOut()
        {
            RefreshAccountBanner();
            ShowLoginOverlay();
            SetLoadingState(false, "Signed out. Please choose a sign-in method.");
        }

        private void HandleProfileUpdated(PlayerProfile profile) => RefreshAccountBanner();

        private void HandleCrystalChanged(int balance)
        {
            RefreshAccountBanner();
            if (crystalShopBalanceText != null)
                crystalShopBalanceText.text = balance.ToString("N0");
        }

        private void RefreshAccountBanner()
        {
            if (accountHeaderBannerText == null) return;
            var profile = AuthenticationManager.Instance?.ActiveProfile;
            if (profile == null)
            {
                accountHeaderBannerText.text = "Not Signed In";
                return;
            }
            int crystals = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance : profile.crystalBalance;
            string shortId = UIStyleHelper.ShortenPlayerId(profile.playerId);
            string badge = profile.IsGoogleLinked ? "<color=#66F088>[GOOGLE]</color>" : "<color=#FFDC6E>[GUEST]</color>";
            accountHeaderBannerText.text = $"{badge} {profile.playerName}  •  ID: {shortId}  •  <color=#C47CFF>{crystals:N0} Crystals</color>";
        }

        private void RefreshCrystalShopUI(string statusMsg)
        {
            int crystals = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance : 0;
            if (crystalShopBalanceText != null) crystalShopBalanceText.text = crystals.ToString("N0");
            if (crystalShopStatusText != null) crystalShopStatusText.text = statusMsg;
        }

        private void SetLoadingState(bool loading, string statusMessage)
        {
            if (loginStatusText != null) loginStatusText.text = statusMessage;
            if (loadingIndicatorText != null)
            {
                loadingIndicatorText.gameObject.SetActive(loading);
                loadingIndicatorText.text = statusMessage;
            }
            if (guestLoginButton != null) guestLoginButton.interactable = !loading;
            if (googleLoginButton != null) googleLoginButton.interactable = !loading;
            if (retryButton != null) retryButton.interactable = !loading;
        }

        private void EnsureSafeAreaAndAuth()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
            if (AuthenticationManager.Instance == null)
            {
                new GameObject("AuthenticationManager").AddComponent<AuthenticationManager>();
            }
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null || uiBuilt) return;
            uiBuilt = true;

            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;

            // Persistent Account Badge at top of Main Menu
            GameObject bannerObj = new GameObject("AccountInfoBanner", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(safeAreaRoot, false);
            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.04f, 1f);
            bRt.anchorMax = new Vector2(0.96f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.anchoredPosition = new Vector2(0f, -14f);
            bRt.sizeDelta = new Vector2(0f, 52f);
            bannerObj.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.13f, 0.90f);
            accountHeaderBannerText = UIStyleHelper.CreateTmp(bannerObj.transform, "BannerText", "Connecting...", 18, theme.normalText);

            BuildLoginOverlay(theme, assets);
            BuildCrystalShopOverlay(theme, assets);
        }

        private void BuildLoginOverlay(DreamDungeonUITheme theme, DreamDungeonUIAssetDatabase assets)
        {
            GameObject overlay = new GameObject("LoginOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(safeAreaRoot, false);
            UIStyleHelper.Stretch(overlay, 0f, 0f, 0f, 0f);
            overlay.GetComponent<Image>().color = theme.mainBackground;
            loginOverlay = overlay;

            // Purple Banner Decorations
            if (assets != null && assets.decorBannerPurple != null)
            {
                AddDecor(overlay.transform, "LoginBannerLeft", assets.decorBannerPurple, new Vector2(0f, 0.78f), new Vector2(0.14f, 0.99f));
                AddDecor(overlay.transform, "LoginBannerRight", assets.decorBannerPurple, new Vector2(0.86f, 0.78f), new Vector2(1f, 0.99f));
            }

            // Gothic Frame Card
            GameObject card = new GameObject("LoginGothicCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(overlay.transform, false);
            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.06f, 0.08f);
            cRt.anchorMax = new Vector2(0.94f, 0.92f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(card.GetComponent<Image>());

            GameObject content = new GameObject("LoginContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(card.transform, false);
            UIStyleHelper.Stretch(content, 48f, 44f, 48f, 64f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Logo DREAM DUNGEON
            GameObject logoObj = new GameObject("LoginLogo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            logoObj.transform.SetParent(content.transform, false);
            logoObj.GetComponent<LayoutElement>().preferredHeight = 260f;
            Image logoImg = logoObj.GetComponent<Image>();
            if (assets != null && assets.logoDreamDungeon != null)
            {
                logoImg.sprite = assets.logoDreamDungeon;
                logoImg.preserveAspect = true;
                logoImg.color = Color.white;
            }
            else
            {
                logoImg.color = Color.clear;
                UIStyleHelper.CreateTmp(logoObj.transform, "LogoFallback", "DREAM DUNGEON", 52, theme.primaryText);
            }

            // Welcome Title
            GameObject welcomeObj = new GameObject("WelcomeTitle", typeof(RectTransform), typeof(LayoutElement));
            welcomeObj.transform.SetParent(content.transform, false);
            welcomeObj.GetComponent<LayoutElement>().preferredHeight = 46f;
            UIStyleHelper.CreateTmp(welcomeObj.transform, "Txt", "WELCOME, DUNGEON MASTER", 30, theme.primaryText);

            // Tagline / Status
            GameObject subObj = new GameObject("LoginStatus", typeof(RectTransform), typeof(LayoutElement));
            subObj.transform.SetParent(content.transform, false);
            subObj.GetComponent<LayoutElement>().preferredHeight = 52f;
            loginStatusText = UIStyleHelper.CreateTmp(subObj.transform, "Txt", "Build Your Lair. Defend Your Dreams.", 20, theme.normalText, TextAlignmentOptions.Center, FontStyles.Normal);

            // Loading Indicator
            GameObject loadObj = new GameObject("LoadingRow", typeof(RectTransform), typeof(LayoutElement));
            loadObj.transform.SetParent(content.transform, false);
            loadObj.GetComponent<LayoutElement>().preferredHeight = 38f;
            loadingIndicatorText = UIStyleHelper.CreateTmp(loadObj.transform, "Txt", "", 20, theme.premiumCurrency);
            loadingIndicatorText.gameObject.SetActive(false);

            // Buttons
            guestLoginButton = UIStyleHelper.CreateThemedButton(content.transform, "ContinueAsGuestButton", "CONTINUE AS GUEST",
                theme.primaryButton, 92f, OnContinueAsGuestClicked);

            googleLoginButton = UIStyleHelper.CreateThemedButton(content.transform, "SignInWithGoogleButton", "SIGN IN WITH GOOGLE",
                theme.secondaryButton, 92f, OnSignInWithGoogleClicked);

            // Error Text
            GameObject errObj = new GameObject("LoginError", typeof(RectTransform), typeof(LayoutElement));
            errObj.transform.SetParent(content.transform, false);
            errObj.GetComponent<LayoutElement>().preferredHeight = 76f;
            loginErrorText = UIStyleHelper.CreateTmp(errObj.transform, "Txt", "", 18, new Color(1f, 0.48f, 0.48f));
            loginErrorText.textWrappingMode = TextWrappingModes.Normal;

            // Retry Button
            retryButton = UIStyleHelper.CreateThemedButton(content.transform, "RetryLoginButton", "RETRY",
                theme.danger, 68f, OnRetryClicked);
            retryButton.gameObject.SetActive(false);

            // Spacer
            GameObject sp = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            sp.transform.SetParent(content.transform, false);
            sp.GetComponent<LayoutElement>().flexibleHeight = 1f;

            // Terms of Service
            GameObject termsObj = new GameObject("TermsText", typeof(RectTransform), typeof(LayoutElement));
            termsObj.transform.SetParent(content.transform, false);
            termsObj.GetComponent<LayoutElement>().preferredHeight = 46f;
            TextMeshProUGUI termsTmp = UIStyleHelper.CreateTmp(termsObj.transform, "Txt",
                "By continuing, you agree to our Terms of Service and Privacy Policy.",
                15, theme.mutedText, TextAlignmentOptions.Center, FontStyles.Normal);
            termsTmp.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildCrystalShopOverlay(DreamDungeonUITheme theme, DreamDungeonUIAssetDatabase assets)
        {
            GameObject shopOv = new GameObject("CrystalShopOverlay", typeof(RectTransform), typeof(Image));
            shopOv.transform.SetParent(safeAreaRoot, false);
            UIStyleHelper.Stretch(shopOv, 0f, 0f, 0f, 0f);
            shopOv.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.96f);
            crystalShopOverlay = shopOv;

            GameObject card = new GameObject("CrystalShopCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(shopOv.transform, false);
            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.04f, 0.06f);
            cRt.anchorMax = new Vector2(0.96f, 0.94f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(card.GetComponent<Image>());

            GameObject content = new GameObject("ShopContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(card.transform, false);
            UIStyleHelper.Stretch(content, 36f, 34f, 36f, 54f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header
            GameObject titleObj = new GameObject("ShopTitle", typeof(RectTransform), typeof(LayoutElement));
            titleObj.transform.SetParent(content.transform, false);
            titleObj.GetComponent<LayoutElement>().preferredHeight = 48f;
            UIStyleHelper.CreateTmp(titleObj.transform, "Txt", "CRYSTAL SHOP", 34, theme.primaryText);

            // Live Crystal Counter (real PNG)
            GameObject balRow = new GameObject("BalanceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            balRow.transform.SetParent(content.transform, false);
            balRow.GetComponent<LayoutElement>().preferredHeight = 72f;
            HorizontalLayoutGroup bhlg = balRow.GetComponent<HorizontalLayoutGroup>();
            bhlg.childAlignment = TextAnchor.MiddleCenter;
            bhlg.childControlWidth = false;
            bhlg.childControlHeight = false;
            crystalShopBalanceText = UIStyleHelper.CreateCurrencyCounter(balRow.transform, "ShopCrystalCounter", true, 260f, 70f);

            // Status
            GameObject stObj = new GameObject("ShopStatus", typeof(RectTransform), typeof(LayoutElement));
            stObj.transform.SetParent(content.transform, false);
            stObj.GetComponent<LayoutElement>().preferredHeight = 40f;
            crystalShopStatusText = UIStyleHelper.CreateTmp(stObj.transform, "Txt", "", 18, theme.normalText, TextAlignmentOptions.Center, FontStyles.Normal);

            // 5 Crystal Product Cards with Real Badges
            Sprite[] badges = assets != null
                ? new[] { assets.badgeNew, assets.badgeSale, assets.badgeHot, null, assets.badgeBestValue }
                : new Sprite[5];

            for (int i = 0; i < CrystalTransactionManager.Catalog.Length; i++)
            {
                var prod = CrystalTransactionManager.Catalog[i];
                string pid = prod.productId;
                Sprite badgeSprite = i < badges.Length ? badges[i] : null;
                CreateCrystalPackRow(content.transform, prod, badgeSprite, theme, assets, () => OnBuyCrystalPackClicked(pid));
            }

            // Back Button (real Button_Back.png)
            GameObject backRow = new GameObject("BackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            backRow.transform.SetParent(content.transform, false);
            backRow.GetComponent<LayoutElement>().preferredHeight = 86f;
            HorizontalLayoutGroup backHlg = backRow.GetComponent<HorizontalLayoutGroup>();
            backHlg.childAlignment = TextAnchor.MiddleCenter;
            backHlg.childControlWidth = false;
            backHlg.childControlHeight = false;

            Button backBtn = UIStyleHelper.CreateThemedButton(backRow.transform, "CloseCrystalShopButton", "BACK",
                theme.secondaryButton, 80f, CloseCrystalShop, assets != null ? assets.buttonBack : null);
            backBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 80f);

            // Purchase Result Popup (Popup_PurchaseSuccess / Popup_PurchaseFailed)
            BuildPurchasePopup(shopOv.transform, theme, assets);

            crystalShopOverlay.SetActive(false);
        }

        private void CreateCrystalPackRow(Transform parent, CrystalProductDefinition prod, Sprite badgeSprite, DreamDungeonUITheme theme, DreamDungeonUIAssetDatabase assets, Action onClick)
        {
            GameObject row = new GameObject($"Pack_{prod.productId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 104f;
            Image bg = row.GetComponent<Image>();
            bg.color = new Color(0.13f, 0.10f, 0.24f, 0.96f);
            Button btn = row.GetComponent<Button>();
            btn.targetGraphic = bg;
            UIStyleHelper.ApplyButtonStates(btn, new Color(0.18f, 0.13f, 0.34f, 1f));
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Gem Icon
            if (assets != null && assets.iconGemPurple != null)
            {
                GameObject gem = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
                gem.transform.SetParent(row.transform, false);
                RectTransform gRt = gem.GetComponent<RectTransform>();
                gRt.anchorMin = new Vector2(0f, 0.5f);
                gRt.anchorMax = new Vector2(0f, 0.5f);
                gRt.pivot = new Vector2(0f, 0.5f);
                gRt.anchoredPosition = new Vector2(18f, 0f);
                gRt.sizeDelta = new Vector2(58f, 68f);
                Image gImg = gem.GetComponent<Image>();
                gImg.sprite = assets.iconGemPurple;
                gImg.preserveAspect = true;
            }

            // Pack Name + Amount
            GameObject lblObj = new GameObject("PackLabel", typeof(RectTransform));
            lblObj.transform.SetParent(row.transform, false);
            RectTransform lRt = lblObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.72f, 1f);
            lRt.offsetMin = new Vector2(90f, 8f);
            lRt.offsetMax = new Vector2(-8f, -8f);
            UIStyleHelper.CreateTmp(lblObj.transform, "Txt",
                $"{prod.displayName}\n<color=#C47CFF>+{prod.crystalAmount:N0} Crystals</color>",
                21, theme.normalText, TextAlignmentOptions.Left);

            // Price Tag (Gold)
            GameObject priceObj = new GameObject("PriceTag", typeof(RectTransform));
            priceObj.transform.SetParent(row.transform, false);
            RectTransform pRt = priceObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.72f, 0f);
            pRt.anchorMax = new Vector2(0.97f, 1f);
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;
            UIStyleHelper.CreateTmp(priceObj.transform, "Txt", prod.displayPrice, 24, theme.primaryText, TextAlignmentOptions.Right);

            // Badge (BestValue / Hot / New / Sale)
            if (badgeSprite != null)
            {
                GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(row.transform, false);
                RectTransform bRt = badgeObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.55f, 0.70f);
                bRt.anchorMax = new Vector2(0.84f, 1.06f);
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                Image bImg = badgeObj.GetComponent<Image>();
                bImg.sprite = badgeSprite;
                bImg.preserveAspect = true;
                bImg.raycastTarget = false;
            }
        }

        private void BuildPurchasePopup(Transform parent, DreamDungeonUITheme theme, DreamDungeonUIAssetDatabase assets)
        {
            purchaseResultPopup = new GameObject("PurchaseResultPopup", typeof(RectTransform), typeof(Image));
            purchaseResultPopup.transform.SetParent(parent, false);
            UIStyleHelper.Stretch(purchaseResultPopup, 0f, 0f, 0f, 0f);
            purchaseResultPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            GameObject box = new GameObject("PopupBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(purchaseResultPopup.transform, false);
            RectTransform bRt = box.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.12f, 0.30f);
            bRt.anchorMax = new Vector2(0.88f, 0.70f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(box.GetComponent<Image>());

            GameObject content = new GameObject("PopupContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(box.transform, false);
            UIStyleHelper.Stretch(content, 32f, 28f, 32f, 44f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            GameObject imgObj = new GameObject("PopupGraphic", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            imgObj.transform.SetParent(content.transform, false);
            imgObj.GetComponent<LayoutElement>().preferredHeight = 165f;
            purchasePopupGraphic = imgObj.GetComponent<Image>();
            if (assets != null && assets.popupPurchaseSuccess != null)
            {
                purchasePopupGraphic.sprite = assets.popupPurchaseSuccess;
                purchasePopupGraphic.preserveAspect = true;
            }

            GameObject msgObj = new GameObject("PopupMsg", typeof(RectTransform), typeof(LayoutElement));
            msgObj.transform.SetParent(content.transform, false);
            msgObj.GetComponent<LayoutElement>().preferredHeight = 76f;
            purchasePopupMessage = UIStyleHelper.CreateTmp(msgObj.transform, "Txt", "", 20, theme.normalText);
            purchasePopupMessage.textWrappingMode = TextWrappingModes.Normal;

            UIStyleHelper.CreateThemedButton(content.transform, "ClosePopupBtn", "OK", theme.primaryButton, 68f,
                () => purchaseResultPopup.SetActive(false));

            purchaseResultPopup.SetActive(false);
        }

        private static void AddDecor(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }
}
