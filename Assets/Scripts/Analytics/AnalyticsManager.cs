using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UGS_CORE_ENABLED || UGS_AUTH_ENABLED || UGS_CLOUDSAVE_ENABLED || UGS_ANALYTICS_ENABLED
using Unity.Services.Core;
#endif
#if UGS_ANALYTICS_ENABLED
using Unity.Services.Analytics;
#endif
#if UNITY_6000_2_OR_NEWER && ENABLE_UNITY_CONSENT
using UnityEngine.UnityConsent;
#endif

namespace DreamDungeon
{
    /// <summary>
    /// Centralized Unity Analytics manager for DREAM DUNGEON.
    /// Reuses UGSInitializationManager (never duplicates UnityServices.InitializeAsync),
    /// manages Unity 6 EndUserConsent / StartDataCollection, validates payloads,
    /// prevents duplicate events, and protects gameplay from any Analytics errors.
    /// </summary>
    public sealed class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private const string ConsentPrefKey = "DD_Analytics_ConsentGranted";
        private const int MaxLogHistory = 50;
        private const float GameplayDuplicateWindowSeconds = 0.5f;
        private const float TestDuplicateWindowSeconds = 0.15f;

        [Header("Analytics Settings")]
        [SerializeField] private bool grantConsentByDefault = true;
        [SerializeField] private bool flushAfterTestEvents = true;
        [SerializeField] private bool verboseConsoleLogging = true;

        private AnalyticsInitStatus initStatus = AnalyticsInitStatus.Uninitialized;
        private string lastError = string.Empty;
        private string analyticsUserId = string.Empty;
        private bool isConsentGranted = true;
        private bool isDataCollectionActive;
        private Task<bool> pendingInitTask;

        private readonly HashSet<string> recordedDeduplicationKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> lastEventRealtimeBySignature = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly List<AnalyticsEventLogEntry> eventHistory = new List<AnalyticsEventLogEntry>();

        public AnalyticsInitStatus InitStatus => initStatus;
        public bool IsInitialized => initStatus == AnalyticsInitStatus.Initialized || initStatus == AnalyticsInitStatus.InitializedOffline;
        public bool IsConsentGranted => isConsentGranted;
        public bool IsDataCollectionActive => isDataCollectionActive;
        public string LastError => lastError;
        public string AnalyticsUserId => analyticsUserId;
        public IReadOnlyList<AnalyticsEventLogEntry> EventHistory => eventHistory;
        public int RecordedEventCount { get; private set; }

        public static bool IsAnalyticsSdkCompiled
        {
            get
            {
#if UGS_ANALYTICS_ENABLED
                return true;
#else
                return false;
#endif
            }
        }

        public event Action<AnalyticsInitStatus> OnAnalyticsStatusChanged;
        public event Action<AnalyticsEventLogEntry> OnEventLogged;

