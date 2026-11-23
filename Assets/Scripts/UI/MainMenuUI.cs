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

        private void Awake()
        {
            Wire("PlayButton", Play);
            Wire("MonstersButton", ShowMonsters);
            Wire("ShopButton", ShowShop);
            Wire("SettingsButton", ShowSettings);
            Wire("CloseComingSoonButton", CloseComingSoon);

            GameObject safeObj = GameObject.Find("SafeAreaRoot");
            RectTransform safe = safeObj != null ? safeObj.GetComponent<RectTransform>() : null;

            accountUI = FindAnyObjectByType<AccountUIManager>() ?? gameObject.AddComponent<AccountUIManager>();
            accountSettingsUI = FindAnyObjectByType<AccountSettingsUI>() ?? gameObject.AddComponent<AccountSettingsUI>();
            accountUI.InitializeAndShowIfNeeded(safe);
            accountSettingsUI.Configure(safe);
        }

        private static void Wire(string objectName, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            if (button != null) button.onClick.AddListener(action);
        }

        public void Play()
        {
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

        public void ShowMonsters() => ShowComingSoon("MONSTERS");
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
