using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Editor
{
    public static class ScreenshotSystemTests
    {
        [MenuItem("Dream Dungeon/Screenshot System/Run Verification Tests")]
        public static void RunVerificationTests()
        {
            int passed = 0;
            int failed = 0;

            void Check(string name, bool condition, string detail)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[ScreenshotSystemTests] [PASS] {name}: {detail}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[ScreenshotSystemTests] [FAIL] {name}: {detail}");
                }
            }

            // 1. Filename format check
            string generatedName = ScreenshotManager.GenerateFileName();
            bool nameValid = Regex.IsMatch(generatedName, @"^DREAM_DUNGEON_\d{8}_\d{6}\.png$");
            Check("Filename Format", nameValid, generatedName);

            // 2. Gameplay scene routing check
            Check("Gameplay Scene Routing",
                ScreenshotOverlayUI.IsGameplayScene("DungeonEditor") &&
                ScreenshotOverlayUI.IsGameplayScene("Battle") &&
                !ScreenshotOverlayUI.IsGameplayScene("Boot") &&
                !ScreenshotOverlayUI.IsGameplayScene("MainMenu"),
                "Enabled on DungeonEditor & Battle, hidden on Boot & MainMenu");

            // 3. Procedural icons check
            Sprite cam = ScreenshotIconFactory.GetCameraIcon();
            Sprite share = ScreenshotIconFactory.GetShareIcon();
            Sprite save = ScreenshotIconFactory.GetSaveIcon();
            Check("Screenshot Icons", cam != null && share != null && save != null, "Camera, Share, Save sprites generated");

            // 4. Create sample PNG bytes for storage & temp share verification
            Texture2D sampleTex = new Texture2D(16, 16, TextureFormat.RGB24, false);
            byte[] pngBytes = sampleTex.EncodeToPNG();
            Object.DestroyImmediate(sampleTex);
            bool validPngHeader = pngBytes != null && pngBytes.Length > 8 &&
                                  pngBytes[0] == 0x89 && pngBytes[1] == 0x50 && pngBytes[2] == 0x4E && pngBytes[3] == 0x47;
            Check("PNG Encoding", validPngHeader, pngBytes != null ? $"{pngBytes.Length} bytes" : "null");

            // 5. Editor GalleryManager save test
            GameObject testGo = new GameObject("TempGalleryTest");
            GalleryManager gallery = testGo.AddComponent<GalleryManager>();
            bool saveSuccess = false;
            string savedPath = string.Empty;
            string saveMsg = string.Empty;
            string testFileName = "DREAM_DUNGEON_TEST_VERIFY.png";

            gallery.SavePng(pngBytes, testFileName, (ok, path, msg) =>
            {
                saveSuccess = ok;
                savedPath = path;
                saveMsg = msg;
            });

            bool savedFileExists = saveSuccess && !string.IsNullOrEmpty(savedPath) && File.Exists(savedPath);
            Check("Editor Gallery Save", savedFileExists, $"{savedPath} ({saveMsg})");
            if (savedFileExists)
            {
                File.Delete(savedPath);
            }
            Object.DestroyImmediate(testGo);

            // 6. NativeShareManager temp PNG test
            string tempSharePath = NativeShareManager.WriteTempPng(pngBytes, testFileName);
            bool tempExists = !string.IsNullOrEmpty(tempSharePath) && File.Exists(tempSharePath) && new FileInfo(tempSharePath).Length > 0;
            Check("Temp Share PNG", tempExists, tempSharePath);
            if (tempExists)
            {
                File.Delete(tempSharePath);
            }

            // 7. Android Native Share Library files check
            string androidLibRoot = "Assets/Plugins/Android/DreamDungeonShare.androidlib";
            bool androidLibPresent =
                File.Exists($"{androidLibRoot}/AndroidManifest.xml") &&
                File.Exists($"{androidLibRoot}/project.properties") &&
                File.Exists($"{androidLibRoot}/src/com/dreamdungeon/share/DreamDungeonFileProvider.java") &&
                File.Exists($"{androidLibRoot}/src/com/dreamdungeon/share/DreamDungeonShareBridge.java");
            Check("Android FileProvider & Share Bridge", androidLibPresent, androidLibRoot);

            Debug.Log($"[ScreenshotSystemTests] Summary: {passed} passed, {failed} failed.");
            if (failed > 0)
            {
                throw new System.Exception($"ScreenshotSystemTests failed with {failed} error(s).");
            }
        }
    }
}