        public static AnalyticsManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            AnalyticsManager existing = FindAnyObjectByType<AnalyticsManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }
            var go = new GameObject("AnalyticsManager");
            return go.AddComponent<AnalyticsManager>();
        }

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

            isConsentGranted = PlayerPrefs.GetInt(ConsentPrefKey, grantConsentByDefault ? 1 : 0) == 1;
        }

        private void Start()
        {
            // Hook into existing UGSInitializationManager if present; otherwise initialize cleanly.
            if (UGSInitializationManager.Instance != null)
            {
                UGSInitializationManager.Instance.OnUgsInitialized -= HandleUgsInitialized;
                UGSInitializationManager.Instance.OnUgsInitialized += HandleUgsInitialized;
            }

            _ = InitializeAsync();
        }

        private void OnDestroy()
        {
            if (UGSInitializationManager.Instance != null)
            {
                UGSInitializationManager.Instance.OnUgsInitialized -= HandleUgsInitialized;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleUgsInitialized(UgsInitState state)
        {
            if (!IsInitialized && initStatus != AnalyticsInitStatus.Initializing)
            {
                _ = InitializeAsync();
            }
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
            initStatus = AnalyticsInitStatus.Initializing;
            lastError = string.Empty;
            OnAnalyticsStatusChanged?.Invoke(initStatus);

#if !UGS_ANALYTICS_ENABLED
            await Task.Yield();
            initStatus = AnalyticsInitStatus.SdkNotEnabled;
            lastError = "UGS_ANALYTICS_ENABLED define symbol is not active or com.unity.services.analytics is not installed.";
            Debug.LogWarning($"[Analytics] {lastError}");
            OnAnalyticsStatusChanged?.Invoke(initStatus);
            return false;
#else
            try
            {
                // 1. Reuse existing UGSInitializationManager — never duplicate UnityServices.InitializeAsync().
                UGSInitializationManager ugsInit = UGSInitializationManager.Instance ?? FindAnyObjectByType<UGSInitializationManager>();
                if (ugsInit == null)
                {
                    ugsInit = new GameObject("UGSInitializationManager").AddComponent<UGSInitializationManager>();
                }

                bool ugsOk = await ugsInit.InitializeAsync();
                if (!ugsOk || !ugsInit.IsLiveUgsInitialized)
                {
                    // Offline or failed UGS init: do not crash gameplay.
                    bool offline = Application.internetReachability == NetworkReachability.NotReachable;
                    initStatus = offline ? AnalyticsInitStatus.InitializedOffline : AnalyticsInitStatus.Failed;
                    lastError = offline
                        ? "No internet connection during UGS initialization. Events will not upload until online."
                        : $"UGS initialization failed: {ugsInit.LastError}";
                    Debug.LogWarning($"[Analytics] {lastError}");
                    OnAnalyticsStatusChanged?.Invoke(initStatus);
                    return false;
                }

                // 2. Apply Consent & Start Data Collection according to installed Unity 6 + Analytics SDK 6.1+ API.
                ApplyConsentAndDataCollection(isConsentGranted);

                try
                {
                    analyticsUserId = AnalyticsService.Instance.GetAnalyticsUserID() ?? string.Empty;
                }
                catch
                {
                    analyticsUserId = string.Empty;
                }

                bool isOfflineNow = Application.internetReachability == NetworkReachability.NotReachable;
                initStatus = isOfflineNow ? AnalyticsInitStatus.InitializedOffline : AnalyticsInitStatus.Initialized;

                string projectId = string.IsNullOrWhiteSpace(Application.cloudProjectId) ? "<unlinked>" : Application.cloudProjectId;
                string envName = ugsInit.EnvironmentName;
                string collectEndpoint = $"https://collect.analytics.unity3d.com/api/analytics/collect/v2/projects/{projectId}/environments/{envName}";
                Debug.Log($"[Analytics] Initialized Successfully (ProjectId={projectId}, Env={envName}, Consent={isConsentGranted}, Offline={isOfflineNow})\n[Analytics] Collect Endpoint: {collectEndpoint}");

                OnAnalyticsStatusChanged?.Invoke(initStatus);
                return true;
            }
            catch (Exception ex)
            {
                initStatus = AnalyticsInitStatus.Failed;
                lastError = ex.Message;
                Debug.LogError($"[Analytics] Initialization Failed: {ex.Message}");
                OnAnalyticsStatusChanged?.Invoke(initStatus);
                return false;
            }
#endif
        }

        public void SetAnalyticsConsent(bool granted)
        {
            isConsentGranted = granted;
            PlayerPrefs.SetInt(ConsentPrefKey, granted ? 1 : 0);
            PlayerPrefs.Save();

            try
            {
                ApplyConsentAndDataCollection(granted);
                Debug.Log($"[Analytics] Consent updated: AnalyticsIntent={(granted ? "Granted" : "Denied")}");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Debug.LogError($"[Analytics] Failed to update consent: {ex.Message}");
            }
        }

        private void ApplyConsentAndDataCollection(bool granted)
        {
#if UGS_ANALYTICS_ENABLED
#if UNITY_6000_2_OR_NEWER && ENABLE_UNITY_CONSENT
            // Unity 6.2+ Developer Data Framework (EndUserConsent API)
            ConsentState consentState = EndUserConsent.GetConsentState();
            consentState.AnalyticsIntent = granted ? ConsentStatus.Granted : ConsentStatus.Denied;
            EndUserConsent.SetConsentState(consentState);
            isDataCollectionActive = granted;
#else
            // Unity 6.0 / 6.1 legacy Analytics consent API
            if (granted)
            {
                AnalyticsService.Instance.StartDataCollection();
                isDataCollectionActive = true;
            }
            else
            {
                AnalyticsService.Instance.StopDataCollection();
                isDataCollectionActive = false;
            }
#endif
#else
            isDataCollectionActive = false;
#endif
        }

        // =========================================================================
        // 5 CUSTOM EVENTS (Matching Unity Dashboard -> Analytics -> Event Manager)
        // =========================================================================

        /// <summary>
        /// EVENT 1: dungeon_created
        /// Parameters: dungeon_level (int), room_count (int)
        /// </summary>
        public bool TrackDungeonCreated(
            int dungeonLevel,
            int roomCount,
            AnalyticsEventSource source = AnalyticsEventSource.Gameplay,
            string deduplicationKey = null)
        {
            if (dungeonLevel < 1 || roomCount < 0)
            {
                RecordValidationFailure(DreamDungeonAnalyticsSchema.EventDungeonCreated, source,
                    $"Invalid parameters: dungeon_level={dungeonLevel}, room_count={roomCount}");
                return false;
            }

            var parameters = new Dictionary<string, object>(2)
            {
                { DreamDungeonAnalyticsSchema.ParamDungeonLevel, dungeonLevel },
                { DreamDungeonAnalyticsSchema.ParamRoomCount, roomCount }
            };

            return RecordCustomEventInternal(
                DreamDungeonAnalyticsSchema.EventDungeonCreated,
                parameters,
                source,
                deduplicationKey);
        }

        /// <summary>
        /// EVENT 2: raid_started
        /// Parameters: dungeon_level (int), monster_count (int), trap_count (int)
        /// </summary>
        public bool TrackRaidStarted(
            int dungeonLevel,
            int monsterCount,
            int trapCount,
            AnalyticsEventSource source = AnalyticsEventSource.Gameplay,
            string deduplicationKey = null)
        {
            if (dungeonLevel < 1 || monsterCount < 0 || trapCount < 0)
            {
                RecordValidationFailure(DreamDungeonAnalyticsSchema.EventRaidStarted, source,
                    $"Invalid parameters: dungeon_level={dungeonLevel}, monster_count={monsterCount}, trap_count={trapCount}");
                return false;
            }

            var parameters = new Dictionary<string, object>(3)
            {
                { DreamDungeonAnalyticsSchema.ParamDungeonLevel, dungeonLevel },
                { DreamDungeonAnalyticsSchema.ParamMonsterCount, monsterCount },
                { DreamDungeonAnalyticsSchema.ParamTrapCount, trapCount }
            };

            return RecordCustomEventInternal(
                DreamDungeonAnalyticsSchema.EventRaidStarted,
                parameters,
                source,
                deduplicationKey);
        }

        /// <summary>
        /// EVENT 3: raid_completed
        /// Parameters: wave (int), score (int), duration (float)
        /// </summary>
        public bool TrackRaidCompleted(
            int wave,
            int score,
            float duration,
            AnalyticsEventSource source = AnalyticsEventSource.Gameplay,
            string deduplicationKey = null)
        {
            if (wave < 0 || score < 0 || float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                RecordValidationFailure(DreamDungeonAnalyticsSchema.EventRaidCompleted, source,
                    $"Invalid parameters: wave={wave}, score={score}, duration={duration}");
                return false;
            }

            float sanitizedDuration = Mathf.Round(duration * 100f) / 100f;
            var parameters = new Dictionary<string, object>(3)
            {
                { DreamDungeonAnalyticsSchema.ParamWave, wave },
                { DreamDungeonAnalyticsSchema.ParamScore, score },
                { DreamDungeonAnalyticsSchema.ParamDuration, sanitizedDuration }
            };

            return RecordCustomEventInternal(
                DreamDungeonAnalyticsSchema.EventRaidCompleted,
                parameters,
                source,
                deduplicationKey);
        }

        /// <summary>
        /// EVENT 4: raid_failed
        /// Parameters: wave (int), core_hp (int), duration (float)
        /// </summary>
        public bool TrackRaidFailed(
            int wave,
            int coreHp,
            float duration,
            AnalyticsEventSource source = AnalyticsEventSource.Gameplay,
            string deduplicationKey = null)
        {
            if (wave < 0 || coreHp < 0 || float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                RecordValidationFailure(DreamDungeonAnalyticsSchema.EventRaidFailed, source,
                    $"Invalid parameters: wave={wave}, core_hp={coreHp}, duration={duration}");
                return false;
            }

            float sanitizedDuration = Mathf.Round(duration * 100f) / 100f;
            var parameters = new Dictionary<string, object>(3)
            {
                { DreamDungeonAnalyticsSchema.ParamWave, wave },
                { DreamDungeonAnalyticsSchema.ParamCoreHp, coreHp },
                { DreamDungeonAnalyticsSchema.ParamDuration, sanitizedDuration }
            };

            return RecordCustomEventInternal(
                DreamDungeonAnalyticsSchema.EventRaidFailed,
                parameters,
                source,
                deduplicationKey);
        }

        /// <summary>
        /// EVENT 5: crystal_spent
        /// Parameters: amount (int), item_id (string), balance_after (int)
        /// Must only be called AFTER the Crystal spend succeeds. Never send PII.
        /// </summary>
        public bool TrackCrystalSpent(
            int amount,
            string itemId,
            int balanceAfter,
            AnalyticsEventSource source = AnalyticsEventSource.Gameplay,
            string deduplicationKey = null)
        {
            string cleanItemId = SanitizeItemId(itemId);
            if (amount <= 0 || balanceAfter < 0 || string.IsNullOrEmpty(cleanItemId))
            {
                RecordValidationFailure(DreamDungeonAnalyticsSchema.EventCrystalSpent, source,
                    $"Invalid parameters: amount={amount}, item_id='{itemId}', balance_after={balanceAfter}");
                return false;
            }

            var parameters = new Dictionary<string, object>(3)
            {
                { DreamDungeonAnalyticsSchema.ParamAmount, amount },
                { DreamDungeonAnalyticsSchema.ParamItemId, cleanItemId },
                { DreamDungeonAnalyticsSchema.ParamBalanceAfter, balanceAfter }
            };

            return RecordCustomEventInternal(
                DreamDungeonAnalyticsSchema.EventCrystalSpent,
                parameters,
                source,
                deduplicationKey);
        }

        /// <summary>
        /// Requests the Unity Analytics SDK to flush queued events.
        /// Note: Flush initiates an async network upload; verify actual upload in Unity Analytics Debug Panel
        /// (or UNITY_ANALYTICS_EVENT_LOGS) and verify server receipt in Unity Dashboard -> Event Browser.
        /// </summary>
        public bool RequestFlush()
        {
#if UGS_ANALYTICS_ENABLED
            if (!IsInitialized || !isDataCollectionActive)
            {
                Debug.LogWarning("[Analytics] Flush skipped: Analytics is not initialized or consent is not granted.");
                return false;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("[Analytics] Flush skipped: No internet connection. SDK will retain events until online.");
                return false;
            }

            try
            {
                AnalyticsService.Instance.Flush();
                Debug.Log("[Analytics] Flush Requested (check Analytics Debug Panel / UNITY_ANALYTICS_EVENT_LOGS for upload status).");
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Debug.LogError($"[Analytics] Flush error: {ex.Message}");
                return false;
            }
#else
            Debug.LogWarning("[Analytics] Flush skipped: UGS_ANALYTICS_ENABLED is not compiled.");
            return false;
#endif
        }

        private bool RecordCustomEventInternal(
            string eventName,
            Dictionary<string, object> parameters,
            AnalyticsEventSource source,
            string deduplicationKey)
        {
            string payloadPreview = DreamDungeonAnalyticsSchema.FormatParametersJson(parameters);

            // 1. Deduplication check (idempotency key for gameplay results, plus rapid double-click guard)
            if (!string.IsNullOrWhiteSpace(deduplicationKey))
            {
                string scopedKey = $"{eventName}:{deduplicationKey.Trim()}";
                if (!recordedDeduplicationKeys.Add(scopedKey))
                {
                    AddLogEntry(eventName, source, AnalyticsEventRecordStatus.SkippedDuplicate, payloadPreview,
                        $"Duplicate key skipped ({deduplicationKey}).");
                    if (verboseConsoleLogging)
                    {
                        Debug.Log($"[Analytics] Duplicate Event Skipped: {eventName} (Key={deduplicationKey})");
                    }
                    return false;
                }
            }

            string signature = $"{source}:{eventName}:{payloadPreview}";
            float now = Time.realtimeSinceStartup;
            float cooldown = source == AnalyticsEventSource.TestPanel ? TestDuplicateWindowSeconds : GameplayDuplicateWindowSeconds;
            if (lastEventRealtimeBySignature.TryGetValue(signature, out float lastTime) && (now - lastTime) < cooldown)
            {
                AddLogEntry(eventName, source, AnalyticsEventRecordStatus.SkippedDuplicate, payloadPreview,
                    "Rapid duplicate suppressed.");
                return false;
            }
            lastEventRealtimeBySignature[signature] = now;

#if !UGS_ANALYTICS_ENABLED
            AddLogEntry(eventName, source, AnalyticsEventRecordStatus.SdkNotEnabled, payloadPreview,
                "UGS_ANALYTICS_ENABLED is not enabled.");
            Debug.LogWarning($"[Analytics] Cannot record '{eventName}': UGS_ANALYTICS_ENABLED is not compiled.");
            return false;
#else
            if (!IsInitialized)
            {
                AddLogEntry(eventName, source, AnalyticsEventRecordStatus.NotInitialized, payloadPreview,
                    $"Analytics not initialized (State={initStatus}).");
                Debug.LogWarning($"[Analytics] Event not recorded ({eventName}): Analytics is not initialized yet (State={initStatus}).");
                return false;
            }

            if (!isDataCollectionActive)
            {
                AddLogEntry(eventName, source, AnalyticsEventRecordStatus.NotInitialized, payloadPreview,
                    "Analytics data collection is disabled (consent denied).");
                Debug.LogWarning($"[Analytics] Event not recorded ({eventName}): Analytics consent is not granted.");
                return false;
            }

            try
            {
                var customEvent = new CustomEvent(eventName);
                foreach (var kv in parameters)
                {
                    customEvent[kv.Key] = kv.Value;
                }

                // Real Unity Analytics SDK call
                AnalyticsService.Instance.RecordEvent(customEvent);
                RecordedEventCount++;

                string sourceTag = source == AnalyticsEventSource.TestPanel ? "[TEST]" : "[GAMEPLAY]";
                bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;
                string note = isOffline
                    ? "Recorded by SDK (offline; queued locally until online)."
                    : "Recorded by SDK.";

                AddLogEntry(eventName, source, AnalyticsEventRecordStatus.Recorded, payloadPreview, note);

                if (verboseConsoleLogging)
                {
                    Debug.Log($"[Analytics] Event Recorded: {eventName} {sourceTag} | {payloadPreview}");
                }

                if (source == AnalyticsEventSource.TestPanel && flushAfterTestEvents && !isOffline)
                {
                    AnalyticsService.Instance.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                AddLogEntry(eventName, source, AnalyticsEventRecordStatus.Error, payloadPreview, ex.Message);
                Debug.LogError($"[Analytics] Failed to record event '{eventName}': {ex.Message}");
                return false;
            }
#endif
        }

        private void RecordValidationFailure(string eventName, AnalyticsEventSource source, string reason)
        {
            AddLogEntry(eventName, source, AnalyticsEventRecordStatus.ValidationFailed, "{}", reason);
            Debug.LogWarning($"[Analytics] Validation failed for '{eventName}': {reason}");
        }

        private void AddLogEntry(
            string eventName,
            AnalyticsEventSource source,
            AnalyticsEventRecordStatus status,
            string payloadPreview,
            string note)
        {
            var entry = new AnalyticsEventLogEntry
            {
                timestampUtc = DateTime.UtcNow.ToString("HH:mm:ss"),
                eventName = eventName,
                source = source,
                status = status,
                payloadPreview = payloadPreview,
                note = note
            };

            eventHistory.Add(entry);
            if (eventHistory.Count > MaxLogHistory)
            {
                eventHistory.RemoveAt(0);
            }

            OnEventLogged?.Invoke(entry);
        }

        private static string SanitizeItemId(string rawItemId)
        {
            if (string.IsNullOrWhiteSpace(rawItemId)) return "unknown_item";

            string trimmed = rawItemId.Trim();
            // Reject accidental email/PII patterns
            if (trimmed.Contains("@")) return "sanitized_item";

            if (trimmed.Length > 64)
            {
                trimmed = trimmed.Substring(0, 64);
            }
            return trimmed;
        }
    }
}
