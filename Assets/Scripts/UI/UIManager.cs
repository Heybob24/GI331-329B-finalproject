using UnityEngine;

namespace DreamDungeon
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels;

        public void Show(GameObject panel)
        {
            if (panel == null) return;
            if (panels != null)
                foreach (GameObject item in panels)
                    if (item != null) item.SetActive(item == panel);
            panel.SetActive(true);
        }

        public void Hide(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
