using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum GameModeType
    {
        StandardRaid = 0,
        Campaign = 1,
        Endless = 2,
        Challenge = 3
    }

    [Serializable]
    public sealed class ScoreCalculationConfig
    {
        [Min(1)] public int normalEnemyPoints = 10;
        [Min(1)] public int eliteEnemyPoints = 50;
        [Min(1)] public int bossDefeatedPoints = 200;
        [Min(1)] public int waveCompletedPoints = 100;
        [Min(0)] public int maxCoreHpPercentageBonus = 250;
        [Min(0)] public int survivalPointsPerSecond = 2;
        [Min(0)] public int challengeObjectiveBonusPoints = 300;
    }

    [Serializable]
    public struct RunScoreBreakdown
    {
        public int WavesCompleted;
        public int HighestWaveReached;
        public int NormalEnemiesDefeated;
        public int EliteEnemiesDefeated;
        public int TotalEnemiesDefeated;
        public int BossesDefeated;
        public int CoreHpRemaining;
        public int CoreMaxHp;
        public float RunDurationSeconds;
        public bool ChallengeCompleted;

        public int WaveScore;
        public int EnemyScore;
        public int EliteScore;
        public int BossScore;
        public int CoreHpBonusScore;
        public int TimeBonusScore;
        public int ObjectiveBonusScore;
        public int TotalScore;
    }

    [Serializable]
    public sealed class ChallengeRecordEntry
    {
        public string challengeId = string.Empty;
        public bool isCompleted = false;
        public bool firstClearClaimed = false;
        public int bestScore = 0;
        public float bestClearTimeSeconds = 0f;
        public int totalAttempts = 0;
        public int totalClears = 0;
    }

    [Serializable]
    public sealed class EndlessChallengeSaveState
    {
        public int saveVersion = 1;
        public int highestEndlessWave = 0;
        public int highestEndlessScore = 0;
        public int totalEndlessRuns = 0;
        public int totalEndlessEnemiesDefeated = 0;
        public int totalEndlessElitesDefeated = 0;
        public int totalEndlessBossesDefeated = 0;
        public List<ChallengeRecordEntry> challengeRecords = new List<ChallengeRecordEntry>();
        public List<string> claimedRunResultIds = new List<string>();
    }

    public static class ScoreCalculator
    {
        public static RunScoreBreakdown Calculate(
            ScoreCalculationConfig config,
            int wavesCompleted,
            int highestWaveReached,
            int normalEnemiesDefeated,
            int eliteEnemiesDefeated,
            int bossesDefeated,
            int coreHpRemaining,
            int coreMaxHp,
            float runDurationSeconds,
            bool challengeCompleted = false)
        {
            if (config == null) config = new ScoreCalculationConfig();

            int safeWaves = Mathf.Max(0, wavesCompleted);
            int safeHighestWave = Mathf.Max(safeWaves, highestWaveReached);
            int safeNormal = Mathf.Max(0, normalEnemiesDefeated);
            int safeElites = Mathf.Max(0, eliteEnemiesDefeated);
            int safeBosses = Mathf.Max(0, bossesDefeated);
            int safeCoreHp = Mathf.Max(0, coreHpRemaining);
            int safeCoreMax = Mathf.Max(1, coreMaxHp);
            float safeDuration = Mathf.Max(0f, runDurationSeconds);

            int wavePts = safeWaves * Mathf.Max(1, config.waveCompletedPoints);
            int enemyPts = safeNormal * Mathf.Max(1, config.normalEnemyPoints);
            int elitePts = safeElites * Mathf.Max(1, config.eliteEnemyPoints);
            int bossPts = safeBosses * Mathf.Max(1, config.bossDefeatedPoints);

            float hpRatio = Mathf.Clamp01((float)safeCoreHp / safeCoreMax);
            int coreBonus = Mathf.RoundToInt(hpRatio * Mathf.Max(0, config.maxCoreHpPercentageBonus));
            int timeBonus = Mathf.Min(1000, Mathf.FloorToInt(safeDuration) * Mathf.Max(0, config.survivalPointsPerSecond));
            int objBonus = challengeCompleted ? Mathf.Max(0, config.challengeObjectiveBonusPoints) : 0;

            long total = (long)wavePts + enemyPts + elitePts + bossPts + coreBonus + timeBonus + objBonus;
            int clampedTotal = (int)Mathf.Clamp(total, 0, 100000000);

            return new RunScoreBreakdown
            {
                WavesCompleted = safeWaves,
                HighestWaveReached = safeHighestWave,
                NormalEnemiesDefeated = safeNormal,
                EliteEnemiesDefeated = safeElites,
                TotalEnemiesDefeated = safeNormal + safeElites,
                BossesDefeated = safeBosses,
                CoreHpRemaining = safeCoreHp,
                CoreMaxHp = safeCoreMax,
                RunDurationSeconds = safeDuration,
                ChallengeCompleted = challengeCompleted,
                WaveScore = wavePts,
                EnemyScore = enemyPts,
                EliteScore = elitePts,
                BossScore = bossPts,
                CoreHpBonusScore = coreBonus,
                TimeBonusScore = timeBonus,
                ObjectiveBonusScore = objBonus,
                TotalScore = clampedTotal
            };
        }
    }
}
