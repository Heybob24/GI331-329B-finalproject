using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;

namespace DreamDungeon
{
    /// <summary>
    /// Handles saving PNG screenshots to the PC ProjectFolder/Screenshots directory in Unity Editor,
    /// and to the Android Mobile Gallery ("DREAM DUNGEON" album) via NativeGallery on Android.
    /// </summary>
    public sealed class GalleryManager : MonoBehaviour
    {
        public const string AlbumName = "DREAM DUNGEON";
        public const string EditorFolderName = "Screenshots";

        public static GalleryManager Instance { get; private set; }

        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        public static GalleryManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            GalleryManager existing = FindAnyObjectByType<GalleryManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("GalleryManager");
            Instance = go.AddComponent<GalleryManager>();
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
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GalleryManager] Callback error: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string GetEditorScreenshotsDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", EditorFolderName));
        }

        /// <summary>
        /// Saves PNG bytes to the appropriate platform destination and invokes onComplete(success, savedPath, userMessage)
        /// on the Unity main thread.
        /// </summary>
        public void SavePng(byte[] pngBytes, string fileName, Action<bool, string, string> onComplete)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                onComplete?.Invoke(false, string.Empty, "Screenshot data is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = ScreenshotManager.GenerateFileName();
            }

#if UNITY_EDITOR
            SaveToEditorProjectFolder(pngBytes, fileName, onComplete);
#elif UNITY_ANDROID
            SaveToAndroidGallery(pngBytes, fileName, onComplete);
#else
            SaveToPersistentFolder(pngBytes, fileName, onComplete);
#endif
        }

#if UNITY_EDITOR
        private void SaveToEditorProjectFolder(byte[] pngBytes, string fileName, Action<bool, string, string> onComplete)
        {
            try
            {
                string directory = GetEditorScreenshotsDirectory();
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fullPath = Path.GetFullPath(Path.Combine(directory, fileName));
                File.WriteAllBytes(fullPath, pngBytes);

                Debug.Log($"[GalleryManager] Screenshot saved (Unity Editor): {fullPath}");
                onComplete?.Invoke(true, fullPath, "Screenshot Saved!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GalleryManager] Failed to save screenshot in Unity Editor: {ex.Message}");
                onComplete?.Invoke(false, string.Empty, "Failed to save screenshot.");
            }
        }
#elif UNITY_ANDROID
        private void SaveToAndroidGallery(byte[] pngBytes, string fileName, Action<bool, string, string> onComplete)
        {
            try
            {
                NativeGallery.SaveImageToGallery(
                    pngBytes,
                    AlbumName,
                    fileName,
                    (success, path) =>
                    {
                        string resolvedPath = path ?? string.Empty;
                        string message = success
                            ? "Screenshot Saved!"
                            : "Failed to save screenshot to Gallery.";

                        mainThreadActions.Enqueue(() =>
                        {
                            if (success)
                            {
                                Debug.Log($"[GalleryManager] Screenshot saved to Android Gallery ({AlbumName}): {resolvedPath}");
                            }
                            else
                            {
                                Debug.LogError($"[GalleryManager] NativeGallery failed to save screenshot ({fileName}).");
                            }

                            onComplete?.Invoke(success, resolvedPath, message);
                        });
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GalleryManager] Android NativeGallery save exception: {ex.Message}");
                onComplete?.Invoke(false, string.Empty, "Failed to save screenshot to Gallery.");
            }
        }
#else
        private void SaveToPersistentFolder(byte[] pngBytes, string fileName, Action<bool, string, string> onComplete)
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, EditorFolderName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fullPath = Path.Combine(directory, fileName);
                File.WriteAllBytes(fullPath, pngBytes);

                Debug.Log($"[GalleryManager] Screenshot saved: {fullPath}");
                onComplete?.Invoke(true, fullPath, "Screenshot Saved!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GalleryManager] Failed to save screenshot: {ex.Message}");
                onComplete?.Invoke(false, string.Empty, "Failed to save screenshot.");
            }
        }
#endif
    }
}
