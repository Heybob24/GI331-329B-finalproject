using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class BattleHUD : MonoBehaviour
    {
        private BattleManager manager;
        private Text waveText;
        private Text aliveText;
        private Text coreText;
        private Image coreHpFill;
        private Text speedText;
        private Text statusText;
        private Text resultTitleText;
        private Text resultDetailsText;
        private Text resultRewardText;

        private GameObject resultPanel;
        private Button continueButton;
        private Button retryButton;
        private Button returnButton;
        private Button pauseButton;
        private Button speedButton;

        private GameObject bossBarPanel;
        private Text bossNameText;
        private Text bossHpText;
        private Image bossHpFill;
        private CampaignResultUI campaignResultUI;

        private bool paused;

        private void Awake()
        {
            BuildUI();
        }

        public void Bind(BattleManager battle)
        {
            manager = battle;
            Refresh();
        }

        public void Refresh()
        {
            if (manager == null) return;

            if (waveText != null) waveText.text = $"WAVE  {manager.CurrentWave} / {manager.TotalWaves}";
            if (aliveText != null) aliveText.text = $"RAIDERS  {manager.LivingAdventurers}";

            if (coreText != null) coreText.text = $"CORE HP  {manager.CoreHP} / {manager.CoreMaxHP}";
            if (coreHpFill != null)
            {
                float ratio = manager.CoreMaxHP > 0 ? (float)manager.CoreHP / manager.CoreMaxHP : 0f;
                coreHpFill.fillAmount = Mathf.Clamp01(ratio);
                coreHpFill.color = ratio > 0.5f ? new Color(0.25f, 0.88f, 0.40f) : (ratio > 0.25f ? new Color(0.95f, 0.72f, 0.2f) : new Color(0.92f, 0.25f, 0.25f));
            }

            if (speedText != null) speedText.text = $"x{manager.Speed:0}";
            if (statusText != null) statusText.text = manager.Status;
            if (pauseButton != null) SetButtonLabel(pauseButton, paused ? "RESUME" : "PAUSE");

            // Boss Health Bar refresh
            if (bossBarPanel != null)
            {
                BossBattleActor activeBoss = null;
                if (manager.Bosses != null)
                {
                    for (int i = 0; i < manager.Bosses.Count; i++)
                    {
                        if (manager.Bosses[i] != null && manager.Bosses[i].IsAlive)
                        {
                            activeBoss = manager.Bosses[i];
                            break;
                        }
                    }
                }

                if (activeBoss != null)
                {
                    bossBarPanel.SetActive(true);
                    if (bossNameText != null)
                    {
                        string rageText = activeBoss.PhaseController != null && activeBoss.PhaseController.IsEnraged ? " <color=red>[ENRAGED]</color>" : "";
                        bossNameText.text = $"BOSS: {activeBoss.Name}{rageText}";
                    }
                    if (bossHpText != null)
                    {
                        bossHpText.text = $"{activeBoss.CurrentHP} / {activeBoss.MaxHP}";
                    }
                    if (bossHpFill != null)
                    {
                        float bRatio = activeBoss.MaxHP > 0 ? (float)activeBoss.CurrentHP / activeBoss.MaxHP : 0f;
                        bossHpFill.fillAmount = Mathf.Clamp01(bRatio);
                    }
                }
                else
                {
                    bossBarPanel.SetActive(false);
                }
            }
        }

        public void ShowResult(bool victory, int wave, int defeated, int coreHp, int gold, int soul, int exp)
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            if (campaignResultUI == null)
            {
                campaignResultUI = gameObject.GetComponent<CampaignResultUI>() ?? gameObject.AddComponent<CampaignResultUI>();
            }
            if (resultPanel != null)
            {
                campaignResultUI.AttachToResultPanel(manager, resultPanel.transform);
                campaignResultUI.RefreshButtonStates(victory);
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = victory ? "VICTORY!" : "DEFEAT";
                resultTitleText.color = victory ? new Color(0.96f, 0.82f, 0.35f) : new Color(0.95f, 0.35f, 0.35f);
            }

            if (resultDetailsText != null)
            {
                resultDetailsText.text = campaignResultUI != null
                    ? campaignResultUI.FormatCampaignResultDetails(victory, wave, defeated, coreHp)
                    : (victory
                        ? $"All {wave} Waves Cleared!\nAdventurers Defeated: {defeated}\nCore Integrity: {coreHp} HP"
                        : $"Defeated on Wave {wave}\nAdventurers Defeated: {defeated}\nCore Destroyed");
            }

            if (resultRewardText != null)
            {
                resultRewardText.text = victory
                    ? $"REWARDS\nGold +{gold}   Soul +{soul}   EXP +{exp}"
                    : (gold > 0 || soul > 0 ? $"REWARDS\nGold +{gold}   Soul +{soul}" : "No rewards claimed.");
            }

            if (continueButton != null) continueButton.gameObject.SetActive(victory);
            if (retryButton != null) retryButton.gameObject.SetActive(!victory);
            if (returnButton != null) returnButton.gameObject.SetActive(true);

            Refresh();
        }

        public void ShowError(string message)
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = "RAID CANNOT START";
                resultTitleText.color = new Color(0.95f, 0.4f, 0.35f);
            }

            if (resultDetailsText != null)
            {
                resultDetailsText.text = message;
            }

            if (resultRewardText != null)
            {
                resultRewardText.text = string.Empty;
            }

            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            if (returnButton != null) returnButton.gameObject.SetActive(true);

            Refresh();
        }

        private void BuildUI()
        {
            if (GetComponentInChildren<Canvas>() != null) return;

            GameObject canvasObject = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safe = MakeRect("SafeAreaRoot", canvasObject.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaController>();

            // --- Top HUD Panel (Compact 260px) ---
            RectTransform hudTop = Panel(safe, "HudTop", new Color(0.04f, 0.05f, 0.08f, 0.90f),
                                         new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 260f));

            waveText = Label(hudTop, "WaveLabel", "WAVE  1 / 5", 32,
                             new Vector2(0.04f, 0.65f), new Vector2(0.55f, 0.95f), Vector2.zero);
            waveText.alignment = TextAnchor.MiddleLeft;

            aliveText = Label(hudTop, "AliveLabel", "RAIDERS  0", 24,
                              new Vector2(0.04f, 0.38f), new Vector2(0.55f, 0.65f), Vector2.zero);
            aliveText.alignment = TextAnchor.MiddleLeft;
            aliveText.color = new Color(0.9f, 0.85f, 0.7f);

            // Core HP Label & Bar
            coreText = Label(hudTop, "CoreLabel", "CORE HP  500 / 500", 22,
                             new Vector2(0.04f, 0.12f), new Vector2(0.38f, 0.38f), Vector2.zero);
            coreText.alignment = TextAnchor.MiddleLeft;

            RectTransform coreBarBg = Panel(hudTop, "CoreBarBg", new Color(0.12f, 0.10f, 0.15f),
                                            new Vector2(0.38f, 0.16f), new Vector2(0.68f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero);
            RectTransform coreBarFillRt = Panel(coreBarBg, "CoreBarFill", new Color(0.28f, 0.90f, 0.38f),
                                                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero);
            coreHpFill = coreBarFillRt.GetComponent<Image>();
            coreHpFill.type = Image.Type.Filled;
            coreHpFill.fillMethod = Image.FillMethod.Horizontal;
            coreHpFill.fillAmount = 1f;

            // Speed & Pause buttons (anchored to top-right with fixed touchable dimensions)
            speedButton = FixedButton(hudTop, "SpeedButton", "x1", new Vector2(1f, 1f), new Vector2(-230f, -60f), new Vector2(120f, 68f), OnSpeed);
            speedText = speedButton.GetComponentInChildren<Text>();

            pauseButton = FixedButton(hudTop, "PauseButton", "PAUSE", new Vector2(1f, 1f), new Vector2(-95f, -60f), new Vector2(130f, 68f), OnPause);

            // Status notification bar at bottom of Top HUD
            statusText = Label(hudTop, "StatusLabel", "Preparing raid…", 20,
                               new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.14f), Vector2.zero);
            statusText.color = new Color(0.75f, 0.80f, 0.85f);

            // --- Boss Health Bar Panel (Visible when Boss is alive) ---
            RectTransform bossRt = Panel(safe, "BossBarPanel", new Color(0.12f, 0.03f, 0.03f, 0.92f),
                                         new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 75f));
            bossRt.anchoredPosition = new Vector2(0f, -270f);
            bossBarPanel = bossRt.gameObject;

            bossNameText = Label(bossRt, "BossName", "BOSS: Cave Dragon", 22,
                                 new Vector2(0.04f, 0.52f), new Vector2(0.60f, 0.95f), Vector2.zero);
            bossNameText.alignment = TextAnchor.MiddleLeft;
            bossNameText.color = new Color(1f, 0.85f, 0.4f);

            bossHpText = Label(bossRt, "BossHP", "1500 / 1500", 20,
                               new Vector2(0.60f, 0.52f), new Vector2(0.96f, 0.95f), Vector2.zero);
            bossHpText.alignment = TextAnchor.MiddleRight;
            bossHpText.color = new Color(0.9f, 0.9f, 0.9f);

            RectTransform bossBarBg = Panel(bossRt, "BossBarBg", new Color(0.2f, 0.05f, 0.05f),
                                            new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero);
            RectTransform bossBarFillRt = Panel(bossBarBg, "BossBarFill", new Color(0.95f, 0.22f, 0.22f),
                                                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero);
            bossHpFill = bossBarFillRt.GetComponent<Image>();
            bossHpFill.type = Image.Type.Filled;
            bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            bossHpFill.fillAmount = 1f;

            bossBarPanel.SetActive(false);

            // --- Result Panel Overlay ---
            resultPanel = Panel(safe, "ResultPanel", new Color(0.02f, 0.025f, 0.04f, 0.96f),
                                new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.80f), new Vector2(0.5f, 0.5f), Vector2.zero).gameObject;

            resultTitleText = Label(resultPanel.transform, "ResultTitle", "VICTORY", 54,
                                    new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f), Vector2.zero);

            resultDetailsText = Label(resultPanel.transform, "ResultDetails", "", 30,
                                      new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.75f), Vector2.zero);

            resultRewardText = Label(resultPanel.transform, "ResultReward", "", 28,
                                     new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.48f), Vector2.zero);
            resultRewardText.color = new Color(0.96f, 0.85f, 0.45f);

            continueButton = Button(resultPanel.transform, "ContinueButton", "CONTINUE",
                                    new Vector2(0.5f, 0.20f), new Vector2(0.76f, 0.09f), () => manager?.ContinueToDungeon());

            retryButton = Button(resultPanel.transform, "RetryButton", "RETRY",
                                 new Vector2(0.5f, 0.20f), new Vector2(0.76f, 0.09f), () => manager?.Retry());

            returnButton = Button(resultPanel.transform, "ReturnButton", "RETURN TO DUNGEON",
                                  new Vector2(0.5f, 0.08f), new Vector2(0.76f, 0.08f), () => manager?.ContinueToDungeon());

            resultPanel.SetActive(false);
        }

        private void OnPause()
        {
            if (manager == null) return;
            paused = !paused;
            manager.SetPaused(paused);
            Refresh();
        }

        private void OnSpeed()
        {
            manager?.CycleSpeed();
            Refresh();
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private static Text Label(Transform parent, string name, string value, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rt = Panel(parent, name, new Color(0.28f, 0.20f, 0.35f, 0.96f), anchor, anchor, new Vector2(0.5f, 0.5f), new Vector2(size.x * 1080f, size.y * 1920f));
            Button button = rt.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            RectTransform textRect = MakeRect("Label", rt);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return button;
        }

        private static Button FixedButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, UnityEngine.Events.UnityAction action)
        {
            RectTransform rt = MakeRect(name, parent);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.24f, 0.18f, 0.32f, 0.96f);

            Button button = rt.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            RectTransform textRect = MakeRect("Label", rt);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;

            return button;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>() : null;
            if (text != null) text.text = label;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
