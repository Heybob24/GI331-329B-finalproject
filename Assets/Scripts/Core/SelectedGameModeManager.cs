using System;
using UnityEngine;

namespace DreamDungeon
{
    public enum SelectedGameMode
    {
        Campaign = 0,
        Endless = 1,
        Challenge = 2,
        Custom = 3
    }

    public sealed class SelectedGameModeManager : MonoBehaviour
    {
        public static SelectedGameModeManager Instance { get; private set; }

        private const string PrefKeyMode = "DD_SelectedGameMode";
        private const string PrefKeyDungeonName = "DD_CustomDungeonName";
        private const string PrefKeyDifficulty = "DD_SelectedDifficulty";

        public static bool ReturnToModeSelectPending { get; private set; }

        [SerializeField] private SelectedGameMode currentMode = SelectedGameMode.Campaign;
        [SerializeField] private string dungeonName = "My Dungeon";
        [SerializeField] private StageDifficulty selectedDifficulty = StageDifficulty.Normal;

        public SelectedGameMode CurrentMode => currentMode;
        public string DungeonName => string.IsNullOrWhiteSpace(dungeonName) ? "My Dungeon" : dungeonName;
        public StageDifficulty SelectedDifficulty => selectedDifficulty;

        public event Action<SelectedGameMode> OnGameModeChanged;
        public event Action<string> OnDungeonNameChanged;
        public event Action<StageDifficulty> OnDifficultyChanged;

        public static SelectedGameModeManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            SelectedGameModeManager existing = FindAnyObjectByType<SelectedGameModeManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }
            var go = new GameObject("SelectedGameModeManager");
            return go.AddComponent<SelectedGameModeManager>();
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

            LoadPreferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LoadPreferences()
        {
            int savedMode = PlayerPrefs.GetInt(PrefKeyMode, (int)SelectedGameMode.Campaign);
            if (Enum.IsDefined(typeof(SelectedGameMode), savedMode))
            {
                currentMode = (SelectedGameMode)savedMode;
            }

            string savedName = PlayerPrefs.GetString(PrefKeyDungeonName, "My Dungeon");
            dungeonName = string.IsNullOrWhiteSpace(savedName) ? "My Dungeon" : savedName.Trim();

            int savedDiff = PlayerPrefs.GetInt(PrefKeyDifficulty, (int)StageDifficulty.Normal);
            if (Enum.IsDefined(typeof(StageDifficulty), savedDiff))
            {
                selectedDifficulty = (StageDifficulty)savedDiff;
            }
        }

        public void SelectMode(SelectedGameMode mode)
        {
            currentMode = mode;
            PlayerPrefs.SetInt(PrefKeyMode, (int)mode);
            PlayerPrefs.Save();
            ApplyModeToBattleSession();
            OnGameModeChanged?.Invoke(currentMode);
        }

        public void SetDungeonName(string newName)
        {
            string cleaned = string.IsNullOrWhiteSpace(newName) ? "My Dungeon" : newName.Trim();
            if (cleaned.Length > 28) cleaned = cleaned.Substring(0, 28);
            dungeonName = cleaned;
            PlayerPrefs.SetString(PrefKeyDungeonName, dungeonName);
            PlayerPrefs.Save();
            OnDungeonNameChanged?.Invoke(dungeonName);
        }

        public void SetDifficulty(StageDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            PlayerPrefs.SetInt(PrefKeyDifficulty, (int)selectedDifficulty);
            PlayerPrefs.Save();
            OnDifficultyChanged?.Invoke(selectedDifficulty);
        }

        public void CycleDifficulty(int delta)
        {
            int count = 3; // Normal (0), Hard (1), VeryHard (2)
            int cur = Mathf.Clamp((int)selectedDifficulty, 0, count - 1);
            int next = (cur + delta + count) % count;
            SetDifficulty((StageDifficulty)next);
        }

