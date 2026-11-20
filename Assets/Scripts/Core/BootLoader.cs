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

            yield return null;
            SceneLoader.Load("MainMenu");
        }
    }
}
