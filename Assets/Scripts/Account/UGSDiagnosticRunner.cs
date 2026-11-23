using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamDungeon
{
    public enum DiagnosticCheckStatus
    {
        Pass,
        Warning,
        Fail
    }

    [Serializable]
    public sealed class DiagnosticCheckItem
    {
        public string label;
        public DiagnosticCheckStatus status;
        public string detail;
    }

    [Serializable]
    public sealed class UGSDiagnosticReport
    {
        public string timestampUtc = string.Empty;
        public bool isLiveUgsMode;
        public List<DiagnosticCheckItem> items = new List<DiagnosticCheckItem>();

        public string ToFormattedLog()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== DREAM DUNGEON — UGS CONNECTION DIAGNOSTICS ==========");
            sb.AppendLine($"Timestamp (UTC): {timestampUtc}");
            sb.AppendLine($"Runtime Mode   : {(isLiveUgsMode ? "LIVE UGS SDK" : "LOCAL SANDBOX FALLBACK (UGS SDK not enabled)")}");
            sb.AppendLine("----------------------------------------------------------------");
            foreach (var item in items)
            {
                string tag = item.status switch
                {
                    DiagnosticCheckStatus.Pass => "[PASS]",
                    DiagnosticCheckStatus.Warning => "[WARN]",
                    _ => "[FAIL]"
                };
                sb.AppendLine($"{tag} {item.label}: {item.detail}");
            }
            sb.AppendLine("================================================================");
            return sb.ToString();
        }
    }

    public static class UGSDiagnosticRunner
    {
        public static async Task<UGSDiagnosticReport> RunAllChecksAsync()
        {
            var report = new UGSDiagnosticReport
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                isLiveUgsMode = UGSInitializationManager.IsLiveUgsCoreCompiled &&
                                UGSInitializationManager.IsLiveUgsAuthCompiled &&
                                UGSInitializationManager.IsLiveUgsCloudSaveCompiled
            };

            // Ensure core managers exist
            UGSInitializationManager ugsInit = UGSInitializationManager.Instance;
            if (ugsInit == null)
            {
                ugsInit = new GameObject("UGSInitializationManager").AddComponent<UGSInitializationManager>();
            }

            AuthenticationManager auth = AuthenticationManager.Instance;
            if (auth == null)
            {
                auth = new GameObject("AuthenticationManager").AddComponent<AuthenticationManager>();
            }
            auth.EnsureCompanionManagers();

            PlayerDatabaseManager db = PlayerDatabaseManager.Instance;
            GoogleLoginManager googleMgr = auth.GetComponent<GoogleLoginManager>();

            // 1. UGS Initialized
            bool initOk = await ugsInit.InitializeAsync();
            if (initOk && ugsInit.IsLiveUgsInitialized)
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "1. UGS Initialized",
                    status = DiagnosticCheckStatus.Pass,
                    detail = $"UnityServices initialized live (Env={ugsInit.EnvironmentName})."
                });
            }
            else if (initOk)
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "1. UGS Initialized",
                    status = DiagnosticCheckStatus.Warning,
                    detail = "Initialized in Local Sandbox Fallback (install UGS packages & sync Define Symbols for live UGS)."
                });
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "1. UGS Initialized",
                    status = DiagnosticCheckStatus.Fail,
                    detail = $"Initialization failed: {ugsInit.LastError}"
                });
            }

            // 2. Project ID
            string projectId = ugsInit.CloudProjectId;
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "2. Project ID",
                    status = DiagnosticCheckStatus.Pass,
                    detail = projectId
                });
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "2. Project ID",
                    status = DiagnosticCheckStatus.Warning,
                    detail = "Application.cloudProjectId is empty at runtime (verify Project Settings > Services linking)."
                });
            }

            // 3. Guest Login Status & 4. Player ID
            bool hadExistingSession = auth.HasExistingSessionToken();
            AuthOperationResult guestRes = auth.IsSignedIn
                ? new AuthOperationResult { Success = true, Profile = auth.ActiveProfile, Message = "Already signed in." }
                : await auth.SignInOrRestoreGuestAsync();

            if (guestRes.Success && auth.IsSignedIn)
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "3. Guest Login Status",
                    status = UGSInitializationManager.IsLiveUgsAuthCompiled ? DiagnosticCheckStatus.Pass : DiagnosticCheckStatus.Warning,
                    detail = $"{(UGSInitializationManager.IsLiveUgsAuthCompiled ? "Live UGS Auth" : "Local Fallback")} — " +
                             $"Signed In ({(hadExistingSession ? "Restored Session" : "New Session")}, Type={auth.ActiveProfile.loginType})"
                });

                report.items.Add(new DiagnosticCheckItem
                {
                    label = "4. Player ID",
                    status = DiagnosticCheckStatus.Pass,
                    detail = auth.PlayerId
                });
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "3. Guest Login Status",
                    status = DiagnosticCheckStatus.Fail,
                    detail = guestRes.Message
                });

                report.items.Add(new DiagnosticCheckItem
                {
                    label = "4. Player ID",
                    status = DiagnosticCheckStatus.Fail,
                    detail = "Unavailable (not signed in)."
                });
            }

            // 5. Cloud Save Write
            if (auth.IsSignedIn && db != null)
            {
                long revBefore = db.CurrentData != null ? db.CurrentData.revision : 0;
                DatabaseOperationResult saveRes = await db.SaveCurrentPlayerDataAsync(captureFromGameSystems: true);
                if (saveRes.Success)
                {
                    long revAfter = saveRes.Data != null ? saveRes.Data.revision : revBefore;
                    report.items.Add(new DiagnosticCheckItem
                    {
                        label = "5. Cloud Save Write",
                        status = UGSInitializationManager.IsLiveUgsCloudSaveCompiled ? DiagnosticCheckStatus.Pass : DiagnosticCheckStatus.Warning,
                        detail = $"{(UGSInitializationManager.IsLiveUgsCloudSaveCompiled ? "Live Cloud Save" : "Local DB Fallback")} — " +
                                 $"Write OK (Rev {revBefore} -> {revAfter})."
                    });
                }
                else
                {
                    report.items.Add(new DiagnosticCheckItem
                    {
                        label = "5. Cloud Save Write",
                        status = DiagnosticCheckStatus.Fail,
                        detail = $"Write failed: {saveRes.Message}"
                    });
                }

                // 6. Cloud Save Read
                DatabaseOperationResult readRes = await db.LoadOrCreatePlayerDataAsync(auth.ActiveProfile);
                if (readRes.Success && readRes.Data != null &&
                    string.Equals(readRes.Data.accountData?.playerId, auth.PlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    report.items.Add(new DiagnosticCheckItem
                    {
                        label = "6. Cloud Save Read",
                        status = UGSInitializationManager.IsLiveUgsCloudSaveCompiled ? DiagnosticCheckStatus.Pass : DiagnosticCheckStatus.Warning,
                        detail = $"{(UGSInitializationManager.IsLiveUgsCloudSaveCompiled ? "Live Cloud Save" : "Local DB Fallback")} — " +
                                 $"Read OK (PlayerId={readRes.Data.accountData.playerId}, Rev={readRes.Data.revision}, Crystal={readRes.Data.currencyData.crystalBalance})."
                    });
                }
                else
                {
                    report.items.Add(new DiagnosticCheckItem
                    {
                        label = "6. Cloud Save Read",
                        status = DiagnosticCheckStatus.Fail,
                        detail = $"Read failed: {readRes.Message}"
                    });
                }
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "5. Cloud Save Write",
                    status = DiagnosticCheckStatus.Fail,
                    detail = "Skipped (not signed in or PlayerDatabaseManager missing)."
                });
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "6. Cloud Save Read",
                    status = DiagnosticCheckStatus.Fail,
                    detail = "Skipped (not signed in or PlayerDatabaseManager missing)."
                });
            }

            // 7. Google Provider Configuration
            bool hasWebClientId = googleMgr != null && !string.IsNullOrWhiteSpace(googleMgr.GoogleWebClientId);
            bool hasGoogleSdk = UGSInitializationManager.IsGoogleSignInPluginCompiled;
            if (hasWebClientId && hasGoogleSdk && UGSInitializationManager.IsLiveUgsAuthCompiled)
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "7. Google Provider Configuration",
                    status = DiagnosticCheckStatus.Pass,
                    detail = "Google Web Client ID configured + GOOGLE_SIGNIN_ENABLED + UGS_AUTH_ENABLED active."
                });
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "7. Google Provider Configuration",
                    status = DiagnosticCheckStatus.Warning,
                    detail = $"Pending setup — WebClientId={(hasWebClientId ? "Configured" : "Empty")}, " +
                             $"GOOGLE_SIGNIN_ENABLED={hasGoogleSdk}, UGS_AUTH_ENABLED={UGSInitializationManager.IsLiveUgsAuthCompiled}."
                });
            }

            // 8. Account Linking Status
            if (auth.IsSignedIn && auth.ActiveProfile != null)
            {
                bool linked = auth.ActiveProfile.IsGoogleLinked;
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "8. Account Linking Status",
                    status = DiagnosticCheckStatus.Pass,
                    detail = linked
                        ? $"Linked to Google (LoginType={auth.ActiveProfile.loginType}, PlayerId={auth.PlayerId})."
                        : $"Guest account ready to link (LoginType={auth.ActiveProfile.loginType}, PlayerId={auth.PlayerId})."
                });
            }
            else
            {
                report.items.Add(new DiagnosticCheckItem
                {
                    label = "8. Account Linking Status",
                    status = DiagnosticCheckStatus.Warning,
                    detail = "No active signed-in profile."
                });
            }

            Debug.Log(report.ToFormattedLog());
            return report;
        }
    }
}
