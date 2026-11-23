using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DreamDungeon.Editor
{
    public sealed class UGSDiagnosticWindow : EditorWindow
    {
        private sealed class PackageCheck
        {
            public string PackageName;
            public string DisplayName;
            public string DefineSymbol;
            public bool IsInstalled;
            public string InstalledVersion;
            public bool IsDefineActive;
        }

        private string cloudProjectId = string.Empty;
        private string organizationId = string.Empty;
        private string projectName = string.Empty;
        private string cloudEnabledRaw = string.Empty;

        private readonly List<PackageCheck> packageChecks = new List<PackageCheck>();
        private UGSDiagnosticReport latestReport;
        private bool isRunningDiagnostics;
        private Vector2 scrollPos;

        [MenuItem("Dream Dungeon/UGS Dashboard Diagnostics & Setup")]
        public static void ShowWindow()
        {
            var win = GetWindow<UGSDiagnosticWindow>("UGS Diagnostics");
            win.minSize = new Vector2(620f, 520f);
            win.RefreshInspection();
        }

        private void OnEnable()
        {
            RefreshInspection();
        }

        private void RefreshInspection()
        {
            InspectProjectSettings();
            InspectPackagesAndDefines();
            Repaint();
        }

        private void InspectProjectSettings()
        {
            cloudProjectId = string.Empty;
            organizationId = string.Empty;
            projectName = string.Empty;
            cloudEnabledRaw = string.Empty;

            string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "ProjectSettings.asset");
            if (!File.Exists(settingsPath)) return;

            string text = File.ReadAllText(settingsPath);
            cloudProjectId = ExtractYamlValue(text, "cloudProjectId");
            organizationId = ExtractYamlValue(text, "organizationId");
            projectName = ExtractYamlValue(text, "projectName");
            cloudEnabledRaw = ExtractYamlValue(text, "cloudEnabled");
        }

        private static string ExtractYamlValue(string content, string key)
        {
            Match m = Regex.Match(content, $@"^\s*{Regex.Escape(key)}:\s*(.*)$", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private void InspectPackagesAndDefines()
        {
            packageChecks.Clear();
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            string manifestText = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : string.Empty;

            HashSet<string> activeDefines = GetActiveDefineSymbols();

            AddPackageCheck(manifestText, activeDefines, "com.unity.services.core", "UGS Core", "UGS_CORE_ENABLED");
            AddPackageCheck(manifestText, activeDefines, "com.unity.services.authentication", "UGS Authentication", "UGS_AUTH_ENABLED");
            AddPackageCheck(manifestText, activeDefines, "com.unity.services.cloudsave", "UGS Cloud Save", "UGS_CLOUDSAVE_ENABLED");
            AddPackageCheck(manifestText, activeDefines, "com.unity.services.cloudcode", "UGS Cloud Code (Optional)", "UGS_CLOUDCODE_ENABLED");
            AddPackageCheck(manifestText, activeDefines, "com.unity.purchasing", "Unity IAP (Optional)", "UNITY_IAP_ENABLED");
        }

        private void AddPackageCheck(string manifestText, HashSet<string> activeDefines, string pkgName, string displayName, string defineSymbol)
        {
            Match m = Regex.Match(manifestText, $@"""{Regex.Escape(pkgName)}""\s*:\s*""([^""]+)""");
            bool installed = m.Success;
            string version = installed ? m.Groups[1].Value : "Not Installed";
            packageChecks.Add(new PackageCheck
            {
                PackageName = pkgName,
                DisplayName = displayName,
                DefineSymbol = defineSymbol,
                IsInstalled = installed,
                InstalledVersion = version,
                IsDefineActive = activeDefines.Contains(defineSymbol)
            });
        }

        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown)
            {
                group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            }
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }

        private static HashSet<string> GetActiveDefineSymbols()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            string raw = PlayerSettings.GetScriptingDefineSymbols(GetCurrentNamedBuildTarget());
            if (string.IsNullOrWhiteSpace(raw)) return set;

            string[] parts = raw.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string p in parts)
            {
                set.Add(p.Trim());
            }
            return set;
        }

        private void SyncDefineSymbolsWithInstalledPackages()
        {
            InspectPackagesAndDefines();
            NamedBuildTarget target = GetCurrentNamedBuildTarget();
            HashSet<string> defines = GetActiveDefineSymbols();

            bool changed = false;
            foreach (var pkg in packageChecks)
            {
                if (pkg.IsInstalled && !defines.Contains(pkg.DefineSymbol))
                {
                    defines.Add(pkg.DefineSymbol);
                    changed = true;
                }
                else if (!pkg.IsInstalled && defines.Contains(pkg.DefineSymbol))
                {
                    defines.Remove(pkg.DefineSymbol);
                    changed = true;
                }
            }

            if (changed)
            {
                string joined = string.Join(";", defines);
                PlayerSettings.SetScriptingDefineSymbols(target, joined);
                Debug.Log($"[UGSDiagnosticWindow] Updated Scripting Define Symbols for {target.TargetName}: {joined}");
            }
            else
            {
                Debug.Log("[UGSDiagnosticWindow] Scripting Define Symbols are already in sync with Packages/manifest.json.");
            }

            RefreshInspection();
        }

        private async void RunDiagnosticsAsync()
        {
            if (isRunningDiagnostics) return;
            isRunningDiagnostics = true;
            Repaint();
            try
            {
                latestReport = await UGSDiagnosticRunner.RunAllChecksAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGSDiagnosticWindow] Diagnostic run failed: {ex.Message}");
            }
            finally
            {
                isRunningDiagnostics = false;
                Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("DREAM DUNGEON — Unity Gaming Services (UGS) Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool inspects your existing Unity Project Linking and installed UGS packages (read-only). " +
                "It will NEVER create a new Unity Cloud project or modify your Project ID.",
                MessageType.Info);

            EditorGUILayout.Space(6);

            // 1. Project Linking Section
            EditorGUILayout.LabelField("1. Unity Project Linking (ProjectSettings.asset)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Cloud Project ID:", string.IsNullOrEmpty(cloudProjectId) ? "<NOT LINKED>" : cloudProjectId);
                EditorGUILayout.LabelField("Organization ID:", string.IsNullOrEmpty(organizationId) ? "<empty>" : organizationId);
                EditorGUILayout.LabelField("Project Name:", string.IsNullOrEmpty(projectName) ? "<empty>" : projectName);
                EditorGUILayout.LabelField("cloudEnabled Flag:", string.IsNullOrEmpty(cloudEnabledRaw) ? "<unknown>" : cloudEnabledRaw);
            }

            EditorGUILayout.Space(6);

            // 2. Installed Packages & Define Symbols Section
            EditorGUILayout.LabelField("2. UGS SDK Packages & Scripting Define Symbols", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                foreach (var pkg in packageChecks)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(pkg.DisplayName, GUILayout.Width(175));
                        EditorGUILayout.LabelField(pkg.IsInstalled ? $"v{pkg.InstalledVersion}" : "Not Installed", GUILayout.Width(120));
                        EditorGUILayout.LabelField($"{pkg.DefineSymbol}: {(pkg.IsDefineActive ? "ON" : "OFF")}");
                    }
                }

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Refresh Inspection", GUILayout.Height(26)))
                    {
                        RefreshInspection();
                    }
                    if (GUILayout.Button("Sync Define Symbols to Installed Packages", GUILayout.Height(26)))
                    {
                        SyncDefineSymbolsWithInstalledPackages();
                    }
                }
            }

            EditorGUILayout.Space(6);

            // 3. Runtime 8-Point Diagnostic Runner
            EditorGUILayout.LabelField("3. Runtime 8-Point UGS Connection Verification", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to execute live/runtime UGS Initialization, Guest Login, Cloud Save Write/Read, and Account Linking diagnostics.", MessageType.Warning);
                }

                EditorGUI.BeginDisabledGroup(!Application.isPlaying || isRunningDiagnostics);
                if (GUILayout.Button(isRunningDiagnostics ? "Running Diagnostics..." : "Run 8-Point UGS Diagnostics", GUILayout.Height(30)))
                {
                    RunDiagnosticsAsync();
                }
                EditorGUI.EndDisabledGroup();

                if (latestReport != null && latestReport.items != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(
                        $"Last Run: {latestReport.timestampUtc}  |  Mode: {(latestReport.isLiveUgsMode ? "LIVE UGS SDK" : "LOCAL SANDBOX FALLBACK")}",
                        EditorStyles.miniBoldLabel);

                    foreach (var item in latestReport.items)
                    {
                        MessageType msgType = item.status switch
                        {
                            DiagnosticCheckStatus.Pass => MessageType.Info,
                            DiagnosticCheckStatus.Warning => MessageType.Warning,
                            _ => MessageType.Error
                        };
                        EditorGUILayout.HelpBox($"{item.label}\n{item.detail}", msgType);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
