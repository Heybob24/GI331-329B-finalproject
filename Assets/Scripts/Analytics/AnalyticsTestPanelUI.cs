using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    /// <summary>
    /// Standalone Analytics Test Panel UI for Unity Editor and Development Builds ONLY.
    /// Never included/displayed in Release Builds and never sends events automatically on startup.
    /// </summary>
    public sealed class AnalyticsTestPanelUI : MonoBehaviour
    {
        public static AnalyticsTestPanelUI Instance { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameObject panelRoot;
        private Button toggleButton;
        private TextMeshProUGUI toggleButtonLabel;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI logText;
        private Button consentToggleButton;
        private TextMeshProUGUI consentToggleLabel;
        private bool isPanelVisible;
        private bool uiBuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapInDevBuild()
        {
            if (Instance != null) return;
            var go = new GameObject("AnalyticsTestPanelUI");
            go.AddComponent<AnalyticsTestPanelUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AnalyticsManager.EnsureInstance();
            BuildUI();
            SubscribeEvents();
            RefreshStatus();
            RefreshLog();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void SubscribeEvents()
        {
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.OnAnalyticsStatusChanged -= HandleStatusChanged;
                AnalyticsManager.Instance.OnAnalyticsStatusChanged += HandleStatusChanged;
                AnalyticsManager.Instance.OnEventLogged -= HandleEventLogged;
                AnalyticsManager.Instance.OnEventLogged += HandleEventLogged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.OnAnalyticsStatusChanged -= HandleStatusChanged;
                AnalyticsManager.Instance.OnEventLogged -= HandleEventLogged;
            }
        }

        private void HandleStatusChanged(AnalyticsInitStatus _)
        {
            RefreshStatus();
        }

        private void HandleEventLogged(AnalyticsEventLogEntry _)
        {
            RefreshStatus();
            RefreshLog();
        }

        public void TogglePanel()
        {
            isPanelVisible = !isPanelVisible;
            if (panelRoot != null)
            {
                panelRoot.SetActive(isPanelVisible);
            }
            if (toggleButtonLabel != null)
            {
                toggleButtonLabel.text = isPanelVisible ? "Hide Analytics Test Panel" : "Show Analytics Test Panel";
            }
            if (isPanelVisible)
            {
                RefreshStatus();
                RefreshLog();
            }
        }

        private void BuildUI()
        {
            if (uiBuilt) return;
            uiBuilt = true;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // 1. Floating Toggle Button ("Show Analytics Test Panel")
            GameObject toggleGo = CreateButton(
                transform,
                "ShowAnalyticsTestPanelButton",
                "Show Analytics Test Panel",
                new Color(0.14f, 0.24f, 0.38f, 0.94f),
                TogglePanel,
                out toggleButton,
                out toggleButtonLabel,
                fontSize: 22f);

            RectTransform toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0.02f, 0.015f);
            toggleRt.anchorMax = new Vector2(0.36f, 0.055f);
            toggleRt.offsetMin = Vector2.zero;
            toggleRt.offsetMax = Vector2.zero;

            // 2. Main Test Panel Container (Hidden by default)
            panelRoot = new GameObject("AnalyticsTestPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(transform, false);
            RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.05f, 0.07f);
            panelRt.anchorMax = new Vector2(0.95f, 0.92f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            Image panelBg = panelRoot.GetComponent<Image>();
            panelBg.color = new Color(0.07f, 0.09f, 0.14f, 0.96f);

            VerticalLayoutGroup vlg = panelRoot.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header Row
            GameObject headerRow = CreateRow(panelRoot.transform, "HeaderRow", 62f);
            TextMeshProUGUI title = CreateLabel(headerRow.transform, "Title", "UNITY ANALYTICS TEST PANEL (DEV ONLY)", 28f, FontStyles.Bold, new Color(0.95f, 0.84f, 0.48f, 1f));
            AddFlexibleLayout(title.gameObject, 1f);
            GameObject closeBtnGo = CreateButton(headerRow.transform, "CloseButton", "X", new Color(0.55f, 0.18f, 0.20f, 1f), TogglePanel, out _, out _, 24f);
            AddFixedWidth(closeBtnGo, 70f, 56f);

            // Status Box
            GameObject statusBox = new GameObject("StatusBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            statusBox.transform.SetParent(panelRoot.transform, false);
            statusBox.GetComponent<Image>().color = new Color(0.11f, 0.14f, 0.20f, 1f);
            statusBox.GetComponent<LayoutElement>().preferredHeight = 148f;
            statusText = CreateLabel(statusBox.transform, "StatusText", "Checking Analytics status...", 20f, FontStyles.Normal, Color.white);
            StretchWithPadding(statusText.rectTransform, 14f, 10f);
            statusText.alignment = TextAlignmentOptions.TopLeft;

            // Control Row (Re-Init / Consent / Flush)
            GameObject ctrlRow = CreateRow(panelRoot.transform, "ControlRow", 60f);
            GameObject initBtn = CreateButton(ctrlRow.transform, "InitButton", "Initialize", new Color(0.16f, 0.42f, 0.30f, 1f), OnClickReinitialize, out _, out _, 20f);
            AddFlexibleLayout(initBtn, 1f);
            GameObject consentBtn = CreateButton(ctrlRow.transform, "ConsentButton", "Consent: ON", new Color(0.24f, 0.34f, 0.48f, 1f), OnClickToggleConsent, out consentToggleButton, out consentToggleLabel, 20f);
            AddFlexibleLayout(consentBtn, 1f);
            GameObject flushBtn = CreateButton(ctrlRow.transform, "FlushButton", "Flush Queue", new Color(0.42f, 0.30f, 0.14f, 1f), OnClickFlush, out _, out _, 20f);
            AddFlexibleLayout(flushBtn, 1f);

            // Section Title: 5 Custom Events
            TextMeshProUGUI sectionLabel = CreateLabel(panelRoot.transform, "EventsHeader", "CUSTOM EVENTS (Calls Real Unity Analytics SDK)", 22f, FontStyles.Bold, new Color(0.72f, 0.86f, 1f, 1f));
            AddFixedHeight(sectionLabel.gameObject, 34f);

            // 5 Required Test Event Buttons
            AddTestEventButton(panelRoot.transform, "BtnTestDungeonCreated",
                "1. Test Dungeon Created  (lvl=3, rooms=5)",
                new Color(0.18f, 0.32f, 0.52f, 1f), OnClickTestDungeonCreated);

            AddTestEventButton(panelRoot.transform, "BtnTestRaidStarted",
                "2. Test Raid Started  (lvl=3, monsters=8, traps=4)",
                new Color(0.26f, 0.28f, 0.54f, 1f), OnClickTestRaidStarted);

            AddTestEventButton(panelRoot.transform, "BtnTestRaidCompleted",
                "3. Test Raid Completed  (wave=5, score=1250, dur=93.5)",
                new Color(0.16f, 0.44f, 0.28f, 1f), OnClickTestRaidCompleted);

            AddTestEventButton(panelRoot.transform, "BtnTestRaidFailed",
                "4. Test Raid Failed  (wave=3, core_hp=0, dur=65.2)",
                new Color(0.52f, 0.20f, 0.22f, 1f), OnClickTestRaidFailed);

            AddTestEventButton(panelRoot.transform, "BtnTestCrystalSpent",
                "5. Test Crystal Spent  (amt=100, item=monster_room, bal=500)",
                new Color(0.46f, 0.22f, 0.48f, 1f), OnClickTestCrystalSpent);

            // Log Header
            TextMeshProUGUI logHeader = CreateLabel(panelRoot.transform, "LogHeader", "RECORDED EVENTS LOG (Verify Upload in Analytics Debug Panel & Dashboard)", 19f, FontStyles.Bold, new Color(0.85f, 0.85f, 0.85f, 1f));
            AddFixedHeight(logHeader.gameObject, 30f);

            // Log Box
            GameObject logBox = new GameObject("LogBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            logBox.transform.SetParent(panelRoot.transform, false);
            logBox.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 1f);
            LayoutElement logLe = logBox.GetComponent<LayoutElement>();
            logLe.flexibleHeight = 1f;
            logLe.minHeight = 260f;

            logText = CreateLabel(logBox.transform, "LogText", "No events recorded yet. Press a test button above or play DREAM DUNGEON.", 18f, FontStyles.Normal, new Color(0.84f, 0.94f, 0.86f, 1f));
            StretchWithPadding(logText.rectTransform, 12f, 10f);
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.overflowMode = TextOverflowModes.Truncate;

            panelRoot.SetActive(false);
        }

        private void AddTestEventButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = CreateButton(parent, name, label, color, onClick, out _, out _, 21f);
            AddFixedHeight(btnGo, 62f);
        }

        private async void OnClickReinitialize()
        {
            AnalyticsManager mgr = AnalyticsManager.EnsureInstance();
            await mgr.InitializeAsync();
            RefreshStatus();
            RefreshLog();
        }

        private void OnClickToggleConsent()
        {
            AnalyticsManager mgr = AnalyticsManager.EnsureInstance();
            mgr.SetAnalyticsConsent(!mgr.IsConsentGranted);
            RefreshStatus();
        }

        private void OnClickFlush()
        {
            AnalyticsManager.EnsureInstance().RequestFlush();
            RefreshStatus();
        }

        // Sample test payloads matching STEP 4 specification (separated from Gameplay via AnalyticsEventSource.TestPanel)
        private void OnClickTestDungeonCreated()
        {
            AnalyticsManager.EnsureInstance().TrackDungeonCreated(
                dungeonLevel: 3,
                roomCount: 5,
                source: AnalyticsEventSource.TestPanel);
        }

        private void OnClickTestRaidStarted()
        {
            AnalyticsManager.EnsureInstance().TrackRaidStarted(
                dungeonLevel: 3,
                monsterCount: 8,
                trapCount: 4,
                source: AnalyticsEventSource.TestPanel);
        }

        private void OnClickTestRaidCompleted()
        {
            AnalyticsManager.EnsureInstance().TrackRaidCompleted(
                wave: 5,
                score: 1250,
                duration: 93.5f,
                source: AnalyticsEventSource.TestPanel);
        }

        private void OnClickTestRaidFailed()
        {
            AnalyticsManager.EnsureInstance().TrackRaidFailed(
                wave: 3,
                coreHp: 0,
                duration: 65.2f,
                source: AnalyticsEventSource.TestPanel);
        }

        private void OnClickTestCrystalSpent()
        {
            AnalyticsManager.EnsureInstance().TrackCrystalSpent(
                amount: 100,
                itemId: "monster_room",
                balanceAfter: 500,
                source: AnalyticsEventSource.TestPanel);
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            AnalyticsManager mgr = AnalyticsManager.Instance;
            UGSInitializationManager ugs = UGSInitializationManager.Instance;

            string initState = mgr != null ? mgr.InitStatus.ToString() : "Not Created";
            string sdkCompiled = AnalyticsManager.IsAnalyticsSdkCompiled ? "ENABLED (Live SDK)" : "DISABLED";
            string consent = mgr != null && mgr.IsConsentGranted ? "Granted" : "Denied";
            string projectId = string.IsNullOrWhiteSpace(Application.cloudProjectId) ? "<Unlinked>" : Application.cloudProjectId;
            string env = ugs != null ? ugs.EnvironmentName : "default";
            int count = mgr != null ? mgr.RecordedEventCount : 0;
            string errLine = mgr != null && !string.IsNullOrEmpty(mgr.LastError) ? $"\nLast Error: {mgr.LastError}" : string.Empty;

            statusText.text =
                $"Status: {initState}   |   SDK: {sdkCompiled}\n" +
                $"Consent: {consent}   |   Recorded (SDK): {count}\n" +
                $"Project ID: {projectId}   |   Env: {env}" +
                errLine;

            if (consentToggleLabel != null && mgr != null)
            {
                consentToggleLabel.text = mgr.IsConsentGranted ? "Consent: ON" : "Consent: OFF";
            }
        }

        private void RefreshLog()
        {
            if (logText == null || AnalyticsManager.Instance == null) return;
            var history = AnalyticsManager.Instance.EventHistory;
            if (history.Count == 0)
            {
                logText.text = "No events recorded yet. Press a test button above or play DREAM DUNGEON.";
                return;
            }

            var sb = new StringBuilder();
            int start = Mathf.Max(0, history.Count - 14);
            for (int i = history.Count - 1; i >= start; i--)
            {
                AnalyticsEventLogEntry e = history[i];
                string src = e.source == AnalyticsEventSource.TestPanel ? "TEST" : "GAME";
                sb.Append('[').Append(e.timestampUtc).Append("] [").Append(src).Append("] ")
                  .Append(e.eventName).Append(" -> ").Append(e.status)
                  .Append(" | ").Append(e.payloadPreview).AppendLine();
            }
            logText.text = sb.ToString();
        }

        private static GameObject CreateRow(Transform parent, string name, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static GameObject CreateButton(
            Transform parent,
            string name,
            string text,
            Color bgColor,
            UnityEngine.Events.UnityAction onClick,
            out Button button,
            out TextMeshProUGUI label,
            float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = bgColor;

            button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);

            label = CreateLabel(go.transform, "Label", text, fontSize, FontStyles.Bold, Color.white);
            StretchWithPadding(label.rectTransform, 10f, 4f);
            label.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize, FontStyles style, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchWithPadding(RectTransform rt, float horizontal, float vertical)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(horizontal, vertical);
            rt.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void AddFixedHeight(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        private static void AddFixedWidth(GameObject go, float width, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
        }

        private static void AddFlexibleLayout(GameObject go, float flexWidth)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWidth;
        }
#else
        private void Awake()
        {
            // Never active in Release Builds
            Destroy(gameObject);
        }
#endif
    }
}
