using System;
using System.Collections;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class ScreenshotCaptureData
    {
        public Texture2D PreviewTexture { get; }
        public byte[] PngBytes { get; }
        public string FileName { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsSaved { get; set; }
        public string SavedPath { get; set; }

        public ScreenshotCaptureData(Texture2D previewTexture, byte[] pngBytes, string fileName, int width, int height)
        {
            PreviewTexture = previewTexture;
            PngBytes = pngBytes;
            FileName = fileName;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Handles full-screen screenshot capture via WaitForEndOfFrame, PNG encoding,
    /// Texture2D memory management, and coordination with GalleryManager and NativeShareManager.
    /// </summary>
    public sealed class ScreenshotManager : MonoBehaviour
    {
        public const string FilePrefix = "DREAM_DUNGEON";

        public static ScreenshotManager Instance { get; private set; }

        public bool IsCapturing { get; private set; }
        public bool IsSaving { get; private set; }
        public bool IsSharing { get; private set; }
        public bool IsBusy => IsCapturing || IsSaving || IsSharing;
        public ScreenshotCaptureData LatestCapture { get; private set; }

        public event Action<ScreenshotCaptureData> ScreenshotCaptured;
        public event Action<bool, string, string> ScreenshotSaved;
        public event Action<bool, string> ScreenshotShared;

        public static ScreenshotManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            ScreenshotManager existing = FindAnyObjectByType<ScreenshotManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("ScreenshotManager");
            Instance = go.AddComponent<ScreenshotManager>();
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

            GalleryManager.EnsureInstance();
            NativeShareManager.EnsureInstance();
        }

        private void OnDestroy()
        {
            ReleaseLatestCapture();
            if (Instance == this) Instance = null;
        }

        public static string GenerateFileName()
        {
            return $"{FilePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        }

        public void CaptureScreenshot(
            Action<ScreenshotCaptureData> onCaptured = null,
            Action<string> onError = null,
            Action beforeCaptureHideUI = null,
            Action afterCaptureRestoreUI = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[ScreenshotManager] Capture ignored: another screenshot operation is already in progress.");
                return;
            }

            StartCoroutine(CaptureRoutine(onCaptured, onError, beforeCaptureHideUI, afterCaptureRestoreUI));
        }

        private IEnumerator CaptureRoutine(
            Action<ScreenshotCaptureData> onCaptured,
            Action<string> onError,
            Action beforeCaptureHideUI,
            Action afterCaptureRestoreUI)
        {
            IsCapturing = true;
            Texture2D capturedTexture = null;
            byte[] pngBytes = null;
            string errorMessage = null;
            int width = 0;
            int height = 0;

            try
            {
                beforeCaptureHideUI?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenshotManager] beforeCaptureHideUI threw: {ex.Message}");
            }

            yield return new WaitForEndOfFrame();

            try
            {
                width = Screen.width;
                height = Screen.height;

                if (width <= 0 || height <= 0)
                {
                    errorMessage = "Invalid screen dimensions for screenshot capture.";
                }
                else
                {
                    capturedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    capturedTexture.name = "DreamDungeon_ScreenshotPreview";
                    capturedTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                    capturedTexture.Apply(false, false);

                    pngBytes = capturedTexture.EncodeToPNG();
                    if (pngBytes == null || pngBytes.Length == 0)
                    {
                        errorMessage = "Failed to encode screenshot as PNG.";
                        Destroy(capturedTexture);
                        capturedTexture = null;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Screenshot capture failed: {ex.Message}";
                if (capturedTexture != null)
                {
                    Destroy(capturedTexture);
                    capturedTexture = null;
                }
            }
            finally
            {
                try
                {
                    afterCaptureRestoreUI?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ScreenshotManager] afterCaptureRestoreUI threw: {ex.Message}");
                }
                IsCapturing = false;
            }

            if (!string.IsNullOrEmpty(errorMessage) || capturedTexture == null || pngBytes == null)
            {
                string finalErr = string.IsNullOrEmpty(errorMessage) ? "Screenshot capture failed." : errorMessage;
                Debug.LogError($"[ScreenshotManager] {finalErr}");
                onError?.Invoke(finalErr);
                yield break;
            }

            // Release previous Texture2D to avoid memory leaks
            ReleaseLatestCapture();

            string fileName = GenerateFileName();
            LatestCapture = new ScreenshotCaptureData(capturedTexture, pngBytes, fileName, width, height);

            Debug.Log($"[ScreenshotManager] Captured screenshot {fileName} ({width}x{height}, {pngBytes.Length} bytes).");
            ScreenshotCaptured?.Invoke(LatestCapture);
            onCaptured?.Invoke(LatestCapture);
        }

        public void SaveLatestScreenshot(Action<bool, string, string> onComplete = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[ScreenshotManager] Save ignored: screenshot system is busy.");
                return;
            }

            if (LatestCapture == null || LatestCapture.PngBytes == null || LatestCapture.PngBytes.Length == 0)
            {
                const string msg = "No screenshot available to save.";
                Debug.LogWarning($"[ScreenshotManager] {msg}");
                onComplete?.Invoke(false, string.Empty, msg);
                return;
            }

            IsSaving = true;
            ScreenshotCaptureData capture = LatestCapture;
            GalleryManager gallery = GalleryManager.EnsureInstance();

            gallery.SavePng(capture.PngBytes, capture.FileName, (success, savedPath, message) =>
            {
                IsSaving = false;
                if (success)
                {
                    capture.IsSaved = true;
                    capture.SavedPath = savedPath;
                    Debug.Log($"[ScreenshotManager] Screenshot saved at: {savedPath}");
                }
                else
                {
                    Debug.LogError($"[ScreenshotManager] Save failed: {message}");
                }

                ScreenshotSaved?.Invoke(success, savedPath, message);
                onComplete?.Invoke(success, savedPath, message);
            });
        }

        public void ShareLatestScreenshot(Action<bool, string> onComplete = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[ScreenshotManager] Share ignored: screenshot system is busy.");
                return;
            }

            if (LatestCapture == null || LatestCapture.PngBytes == null || LatestCapture.PngBytes.Length == 0)
            {
                const string msg = "No screenshot available to share.";
                Debug.LogWarning($"[ScreenshotManager] {msg}");
                onComplete?.Invoke(false, msg);
                return;
            }

            IsSharing = true;
            ScreenshotCaptureData capture = LatestCapture;
            NativeShareManager shareManager = NativeShareManager.EnsureInstance();

            shareManager.SharePng(capture.PngBytes, capture.FileName, (success, message) =>
            {
                IsSharing = false;
                ScreenshotShared?.Invoke(success, message);
                onComplete?.Invoke(success, message);
            });
        }

        public void ReleaseLatestCapture()
        {
            if (LatestCapture != null)
            {
                if (LatestCapture.PreviewTexture != null)
                {
                    Destroy(LatestCapture.PreviewTexture);
                }
                LatestCapture = null;
            }
        }
    }
}
