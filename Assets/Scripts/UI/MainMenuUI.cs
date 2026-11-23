using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        public UIManager uiManager;
        public GameObject comingSoonPanel;
        public UnityEngine.UI.Text comingSoonTitle;

        private AccountUIManager accountUI;
        private AccountSettingsUI accountSettingsUI;
        private SelectGameModeUI selectGameModeUI;
        private MonsterCollectionUI monsterCollectionUI;

        private void Awake()
        {
            SelectedGameModeManager.EnsureInstance();

            Wire("PlayButton", Play);
            Wire("MonstersButton", ShowMonsters);
            Wire("ShopButton", ShowShop);
            Wire("SettingsButton", ShowSettings);
            Wire("CloseComingSoonButton", CloseComingSoon);

            GameObject safeObj = GameObject.Find("SafeAreaRoot");
            RectTransform safe = safeObj != null ? safeObj.GetComponent<RectTransform>() : null;

            ApplyMainMenuTheme(safe);

            selectGameModeUI = FindAnyObjectByType<SelectGameModeUI>() ?? gameObject.AddComponent<SelectGameModeUI>();
            selectGameModeUI.Configure(safe, onSettings: ShowSettings, onBack: null);

            monsterCollectionUI = FindAnyObjectByType<MonsterCollectionUI>() ?? gameObject.AddComponent<MonsterCollectionUI>();
            monsterCollectionUI.Configure(safe);

            accountUI = FindAnyObjectByType<AccountUIManager>() ?? gameObject.AddComponent<AccountUIManager>();
            accountSettingsUI = FindAnyObjectByType<AccountSettingsUI>() ?? gameObject.AddComponent<AccountSettingsUI>();
            accountUI.InitializeAndShowIfNeeded(safe);
            accountSettingsUI.Configure(safe);

            if (SelectedGameModeManager.ConsumeReturnToModeSelectRequest())
            {
                selectGameModeUI.Open();
            }
        }

        private void ApplyMainMenuTheme(RectTransform safe)
        {
            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;

            // Style existing MainMenu buttons with unified Dark Fantasy theme
            StyleButtonByName("PlayButton", theme.primaryButton);
            StyleButtonByName("MonstersButton", theme.secondaryButton);
            StyleButtonByName("ShopButton", theme.secondaryButton);
            StyleButtonByName("SettingsButton", theme.secondaryButton);

            // Replace plain title text with real DREAM DUNGEON Logo if Logo asset is loaded
            if (safe != null && assets != null && assets.logoDreamDungeon != null)
            {
                Transform existingLogo = safe.Find("MainMenuLogoGraphic");
                if (existingLogo == null)
                {
                    GameObject titleObj = GameObject.Find("Title") ?? GameObject.Find("TitleText");
                    if (titleObj != null) titleObj.SetActive(false);

                    GameObject logoGo = new GameObject("MainMenuLogoGraphic", typeof(RectTransform), typeof(Image));
                    logoGo.transform.SetParent(safe, false);
                    RectTransform rt = logoGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.14f, 0.64f);
                    rt.anchorMax = new Vector2(0.86f, 0.90f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    Image img = logoGo.GetComponent<Image>();
                    img.sprite = assets.logoDreamDungeon;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }
            }
        }

        private static void StyleButtonByName(string objectName, Color baseColor)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null) return;
            Image img = obj.GetComponent<Image>();
            Button btn = obj.GetComponent<Button>();
            if (img != null) UIStyleHelper.ApplyGothicPanel(img);
            if (btn != null) UIStyleHelper.ApplyButtonStates(btn, baseColor);
        }

        private static void Wire(string objectName, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            if (button != null) button.onClick.AddListener(action);
        }

        public void Play()
        {
            if (selectGameModeUI != null)
            {
                selectGameModeUI.Open();
                return;
            }

            bool hasExistingCloudDungeon = PlayerDatabaseManager.Instance?.CurrentData?.dungeonData != null &&
                                           !string.IsNullOrEmpty(PlayerDatabaseManager.Instance.CurrentData.dungeonData.dungeonLayoutJson);
            bool hasLocalSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();

            if (!hasExistingCloudDungeon && !hasLocalSave)
            {
                GameManager.RequestNewGame();
                BattleSession.Reset();
            }
            SceneLoader.Load("DungeonEditor");
        }

        public void ShowMonsters()
        {
            if (monsterCollectionUI != null) monsterCollectionUI.Open();
            else ShowComingSoon("MONSTERS");
        }
        public void ShowShop() => accountUI?.OpenCrystalShop();
        public void ShowSettings() => accountSettingsUI?.Open();
        public void CloseComingSoon()
        {
            if (uiManager != null) uiManager.Hide(comingSoonPanel);
            else if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
        }

        private void ShowComingSoon(string feature)
        {
            if (comingSoonTitle != null) comingSoonTitle.text = feature + "\nCOMING SOON";
            if (uiManager != null) uiManager.Show(comingSoonPanel);
            else if (comingSoonPanel != null) comingSoonPanel.SetActive(true);
        }
    }
}
