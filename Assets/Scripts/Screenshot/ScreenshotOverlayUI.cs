using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamDungeon
{
    /// <summary>
    /// Gameplay Screenshot HUD overlay for DREAM DUNGEON.
    /// Automatically attaches to Gameplay scenes (DungeonEditor, Battle, SampleScene),
    /// renders a touch-friendly Fantasy Dungeon camera button inside SafeArea,
    /// manages shutter flash & toast notifications, and opens ScreenshotPreviewUI.
    /// </summary>
    public sealed class ScreenshotOverlayUI : MonoBehaviour
    {
        public static ScreenshotOverlayUI Instance { get; private set; }

        private static readonly string[] GameplaySceneNames =
        {
            "DungeonEditor",
            "Battle",
            "SampleScene"
        };

        private Canvas overlayCanvas;
        private CanvasGroup overlayCanvasGroup;
        private RectTransform safeAreaRoot;
        private RectTransform cameraButtonRect;
        private Button cameraButton;
        private Image flashImage;
        private GameObject toastPanel;
        private CanvasGroup toastCanvasGroup;
        private Image toastBackground;
        private TextMeshProUGUI toastText;
        private ScreenshotPreviewUI previewUI;
        private Coroutine toastCoroutine;
        private Coroutine flashCoroutine;
        private bool wasPreviewOpenBeforeRetake;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            EnsureInstance();
        }

        public static ScreenshotOverlayUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            ScreenshotOverlayUI existing = FindAnyObjectByType<ScreenshotOverlayUI>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("ScreenshotOverlayUI");
            Instance = go.AddComponent<ScreenshotOverlayUI>();
            return Instance;
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

            ScreenshotManager.EnsureInstance();
            BuildOverlayUI();

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySceneVisibility(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (previewUI != null && previewUI.IsVisible)
            {
                previewUI.Hide();
            }

            ApplySceneVisibility(scene.name);
        }

        public static bool IsGameplayScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            for (int i = 0; i < GameplaySceneNames.Length; i++)
            {
                if (string.Equals(sceneName, GameplaySceneNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplySceneVisibility(string sceneName)
        {
            bool show = IsGameplayScene(sceneName);
            if (overlayCanvas != null)
            {
                overlayCanvas.gameObject.SetActive(show);
            }

            if (show)
            {
                PositionCameraButtonForScene(sceneName);
            }
        }

        private void PositionCameraButtonForScene(string sceneName)
        {
            if (cameraButtonRect == null) return;

            if (string.Equals(sceneName, "Battle", StringComparison.OrdinalIgnoreCase))
            {
                // BattleHUD occupies the top area; bottom-right inside SafeArea is completely clear
                cameraButtonRect.anchorMin = new Vector2(1f, 0f);
                cameraButtonRect.anchorMax = new Vector2(1f, 0f);
                cameraButtonRect.pivot = new Vector2(1f, 0f);
                cameraButtonRect.anchoredPosition = new Vector2(-24f, 34f);
            }
            else if (string.Equals(sceneName, "DungeonEditor", StringComparison.OrdinalIgnoreCase))
            {
                // DungeonBuilderRedesignUI top bar is 108px high; place cleanly just below top-right
                cameraButtonRect.anchorMin = new Vector2(1f, 1f);
                cameraButtonRect.anchorMax = new Vector2(1f, 1f);
                cameraButtonRect.pivot = new Vector2(1f, 1f);
                cameraButtonRect.anchoredPosition = new Vector2(-18f, -124f);
            }
            else
            {
                cameraButtonRect.anchorMin = new Vector2(1f, 1f);
                cameraButtonRect.anchorMax = new Vector2(1f, 1f);
                cameraButtonRect.pivot = new Vector2(1f, 1f);
                cameraButtonRect.anchoredPosition = new Vector2(-24f, -24f);
            }
        }

        private void BuildOverlayUI()
        {
            var theme = UIThemeManager.Theme;

            GameObject canvasObj = new GameObject(
                "ScreenshotOverlayCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObj.transform.SetParent(transform, false);

            overlayCanvas = canvasObj.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            overlayCanvasGroup = canvasObj.GetComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;

            // SafeArea container for mobile notches & rounded corners
            GameObject safeObj = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaController));
            safeObj.transform.SetParent(canvasObj.transform, false);
            UIStyleHelper.Stretch(safeObj, 0f, 0f, 0f, 0f);
            safeAreaRoot = safeObj.GetComponent<RectTransform>();

            // --- Fantasy Dungeon Camera Button ---
            BuildCameraButton(safeAreaRoot, theme);

            // --- Shutter Flash Overlay ---
            GameObject flashObj = new GameObject("ShutterFlash", typeof(RectTransform), typeof(Image));
            flashObj.transform.SetParent(canvasObj.transform, false);
            UIStyleHelper.Stretch(flashObj, 0f, 0f, 0f, 0f);
            flashImage = flashObj.GetComponent<Image>();
            flashImage.color = new Color(1f, 0.98f, 0.92f, 0f);
            flashImage.raycastTarget = false;
            flashObj.SetActive(false);

            // --- Preview Modal UI ---
            previewUI = gameObject.AddComponent<ScreenshotPreviewUI>();
            previewUI.Initialize(
                safeAreaRoot,
                OnSaveClicked,
                OnShareClicked,
                OnRetakeClicked,
                OnClosePreviewClicked);

            // --- Toast Notification Banner (top-center, always above preview) ---
            BuildToastBanner(safeAreaRoot, theme);
        }

        private void BuildCameraButton(Transform parent, DreamDungeonUITheme theme)
        {
            GameObject btnObj = new GameObject("ScreenshotCameraButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            cameraButtonRect = btnObj.GetComponent<RectTransform>();
            cameraButtonRect.sizeDelta = new Vector2(92f, 92f);

            Image bg = btnObj.GetComponent<Image>();
            UIStyleHelper.ApplyGothicPanel(bg);

            Outline rim = btnObj.AddComponent<Outline>();
            rim.effectColor = new Color(1.00f, 0.855f, 0.420f, 0.85f);
            rim.effectDistance = new Vector2(2f, -2f);

            cameraButton = btnObj.GetComponent<Button>();
            cameraButton.targetGraphic = bg;
            UIStyleHelper.ApplyButtonStates(cameraButton, new Color(0.24f, 0.15f, 0.42f, 0.96f));
            cameraButton.onClick.AddListener(OnCameraButtonClicked);

            // Camera icon inside button
            GameObject iconObj = new GameObject("CameraIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnObj.transform, false);
            UIStyleHelper.Stretch(iconObj, 16f, 16f, 16f, 16f);

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = ScreenshotIconFactory.GetCameraIcon();
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
        }

        private void BuildToastBanner(Transform parent, DreamDungeonUITheme theme)
        {
            toastPanel = new GameObject("ScreenshotToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            toastPanel.transform.SetParent(parent, false);

            RectTransform rt = toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(540f, 68f);

            toastBackground = toastPanel.GetComponent<Image>();
            UIStyleHelper.ApplyGothicPanel(toastBackground);
            toastBackground.color = new Color(0.10f, 0.28f, 0.18f, 0.96f);
            toastBackground.raycastTarget = false;

            Outline outline = toastPanel.AddComponent<Outline>();
            outline.effectColor = theme.primaryText;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            toastCanvasGroup = toastPanel.GetComponent<CanvasGroup>();
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.blocksRaycasts = false;
            toastCanvasGroup.interactable = false;

            toastText = UIStyleHelper.CreateTmp(
                toastPanel.transform,
                "ToastLabel",
                "Screenshot Saved!",
                24,
                Color.white,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            toastPanel.SetActive(false);
        }

        private void OnCameraButtonClicked()
        {
            TriggerCapture();
        }

        public void TriggerCapture()
        {
            ScreenshotManager manager = ScreenshotManager.EnsureInstance();
            if (manager.IsBusy)
            {
                return;
            }

            if (cameraButton != null) cameraButton.interactable = false;

            manager.CaptureScreenshot(
                onCaptured: capture =>
                {
                    if (cameraButton != null) cameraButton.interactable = true;
                    PlayShutterFlash();
                    previewUI?.Show(capture);
                    ShowToast("Screenshot Captured!", isError: false);
                },
                onError: error =>
                {
                    if (cameraButton != null) cameraButton.interactable = true;
                    if (wasPreviewOpenBeforeRetake && manager.LatestCapture != null)
                    {
                        previewUI?.Show(manager.LatestCapture);
                    }
                    ShowToast(error, isError: true);
                },
                beforeCaptureHideUI: () =>
                {
                    // Hide screenshot UI so the captured image contains pure gameplay
                    if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;
                    if (previewUI != null && previewUI.IsVisible)
                    {
                        previewUI.Hide();
                    }
                },
                afterCaptureRestoreUI: () =>
                {
                    if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 1f;
                });
        }

        private void OnSaveClicked()
        {
            ScreenshotManager manager = ScreenshotManager.EnsureInstance();
            if (manager.IsBusy) return;

            previewUI?.SetBusy(true);
            previewUI?.SetStatus("Saving screenshot...", UIThemeManager.Theme.primaryText);

            manager.SaveLatestScreenshot((success, savedPath, message) =>
            {
                previewUI?.SetBusy(false);
                if (success)
                {
                    previewUI?.MarkSaved(message);
                    ShowToast(message, isError: false);
                }
                else
                {
                    previewUI?.SetStatus(message, UIThemeManager.Theme.danger);
                    ShowToast(message, isError: true);
                }
            });
        }

        private void OnShareClicked()
        {
            ScreenshotManager manager = ScreenshotManager.EnsureInstance();
            if (manager.IsBusy) return;

            previewUI?.SetBusy(true);
            previewUI?.SetStatus("Preparing native share...", UIThemeManager.Theme.primaryText);

            manager.ShareLatestScreenshot((success, message) =>
            {
                previewUI?.SetBusy(false);
                if (success)
                {
                    previewUI?.SetStatus(message, UIThemeManager.Theme.primaryText);
                    ShowToast(message, isError: false);
                }
                else
                {
                    previewUI?.SetStatus(message, UIThemeManager.Theme.danger);
                    ShowToast(message, isError: true);
                }
            });
        }

        private void OnRetakeClicked()
        {
            ScreenshotManager manager = ScreenshotManager.EnsureInstance();
            if (manager.IsBusy) return;

            wasPreviewOpenBeforeRetake = true;
            previewUI?.Hide();
            TriggerCapture();
        }

        private void OnClosePreviewClicked()
        {
            wasPreviewOpenBeforeRetake = false;
            previewUI?.Hide();
            ScreenshotManager.EnsureInstance().ReleaseLatestCapture();
        }

        private void PlayShutterFlash()
        {
            if (flashImage == null) return;
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(ShutterFlashRoutine());
        }

        private IEnumerator ShutterFlashRoutine()
        {
            flashImage.gameObject.SetActive(true);
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(0.55f, 0f, t);
                flashImage.color = new Color(1f, 0.98f, 0.92f, alpha);
                yield return null;
            }

            flashImage.color = new Color(1f, 0.98f, 0.92f, 0f);
            flashImage.gameObject.SetActive(false);
            flashCoroutine = null;
        }

        public void ShowToast(string message, bool isError)
        {
            if (toastPanel == null || toastText == null) return;

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);

            toastText.text = message;
            if (toastBackground != null)
            {
                toastBackground.color = isError
                    ? new Color(0.50f, 0.12f, 0.16f, 0.96f)
                    : new Color(0.11f, 0.34f, 0.22f, 0.96f);
            }

            toastPanel.transform.SetAsLastSibling();
            toastCoroutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toastPanel.SetActive(true);

            // Fade in
            float fadeIn = 0.16f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            toastCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(2.4f);

            // Fade out
            float fadeOut = 0.25f;
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }

            toastCanvasGroup.alpha = 0f;
            toastPanel.SetActive(false);
            toastCoroutine = null;
        }
    }
}
