using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        public UIManager uiManager;
        public GameObject comingSoonPanel;
        public UnityEngine.UI.Text comingSoonTitle;

        private void Awake()
        {
            Wire("PlayButton", Play);
            Wire("MonstersButton", ShowMonsters);
            Wire("ShopButton", ShowShop);
            Wire("SettingsButton", ShowSettings);
            Wire("CloseComingSoonButton", CloseComingSoon);
        }

        private static void Wire(string objectName, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            if (button != null) button.onClick.AddListener(action);
        }

        public void Play()
        {
            GameManager.RequestNewGame();
            BattleSession.Reset();
            SaveManager.Instance?.ResetGame();
            SceneLoader.Load("DungeonEditor");
        }
        public void ShowMonsters() => ShowComingSoon("MONSTERS");
        public void ShowShop() => ShowComingSoon("SHOP");
        public void ShowSettings() => ShowComingSoon("SETTINGS");
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
