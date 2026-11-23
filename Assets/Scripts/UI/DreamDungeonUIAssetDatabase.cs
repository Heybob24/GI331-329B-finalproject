using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(fileName = "DreamDungeonUIAssetDatabase", menuName = "Dream Dungeon/UI/UI Asset Database")]
    public sealed class DreamDungeonUIAssetDatabase : ScriptableObject
    {
        public const string ResourcesPath = "DreamDungeon/DreamDungeonUIAssetDatabase";

        private static DreamDungeonUIAssetDatabase cachedInstance;

        public static DreamDungeonUIAssetDatabase Load()
        {
            if (cachedInstance == null)
            {
                cachedInstance = Resources.Load<DreamDungeonUIAssetDatabase>(ResourcesPath);
            }
            return cachedInstance;
        }

        [Header("Logo")]
        public Sprite logoDreamDungeon;

        [Header("Game Mode Cards")]
        public Sprite modeCardCampaign;
        public Sprite modeCardEndless;
        public Sprite modeCardChallenge;
        public Sprite modeCardCustom;

        [Header("Buttons")]
        public Sprite buttonBack;
        public Sprite buttonPlay;
        public Sprite buttonStartDungeon;
        public Sprite buttonMove;
        public Sprite buttonRemove;
        public Sprite buttonRotate;
        public Sprite buttonPath;
        public Sprite buttonTrap;
        public Sprite buttonUndo;
        public Sprite buttonRedo;

        [Header("Currency")]
        public Sprite currencyCrystalCounter;
        public Sprite currencyGoldCounter;

        [Header("Panels")]
        public Sprite panelDungeonInfo;
        public Sprite panelBuildTools;
        public Sprite panelEmptyFrame;

        [Header("Decorations")]
        public Sprite decorBannerPurple;
        public Sprite decorBannerRed;
        public Sprite decorScroll;
        public Sprite decorSkullEmblem;
        public Sprite decorTreasureChest;

        [Header("Badges")]
        public Sprite badgeBestValue;
        public Sprite badgeHot;
        public Sprite badgeNew;
        public Sprite badgeSale;

        [Header("Popups")]
        public Sprite popupPurchaseSuccess;
        public Sprite popupPurchaseFailed;

        [Header("Sliced Icons — Room Tiles (Icon_RoomTiles_Set)")]
        public Sprite iconRoomStone;
        public Sprite iconRoomWood;
        public Sprite iconRoomBrick;
        public Sprite iconRoomObsidian;

        [Header("Sliced Icons — Monsters & Trap (Icon_Monsters_Set)")]
        public Sprite iconSpikeTrap;
        public Sprite iconGoblin;
        public Sprite iconSlime;
        public Sprite iconSpider;

        [Header("Sliced Icons — Gems (GemIcons_Set)")]
        public Sprite iconGemPurple;
        public Sprite iconGemBlue;
        public Sprite iconGemGreen;
        public Sprite iconGemGold;
        public Sprite iconGemAmber;

        [Header("Sliced Icons — Top Bar Utility (TopBar_UtilityIcons)")]
        public Sprite iconSettings;
        public Sprite iconInfo;
        public Sprite iconStats;
        public Sprite iconCrown;
        public Sprite iconBell;

        [Header("Sliced Icons — Navigation & Pagination (Navigation_Pagination)")]
        public Sprite iconArrowLeft;
        public Sprite iconArrowRight;
        public Sprite iconClose;
        public Sprite iconCompass;
        public Sprite iconDot;

        public Sprite GetModeCard(SelectedGameMode mode)
        {
            return mode switch
            {
                SelectedGameMode.Campaign => modeCardCampaign,
                SelectedGameMode.Endless => modeCardEndless,
                SelectedGameMode.Challenge => modeCardChallenge,
                SelectedGameMode.Custom => modeCardCustom,
                _ => modeCardCampaign
            };
        }

        public Sprite GetRoomFallbackIcon(int index)
        {
            return (Mathf.Abs(index) % 4) switch
            {
                0 => iconRoomStone,
                1 => iconRoomWood,
                2 => iconRoomBrick,
                _ => iconRoomObsidian
            };
        }

        public Sprite GetMonsterFallbackIcon(int index)
        {
            return (Mathf.Abs(index) % 3) switch
            {
                0 => iconGoblin,
                1 => iconSlime,
                _ => iconSpider
            };
        }

        public Sprite GetTrapFallbackIcon()
        {
            return iconSpikeTrap != null ? iconSpikeTrap : iconGemAmber;
        }
    }
}
