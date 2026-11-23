using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if UGS_AUTH_ENABLED
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif

namespace DreamDungeon
{
    public struct AuthOperationResult
    {
        public bool Success;
        public AuthErrorCode ErrorCode;
        public string Message;
        public PlayerProfile Profile;
        public string ConflictingPlayerId;
    }

    public sealed class AuthenticationManager : MonoBehaviour
    {
        public static AuthenticationManager Instance { get; private set; }

        private const string LocalSessionPlayerIdKey = "DD_UGS_CachedSessionPlayerId";
        private const string LocalSessionLoginTypeKey = "DD_UGS_CachedSessionLoginType";

        [Serializable]
        private sealed class LocalAuthMappingEntry
        {
            public string googleTokenHash;
            public string playerId;
        }

        [Serializable]
        private sealed class LocalAuthRegistry
        {
            public List<LocalAuthMappingEntry> googleLinks = new List<LocalAuthMappingEntry>();
        }

        private bool isInitialized;
        private bool isSignedIn;
        private bool isBusy;
        private PlayerProfile activeProfile;

        public bool IsInitialized => isInitialized;
        public bool IsSignedIn => isSignedIn && activeProfile != null && !string.IsNullOrEmpty(activeProfile.playerId);
        public bool IsBusy => isBusy;
        public PlayerProfile ActiveProfile => activeProfile;
        public string PlayerId => activeProfile != null ? activeProfile.playerId : string.Empty;

        public event Action<PlayerProfile> OnSignedIn;
        public event Action OnSignedOut;
        public event Action<AuthErrorCode, string> OnAuthError;
        public event Action<PlayerProfile> OnProfileUpdated;

        private string LocalRegistryPath => Path.Combine(Application.persistentDataPath, "DreamDungeonCloudDB", "auth_registry.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
                return;
            }
            Instance = this;
            if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureCompanionManagers();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureCompanionManagers()
        {
            if (UGSInitializationManager.Instance == null && GetComponent<UGSInitializationManager>() == null)
                gameObject.AddComponent<UGSInitializationManager>();
            if (PlayerDatabaseManager.Instance == null && GetComponent<PlayerDatabaseManager>() == null)
                gameObject.AddComponent<PlayerDatabaseManager>();
            if (CrystalManager.Instance == null && GetComponent<CrystalManager>() == null)
                gameObject.AddComponent<CrystalManager>();
            if (CrystalTransactionManager.Instance == null && GetComponent<CrystalTransactionManager>() == null)
                gameObject.AddComponent<CrystalTransactionManager>();
            if (GetComponent<GuestLoginManager>() == null)
                gameObject.AddComponent<GuestLoginManager>();
            if (GetComponent<GoogleLoginManager>() == null)
                gameObject.AddComponent<GoogleLoginManager>();
            if (GetComponent<AccountLinkManager>() == null)
                gameObject.AddComponent<AccountLinkManager>();
        }

        public async Task<bool> InitializeServicesAsync()
        {
            if (isInitialized) return true;
            EnsureCompanionManagers();

            UGSInitializationManager ugsInit = UGSInitializationManager.Instance ?? GetComponent<UGSInitializationManager>();
            bool ok = ugsInit != null ? await ugsInit.InitializeAsync() : true;
            if (!ok)
            {
                return false;
            }

            isInitialized = true;
            return true;
        }

        public bool HasExistingSessionToken()
        {
#if UGS_AUTH_ENABLED
            return isInitialized && AuthenticationService.Instance.SessionTokenExists;
#else
            return !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(LocalSessionPlayerIdKey, string.Empty));
#endif
        }

