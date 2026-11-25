using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    /// <summary>
    /// Fantasy Dungeon themed modal Preview UI displayed after capturing a screenshot.
    /// Provides Save, Share, Retake, and Close actions inside SafeArea for mobile & PC.
    /// </summary>
    public sealed class ScreenshotPreviewUI : MonoBehaviour
    {
        private GameObject rootPanel;
        private RawImage previewRawImage;
        private AspectRatioFitter previewAspectFitter;
        private TextMeshProUGUI fileInfoText;
        private TextMeshProUGUI statusText;
        private Button saveButton;
        private TextMeshProUGUI saveButtonLabel;
        private Button shareButton;
        private Button retakeButton;
        private Button closeButton;
        private Button headerCloseButton;

        private Action onSaveClicked;
        private Action onShareClicked;
        private Action onRetakeClicked;
        private Action onCloseClicked;

        public bool IsVisible => rootPanel != null && rootPanel.activeSelf;

        public void Initialize(
            Transform safeAreaParent,
            Action saveAction,
            Action shareAction,
            Action retakeAction,
            Action closeAction)
        {
            onSaveClicked = saveAction;
            onShareClicked = shareAction;
            onRetakeClicked = retakeAction;
            onCloseClicked = closeAction;

            if (rootPanel == null)
            {
                BuildUI(safeAreaParent);
            }

            Hide();
        }

        public void Show(ScreenshotCaptureData capture)
        {
            if (rootPanel == null || capture == null) return;

            if (previewRawImage != null)
            {
                previewRawImage.texture = capture.PreviewTexture;
                previewRawImage.color = Color.white;
            }

            if (previewAspectFitter != null && capture.Height > 0)
            {
                previewAspectFitter.aspectRatio = (float)capture.Width / capture.Height;
            }

            if (fileInfoText != null)
            {
                fileInfoText.text = $"{capture.FileName}  •  {capture.Width}×{capture.Height}";
            }

            SetStatus(
                capture.IsSaved ? "Screenshot Saved!" : "Ready to Save or Share",
                capture.IsSaved ? UIThemeManager.Theme.success : UIThemeManager.Theme.mutedText);

            SetBusy(false);
            if (saveButtonLabel != null)
            {
                saveButtonLabel.text = capture.IsSaved ? "SAVED" : "SAVE";
            }

            rootPanel.SetActive(true);
            rootPanel.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }

            if (previewRawImage != null)
            {
                previewRawImage.texture = null;
            }
        }

        public void SetBusy(bool busy)
        {
            if (saveButton != null) saveButton.interactable = !busy;
            if (shareButton != null) shareButton.interactable = !busy;
            if (retakeButton != null) retakeButton.interactable = !busy;
            if (closeButton != null) closeButton.interactable = !busy;
            if (headerCloseButton != null) headerCloseButton.interactable = !busy;
        }

        public void MarkSaved(string message)
        {
            if (saveButtonLabel != null) saveButtonLabel.text = "SAVED";
            SetStatus(message, UIThemeManager.Theme.success);
        }

        public void SetStatus(string message, Color color)
        {
            if (statusText == null) return;
            statusText.text = message ?? string.Empty;
            statusText.color = color;
        }

        private void BuildUI(Transform safeAreaParent)
        {
            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;

            // Fullscreen dimmed backdrop
            rootPanel = new GameObject("ScreenshotPreviewOverlay", typeof(RectTransform), typeof(Image));
            rootPanel.transform.SetParent(safeAreaParent, false);
            UIStyleHelper.Stretch(rootPanel, 0f, 0f, 0f, 0f);
            Image backdrop = rootPanel.GetComponent<Image>();
            backdrop.color = new Color(0.02f, 0.02f, 0.06f, 0.90f);
            backdrop.raycastTarget = true;

            // Center gothic modal frame
            GameObject modal = new GameObject("PreviewModalCard", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(rootPanel.transform, false);
            RectTransform modalRt = modal.GetComponent<RectTransform>();
            modalRt.anchorMin = new Vector2(0.05f, 0.06f);
            modalRt.anchorMax = new Vector2(0.95f, 0.94f);
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;

            Image modalBg = modal.GetComponent<Image>();
            UIStyleHelper.ApplyGothicPanel(modalBg);

            // Inner dark dungeon panel for crisp contrast
            GameObject inner = new GameObject("InnerContent", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            inner.transform.SetParent(modal.transform, false);
            UIStyleHelper.Stretch(inner, 18f, 18f, 18f, 18f);
            inner.GetComponent<Image>().color = new Color(0.065f, 0.050f, 0.125f, 0.96f);

            VerticalLayoutGroup vlg = inner.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 18, 20);
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // --- Header Row ---
            GameObject header = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(inner.transform, false);
            header.GetComponent<LayoutElement>().preferredHeight = 76f;

            HorizontalLayoutGroup headerHlg = header.GetComponent<HorizontalLayoutGroup>();
            headerHlg.spacing = 12f;
            headerHlg.childAlignment = TextAnchor.MiddleCenter;
            headerHlg.childControlWidth = true;
            headerHlg.childControlHeight = true;
            headerHlg.childForceExpandWidth = false;
            headerHlg.childForceExpandHeight = false;

            GameObject titleBox = new GameObject("TitleBox", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            titleBox.transform.SetParent(header.transform, false);
            titleBox.GetComponent<LayoutElement>().flexibleWidth = 1f;
            titleBox.GetComponent<LayoutElement>().preferredHeight = 72f;

            VerticalLayoutGroup titleVlg = titleBox.GetComponent<VerticalLayoutGroup>();
            titleVlg.spacing = 2f;
            titleVlg.childAlignment = TextAnchor.MiddleLeft;
            titleVlg.childControlWidth = true;
            titleVlg.childControlHeight = true;
            titleVlg.childForceExpandWidth = true;
            titleVlg.childForceExpandHeight = false;

            TextMeshProUGUI titleTmp = UIStyleHelper.CreateTmp(
                titleBox.transform, "Title", "SCREENSHOT PREVIEW", 30, theme.primaryText, TextAlignmentOptions.Left, FontStyles.Bold);
            titleTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            fileInfoText = UIStyleHelper.CreateTmp(
                titleBox.transform, "FileInfo", "DREAM_DUNGEON.png", 17, theme.mutedText, TextAlignmentOptions.Left, FontStyles.Normal);
            fileInfoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            // Header Close (X) button
            headerCloseButton = CreateIconActionButton(
                header.transform,
                "HeaderCloseBtn",
                assets != null && assets.iconClose != null ? assets.iconClose : null,
                assets != null && assets.iconClose != null ? string.Empty : "✕",
                theme.danger,
                64f,
                64f,
                () => onCloseClicked?.Invoke());

            // --- Preview Image Container ---
            GameObject previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            previewFrame.transform.SetParent(inner.transform, false);
            LayoutElement frameLe = previewFrame.GetComponent<LayoutElement>();
            frameLe.flexibleHeight = 1f;
            frameLe.minHeight = 240f;

            Image frameBg = previewFrame.GetComponent<Image>();
            frameBg.color = new Color(0.03f, 0.025f, 0.06f, 1f);

            // Subtle gold border around preview
            Outline frameOutline = previewFrame.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0.84f, 0.64f, 0.24f, 0.70f);
            frameOutline.effectDistance = new Vector2(2f, -2f);

            GameObject imageObj = new GameObject("PreviewRawImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            imageObj.transform.SetParent(previewFrame.transform, false);
            UIStyleHelper.Stretch(imageObj, 10f, 10f, 10f, 10f);

            previewRawImage = imageObj.GetComponent<RawImage>();
            previewRawImage.raycastTarget = false;

            previewAspectFitter = imageObj.GetComponent<AspectRatioFitter>();
            previewAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            previewAspectFitter.aspectRatio = 16f / 9f;

            // --- Status Banner ---
            GameObject statusBox = new GameObject("StatusBox", typeof(RectTransform), typeof(LayoutElement));
            statusBox.transform.SetParent(inner.transform, false);
            statusBox.GetComponent<LayoutElement>().preferredHeight = 36f;

            statusText = UIStyleHelper.CreateTmp(
                statusBox.transform, "StatusText", "Ready to Save or Share", 21, theme.mutedText, TextAlignmentOptions.Center, FontStyles.Bold);

            // --- Primary Actions Row (Save + Share) ---
            GameObject primaryRow = new GameObject("PrimaryActionsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            primaryRow.transform.SetParent(inner.transform, false);
            primaryRow.GetComponent<LayoutElement>().preferredHeight = 82f;

            HorizontalLayoutGroup primaryHlg = primaryRow.GetComponent<HorizontalLayoutGroup>();
            primaryHlg.spacing = 16f;
            primaryHlg.childAlignment = TextAnchor.MiddleCenter;
            primaryHlg.childControlWidth = true;
            primaryHlg.childControlHeight = true;
            primaryHlg.childForceExpandWidth = true;
            primaryHlg.childForceExpandHeight = true;

            saveButton = CreateActionButtonWithIcon(
                primaryRow.transform,
                "SaveButton",
                "SAVE",
                ScreenshotIconFactory.GetSaveIcon(),
                theme.success,
                out saveButtonLabel,
                () => onSaveClicked?.Invoke());

            shareButton = CreateActionButtonWithIcon(
                primaryRow.transform,
                "ShareButton",
                "SHARE",
                ScreenshotIconFactory.GetShareIcon(),
                theme.primaryButton,
                out _,
                () => onShareClicked?.Invoke());

            // --- Secondary Actions Row (Retake + Close) ---
            GameObject secondaryRow = new GameObject("SecondaryActionsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            secondaryRow.transform.SetParent(inner.transform, false);
            secondaryRow.GetComponent<LayoutElement>().preferredHeight = 72f;

            HorizontalLayoutGroup secondaryHlg = secondaryRow.GetComponent<HorizontalLayoutGroup>();
            secondaryHlg.spacing = 16f;
            secondaryHlg.childAlignment = TextAnchor.MiddleCenter;
            secondaryHlg.childControlWidth = true;
            secondaryHlg.childControlHeight = true;
            secondaryHlg.childForceExpandWidth = true;
            secondaryHlg.childForceExpandHeight = true;

            retakeButton = CreateActionButtonWithIcon(
                secondaryRow.transform,
                "RetakeButton",
                "NEW SHOT",
                ScreenshotIconFactory.GetCameraIcon(),
                theme.secondaryButton,
                out _,
                () => onRetakeClicked?.Invoke());

            closeButton = CreateActionButtonWithIcon(
                secondaryRow.transform,
                "CloseButton",
                "CLOSE",
                null,
                theme.danger,
                out _,
                () => onCloseClicked?.Invoke());
        }

        private static Button CreateActionButtonWithIcon(
            Transform parent,
            string name,
            string label,
            Sprite icon,
            Color baseColor,
            out TextMeshProUGUI labelText,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);

            Image bg = go.GetComponent<Image>();
            UIStyleHelper.ApplyGothicPanel(bg);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            UIStyleHelper.ApplyButtonStates(btn, baseColor);
            if (onClick != null) btn.onClick.AddListener(onClick);

            HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 8, 8);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            if (icon != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(34f, 34f);

                Image iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
            }

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(160f, 44f);

            labelText = textGo.GetComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 24;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 16;
            labelText.fontSizeMax = 24;
            labelText.raycastTarget = false;

            return btn;
        }

        private static Button CreateIconActionButton(
            Transform parent,
            string name,
            Sprite icon,
            string fallbackText,
            Color baseColor,
            float width,
            float height,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            Image bg = go.GetComponent<Image>();
            UIStyleHelper.ApplyGothicPanel(bg);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            UIStyleHelper.ApplyButtonStates(btn, baseColor);
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (icon != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                UIStyleHelper.Stretch(iconGo, 14f, 14f, 14f, 14f);
                Image iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
            }
            else if (!string.IsNullOrEmpty(fallbackText))
            {
                UIStyleHelper.CreateTmp(go.transform, "Label", fallbackText, 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            }

            return btn;
        }
    }
}
