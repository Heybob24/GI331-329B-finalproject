using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class AccountSettingsUI : MonoBehaviour
    {
        public static AccountSettingsUI Instance { get; private set; }

        private const string PrefSoundEnabled = "DD_Setting_Sound";
        private const string PrefMusicEnabled = "DD_Setting_Music";

        private RectTransform safeAreaRoot;
        private GameObject settingsOverlay;
        private TextMeshProUGUI profileDetailsText;
        private TextMeshProUGUI statusFeedbackText;
        private Button linkGoogleButton;
        private Button syncNowButton;
        private Button logoutButton;

        // Audio & Graphics Settings
        private TextMeshProUGUI soundToggleLabel;
        private TextMeshProUGUI musicToggleLabel;
        private TextMeshProUGUI qualityLabel;

        // Account Conflict Dialog (Google already linked to another Player ID)
        private GameObject conflictDialog;
        private TextMeshProUGUI conflictMessageText;

        // Logout Confirmation Dialog
        private GameObject logoutConfirmDialog;

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
                settingsOverlay.transform.SetAsLastSibling();
                if (conflictDialog != null) conflictDialog.SetActive(false);
                if (logoutConfirmDialog != null) logoutConfirmDialog.SetActive(false);
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
                    string shortId = UIStyleHelper.ShortenPlayerId(profile.playerId);
                    string googleStatus = profile.IsGoogleLinked
                        ? "<color=#66F088>Linked to Google</color>"
                        : "<color=#FFDC6E>Guest Account (Not Linked)</color>";
                    string syncStr = PlayerDatabaseManager.Instance != null ? PlayerDatabaseManager.Instance.SyncState.ToString() : "Idle";
                    long rev = PlayerDatabaseManager.Instance?.CurrentData != null ? PlayerDatabaseManager.Instance.CurrentData.revision : 1;

                    profileDetailsText.text =
                        $"<b>Player ID:</b>  {shortId}\n" +
                        $"<b>Login Provider:</b>  {profile.GetLoginTypeDisplayName()}\n" +
                        $"<b>Account Status:</b>  {googleStatus}\n" +
                        $"<b>Crystal Balance:</b>  <color=#C47CFF>{crystals:N0}</color>\n" +
                        $"<b>Cloud Sync:</b>  {syncStr} (Rev {rev})";
                }
            }

            if (linkGoogleButton != null)
            {
                bool alreadyLinked = profile != null && profile.IsGoogleLinked;
                linkGoogleButton.interactable = profile != null && !alreadyLinked;
                TextMeshProUGUI lbl = linkGoogleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                {
                    lbl.text = alreadyLinked ? "✓ GOOGLE ACCOUNT LINKED" : "LINK GOOGLE ACCOUNT";
                }
            }

            RefreshAudioAndQualityLabels();
        }

        private void RefreshAudioAndQualityLabels()
        {
            bool soundOn = PlayerPrefs.GetInt(PrefSoundEnabled, 1) == 1;
            bool musicOn = PlayerPrefs.GetInt(PrefMusicEnabled, 1) == 1;
            AudioListener.volume = (soundOn || musicOn) ? 1f : 0f;

            if (soundToggleLabel != null) soundToggleLabel.text = soundOn ? "SOUND: ON" : "SOUND: OFF";
            if (musicToggleLabel != null) musicToggleLabel.text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
            if (qualityLabel != null)
            {
                string[] names = QualitySettings.names;
                int idx = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, names.Length - 1));
                string qName = names != null && names.Length > 0 ? names[idx] : "Default";
                qualityLabel.text = $"GRAPHICS: {qName.ToUpperInvariant()}";
            }
        }

        private void ToggleSound()
        {
            int next = PlayerPrefs.GetInt(PrefSoundEnabled, 1) == 1 ? 0 : 1;
            PlayerPrefs.SetInt(PrefSoundEnabled, next);
            PlayerPrefs.Save();
            RefreshAudioAndQualityLabels();
        }

        private void ToggleMusic()
        {
            int next = PlayerPrefs.GetInt(PrefMusicEnabled, 1) == 1 ? 0 : 1;
            PlayerPrefs.SetInt(PrefMusicEnabled, next);
            PlayerPrefs.Save();
            RefreshAudioAndQualityLabels();
        }

        private void CycleGraphicsQuality()
        {
            int count = QualitySettings.names != null ? QualitySettings.names.Length : 1;
            if (count > 1)
            {
                int next = (QualitySettings.GetQualityLevel() + 1) % count;
                QualitySettings.SetQualityLevel(next, true);
            }
            RefreshAudioAndQualityLabels();
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
                SetFeedback($"<color=#FFDC6E>⚠ {res.Message}</color>");
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
            SetFeedback("Synchronizing Cloud Save...");
            DatabaseOperationResult res = await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: true);
            SetFeedback(res.Success
                ? $"<color=#66F088>✓ Synced (Rev {res.Data?.revision})</color>"
                : $"<color=#FF7777>⚠ {res.Message}</color>");
            Refresh();
        }

        private void OnLogoutClicked()
        {
            if (logoutConfirmDialog != null)
            {
                logoutConfirmDialog.SetActive(true);
                logoutConfirmDialog.transform.SetAsLastSibling();
            }
        }

        private async void OnConfirmLogoutClicked()
        {
            if (logoutConfirmDialog != null) logoutConfirmDialog.SetActive(false);
            SetFeedback("Saving progress and signing out...");
            if (AuthenticationManager.Instance != null)
            {
                await AuthenticationManager.Instance.LogoutAsync(clearCachedSession: true);
            }
            Close();
        }

        private void SetFeedback(string msg)
        {
            if (statusFeedbackText != null) statusFeedbackText.text = msg;
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

            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;

            GameObject ov = new GameObject("AccountSettingsOverlay", typeof(RectTransform), typeof(Image));
            ov.transform.SetParent(safeAreaRoot, false);
            UIStyleHelper.Stretch(ov, 0f, 0f, 0f, 0f);
            ov.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.96f);
            settingsOverlay = ov;

            GameObject card = new GameObject("AccountSettingsCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(ov.transform, false);
            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.05f, 0.06f);
            cRt.anchorMax = new Vector2(0.95f, 0.94f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(card.GetComponent<Image>());

            GameObject content = new GameObject("SettingsContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(card.transform, false);
            UIStyleHelper.Stretch(content, 38f, 36f, 38f, 56f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("SettingsTitle", typeof(RectTransform), typeof(LayoutElement));
            titleObj.transform.SetParent(content.transform, false);
            titleObj.GetComponent<LayoutElement>().preferredHeight = 48f;
            UIStyleHelper.CreateTmp(titleObj.transform, "Txt", "SETTINGS & ACCOUNT", 32, theme.primaryText);

            // Profile Box
            GameObject profBox = new GameObject("ProfileBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            profBox.transform.SetParent(content.transform, false);
            profBox.GetComponent<LayoutElement>().preferredHeight = 190f;
            profBox.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.16f, 0.94f);
            profileDetailsText = UIStyleHelper.CreateTmp(profBox.transform, "Txt", "", 19, theme.normalText, TextAlignmentOptions.Left, FontStyles.Normal);

            // Feedback text
            GameObject fbObj = new GameObject("Feedback", typeof(RectTransform), typeof(LayoutElement));
            fbObj.transform.SetParent(content.transform, false);
            fbObj.GetComponent<LayoutElement>().preferredHeight = 46f;
            statusFeedbackText = UIStyleHelper.CreateTmp(fbObj.transform, "Txt", "", 17, theme.normalText);

            // Account Actions
            linkGoogleButton = UIStyleHelper.CreateThemedButton(content.transform, "LinkGoogleButton", "LINK GOOGLE ACCOUNT",
                theme.primaryButton, 74f, OnLinkGoogleClicked);

            syncNowButton = UIStyleHelper.CreateThemedButton(content.transform, "SyncCloudButton", "SYNC CLOUD SAVE NOW",
                theme.secondaryButton, 68f, OnSyncNowClicked);

            logoutButton = UIStyleHelper.CreateThemedButton(content.transform, "LogoutButton", "LOGOUT",
                theme.danger, 68f, OnLogoutClicked);

            // Audio & Graphics Section
            GameObject prefRow = new GameObject("AudioGraphicsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            prefRow.transform.SetParent(content.transform, false);
            prefRow.GetComponent<LayoutElement>().preferredHeight = 64f;
            HorizontalLayoutGroup pHlg = prefRow.GetComponent<HorizontalLayoutGroup>();
            pHlg.spacing = 8f;
            pHlg.childControlWidth = true;
            pHlg.childControlHeight = true;
            pHlg.childForceExpandWidth = true;

            Button sndBtn = UIStyleHelper.CreateThemedButton(prefRow.transform, "SoundBtn", "SOUND: ON", theme.secondaryButton, 62f, ToggleSound);
            soundToggleLabel = sndBtn.GetComponentInChildren<TextMeshProUGUI>();
            Button musBtn = UIStyleHelper.CreateThemedButton(prefRow.transform, "MusicBtn", "MUSIC: ON", theme.secondaryButton, 62f, ToggleMusic);
            musicToggleLabel = musBtn.GetComponentInChildren<TextMeshProUGUI>();

            Button gfxBtn = UIStyleHelper.CreateThemedButton(content.transform, "GraphicsBtn", "GRAPHICS: HIGH", theme.secondaryButton, 62f, CycleGraphicsQuality);
            qualityLabel = gfxBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Back Button
            GameObject backRow = new GameObject("BackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            backRow.transform.SetParent(content.transform, false);
            backRow.GetComponent<LayoutElement>().preferredHeight = 82f;
            HorizontalLayoutGroup bHlg = backRow.GetComponent<HorizontalLayoutGroup>();
            bHlg.childAlignment = TextAnchor.MiddleCenter;
            bHlg.childControlWidth = false;
            bHlg.childControlHeight = false;
            Button closeBtn = UIStyleHelper.CreateThemedButton(backRow.transform, "CloseSettingsBtn", "BACK",
                theme.secondaryButton, 76f, Close, assets != null ? assets.buttonBack : null);
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 76f);

            BuildConflictDialog(ov.transform, theme);
            BuildLogoutConfirmDialog(ov.transform, theme);

            settingsOverlay.SetActive(false);
        }

        private void BuildConflictDialog(Transform parent, DreamDungeonUITheme theme)
        {
            conflictDialog = new GameObject("AccountConflictDialog", typeof(RectTransform), typeof(Image));
            conflictDialog.transform.SetParent(parent, false);
            UIStyleHelper.Stretch(conflictDialog, 0f, 0f, 0f, 0f);
            conflictDialog.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

            GameObject box = new GameObject("ConflictBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(conflictDialog.transform, false);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.10f, 0.28f);
            rt.anchorMax = new Vector2(0.90f, 0.72f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(box.GetComponent<Image>());

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(box.transform, false);
            UIStyleHelper.Stretch(content, 32f, 28f, 32f, 44f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
            tObj.transform.SetParent(content.transform, false);
            tObj.GetComponent<LayoutElement>().preferredHeight = 40f;
            UIStyleHelper.CreateTmp(tObj.transform, "Txt", "GOOGLE ACCOUNT ALREADY LINKED", 24, theme.primaryText);

            GameObject mObj = new GameObject("Msg", typeof(RectTransform), typeof(LayoutElement));
            mObj.transform.SetParent(content.transform, false);
            mObj.GetComponent<LayoutElement>().preferredHeight = 110f;
            conflictMessageText = UIStyleHelper.CreateTmp(mObj.transform, "Txt", "", 18, theme.normalText);
            conflictMessageText.textWrappingMode = TextWrappingModes.Normal;

            UIStyleHelper.CreateThemedButton(content.transform, "KeepGuestBtn", "CANCEL (KEEP GUEST ACCOUNT)", theme.success, 68f, OnKeepGuestClicked);
            UIStyleHelper.CreateThemedButton(content.transform, "SwitchGoogleBtn", "SWITCH TO EXISTING GOOGLE ACCOUNT", theme.primaryButton, 68f, OnSwitchToGoogleAccountClicked);

            conflictDialog.SetActive(false);
        }

        private void BuildLogoutConfirmDialog(Transform parent, DreamDungeonUITheme theme)
        {
            logoutConfirmDialog = new GameObject("LogoutConfirmDialog", typeof(RectTransform), typeof(Image));
            logoutConfirmDialog.transform.SetParent(parent, false);
            UIStyleHelper.Stretch(logoutConfirmDialog, 0f, 0f, 0f, 0f);
            logoutConfirmDialog.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

            GameObject box = new GameObject("LogoutBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(logoutConfirmDialog.transform, false);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.30f);
            rt.anchorMax = new Vector2(0.88f, 0.70f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(box.GetComponent<Image>());

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(box.transform, false);
            UIStyleHelper.Stretch(content, 32f, 28f, 32f, 44f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
            tObj.transform.SetParent(content.transform, false);
            tObj.GetComponent<LayoutElement>().preferredHeight = 40f;
            UIStyleHelper.CreateTmp(tObj.transform, "Txt", "CONFIRM LOGOUT", 26, theme.primaryText);

            GameObject mObj = new GameObject("Msg", typeof(RectTransform), typeof(LayoutElement));
            mObj.transform.SetParent(content.transform, false);
            mObj.GetComponent<LayoutElement>().preferredHeight = 96f;
            TextMeshProUGUI msg = UIStyleHelper.CreateTmp(mObj.transform, "Txt",
                "Sign out of this account?\nYour Cloud Save, Crystals, and Dungeon Data remain safely stored on the server.",
                18, theme.normalText);
            msg.textWrappingMode = TextWrappingModes.Normal;

            UIStyleHelper.CreateThemedButton(content.transform, "ConfirmLogoutBtn", "YES, LOGOUT", theme.danger, 68f, OnConfirmLogoutClicked);
            UIStyleHelper.CreateThemedButton(content.transform, "CancelLogoutBtn", "CANCEL", theme.secondaryButton, 68f,
                () => logoutConfirmDialog.SetActive(false));

            logoutConfirmDialog.SetActive(false);
        }
    }
}
