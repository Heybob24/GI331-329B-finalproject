using UnityEngine;
using System.Collections.Generic;

namespace DreamDungeon
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private readonly HashSet<string> grantedExperienceRewards = new HashSet<string>();
        public int Experience { get; private set; }

        public bool GrantExperience(string rewardId, int amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || amount < 0 || !grantedExperienceRewards.Add(rewardId)) return false;
            Experience = (int)Mathf.Min(int.MaxValue, (long)Experience + amount);
            return true;
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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
