using UnityEngine;

namespace DreamDungeon
{
    /// <summary>
    /// Generates crisp, high-DPI procedural icons for the Fantasy Dungeon Screenshot UI
    /// so buttons render cleanly on mobile without missing sprite references.
    /// </summary>
    public static class ScreenshotIconFactory
    {
        private static Sprite cachedCameraSprite;
        private static Sprite cachedShareSprite;
        private static Sprite cachedSaveSprite;

        public static Sprite GetCameraIcon()
        {
            if (cachedCameraSprite != null) return cachedCameraSprite;

            const int size = 128;
            Texture2D tex = CreateBlankTexture(size, "Icon_ScreenshotCamera");
            Color gold = new Color(1.00f, 0.86f, 0.44f, 1f);
            Color ivory = new Color(0.98f, 0.96f, 0.90f, 1f);
            Color darkLens = new Color(0.11f, 0.08f, 0.20f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            // Camera body (rounded rectangle)
            DrawRoundedRect(tex, 16, 26, 96, 62, 12, gold);
            DrawRoundedRect(tex, 20, 30, 88, 54, 9, ivory);

            // Top viewfinder hump
            DrawRoundedRect(tex, 42, 82, 44, 18, 6, gold);
            DrawRoundedRect(tex, 46, 84, 36, 12, 4, ivory);

            // Small shutter button on top-left
            DrawRoundedRect(tex, 24, 86, 14, 7, 3, gold);

            // Outer lens ring
            DrawCircle(tex, 64f, 57f, 22f, darkLens);
            DrawCircle(tex, 64f, 57f, 17f, gold);
            DrawCircle(tex, 64f, 57f, 12f, darkLens);

            // Lens glint
            DrawCircle(tex, 59f, 62f, 3.5f, ivory);

            // Flash indicator dot on top-right of body
            DrawCircle(tex, 94f, 74f, 4.2f, darkLens);
            DrawCircle(tex, 94f, 74f, 2.2f, gold);

            // Subtle notch cutout in center of lens
            _ = clear;

            tex.Apply(false, true);
            cachedCameraSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedCameraSprite.name = "Sprite_ScreenshotCamera";
            return cachedCameraSprite;
        }

        public static Sprite GetShareIcon()
        {
            if (cachedShareSprite != null) return cachedShareSprite;

            const int size = 96;
            Texture2D tex = CreateBlankTexture(size, "Icon_ScreenshotShare");
            Color white = Color.white;

            Vector2 left = new Vector2(24f, 48f);
            Vector2 topRight = new Vector2(70f, 72f);
            Vector2 bottomRight = new Vector2(70f, 24f);

            DrawThickLine(tex, left, topRight, 5f, white);
            DrawThickLine(tex, left, bottomRight, 5f, white);

            DrawCircle(tex, left.x, left.y, 10.5f, white);
            DrawCircle(tex, topRight.x, topRight.y, 10.5f, white);
            DrawCircle(tex, bottomRight.x, bottomRight.y, 10.5f, white);

            tex.Apply(false, true);
            cachedShareSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedShareSprite.name = "Sprite_ScreenshotShare";
            return cachedShareSprite;
        }

        public static Sprite GetSaveIcon()
        {
            if (cachedSaveSprite != null) return cachedSaveSprite;

            const int size = 96;
            Texture2D tex = CreateBlankTexture(size, "Icon_ScreenshotSave");
            Color white = Color.white;

            // Downward arrow shaft
            DrawRoundedRect(tex, 43, 38, 10, 42, 3, white);
            // Arrow head
            DrawThickLine(tex, new Vector2(48f, 34f), new Vector2(30f, 52f), 5.5f, white);
            DrawThickLine(tex, new Vector2(48f, 34f), new Vector2(66f, 52f), 5.5f, white);
            // Bottom tray
            DrawRoundedRect(tex, 18, 16, 60, 9, 3, white);
            DrawRoundedRect(tex, 18, 16, 9, 22, 3, white);
            DrawRoundedRect(tex, 69, 16, 9, 22, 3, white);

            tex.Apply(false, true);
            cachedSaveSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cachedSaveSprite.name = "Sprite_ScreenshotSave";
            return cachedSaveSprite;
        }

        private static Texture2D CreateBlankTexture(int size, string name)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);
            return tex;
        }

        private static void DrawRoundedRect(Texture2D tex, int x, int y, int width, int height, int radius, Color color)
        {
            int maxX = Mathf.Min(tex.width - 1, x + width);
            int maxY = Mathf.Min(tex.height - 1, y + height);
            int minX = Mathf.Max(0, x);
            int minY = Mathf.Max(0, y);

            for (int py = minY; py < maxY; py++)
            {
                for (int px = minX; px < maxX; px++)
                {
                    float dx = 0f;
                    float dy = 0f;

                    if (px < x + radius) dx = (x + radius) - px;
                    else if (px > x + width - radius - 1) dx = px - (x + width - radius - 1);

                    if (py < y + radius) dy = (y + radius) - py;
                    else if (py > y + height - radius - 1) dy = py - (y + height - radius - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        BlendPixel(tex, px, py, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D tex, float cx, float cy, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 1));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 1));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + radius + 1));

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    if (dist <= radius + 0.5f)
                    {
                        float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                        BlendPixel(tex, px, py, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }
        }

        private static void DrawThickLine(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color color)
        {
            float half = thickness * 0.5f;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - half - 1));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + half + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - half - 1));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + half + 1));

            Vector2 ab = b - a;
            float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    Vector2 p = new Vector2(px, py);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                    Vector2 proj = a + t * ab;
                    float dist = Vector2.Distance(p, proj);
                    if (dist <= half + 0.5f)
                    {
                        float alpha = Mathf.Clamp01(half + 0.5f - dist);
                        BlendPixel(tex, px, py, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color src)
        {
            if (src.a <= 0.001f) return;
            Color dst = tex.GetPixel(x, y);
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f)
            {
                tex.SetPixel(x, y, Color.clear);
                return;
            }

            Color result = new Color(
                (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA,
                (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA,
                (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA,
                outA);
            tex.SetPixel(x, y, result);
        }
    }
}
