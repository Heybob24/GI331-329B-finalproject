using System;
using System.Threading.Tasks;
using UnityEngine;
#if UGS_CORE_ENABLED || UGS_AUTH_ENABLED || UGS_CLOUDSAVE_ENABLED
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif

namespace DreamDungeon
{
    public enum UgsInitState
    {
        Uninitialized,
        Initializing,
        InitializedLiveUgs,
        InitializedLocalFallback,
        Failed
    }

    public sealed class UGSInitializationManager : MonoBehaviour
    {
        public static UGSInitializationManager Instance { get; private set; }

        [Header("UGS Environment Configuration")]
        [Tooltip("Leave empty to use the default environment configured in Unity Dashboard (usually 'production').")]
        [SerializeField] private string environmentName = string.Empty;
        [SerializeField] private bool autoInitializeOnAwake = false;

        private UgsInitState state = UgsInitState.Uninitialized;
        private string lastError = string.Empty;
        private Task<bool> pendingInitTask;

        public UgsInitState State => state;
        public bool IsInitialized => state == UgsInitState.InitializedLiveUgs || state == UgsInitState.InitializedLocalFallback;
        public bool IsLiveUgsInitialized => state == UgsInitState.InitializedLiveUgs;
        public string LastError => lastError;
        public string EnvironmentName => string.IsNullOrWhiteSpace(environmentName) ? "default" : environmentName.Trim();

        /// <summary>
        /// Returns the Unity Cloud Project ID linked in Project Settings > Services (read-only).
        /// Never creates or modifies a Unity Cloud project.
        /// </summary>
        public string CloudProjectId => Application.cloudProjectId ?? string.Empty;
        public bool HasLinkedCloudProjectId => !string.IsNullOrWhiteSpace(CloudProjectId);

        public static bool IsLiveUgsCoreCompiled
        {
            get
            {
#if UGS_CORE_ENABLED || UGS_AUTH_ENABLED || UGS_CLOUDSAVE_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsLiveUgsAuthCompiled
        {
            get
            {
#if UGS_AUTH_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsLiveUgsCloudSaveCompiled
        {
            get
            {
#if UGS_CLOUDSAVE_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsLiveUgsCloudCodeCompiled
        {
            get
            {
#if UGS_CLOUDCODE_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsLiveUnityIapCompiled
        {
            get
            {
#if UNITY_IAP_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsGoogleSignInPluginCompiled
        {
            get
            {
#if GOOGLE_SIGNIN_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public event Action<UgsInitState> OnUgsInitialized;
        public event Action<string> OnUgsInitializationFailed;

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

            if (autoInitializeOnAwake)
            {
                _ = InitializeAsync();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Task<bool> InitializeAsync()
        {
            if (IsInitialized)
            {
                return Task.FromResult(true);
            }

            if (pendingInitTask != null && !pendingInitTask.IsCompleted)
            {
                return pendingInitTask;
            }

            pendingInitTask = InitializeInternalAsync();
            return pendingInitTask;
        }

        private async Task<bool> InitializeInternalAsync()
        {
            state = UgsInitState.Initializing;
            lastError = string.Empty;

#if UGS_CORE_ENABLED || UGS_AUTH_ENABLED || UGS_CLOUDSAVE_ENABLED
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    if (!string.IsNullOrWhiteSpace(environmentName))
                    {
                        var options = new InitializationOptions().SetEnvironmentName(environmentName.Trim());
                        await UnityServices.InitializeAsync(options);
                    }
                    else
                    {
                        await UnityServices.InitializeAsync();
                    }
                }

                state = UgsInitState.InitializedLiveUgs;
                Debug.Log($"[UGSInitializationManager] Unity Gaming Services initialized (ProjectId={CloudProjectId}, Env={EnvironmentName}).");
                OnUgsInitialized?.Invoke(state);
                return true;
            }
            catch (Exception ex)
            {
                state = UgsInitState.Failed;
                lastError = ex.Message;
                Debug.LogError($"[UGSInitializationManager] UnityServices.InitializeAsync() failed: {ex.Message}");
                OnUgsInitializationFailed?.Invoke(lastError);
                return false;
            }
#else
            await Task.Yield();
            state = UgsInitState.InitializedLocalFallback;
            Debug.LogWarning(
                $"[UGSInitializationManager] UGS SDK packages are not yet enabled in Scripting Define Symbols " +
                $"(ProjectId={(HasLinkedCloudProjectId ? CloudProjectId : "<empty>")}). Running in Local Sandbox Fallback mode.");
            OnUgsInitialized?.Invoke(state);
            return true;
#endif
        }

        public string GetSdkStatusSummary()
        {
            return $"State={state} | ProjectId={(HasLinkedCloudProjectId ? CloudProjectId : "UNLINKED")} | " +
                   $"Core={IsLiveUgsCoreCompiled} | Auth={IsLiveUgsAuthCompiled} | CloudSave={IsLiveUgsCloudSaveCompiled} | " +
                   $"CloudCode={IsLiveUgsCloudCodeCompiled} | IAP={IsLiveUnityIapCompiled} | GoogleSDK={IsGoogleSignInPluginCompiled}";
        }
    }
}
