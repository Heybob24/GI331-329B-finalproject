using UnityEngine;

namespace DreamDungeon
{
    public struct BattleRewardResult
    {
        public bool Won;
        public int WaveReached;
        public int DefeatedCount;
        public int Gold;
        public int Soul;
        public int Exp;

        public BattleRewardResult(bool won, int waveReached, int defeated, int gold, int soul, int exp)
        {
            Won = won;
            WaveReached = waveReached;
            DefeatedCount = defeated;
            Gold = System.Math.Max(0, gold);
            Soul = System.Math.Max(0, soul);
            Exp = System.Math.Max(0, exp);
        }
    }

    public static class BattleRewardCalculator
    {
        public static BattleRewardResult Calculate(bool victory, int waveReached, int defeatedCount, BattleConfiguration config)
        {
            return Calculate(victory, waveReached, defeatedCount, config, 1.0f, 1.0f, 0);
        }

        public static BattleRewardResult Calculate(bool victory, int waveReached, int defeatedCount, BattleConfiguration config, float goldMultiplier, float soulMultiplier, int bossesDefeated)
        {
            int baseGold = 0;
            int baseSoul = 0;
            int baseExp = 0;

            if (config == null)
            {
                baseGold = defeatedCount * (victory ? 10 : 5) + (victory ? 100 : 0);
                baseSoul = defeatedCount * (victory ? 2 : 1) + (victory ? 20 : 0);
                baseExp = victory ? 100 : 0;
            }
            else
            {
                baseGold = defeatedCount * (victory ? config.goldPerDefeatedAdventurer : config.defeatGoldPerDefeated);
                baseSoul = defeatedCount * (victory ? config.soulPerDefeatedAdventurer : config.defeatSoulPerDefeated);
                baseExp = victory ? config.victoryBonusExp : 0;

                if (victory)
                {
                    baseGold += config.victoryBonusGold;
                    baseSoul += config.victoryBonusSoul;
                }
            }

            // Milestone 6: 5 EXP per adventurer defeated + 20 EXP per wave cleared + 100 EXP victory bonus + 300 EXP per boss
            int progressionExp = (defeatedCount * 5) + (waveReached * 20) + (victory ? 100 : 0) + (bossesDefeated * 300);
            int exp = Mathf.Max(baseExp, progressionExp);

            int finalGold = Mathf.RoundToInt(baseGold * Mathf.Max(1.0f, goldMultiplier));
            int finalSoul = Mathf.RoundToInt(baseSoul * Mathf.Max(1.0f, soulMultiplier));

            return new BattleRewardResult(victory, waveReached, defeatedCount, finalGold, finalSoul, exp);
        }
    }
}
