using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamDungeon
{
    public struct GoogleCredentialResult
    {
        public bool Success;
        public AuthErrorCode ErrorCode;
        public string IdToken;
        public string Email;
        public string Message;
    }

    public sealed class GoogleLoginManager : MonoBehaviour
    {
        public static GoogleLoginManager Instance { get; private set; }

        [Header("Android Google Sign-In Configuration (Fill in Unity Inspector / Dashboard)")]
        [Tooltip("Web Client ID from Google Cloud Console (OAuth 2.0) — do NOT hardcode secrets in source control.")]
        [SerializeField] private string googleWebClientId = "";

        public string GoogleWebClientId => googleWebClientId;

        /// <summary>
        /// Optional external delegate allowing Android Google Sign-In SDK bridge or automated tests
        /// to supply a real Google OAuth ID Token without modifying this manager.
        /// </summary>
        public Func<Task<GoogleCredentialResult>> CustomGoogleTokenProvider { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task<GoogleCredentialResult> RequestGoogleIdTokenAsync()
        {
            if (CustomGoogleTokenProvider != null)
            {
                return await CustomGoogleTokenProvider.Invoke();
            }

#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_SIGNIN_ENABLED
            // When Google Sign-In Android SDK (e.g. Google Play Games / GoogleSignIn plugin) is installed
            // and GOOGLE_SIGNIN_ENABLED is defined, invoke the native Android OAuth flow here using googleWebClientId.
            await Task.Yield();
            if (string.IsNullOrWhiteSpace(googleWebClientId))
            {
                return new GoogleCredentialResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.AuthenticationFailed,
                    Message = "Google Web Client ID is not configured in GoogleLoginManager."
                };
            }
            return new GoogleCredentialResult
            {
                Success = false,
                ErrorCode = AuthErrorCode.AuthenticationFailed,
                Message = "Attach your Android Google Sign-In SDK bridge to GoogleLoginManager.CustomGoogleTokenProvider."
            };
#else
            // Unity Editor / Local Testing Sandbox: generates a deterministic test Google ID Token for this device
            await Task.Yield();
            string sandboxToken = "EDITOR_GOOGLE_ID_TOKEN_" + SystemInfo.deviceUniqueIdentifier;
            return new GoogleCredentialResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                IdToken = sandboxToken,
                Email = "player.sandbox@gmail.com",
                Message = "Editor Sandbox Google ID Token acquired."
            };
#endif
        }

        public async Task<AuthOperationResult> SignInWithGoogleAsync()
        {
            GoogleCredentialResult cred = await RequestGoogleIdTokenAsync();
            if (!cred.Success)
            {
                return new AuthOperationResult
                {
                    Success = false,
                    ErrorCode = cred.ErrorCode,
                    Message = cred.Message
                };
            }

            AuthenticationManager auth = AuthenticationManager.Instance ?? FindAnyObjectByType<AuthenticationManager>();
            if (auth == null)
            {
                auth = gameObject.GetComponent<AuthenticationManager>() ?? gameObject.AddComponent<AuthenticationManager>();
            }

            return await auth.SignInWithGoogleIdTokenAsync(cred.IdToken, cred.Email);
        }
    }
}