        public float GetRewardMultiplier()
        {
            float diffMult = selectedDifficulty switch
            {
                StageDifficulty.Hard => 1.35f,
                StageDifficulty.VeryHard => 1.75f,
                _ => 1.0f
            };

            float modeMult = currentMode switch
            {
                SelectedGameMode.Endless => 1.25f,
                SelectedGameMode.Challenge => 1.50f,
                SelectedGameMode.Custom => 1.00f,
                _ => 1.00f
            };

            return diffMult * modeMult;
        }

        public void ApplyModeToBattleSession()
        {
            switch (currentMode)
            {
                case SelectedGameMode.Campaign:
                    BattleSession.SetActiveGameMode(GameModeType.Campaign);
                    break;
                case SelectedGameMode.Endless:
                    BattleSession.SetActiveGameMode(GameModeType.Endless);
                    break;
                case SelectedGameMode.Challenge:
                    BattleSession.SetActiveGameMode(GameModeType.Challenge);
                    break;
                case SelectedGameMode.Custom:
                default:
                    BattleSession.SetActiveGameMode(GameModeType.StandardRaid);
                    break;
            }
        }

        public string GetModeDisplayName(SelectedGameMode mode)
        {
            return mode switch
            {
                SelectedGameMode.Campaign => "CAMPAIGN",
                SelectedGameMode.Endless => "ENDLESS",
                SelectedGameMode.Challenge => "CHALLENGE",
                SelectedGameMode.Custom => "CUSTOM",
                _ => "CAMPAIGN"
            };
        }

        public string GetModeDescription(SelectedGameMode mode)
        {
            return mode switch
            {
                SelectedGameMode.Campaign => "Follow the story and defeat the dark lord across 6 World Regions & 18 Stages.",
                SelectedGameMode.Endless => "Survive as long as you can against escalating waves, Elites, and Bosses.",
                SelectedGameMode.Challenge => "Complete special tactical challenges with unique modifiers and bonus rewards.",
                SelectedGameMode.Custom => "Create your own dungeon freely, test layouts, and launch custom defense raids.",
                _ => string.Empty
            };
        }

        public string GetModeLiveProgressSummary(SelectedGameMode mode)
        {
            switch (mode)
            {
                case SelectedGameMode.Campaign:
                {
                    int victories = CampaignManager.Instance != null ? CampaignManager.Instance.TotalVictories : 0;
                    string stageId = CampaignManager.Instance != null && !string.IsNullOrEmpty(CampaignManager.Instance.SelectedStageId)
                        ? CampaignManager.Instance.SelectedStageId
                        : "forest_stage_1";
                    CampaignStageData stage = CampaignManager.Instance != null ? CampaignManager.Instance.GetStage(stageId) : null;
                    string stageName = stage != null ? stage.stageName : "Stage 1-1";
                    return $"Current: {stageName}  •  Victories: {victories}";
                }
                case SelectedGameMode.Endless:
                {
                    int bestWave = EndlessChallengeManager.Instance != null ? EndlessChallengeManager.Instance.HighestEndlessWave : 0;
                    int bestScore = EndlessChallengeManager.Instance != null ? EndlessChallengeManager.Instance.HighestEndlessScore : 0;
                    return $"Highest Wave: {bestWave}  •  Best Score: {bestScore:N0}";
                }
                case SelectedGameMode.Challenge:
                {
                    int cleared = 0;
                    int total = 6;
                    if (EndlessChallengeManager.Instance != null)
                    {
                        var list = EndlessChallengeManager.Instance.Challenges.AllChallenges;
                        total = list.Count;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i] != null && EndlessChallengeManager.Instance.IsChallengeCompleted(list[i].challengeId))
                                cleared++;
                        }
                    }
                    return $"Completed: {cleared} / {total} Challenges";
                }
                case SelectedGameMode.Custom:
                default:
                {
                    int dim = DungeonExpansionManager.Instance != null ? DungeonExpansionManager.Instance.CurrentDimension : 9;
                    return $"Free Sandbox Builder  •  Grid {dim}x{dim}";
                }
            }
        }

        public static void RequestReturnToModeSelect()
        {
            ReturnToModeSelectPending = true;
        }

        public static bool ConsumeReturnToModeSelectRequest()
        {
            bool val = ReturnToModeSelectPending;
            ReturnToModeSelectPending = false;
            return val;
        }
    }
}
