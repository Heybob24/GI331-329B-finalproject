using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public static class UIStyleHelper
    {
        public static void ApplyGothicPanel(Image target, bool tintSecondary = false)
        {
            if (target == null) return;
            var assets = UIThemeManager.Assets;
            var theme = UIThemeManager.Theme;
            if (assets != null && assets.panelEmptyFrame != null)
            {
                target.sprite = assets.panelEmptyFrame;
                target.type = Image.Type.Sliced;
                target.color = tintSecondary ? new Color(0.92f, 0.88f, 1f, 1f) : Color.white;
            }
            else
            {
                target.color = theme.secondaryBackground;
            }
        }

        public static void ApplyButtonStates(Button button, Color baseColor)
        {
            if (button == null) return;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = UIThemeManager.Theme.BuildButtonColors(baseColor);
            if (button.targetGraphic is Image img)
            {
                img.color = Color.white;
            }
        }

        public static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            int fontSize,
            Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch(go, 8f, 2f, 8f, 2f);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateThemedButton(
            Transform parent,
            string name,
            string label,
            Color baseColor,
            float preferredHeight,
            UnityEngine.Events.UnityAction onClick,
            Sprite customSprite = null)
        {
            var theme = UIThemeManager.Theme;
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;

            Image img = go.GetComponent<Image>();
            if (customSprite != null)
            {
                img.sprite = customSprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                ApplyGothicPanel(img);
            }

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ApplyButtonStates(btn, customSprite != null ? Color.white : baseColor);
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (customSprite == null && !string.IsNullOrEmpty(label))
            {
                CreateTmp(go.transform, "Label", label, theme.buttonTextSize, Color.white);
            }

            return btn;
        }

        public static TextMeshProUGUI CreateCurrencyCounter(Transform parent, string name, bool isCrystal, float width = 210f, float height = 66f)
        {
            var assets = UIThemeManager.Assets;
            var theme = UIThemeManager.Theme;
            Sprite sprite = assets != null
                ? (isCrystal ? assets.currencyCrystalCounter : assets.currencyGoldCounter)
                : null;

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            Image bg = go.GetComponent<Image>();
            if (sprite != null)
            {
                bg.sprite = sprite;
                bg.preserveAspect = true;
                bg.color = Color.white;
            }
            else
            {
                bg.color = theme.secondaryBackground;
            }

            GameObject mask = new GameObject("LiveValueBackdrop", typeof(RectTransform), typeof(Image));
            mask.transform.SetParent(go.transform, false);
            RectTransform mRt = mask.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.27f, 0.16f);
            mRt.anchorMax = new Vector2(0.80f, 0.84f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            mask.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 1f);

            TextMeshProUGUI tmp = CreateTmp(mask.transform, "ValueText", "0", theme.currencyTextSize,
                isCrystal ? theme.premiumCurrency : theme.primaryText);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = theme.currencyTextSize;
            return tmp;
        }

        public static string ShortenPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return "Guest";
            string trimmed = playerId.Trim();
            if (trimmed.Length <= 14) return trimmed;
            return trimmed.Substring(0, 8) + "..." + trimmed.Substring(trimmed.Length - 4);
        }

        public static void Stretch(GameObject go, float left, float bottom, float right, float top)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
