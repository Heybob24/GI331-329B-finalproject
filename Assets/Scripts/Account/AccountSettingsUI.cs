using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class AccountSettingsUI : MonoBehaviour
    {
        public static AccountSettingsUI Instance { get; private set; }

        private RectTransform safeAreaRoot;
        private GameObject settingsOverlay;
        private Text profileDetailsText;
        private Text statusFeedbackText;
        private Button linkGoogleButton;
        private Button syncNowButton;
        private Button logoutButton;
        private Button deleteAccountButton;

        // Account Conflict Dialog (Google already linked to another Player ID)
        private GameObject conflictDialog;
        private Text conflictMessageText;

        // Delete Confirmation Dialog
        private GameObject deleteConfirmDialog;

        private bool uiBuilt;

        public bool IsOpen => settingsOverlay != null && settingsOverlay.activeSelf;

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
            if (Instance == this) Instance = null;
        }

        public void Configure(RectTransform safeRoot)
        {
            safeAreaRoot = safeRoot;
        }

        public void Open(RectTransform safeRoot = null)
        {
            if (safeRoot != null) safeAreaRoot = safeRoot;
            EnsureSafeArea();
            if (!uiBuilt) BuildUI();

            if (settingsOverlay != null)
            {
                settingsOverlay.SetActive(true);
                if (conflictDialog != null) conflictDialog.SetActive(false);
                if (deleteConfirmDialog != null) deleteConfirmDialog.SetActive(false);
                Refresh();
            }
        }

        public void Close()
        {
            if (settingsOverlay != null) settingsOverlay.SetActive(false);
        }

        public void Refresh()
        {
            PlayerProfile profile = AuthenticationManager.Instance?.ActiveProfile;
            if (profileDetailsText != null)
            {
                if (profile == null)
                {
                    profileDetailsText.text = "No active player session.";
                }
                else
                {
                    int crystals = CrystalManager.Instance != null ? CrystalManager.Instance.CrystalBalance : profile.crystalBalance;
                    string googleStatus = profile.IsGoogleLinked
                        ? "<color=#66F088>Google Linked</color>"
                        : "<color=#FFD966>Not Linked (Guest Only)</color>";
                    string syncStr = PlayerDatabaseManager.Instance != null ? PlayerDatabaseManager.Instance.SyncState.ToString() : "Idle";
                    long rev = PlayerDatabaseManager.Instance?.CurrentData != null ? PlayerDatabaseManager.Instance.CurrentData.revision : 1;

                    profileDetailsText.text =
                        $"<b>Player Name:</b>  {profile.playerName}\n" +
                        $"<b>Player ID (UGS):</b>  {profile.playerId}\n" +
                        $"<b>Login Method:</b>  {profile.GetLoginTypeDisplayName()}\n" +
                        $"<b>Google Status:</b>  {googleStatus}\n" +
                        $"<b>Crystal Balance:</b>  <color=#70E8FF>{crystals:N0}</color>\n" +
                        $"<b>Last Login (UTC):</b>  {profile.lastLoginUtc}\n" +
                        $"<b>Cloud Sync:</b>  {syncStr} (Rev {rev})";
                }
            }

            if (linkGoogleButton != null)
            {
                bool alreadyLinked = profile != null && profile.IsGoogleLinked;
                linkGoogleButton.interactable = profile != null && !alreadyLinked;
                Text lbl = linkGoogleButton.GetComponentInChildren<Text>();
                if (lbl != null)
                {
                    lbl.text = alreadyLinked ? "✓ GOOGLE LINKED" : "LINK GOOGLE ACCOUNT";
                }
            }
        }

        private async void OnLinkGoogleClicked()
        {
            SetFeedback("Linking Google Account to your Guest profile...");
            AccountLinkManager linkMgr = AccountLinkManager.Instance ?? FindAnyObjectByType<AccountLinkManager>();
            if (linkMgr == null)
            {
                SetFeedback("<color=#FF7777>AccountLinkManager unavailable.</color>");
                return;
            }

            AuthOperationResult res = await linkMgr.LinkGuestWithGoogleAsync();
            if (res.Success)
            {
                SetFeedback($"<color=#66F088>✓ {res.Message}</color>");
                Refresh();
            }
            else if (res.ErrorCode == AuthErrorCode.AccountAlreadyLinked)
            {
                if (conflictDialog != null)
                {
                    if (conflictMessageText != null) conflictMessageText.text = res.Message;
                    conflictDialog.SetActive(true);
                }
                SetFeedback($"<color=#FFD966>⚠ {res.Message}</color>");
            }
            else
            {
                SetFeedback($"<color=#FF7777>⚠ {res.Message}</color>");
            }
        }

        private void OnKeepGuestClicked()
        {
            AccountLinkManager.Instance?.CancelConflictKeepGuest();
            if (conflictDialog != null) conflictDialog.SetActive(false);
            SetFeedback("Cancelled linking. Your Guest account and data remain untouched.");
            Refresh();
        }

        private async void OnSwitchToGoogleAccountClicked()
        {
            if (conflictDialog != null) conflictDialog.SetActive(false);
            SetFeedback("Switching to existing Google Account...");
            if (AccountLinkManager.Instance != null)
            {
                AuthOperationResult res = await AccountLinkManager.Instance.SwitchToExistingGoogleAccountFromConflictAsync();
                SetFeedback(res.Success
                    ? $"<color=#66F088>✓ {res.Message}</color>"
                    : $"<color=#FF7777>⚠ {res.Message}</color>");
                Refresh();
            }
        }

        private async void OnSyncNowClicked()
        {
            if (PlayerDatabaseManager.Instance == null) return;
            SetFeedback("Syncing player data with Cloud Save...");
            var res = await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: true);
            SetFeedback(res.Success
                ? $"<color=#66F088>✓ Cloud Sync Complete (Rev {res.Data?.revision})</color>"
                : $"<color=#FF7777>⚠ {res.Message}</color>");
            Refresh();
        }

        private async void OnLogoutClicked()
        {
            if (AuthenticationManager.Instance == null) return;
            SetFeedback("Saving & signing out...");
            await AuthenticationManager.Instance.LogoutAsync(clearCachedSession: true);
            Close();
        }

        private void OnDeleteAccountRequested()
        {
            if (deleteConfirmDialog != null) deleteConfirmDialog.SetActive(true);
        }

        private async void OnConfirmDeleteAccountClicked()
        {
            if (deleteConfirmDialog != null) deleteConfirmDialog.SetActive(false);
            if (AuthenticationManager.Instance == null) return;
            SetFeedback("Deleting account and cloud data...");
            AuthOperationResult res = await AuthenticationManager.Instance.DeleteCurrentAccountAsync();
            if (res.Success)
            {
                Close();
            }
            else
            {
                SetFeedback($"<color=#FF7777>⚠ {res.Message}</color>");
            }
        }

        private void SetFeedback(string message)
        {
            if (statusFeedbackText != null) statusFeedbackText.text = message;
        }

        private void EnsureSafeArea()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null || uiBuilt) return;
            uiBuilt = true;

            RectTransform overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "AccountSettingsOverlay");
            settingsOverlay = overlay.gameObject;

            RectTransform card = RoomUIFactory.CreatePanel(overlay, "AccountSettingsCard",
                new Color(0.09f, 0.09f, 0.15f, 0.98f), Vector2.zero, new Vector2(940f, 1340f));

            RoomUIFactory.CreateText(card, "SettingsTitle", "ACCOUNT SETTINGS", 40,
                new Vector2(0f, 595f), new Vector2(860f, 64f), new Color(0.96f, 0.78f, 0.43f));

            RectTransform infoPanel = RoomUIFactory.CreatePanel(card, "AccountDetailsPanel",
                new Color(0.13f, 0.14f, 0.21f, 1f), new Vector2(0f, 330f), new Vector2(860f, 380f));
            profileDetailsText = RoomUIFactory.CreateText(infoPanel, "ProfileDetailsText", "", 22,
                Vector2.zero, new Vector2(810f, 340f), Color.white, TextAnchor.MiddleLeft);

            statusFeedbackText = RoomUIFactory.CreateText(card, "SettingsStatusFeedback", "", 20,
                new Vector2(0f, 80f), new Vector2(860f, 76f), new Color(0.85f, 0.90f, 0.96f));

            linkGoogleButton = RoomUIFactory.CreateButton(card, "LinkGoogleAccountButton", "LINK GOOGLE ACCOUNT",
                new Vector2(0f, -40f), new Vector2(680f, 92f), new Color(0.16f, 0.38f, 0.56f, 1f), 26);
            linkGoogleButton.onClick.AddListener(OnLinkGoogleClicked);

            syncNowButton = RoomUIFactory.CreateButton(card, "SyncNowButton", "SYNC CLOUD DATA NOW",
                new Vector2(0f, -160f), new Vector2(680f, 84f), new Color(0.20f, 0.38f, 0.28f, 1f), 24);
            syncNowButton.onClick.AddListener(OnSyncNowClicked);

            logoutButton = RoomUIFactory.CreateButton(card, "LogoutButton", "LOGOUT",
                new Vector2(-175f, -285f), new Vector2(325f, 84f), new Color(0.38f, 0.24f, 0.16f, 1f), 24);
            logoutButton.onClick.AddListener(OnLogoutClicked);

            deleteAccountButton = RoomUIFactory.CreateButton(card, "DeleteAccountButton", "DELETE ACCOUNT",
                new Vector2(175f, -285f), new Vector2(325f, 84f), new Color(0.48f, 0.14f, 0.16f, 1f), 22);
            deleteAccountButton.onClick.AddListener(OnDeleteAccountRequested);

            Button closeBtn = RoomUIFactory.CreateButton(card, "CloseAccountSettingsButton", "BACK",
                new Vector2(0f, -525f), new Vector2(360f, 84f), new Color(0.22f, 0.24f, 0.32f, 1f), 24);
            closeBtn.onClick.AddListener(Close);

            // Conflict Dialog
            RectTransform conflictOv = RoomUIFactory.CreateOverlay(card, "AccountConflictDialog");
            conflictDialog = conflictOv.gameObject;
            RectTransform conflictCard = RoomUIFactory.CreatePanel(conflictOv, "ConflictCard",
                new Color(0.14f, 0.10f, 0.18f, 1f), Vector2.zero, new Vector2(820f, 560f));
            RoomUIFactory.CreateText(conflictCard, "ConflictTitle", "GOOGLE ACCOUNT ALREADY LINKED", 28,
                new Vector2(0f, 195f), new Vector2(760f, 56f), new Color(1.0f, 0.78f, 0.35f));
            conflictMessageText = RoomUIFactory.CreateText(conflictCard, "ConflictMsg", "", 21,
                new Vector2(0f, 50f), new Vector2(740f, 180f), Color.white);

            Button keepGuestBtn = RoomUIFactory.CreateButton(conflictCard, "KeepGuestButton", "CANCEL (KEEP GUEST)",
                new Vector2(0f, -105f), new Vector2(620f, 80f), new Color(0.24f, 0.32f, 0.42f, 1f), 22);
            keepGuestBtn.onClick.AddListener(OnKeepGuestClicked);

            Button switchAccBtn = RoomUIFactory.CreateButton(conflictCard, "SwitchGoogleAccountButton", "SWITCH TO EXISTING GOOGLE ACCOUNT",
                new Vector2(0f, -205f), new Vector2(620f, 80f), new Color(0.48f, 0.24f, 0.16f, 1f), 21);
            switchAccBtn.onClick.AddListener(OnSwitchToGoogleAccountClicked);
            conflictDialog.SetActive(false);

            // Delete Confirmation Dialog
            RectTransform delOv = RoomUIFactory.CreateOverlay(card, "DeleteAccountConfirmDialog");
            deleteConfirmDialog = delOv.gameObject;
            RectTransform delCard = RoomUIFactory.CreatePanel(delOv, "DeleteCard",
                new Color(0.18f, 0.08f, 0.10f, 1f), Vector2.zero, new Vector2(800f, 480f));
            RoomUIFactory.CreateText(delCard, "DelTitle", "CONFIRM ACCOUNT DELETION", 28,
                new Vector2(0f, 160f), new Vector2(740f, 56f), new Color(1f, 0.45f, 0.45f));
            RoomUIFactory.CreateText(delCard, "DelMsg",
                "This will permanently delete your account, Dungeon Data, Progression, and Crystal records from the server. This cannot be undone.",
                21, new Vector2(0f, 35f), new Vector2(720f, 150f), Color.white);

            Button confirmDelBtn = RoomUIFactory.CreateButton(delCard, "ConfirmDeleteBtn", "PERMANENTLY DELETE",
                new Vector2(-170f, -135f), new Vector2(320f, 80f), new Color(0.62f, 0.14f, 0.16f, 1f), 21);
            confirmDelBtn.onClick.AddListener(OnConfirmDeleteAccountClicked);

            Button cancelDelBtn = RoomUIFactory.CreateButton(delCard, "CancelDeleteBtn", "CANCEL",
                new Vector2(170f, -135f), new Vector2(320f, 80f), new Color(0.24f, 0.26f, 0.34f, 1f), 22);
            cancelDelBtn.onClick.AddListener(() => deleteConfirmDialog.SetActive(false));
            deleteConfirmDialog.SetActive(false);

            settingsOverlay.SetActive(false);
        }
    }
}
