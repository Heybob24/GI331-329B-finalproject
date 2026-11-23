using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    [CreateAssetMenu(fileName = "DreamDungeonUITheme", menuName = "Dream Dungeon/UI/Global UI Theme")]
    public sealed class DreamDungeonUITheme : ScriptableObject
    {
        public const string ResourcesPath = "DreamDungeon/DreamDungeonUITheme";

        [Header("Global Color Palette")]
        public Color mainBackground      = new Color(0.039f, 0.039f, 0.090f, 0.99f); // Dark Navy / Black
        public Color secondaryBackground = new Color(0.094f, 0.071f, 0.169f, 0.96f); // Dark Purple
        public Color primaryText         = new Color(1.000f, 0.855f, 0.420f, 1.00f); // Warm Gold
        public Color normalText          = new Color(0.925f, 0.925f, 0.960f, 1.00f); // Ivory / Light Gray
        public Color mutedText           = new Color(0.680f, 0.700f, 0.790f, 1.00f);
        public Color primaryButton       = new Color(0.840f, 0.540f, 0.140f, 1.00f); // Gold / Orange
        public Color secondaryButton     = new Color(0.240f, 0.155f, 0.420f, 1.00f); // Dark Purple
        public Color premiumCurrency     = new Color(0.760f, 0.480f, 1.000f, 1.00f); // Crystal Purple
        public Color danger              = new Color(0.640f, 0.165f, 0.210f, 1.00f); // Dark Red
        public Color success             = new Color(0.180f, 0.680f, 0.400f, 1.00f); // Emerald Green
        public Color disabled            = new Color(0.270f, 0.270f, 0.330f, 0.85f); // Dark Gray

        [Header("Typography Sizes (TextMeshPro)")]
        public int mainTitleSize    = 44;
        public int sectionTitleSize = 28;
        public int bodyTextSize     = 20;
        public int buttonTextSize   = 22;
        public int currencyTextSize = 22;
        public int smallLabelSize   = 15;

        public ColorBlock BuildButtonColors(Color baseColor)
        {
            return new ColorBlock
            {
                normalColor      = baseColor,
                highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f),
                pressedColor     = Color.Lerp(baseColor, Color.black, 0.22f),
                selectedColor    = Color.Lerp(baseColor, primaryText, 0.22f),
                disabledColor    = disabled,
                colorMultiplier  = 1f,
                fadeDuration     = 0.08f
            };
        }

        public static DreamDungeonUITheme CreateRuntimeDefault()
        {
            var theme = CreateInstance<DreamDungeonUITheme>();
            theme.name = "DefaultDreamDungeonUITheme";
            return theme;
        }
    }
}
