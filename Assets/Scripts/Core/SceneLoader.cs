using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamDungeon
{
    public static class SceneLoader
    {
        public static bool Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader: scene name cannot be empty.");
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    SceneManager.LoadScene(sceneName);
                    return true;
                }
            }

            Debug.LogError($"SceneLoader: '{sceneName}' is not included in Build Settings.");
            return false;
        }
    }
}