        public async Task<AuthOperationResult> SignInOrRestoreGuestAsync()
        {
            if (isBusy) return Fail(AuthErrorCode.AuthenticationFailed, "Authentication is already in progress.");
            await InitializeServicesAsync();

            if (PlayerDatabaseManager.Instance != null && !PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                return Fail(AuthErrorCode.NoInternet, "No internet connection. Unable to sign in.");
            }

            isBusy = true;
            try
            {
                string ugsPlayerId;
                AuthLoginType resolvedType = AuthLoginType.Guest;

#if UGS_AUTH_ENABLED
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    // Restores existing session if SessionTokenExists; otherwise creates a new Anonymous account
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                ugsPlayerId = AuthenticationService.Instance.PlayerId;
                bool hasGoogleLinked = AuthenticationService.Instance.PlayerInfo?.Identities != null &&
                                       AuthenticationService.Instance.PlayerInfo.Identities.Exists(id =>
                                           string.Equals(id.TypeId, "google.com", StringComparison.OrdinalIgnoreCase));
                resolvedType = hasGoogleLinked ? AuthLoginType.GoogleLinked : AuthLoginType.Guest;
#else
                await Task.Yield();
                string cachedId = PlayerPrefs.GetString(LocalSessionPlayerIdKey, string.Empty);
                int cachedTypeInt = PlayerPrefs.GetInt(LocalSessionLoginTypeKey, (int)AuthLoginType.Guest);

                if (!string.IsNullOrWhiteSpace(cachedId))
                {
                    ugsPlayerId = cachedId.Trim();
                    resolvedType = (AuthLoginType)cachedTypeInt;
                }
                else
                {
                    ugsPlayerId = "UGS_" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
                    resolvedType = AuthLoginType.Guest;
                    PlayerPrefs.SetString(LocalSessionPlayerIdKey, ugsPlayerId);
                    PlayerPrefs.SetInt(LocalSessionLoginTypeKey, (int)resolvedType);
                    PlayerPrefs.Save();
                }
#endif

                return await FinalizeSignInAndLoadDatabaseAsync(ugsPlayerId, resolvedType);
            }
            catch (Exception ex)
            {
                return Fail(AuthErrorCode.AuthenticationFailed, $"Guest authentication failed: {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task<AuthOperationResult> SignInWithGoogleIdTokenAsync(string googleIdToken, string optionalGoogleEmail = "")
        {
            if (isBusy) return Fail(AuthErrorCode.AuthenticationFailed, "Authentication is already in progress.");
            await InitializeServicesAsync();

            if (PlayerDatabaseManager.Instance != null && !PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                return Fail(AuthErrorCode.NoInternet, "No internet connection. Google Sign-In unavailable.");
            }

            if (string.IsNullOrWhiteSpace(googleIdToken))
            {
                return Fail(AuthErrorCode.GoogleSignInCancelled, "Google Sign-In was cancelled or returned an empty token.");
            }

            if (googleIdToken.StartsWith("EXPIRED_", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(AuthErrorCode.GoogleTokenExpired, "Google Sign-In token has expired. Please sign in again.");
            }

            isBusy = true;
            try
            {
                string ugsPlayerId;

#if UGS_AUTH_ENABLED
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut(true);
                }
                await AuthenticationService.Instance.SignInWithGoogleAsync(googleIdToken);
                ugsPlayerId = AuthenticationService.Instance.PlayerId;
#else
                await Task.Yield();
                string tokenHash = ComputeTokenHash(googleIdToken);
                LocalAuthRegistry registry = LoadLocalRegistry();
                LocalAuthMappingEntry existing = registry.googleLinks.Find(e =>
                    e != null && string.Equals(e.googleTokenHash, tokenHash, StringComparison.Ordinal));

                if (existing != null && !string.IsNullOrWhiteSpace(existing.playerId))
                {
                    ugsPlayerId = existing.playerId;
                }
                else
                {
                    // Never use Google email/name as Player ID — generate a stable UGS-style Player ID
                    ugsPlayerId = "UGS_" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
                    registry.googleLinks.Add(new LocalAuthMappingEntry
                    {
                        googleTokenHash = tokenHash,
                        playerId = ugsPlayerId
                    });
                    SaveLocalRegistry(registry);
                }

                PlayerPrefs.SetString(LocalSessionPlayerIdKey, ugsPlayerId);
                PlayerPrefs.SetInt(LocalSessionLoginTypeKey, (int)AuthLoginType.Google);
                PlayerPrefs.Save();
#endif

                var result = await FinalizeSignInAndLoadDatabaseAsync(ugsPlayerId, AuthLoginType.Google);
                if (result.Success && activeProfile != null && !string.IsNullOrWhiteSpace(optionalGoogleEmail))
                {
                    activeProfile.googleAccountEmail = optionalGoogleEmail;
                    OnProfileUpdated?.Invoke(activeProfile);
                }
                return result;
            }
            catch (Exception ex)
            {
                return Fail(AuthErrorCode.AuthenticationFailed, $"Google Sign-In failed: {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task<AuthOperationResult> LinkCurrentAccountWithGoogleIdTokenAsync(string googleIdToken, string optionalGoogleEmail = "")
        {
            if (isBusy) return Fail(AuthErrorCode.LinkAccountFailed, "Another authentication request is in progress.");
            if (!IsSignedIn) return Fail(AuthErrorCode.LinkAccountFailed, "You must be signed in as Guest before linking a Google account.");

            if (PlayerDatabaseManager.Instance != null && !PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                return Fail(AuthErrorCode.NoInternet, "No internet connection. Cannot link Google Account offline.");
            }

            if (string.IsNullOrWhiteSpace(googleIdToken))
            {
                return Fail(AuthErrorCode.GoogleSignInCancelled, "Google Sign-In was cancelled.");
            }

            if (googleIdToken.StartsWith("EXPIRED_", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(AuthErrorCode.GoogleTokenExpired, "Google token expired. Please try linking again.");
            }

            isBusy = true;
            try
            {
                string currentGuestPlayerId = activeProfile.playerId;

#if UGS_AUTH_ENABLED
                try
                {
                    await AuthenticationService.Instance.LinkWithGoogleAsync(googleIdToken);
                }
                catch (AuthenticationException authEx) when (authEx.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    return new AuthOperationResult
                    {
                        Success = false,
                        ErrorCode = AuthErrorCode.AccountAlreadyLinked,
                        Message = "This Google account is already linked to another DREAM DUNGEON account. Your Guest data has been kept safe.",
                        Profile = activeProfile
                    };
                }
#else
                await Task.Yield();
                string tokenHash = ComputeTokenHash(googleIdToken);
                LocalAuthRegistry registry = LoadLocalRegistry();
                LocalAuthMappingEntry existing = registry.googleLinks.Find(e =>
                    e != null && string.Equals(e.googleTokenHash, tokenHash, StringComparison.Ordinal));

                if (existing != null && !string.Equals(existing.playerId, currentGuestPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    // Account Already Linked conflict: do NOT overwrite or delete Guest data!
                    return new AuthOperationResult
                    {
                        Success = false,
                        ErrorCode = AuthErrorCode.AccountAlreadyLinked,
                        Message = "This Google account is already linked to another DREAM DUNGEON profile. You can cancel to keep your Guest account, or switch to the existing Google account.",
                        Profile = activeProfile,
                        ConflictingPlayerId = existing.playerId
                    };
                }

                if (existing == null)
                {
                    registry.googleLinks.Add(new LocalAuthMappingEntry
                    {
                        googleTokenHash = tokenHash,
                        playerId = currentGuestPlayerId
                    });
                    SaveLocalRegistry(registry);
                }

                PlayerPrefs.SetString(LocalSessionPlayerIdKey, currentGuestPlayerId);
                PlayerPrefs.SetInt(LocalSessionLoginTypeKey, (int)AuthLoginType.GoogleLinked);
                PlayerPrefs.Save();
#endif

                // Preserve Guest Player ID, Crystal Balance, Dungeon Data, Progression, and Purchase History
                activeProfile.loginType = AuthLoginType.GoogleLinked;
                if (!string.IsNullOrWhiteSpace(optionalGoogleEmail))
                {
                    activeProfile.googleAccountEmail = optionalGoogleEmail;
                }

                if (PlayerDatabaseManager.Instance?.CurrentData?.accountData != null)
                {
                    var acc = PlayerDatabaseManager.Instance.CurrentData.accountData;
                    acc.loginType = AuthLoginType.GoogleLinked;
                    acc.authenticationProvider = "Google";
                    acc.linkedGoogleSubjectHash = ComputeTokenHash(googleIdToken);
                    acc.lastLoginUtc = DateTime.UtcNow.ToString("o");
                    await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: true);
                }

                OnProfileUpdated?.Invoke(activeProfile);
                return new AuthOperationResult
                {
                    Success = true,
                    ErrorCode = AuthErrorCode.None,
                    Message = "Google Account linked successfully! All Guest progress and Crystals were preserved.",
                    Profile = activeProfile
                };
            }
            catch (Exception ex)
            {
                return Fail(AuthErrorCode.LinkAccountFailed, $"Failed to link Google Account: {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task LogoutAsync(bool clearCachedSession = true)
        {
            if (IsSignedIn && PlayerDatabaseManager.Instance != null && PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: true);
            }

#if UGS_AUTH_ENABLED
            if (isInitialized && AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut(clearCachedSession);
            }
#else
            if (clearCachedSession)
            {
                PlayerPrefs.DeleteKey(LocalSessionPlayerIdKey);
                PlayerPrefs.DeleteKey(LocalSessionLoginTypeKey);
                PlayerPrefs.Save();
            }
#endif

            // Logout does NOT delete player data on the server
            isSignedIn = false;
            activeProfile = null;
            PlayerDatabaseManager.Instance?.ClearActivePlayerDataInMemory();
            CrystalManager.Instance?.ResetInMemory();
            OnSignedOut?.Invoke();
        }

        public async Task<AuthOperationResult> DeleteCurrentAccountAsync()
        {
            if (!IsSignedIn) return Fail(AuthErrorCode.AuthenticationFailed, "No signed-in account to delete.");
            if (PlayerDatabaseManager.Instance != null && !PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                return Fail(AuthErrorCode.NoInternet, "No internet connection. Cannot delete account while offline.");
            }

            string targetPlayerId = activeProfile.playerId;

            if (PlayerDatabaseManager.Instance != null)
            {
                await PlayerDatabaseManager.Instance.DeletePlayerDataAsync(targetPlayerId);
            }

#if UGS_AUTH_ENABLED
            await AuthenticationService.Instance.DeleteAccountAsync();
#else
            LocalAuthRegistry registry = LoadLocalRegistry();
            registry.googleLinks.RemoveAll(e => e != null && string.Equals(e.playerId, targetPlayerId, StringComparison.OrdinalIgnoreCase));
            SaveLocalRegistry(registry);
            PlayerPrefs.DeleteKey(LocalSessionPlayerIdKey);
            PlayerPrefs.DeleteKey(LocalSessionLoginTypeKey);
            PlayerPrefs.Save();
#endif

            isSignedIn = false;
            activeProfile = null;
            CrystalManager.Instance?.ResetInMemory();
            OnSignedOut?.Invoke();

            return new AuthOperationResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                Message = "Account and associated Player Database records have been permanently deleted."
            };
        }

        private async Task<AuthOperationResult> FinalizeSignInAndLoadDatabaseAsync(string ugsPlayerId, AuthLoginType loginType)
        {
            PlayerProfile initialProfile = PlayerProfile.CreateNew(ugsPlayerId, loginType);

            var dbRes = await PlayerDatabaseManager.Instance.LoadOrCreatePlayerDataAsync(initialProfile);
            if (!dbRes.Success)
            {
                return Fail(dbRes.ErrorCode, dbRes.Message);
            }

            activeProfile = dbRes.Data != null ? dbRes.Data.ToProfile() : initialProfile;
            if (loginType == AuthLoginType.GoogleLinked && activeProfile.loginType == AuthLoginType.Guest)
            {
                activeProfile.loginType = AuthLoginType.GoogleLinked;
            }
            isSignedIn = true;

            // Recover any pending IAP purchases for this Player ID
            if (CrystalTransactionManager.Instance != null)
            {
                await CrystalTransactionManager.Instance.RecoverPendingPurchasesAsync();
                if (PlayerDatabaseManager.Instance.CurrentData != null)
                {
                    activeProfile.crystalBalance = PlayerDatabaseManager.Instance.CurrentData.currencyData.crystalBalance;
                }
            }

            OnSignedIn?.Invoke(activeProfile);
            OnProfileUpdated?.Invoke(activeProfile);

            return new AuthOperationResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                Message = $"Signed in as {activeProfile.playerName} ({activeProfile.GetLoginTypeDisplayName()}).",
                Profile = activeProfile
            };
        }

        private AuthOperationResult Fail(AuthErrorCode code, string message)
        {
            OnAuthError?.Invoke(code, message);
            return new AuthOperationResult
            {
                Success = false,
                ErrorCode = code,
                Message = message,
                Profile = activeProfile
            };
        }

        private LocalAuthRegistry LoadLocalRegistry()
        {
            if (!File.Exists(LocalRegistryPath)) return new LocalAuthRegistry();
            try
            {
                string json = File.ReadAllText(LocalRegistryPath);
                return JsonUtility.FromJson<LocalAuthRegistry>(json) ?? new LocalAuthRegistry();
            }
            catch
            {
                return new LocalAuthRegistry();
            }
        }

        private void SaveLocalRegistry(LocalAuthRegistry registry)
        {
            string dir = Path.GetDirectoryName(LocalRegistryPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(LocalRegistryPath, JsonUtility.ToJson(registry, true));
        }

        public static string ComputeTokenHash(string token)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
