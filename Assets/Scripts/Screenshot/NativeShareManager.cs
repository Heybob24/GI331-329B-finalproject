using System;
using System.IO;
using UnityEngine;

namespace DreamDungeon
{
    /// <summary>
    /// Handles native OS image sharing.
    /// Writes a temporary PNG to Application.temporaryCachePath, verifies it exists,
    /// and on Android launches the real Android Native Share Sheet via ACTION_SEND + FileProvider.
    /// </summary>
    public sealed class NativeShareManager : MonoBehaviour
    {
        public const string ShareTitle = "Share DREAM DUNGEON Screenshot";
        public const string ShareSubject = "DREAM DUNGEON Screenshot";
        public const string ShareMessage = "Check out my dungeon in DREAM DUNGEON!";

        public static NativeShareManager Instance { get; private set; }

        public string LastSharedTempPath { get; private set; }

        public static NativeShareManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            NativeShareManager existing = FindAnyObjectByType<NativeShareManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("NativeShareManager");
            Instance = go.AddComponent<NativeShareManager>();
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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Writes PNG bytes to Application.temporaryCachePath, validates file existence,
        /// and opens the Android Native Share Sheet.
        /// </summary>
        public void SharePng(byte[] pngBytes, string fileName, Action<bool, string> onComplete = null)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                const string err = "Cannot share: screenshot data is empty.";
                Debug.LogError($"[NativeShareManager] {err}");
                onComplete?.Invoke(false, err);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = ScreenshotManager.GenerateFileName();
            }

            string tempPath = WriteTempPng(pngBytes, fileName);
            if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
            {
                const string err = "Failed to prepare temporary screenshot file for sharing.";
                Debug.LogError($"[NativeShareManager] {err}");
                onComplete?.Invoke(false, err);
                return;
            }

            FileInfo info = new FileInfo(tempPath);
            if (info.Length <= 0)
            {
                const string err = "Temporary screenshot file is empty.";
                Debug.LogError($"[NativeShareManager] {err}");
                onComplete?.Invoke(false, err);
                return;
            }

            LastSharedTempPath = tempPath;
            Debug.Log($"[NativeShareManager] Verified temp PNG for share: {tempPath} ({info.Length} bytes)");

#if UNITY_ANDROID && !UNITY_EDITOR
            ShareOnAndroid(tempPath, onComplete);
#elif UNITY_EDITOR
            Debug.Log($"[NativeShareManager] Native Share Sheet opens on Android devices. Temp PNG ready at: {tempPath}");
            onComplete?.Invoke(true, "Share ready (Android Native Share Sheet on device)");
#else
            Debug.Log($"[NativeShareManager] Native Share is supported on Android. Temp PNG saved at: {tempPath}");
            onComplete?.Invoke(false, "Native Share is only available on Android devices.");
#endif
        }

        public static string WriteTempPng(byte[] pngBytes, string fileName)
        {
            try
            {
                string cacheDir = Application.temporaryCachePath;
                if (string.IsNullOrEmpty(cacheDir))
                {
                    Debug.LogError("[NativeShareManager] Application.temporaryCachePath is empty.");
                    return string.Empty;
                }

                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                string safeFileName = Path.GetFileName(fileName);
                string fullPath = Path.Combine(cacheDir, safeFileName);
                File.WriteAllBytes(fullPath, pngBytes);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NativeShareManager] Failed to write temp PNG: {ex.Message}");
                return string.Empty;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void ShareOnAndroid(string filePath, Action<bool, string> onComplete)
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass shareBridge = new AndroidJavaClass("com.dreamdungeon.share.DreamDungeonShareBridge"))
                {
                    if (activity == null)
                    {
                        const string err = "Android currentActivity is null.";
                        Debug.LogError($"[NativeShareManager] {err}");
                        onComplete?.Invoke(false, "Failed to open Android Share Sheet.");
                        return;
                    }

                    string authority = $"{Application.identifier}.dreamdungeon.fileprovider";
                    bool launched = shareBridge.CallStatic<bool>(
                        "shareImage",
                        activity,
                        filePath,
                        authority,
                        ShareTitle,
                        ShareSubject,
                        ShareMessage);

                    if (launched)
                    {
                        Debug.Log($"[NativeShareManager] Android Native Share Sheet opened for: {filePath}");
                        onComplete?.Invoke(true, "Opening Share Sheet...");
                    }
                    else
                    {
                        Debug.LogError("[NativeShareManager] DreamDungeonShareBridge.shareImage returned false.");
                        onComplete?.Invoke(false, "Failed to open Android Share Sheet.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NativeShareManager] Android Share Intent failed: {ex.Message}");
                onComplete?.Invoke(false, "Failed to open Android Share Sheet.");
            }
        }
#endif
    }
}
