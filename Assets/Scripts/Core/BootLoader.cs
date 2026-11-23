using System.Collections;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class BootLoader : MonoBehaviour
    {
        private IEnumerator Start()
        {
            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();

            SelectedGameModeManager.EnsureInstance();

            if (UGSInitializationManager.Instance == null)
                new GameObject("UGSInitializationManager").AddComponent<UGSInitializationManager>();

            if (AuthenticationManager.Instance == null)
                new GameObject("AuthenticationManager").AddComponent<AuthenticationManager>();

            yield return null;
            SceneLoader.Load("MainMenu");
        }
    }
}
