using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class AccountUIManager : MonoBehaviour
    {
        public static AccountUIManager Instance { get; private set; }

        private RectTransform safeAreaRoot;
        private GameObject loginOverlay;
        private Text loginStatusText;
        private Text loginErrorText;
        private Text loadingIndicatorText;
        private Text accountHeaderBannerText;
        private Button guestLoginButton;
        private Button googleLoginButton;
        private Button retryButton;

        // Crystal IAP Shop Overlay (5 Google Play SKUs)
        private GameObject crystalShopOverlay;
        private Text crystalShopBalanceText;
        private Text crystalShopStatusText;

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
                SetLoadingState(true, "Restoring your DREAM DUNGEON session...");
                _ = ContinueAsGuestFlowAsync();
            }
            else
            {
                ShowLoginOverlay();
                SetLoadingState(false, "Choose a sign-in method to enter DREAM DUNGEON.");
            }
        }

        public void ShowLoginOverlay()
        {
            EnsureSafeAreaAndAuth();
            if (!uiBuilt) BuildUI();
            if (loginOverlay != null) loginOverlay.SetActive(true);
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
            SetLoadingState(true, "Signing in with Google...");

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

        private async System.Threading.Tasks.Task ContinueAsGuestFlowAsync()
        {
            EnsureSafeAreaAndAuth();
            SetLoadingState(true, "Connecting as Guest (UGS Anonymous)...");

            GuestLoginManager guestMgr = GuestLoginManager.Instance ?? FindAnyObjectByType<GuestLoginManager>();
            AuthOperationResult res = guestMgr != null
                ? await guestMgr.ContinueAsGuestAsync()
                : await AuthenticationManager.Instance.SignInOrRestoreGuestAsync();

            HandleAuthResult(res);
        }

        private void HandleAuthResult(AuthOperationResult res)
        {
            SetLoadingState(false, res.Success ? res.Message : "Sign-in paused.");
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
            RefreshAccountBanner();
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
            SetLoadingState(false, "Signed out. Please sign in to continue.");
        }

        private void HandleProfileUpdated(PlayerProfile profile)
        {
            RefreshAccountBanner();
        }

        private void HandleCrystalChanged(int balance)
        {
            RefreshAccountBanner();
            if (crystalShopBalanceText != null)
            {
                crystalShopBalanceText.text = $"CRYSTAL BALANCE:  <color=#70E8FF>{balance:N0}</color>";
            }
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
            string syncStr = PlayerDatabaseManager.Instance != null ? PlayerDatabaseManager.Instance.SyncState.ToString() : "Synced";
            accountHeaderBannerText.text =
                $"{profile.playerName}  •  {profile.GetLoginTypeDisplayName()}  •  ID: {profile.playerId}\n" +
                $"Crystals: <color=#70E8FF>{crystals:N0}</color>   •   Cloud Status: {syncStr}";
        }

        private void RefreshCrystalShopUI(string statusMsg)
        {
            int crystals = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance : 0;
            if (crystalShopBalanceText != null)
                crystalShopBalanceText.text = $"CRYSTAL BALANCE:  <color=#70E8FF>{crystals:N0}</color>";
            if (crystalShopStatusText != null)
                crystalShopStatusText.text = statusMsg;
        }

        private void SetLoadingState(bool loading, string statusMessage)
        {
            if (loginStatusText != null) loginStatusText.text = statusMessage;
            if (loadingIndicatorText != null)
            {
                loadingIndicatorText.gameObject.SetActive(loading);
                loadingIndicatorText.text = "⟳ Connecting to Unity Gaming Services...";
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
                GameObject authGo = new GameObject("AuthenticationManager");
                authGo.AddComponent<AuthenticationManager>();
            }
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null || uiBuilt) return;
            uiBuilt = true;

            // Persistent subtle Account Info banner at top of Main Menu
            accountHeaderBannerText = RoomUIFactory.CreateText(safeAreaRoot, "AccountInfoBanner",
                "Connecting...", 18, new Vector2(0f, 845f), new Vector2(960f, 64f), new Color(0.84f, 0.86f, 0.94f));

            // 1. Dark Fantasy Login Overlay
            RectTransform overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "LoginOverlay");
            overlay.GetComponent<Image>().color = new Color(0.045f, 0.03f, 0.08f, 0.98f);
            loginOverlay = overlay.gameObject;

            RectTransform card = RoomUIFactory.CreatePanel(overlay, "LoginCard",
                new Color(0.10f, 0.08f, 0.16f, 0.98f), Vector2.zero, new Vector2(920f, 1180f));

            RoomUIFactory.CreateText(card, "LoginLogo", "DREAM\nDUNGEON", 72,
                new Vector2(0f, 400f), new Vector2(840f, 220f), new Color(0.96f, 0.78f, 0.43f));
            RoomUIFactory.CreateText(card, "LoginSubtitle", "PLAYER ACCOUNT & CLOUD SYNC", 22,
                new Vector2(0f, 260f), new Vector2(840f, 48f), new Color(0.75f, 0.72f, 0.88f));

            loginStatusText = RoomUIFactory.CreateText(card, "LoginStatusText", "Ready to sign in.", 22,
                new Vector2(0f, 165f), new Vector2(820f, 60f), Color.white);

            loadingIndicatorText = RoomUIFactory.CreateText(card, "LoadingIndicatorText", "", 22,
                new Vector2(0f, 100f), new Vector2(820f, 46f), new Color(0.60f, 0.88f, 1.0f));
            loadingIndicatorText.gameObject.SetActive(false);

            guestLoginButton = RoomUIFactory.CreateButton(card, "ContinueAsGuestButton", "CONTINUE AS GUEST",
                new Vector2(0f, -20f), new Vector2(660f, 104f), new Color(0.46f, 0.24f, 0.14f, 1f), 28);
            guestLoginButton.onClick.AddListener(OnContinueAsGuestClicked);

            googleLoginButton = RoomUIFactory.CreateButton(card, "SignInWithGoogleButton", "SIGN IN WITH GOOGLE",
                new Vector2(0f, -155f), new Vector2(660f, 104f), new Color(0.16f, 0.36f, 0.56f, 1f), 28);
            googleLoginButton.onClick.AddListener(OnSignInWithGoogleClicked);

            loginErrorText = RoomUIFactory.CreateText(card, "LoginErrorText", "", 20,
                new Vector2(0f, -295f), new Vector2(820f, 95f), new Color(1f, 0.5f, 0.5f));

            retryButton = RoomUIFactory.CreateButton(card, "RetryLoginButton", "RETRY",
                new Vector2(0f, -420f), new Vector2(360f, 76f), new Color(0.32f, 0.22f, 0.38f, 1f), 24);
            retryButton.onClick.AddListener(OnRetryClicked);
            retryButton.gameObject.SetActive(false);

            // 2. Crystal IAP Shop Overlay (5 Google Play Products)
            RectTransform shopOv = RoomUIFactory.CreateOverlay(safeAreaRoot, "CrystalShopOverlay");
            crystalShopOverlay = shopOv.gameObject;

            RectTransform shopCard = RoomUIFactory.CreatePanel(shopOv, "CrystalShopCard",
                new Color(0.09f, 0.08f, 0.15f, 0.98f), Vector2.zero, new Vector2(940f, 1340f));

            RoomUIFactory.CreateText(shopCard, "ShopTitle", "CRYSTAL SHOP (GOOGLE PLAY IAP)", 34,
                new Vector2(0f, 590f), new Vector2(860f, 64f), new Color(0.70f, 0.92f, 1.0f));

            crystalShopBalanceText = RoomUIFactory.CreateText(shopCard, "ShopBalance", "CRYSTAL BALANCE: 0", 26,
                new Vector2(0f, 515f), new Vector2(840f, 52f), Color.white);

            crystalShopStatusText = RoomUIFactory.CreateText(shopCard, "ShopStatus", "", 20,
                new Vector2(0f, 445f), new Vector2(840f, 64f), new Color(0.85f, 0.88f, 0.94f));

            float startY = 310f;
            for (int i = 0; i < CrystalTransactionManager.Catalog.Length; i++)
            {
                var prod = CrystalTransactionManager.Catalog[i];
                string pid = prod.productId;
                Button buyBtn = RoomUIFactory.CreateButton(shopCard, $"Buy_{pid}",
                    $"{prod.displayName}   •   +{prod.crystalAmount:N0} Crystals   ({prod.displayPrice})",
                    new Vector2(0f, startY - i * 130f), new Vector2(820f, 104f), new Color(0.18f, 0.24f, 0.38f, 1f), 23);
                buyBtn.onClick.AddListener(() => OnBuyCrystalPackClicked(pid));
            }

            Button closeShopBtn = RoomUIFactory.CreateButton(shopCard, "CloseCrystalShopButton", "BACK",
                new Vector2(0f, -540f), new Vector2(360f, 84f), new Color(0.24f, 0.22f, 0.32f, 1f), 24);
            closeShopBtn.onClick.AddListener(CloseCrystalShop);

            crystalShopOverlay.SetActive(false);
        }
    }
}
