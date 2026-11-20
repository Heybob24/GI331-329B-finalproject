using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [SerializeField] private List<DifficultyData> difficulties = new List<DifficultyData>();
        private StageDifficulty selectedDifficulty = StageDifficulty.Easy;

        public IReadOnlyList<DifficultyData> AllDifficulties
        {
            get
            {
                EnsureDefaultDifficulties();
                return difficulties;
            }
        }

        public StageDifficulty SelectedDifficulty => selectedDifficulty;
        public DifficultyData CurrentDifficultyData => GetDifficultyData(selectedDifficulty);

        public event Action<DifficultyData> OnDifficultyChanged;

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
            EnsureDefaultDifficulties();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureDefaultDifficulties()
        {
            if (difficulties != null && difficulties.Count >= 5) return;
            difficulties = new List<DifficultyData>
            {
                MakeDifficulty("easy", "Easy", StageDifficulty.Easy,
                    hp: 1.0f, atk: 1.0f, def: 1.0f, count: 1.0f,
                    gold: 1.0f, soul: 1.0f, exp: 1.0f, reqLvl: 1, reqText: ""),

                MakeDifficulty("normal", "Normal", StageDifficulty.Normal,
                    hp: 1.25f, atk: 1.20f, def: 1.15f, count: 1.15f,
                    gold: 1.25f, soul: 1.25f, exp: 1.25f, reqLvl: 1, reqText: ""),

                MakeDifficulty("hard", "Hard", StageDifficulty.Hard,
                    hp: 1.60f, atk: 1.45f, def: 1.35f, count: 1.30f,
                    gold: 1.60f, soul: 1.60f, exp: 1.55f, reqLvl: 3, reqText: "Requires Dungeon Level 3"),

                MakeDifficulty("very_hard", "Very Hard", StageDifficulty.VeryHard,
                    hp: 2.10f, atk: 1.80f, def: 1.65f, count: 1.50f,
                    gold: 2.10f, soul: 2.10f, exp: 2.00f, reqLvl: 5, reqText: "Requires Dungeon Level 5"),

                MakeDifficulty("nightmare", "Nightmare", StageDifficulty.Nightmare,
                    hp: 2.80f, atk: 2.30f, def: 2.00f, count: 1.75f,
                    gold: 3.00f, soul: 3.00f, exp: 2.75f, reqLvl: 8, reqText: "Requires Dungeon Level 8")
            };
        }

        public DifficultyData GetDifficultyData(StageDifficulty level)
        {
            EnsureDefaultDifficulties();
            foreach (DifficultyData d in difficulties)
            {
                if (d != null && d.difficultyLevel == level) return d;
            }
            return difficulties.Count > 0 ? difficulties[0] : null;
        }

        public DifficultyData GetDifficultyById(string difficultyId)
        {
            EnsureDefaultDifficulties();
            if (string.IsNullOrWhiteSpace(difficultyId)) return GetDifficultyData(StageDifficulty.Easy);
            foreach (DifficultyData d in difficulties)
            {
                if (d != null && string.Equals(d.difficultyId, difficultyId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return d;
            }
            return GetDifficultyData(StageDifficulty.Easy);
        }

        public bool IsDifficultyUnlocked(StageDifficulty level, out string reason)
        {
            DifficultyData data = GetDifficultyData(level);
            if (data == null)
            {
                reason = "Difficulty data not found.";
                return false;
            }

            int currentDungeonLevel = DungeonProgressionManager.Instance != null
                ? DungeonProgressionManager.Instance.CurrentLevel
                : 1;

            if (currentDungeonLevel < data.requiredDungeonLevel)
            {
                reason = !string.IsNullOrWhiteSpace(data.unlockRequirement)
                    ? data.unlockRequirement
                    : $"Unlocks at Dungeon Level {data.requiredDungeonLevel}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TrySelectDifficulty(StageDifficulty level, out string error)
        {
            if (!IsDifficultyUnlocked(level, out error))
            {
                return false;
            }

            selectedDifficulty = level;
            OnDifficultyChanged?.Invoke(CurrentDifficultyData);
            return true;
        }

        public void ForceSelectDifficulty(StageDifficulty level)
        {
            selectedDifficulty = level;
            OnDifficultyChanged?.Invoke(CurrentDifficultyData);
        }

        private static DifficultyData MakeDifficulty(string id, string name, StageDifficulty level,
            float hp, float atk, float def, float count, float gold, float soul, float exp, int reqLvl, string reqText)
        {
            DifficultyData d = ScriptableObject.CreateInstance<DifficultyData>();
            d.name = id;
            d.difficultyId = id;
            d.difficultyName = name;
            d.difficultyLevel = level;
            d.enemyHealthMultiplier = hp;
            d.enemyDamageMultiplier = atk;
            d.enemyDefenseMultiplier = def;
            d.enemyCountMultiplier = count;
            d.rewardGoldMultiplier = gold;
            d.rewardSoulMultiplier = soul;
            d.rewardEXPMultiplier = exp;
            d.requiredDungeonLevel = reqLvl;
            d.unlockRequirement = reqText;
            return d;
        }
    }
}
