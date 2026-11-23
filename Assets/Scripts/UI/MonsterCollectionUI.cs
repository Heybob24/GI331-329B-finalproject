using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class MonsterCollectionUI : MonoBehaviour
    {
        private RectTransform safeAreaRoot;
        private GameObject overlayRoot;
        private Transform listContainer;
        private TextMeshProUGUI detailText;

        public void Configure(RectTransform safeRoot)
        {
            safeAreaRoot = safeRoot;
        }

        public void Open()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
            if (overlayRoot == null) BuildUI();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
                overlayRoot.transform.SetAsLastSibling();
                RefreshList();
            }
        }

        public void Close()
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null) return;
            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;

            overlayRoot = new GameObject("MonsterCollectionOverlay", typeof(RectTransform), typeof(Image));
            overlayRoot.transform.SetParent(safeAreaRoot, false);
            UIStyleHelper.Stretch(overlayRoot, 0f, 0f, 0f, 0f);
            overlayRoot.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.96f);

            GameObject card = new GameObject("CollectionCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(overlayRoot.transform, false);
            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.04f, 0.06f);
            cRt.anchorMax = new Vector2(0.96f, 0.94f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            UIStyleHelper.ApplyGothicPanel(card.GetComponent<Image>());

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(card.transform, false);
            UIStyleHelper.Stretch(content, 32f, 30f, 32f, 52f);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
            titleObj.transform.SetParent(content.transform, false);
            titleObj.GetComponent<LayoutElement>().preferredHeight = 46f;
            UIStyleHelper.CreateTmp(titleObj.transform, "Txt", "MONSTER COLLECTION", 32, theme.primaryText);

            // Scroll List
            GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
            scrollObj.transform.SetParent(content.transform, false);
            scrollObj.GetComponent<LayoutElement>().flexibleHeight = 1f;
            scrollObj.GetComponent<LayoutElement>().minHeight = 600f;
            scrollObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.11f, 0.85f);
            scrollObj.GetComponent<Mask>().showMaskGraphic = true;

            GameObject listObj = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObj.transform.SetParent(scrollObj.transform, false);
            RectTransform lRt = listObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 1f);
            lRt.anchorMax = new Vector2(1f, 1f);
            lRt.pivot = new Vector2(0.5f, 1f);
            lRt.anchoredPosition = Vector2.zero;
            lRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup lVlg = listObj.GetComponent<VerticalLayoutGroup>();
            lVlg.padding = new RectOffset(8, 8, 8, 8);
            lVlg.spacing = 8f;
            lVlg.childControlWidth = true;
            lVlg.childControlHeight = true;
            lVlg.childForceExpandWidth = true;
            lVlg.childForceExpandHeight = false;
            listObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = lRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 30f;
            listContainer = listObj.transform;

            // Detail Footer
            GameObject detailBox = new GameObject("DetailBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            detailBox.transform.SetParent(content.transform, false);
            detailBox.GetComponent<LayoutElement>().preferredHeight = 96f;
            detailBox.GetComponent<Image>().color = new Color(0.09f, 0.08f, 0.18f, 0.95f);
            detailText = UIStyleHelper.CreateTmp(detailBox.transform, "Txt", "Tap any Monster to inspect stats and role.", 17, theme.normalText, TextAlignmentOptions.Left, FontStyles.Normal);
            detailText.textWrappingMode = TextWrappingModes.Normal;

            // Back Button
            GameObject backRow = new GameObject("BackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            backRow.transform.SetParent(content.transform, false);
            backRow.GetComponent<LayoutElement>().preferredHeight = 82f;
            HorizontalLayoutGroup bHlg = backRow.GetComponent<HorizontalLayoutGroup>();
            bHlg.childAlignment = TextAnchor.MiddleCenter;
            bHlg.childControlWidth = false;
            bHlg.childControlHeight = false;
            Button backBtn = UIStyleHelper.CreateThemedButton(backRow.transform, "CloseMonstersBtn", "BACK",
                theme.secondaryButton, 76f, Close, assets != null ? assets.buttonBack : null);
            backBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 76f);

            overlayRoot.SetActive(false);
        }

        private void RefreshList()
        {
            if (listContainer == null) return;
            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            MonsterDatabase db = Resources.Load<MonsterDatabase>("DreamDungeon/MonsterDatabase") ?? Resources.Load<MonsterDatabase>("MonsterDatabase");
            if (db == null || db.AllMonsters == null) return;

            var theme = UIThemeManager.Theme;
            var assets = UIThemeManager.Assets;
            int idx = 0;

            foreach (MonsterData mon in db.AllMonsters)
            {
                if (mon == null) continue;
                MonsterData captured = mon;

                GameObject row = new GameObject($"Mon_{mon.monsterId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(listContainer, false);
                row.GetComponent<LayoutElement>().preferredHeight = 90f;
                Image bg = row.GetComponent<Image>();
                bg.color = new Color(0.13f, 0.11f, 0.22f, 0.96f);
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (detailText != null)
                    {
                        detailText.text = $"<b>{captured.monsterName}</b> ({captured.rarity} • {captured.role})\n" +
                                          $"HP: {captured.MaxHPAtLevel(1)}  |  ATK: {captured.AttackAtLevel(1)}  |  DEF: {captured.DefenseAtLevel(1)}  |  Cost: {captured.goldCost}G\n" +
                                          $"{captured.description}";
                    }
                });

                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iRt = iconObj.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0.5f);
                iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.anchoredPosition = new Vector2(12f, 0f);
                iRt.sizeDelta = new Vector2(68f, 68f);
                Image iImg = iconObj.GetComponent<Image>();
                iImg.sprite = mon.icon != null ? mon.icon : (assets != null ? assets.GetMonsterFallbackIcon(idx) : null);
                iImg.preserveAspect = true;

                GameObject txtObj = new GameObject("Info", typeof(RectTransform));
                txtObj.transform.SetParent(row.transform, false);
                RectTransform tRt = txtObj.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 1f);
                tRt.offsetMin = new Vector2(92f, 6f);
                tRt.offsetMax = new Vector2(-12f, -6f);
                UIStyleHelper.CreateTmp(txtObj.transform, "Txt",
                    $"<color=#FFDC6E>{mon.monsterName}</color>  [{mon.rarity}]\n" +
                    $"HP {mon.MaxHPAtLevel(1)}  •  ATK {mon.AttackAtLevel(1)}  •  DEF {mon.DefenseAtLevel(1)}  •  Lv.{mon.unlockDungeonLevel}+",
                    18, theme.normalText, TextAlignmentOptions.Left);
                idx++;
            }
        }
    }
}
