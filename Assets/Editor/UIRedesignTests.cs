using System.IO;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Editor
{
    public static class UIRedesignTests
    {
        [MenuItem("Dream Dungeon/UI Redesign/3. Run UI Redesign Verification Tests")]
        public static void RunVerificationTests()
        {
            int passed = 0;
            int failed = 0;

            void Check(string name, bool condition, string detail)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[UIRedesignTests] [PASS] {name}: {detail}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[UIRedesignTests] [FAIL] {name}: {detail}");
                }
            }

            // 1. UI Assets organized
            string uiRoot = "Assets/Art/UI/DREAM_DUNGEON_UI_Assets";
            Check("UI Assets Folder", Directory.Exists(uiRoot), uiRoot);
            Check("README_Unity_Import.txt", File.Exists($"{uiRoot}/README_Unity_Import.txt"), "Present");
            Check("Original Reference Sheet", File.Exists($"{uiRoot}/Original/00_Original_Asset_Sheet.png"), "Kept in Original/");

            // 2. Database populated
            DreamDungeonUIAssetDatabase db = AssetDatabase.LoadAssetAtPath<DreamDungeonUIAssetDatabase>(
                "Assets/Resources/DreamDungeon/DreamDungeonUIAssetDatabase.asset");
            Check("DreamDungeonUIAssetDatabase", db != null, db != null ? "Loaded" : "Run Step 1 Import first");

            if (db != null)
            {
                Check("Logo Sprite", db.logoDreamDungeon != null, "Logo_DreamDungeon");
                Check("All 4 Mode Cards",
                    db.modeCardCampaign != null && db.modeCardEndless != null && db.modeCardChallenge != null && db.modeCardCustom != null,
                    "Campaign, Endless, Challenge, Custom");
                Check("All 10 Action Buttons",
                    db.buttonBack != null && db.buttonPlay != null && db.buttonStartDungeon != null &&
                    db.buttonMove != null && db.buttonRemove != null && db.buttonRotate != null &&
                    db.buttonPath != null && db.buttonTrap != null && db.buttonUndo != null && db.buttonRedo != null,
                    "10/10 buttons assigned");
                Check("Currency Counters", db.currencyGoldCounter != null && db.currencyCrystalCounter != null, "Gold + Crystal");
                Check("Panels", db.panelEmptyFrame != null && db.panelDungeonInfo != null && db.panelBuildTools != null, "3/3 panels");
                Check("Sliced Room Tile Icons",
                    db.iconRoomStone != null && db.iconRoomWood != null && db.iconRoomBrick != null && db.iconRoomObsidian != null,
                    "4/4 sliced icons");
                Check("Sliced Monster/Trap Icons",
                    db.iconSpikeTrap != null && db.iconGoblin != null && db.iconSlime != null && db.iconSpider != null,
                    "4/4 sliced icons");
                Check("Sliced Gem Icons",
                    db.iconGemPurple != null && db.iconGemBlue != null && db.iconGemGreen != null && db.iconGemGold != null && db.iconGemAmber != null,
                    "5/5 sliced icons");
                Check("Sliced Utility Icons",
                    db.iconSettings != null && db.iconInfo != null && db.iconStats != null && db.iconCrown != null && db.iconBell != null,
                    "5/5 sliced icons");
            }

            // 3. All 7 UI Prefabs
            string[] prefabs =
            {
                "PF_ModeCard", "PF_CurrencyCounter", "PF_BuildToolTab", "PF_ItemSlot",
                "PF_DungeonInfoPanel", "PF_BuildToolsPanel", "PF_ConfirmationPopup"
            };
            int prefabCount = 0;
            foreach (string p in prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/UI/{p}.prefab") != null)
                    prefabCount++;
            }
            Check("7 Reusable UI Prefabs", prefabCount == prefabs.Length, $"{prefabCount}/{prefabs.Length} prefabs found in Assets/Prefabs/UI/");

            // 4. SelectedGameModeManager logic
            var tempGo = new GameObject("TempModeMgrTest");
            var mgr = tempGo.AddComponent<SelectedGameModeManager>();
            mgr.SelectMode(SelectedGameMode.Endless);
            bool endlessOk = mgr.CurrentMode == SelectedGameMode.Endless && BattleSession.ActiveGameMode == GameModeType.Endless;
            mgr.SelectMode(SelectedGameMode.Challenge);
            bool challengeOk = mgr.CurrentMode == SelectedGameMode.Challenge && BattleSession.ActiveGameMode == GameModeType.Challenge;
            mgr.SelectMode(SelectedGameMode.Custom);
            bool customOk = mgr.CurrentMode == SelectedGameMode.Custom && BattleSession.ActiveGameMode == GameModeType.StandardRaid;
            mgr.SelectMode(SelectedGameMode.Campaign);
            bool campaignOk = mgr.CurrentMode == SelectedGameMode.Campaign && BattleSession.ActiveGameMode == GameModeType.Campaign;
            UnityEngine.Object.DestroyImmediate(tempGo);
            Check("SelectedGameMode Integration", endlessOk && challengeOk && customOk && campaignOk, "All 4 modes sync with BattleSession");

            Debug.Log($"[UIRedesignTests] Completed: {passed} PASSED, {failed} FAILED.");
        }
    }
}
