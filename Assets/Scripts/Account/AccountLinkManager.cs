using System.Threading.Tasks;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class AccountLinkManager : MonoBehaviour
    {
        public static AccountLinkManager Instance { get; private set; }

        private string lastConflictingGoogleIdToken = string.Empty;
        private string lastConflictingGoogleEmail = string.Empty;

        public bool HasPendingConflict => !string.IsNullOrEmpty(lastConflictingGoogleIdToken);

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

        public async Task<AuthOperationResult> LinkGuestWithGoogleAsync()
        {
            lastConflictingGoogleIdToken = string.Empty;
            lastConflictingGoogleEmail = string.Empty;

            GoogleLoginManager googleMgr = GoogleLoginManager.Instance ?? FindAnyObjectByType<GoogleLoginManager>();
            if (googleMgr == null)
            {
                googleMgr = gameObject.GetComponent<GoogleLoginManager>() ?? gameObject.AddComponent<GoogleLoginManager>();
            }

            GoogleCredentialResult cred = await googleMgr.RequestGoogleIdTokenAsync();
            if (!cred.Success)
            {
                return new AuthOperationResult
                {
                    Success = false,
                    ErrorCode = cred.ErrorCode,
                    Message = cred.Message
                };
            }

            return await LinkGuestWithGoogleIdTokenAsync(cred.IdToken, cred.Email);
        }

        public async Task<AuthOperationResult> LinkGuestWithGoogleIdTokenAsync(string googleIdToken, string googleEmail = "")
        {
            AuthenticationManager auth = AuthenticationManager.Instance ?? FindAnyObjectByType<AuthenticationManager>();
            if (auth == null)
            {
                return new AuthOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.LinkAccountFailed,
                    Message = "AuthenticationManager is unavailable."
                };
            }

            AuthOperationResult res = await auth.LinkCurrentAccountWithGoogleIdTokenAsync(googleIdToken, googleEmail);
            if (!res.Success && res.ErrorCode == AuthErrorCode.AccountAlreadyLinked)
            {
                lastConflictingGoogleIdToken = googleIdToken;
                lastConflictingGoogleEmail = googleEmail;
            }
            return res;
        }

        public void CancelConflictKeepGuest()
        {
            lastConflictingGoogleIdToken = string.Empty;
            lastConflictingGoogleEmail = string.Empty;
        }

        public async Task<AuthOperationResult> SwitchToExistingGoogleAccountFromConflictAsync()
        {
            if (string.IsNullOrEmpty(lastConflictingGoogleIdToken))
            {
                return new AuthOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.AuthenticationFailed,
                    Message = "No pending Google account conflict to switch to."
                };
            }

            string token = lastConflictingGoogleIdToken;
            string email = lastConflictingGoogleEmail;
            lastConflictingGoogleIdToken = string.Empty;
            lastConflictingGoogleEmail = string.Empty;

            AuthenticationManager auth = AuthenticationManager.Instance ?? FindAnyObjectByType<AuthenticationManager>();
            if (auth == null)
            {
                return new AuthOperationResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.AuthenticationFailed,
                    Message = "AuthenticationManager is unavailable."
                };
            }

            // Save current Guest state first so Guest data is never lost on the server
            await auth.LogoutAsync(clearCachedSession: true);
            return await auth.SignInWithGoogleIdTokenAsync(token, email);
        }
    }
}
