using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class MonsterShopUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private MonsterShopManager shop;
        [SerializeField] private MonsterDatabase database;
        [SerializeField] private CurrencyManager currency;
        private RectTransform overlay; private Transform content; private Text status; private Text balance;
        private bool isPurchasing;
        public void Configure(RectTransform safe, MonsterShopManager manager, MonsterDatabase db, CurrencyManager money) { safeAreaRoot = safe; shop = manager; database = db; currency = money; }
        private void Awake()
        {
            if (shop == null) shop = FindAnyObjectByType<MonsterShopManager>();
            if (database == null && shop != null) database = shop.Database;
            if (currency == null && shop != null) currency = shop.Currency;
            if (safeAreaRoot == null && GameObject.Find("SafeAreaRoot") != null) safeAreaRoot = GameObject.Find("SafeAreaRoot").GetComponent<RectTransform>();
            Build();
            if (shop != null) shop.StatusChanged += SetStatus;
            if (currency != null) currency.CurrencyChanged += UpdateBalance;
            if (shop != null && shop.Inventory != null) shop.Inventory.InventoryChanged += Refresh;
        }
        private void OnDestroy()
        {
            if (shop != null) shop.StatusChanged -= SetStatus;
            if (currency != null) currency.CurrencyChanged -= UpdateBalance;
            if (shop != null && shop.Inventory != null) shop.Inventory.InventoryChanged -= Refresh;
        }
        public void Open() { if (overlay == null) Build(); if (overlay == null) return; overlay.gameObject.SetActive(true); Refresh(); if (currency != null) UpdateBalance(currency.Gold, currency.Soul, currency.Crystal); }
        public void Close() { if (overlay != null) overlay.gameObject.SetActive(false); }
        private void Build()
        {
            if (safeAreaRoot == null || overlay != null) return;
            overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "MonsterShopPanel"); var window = RoomUIFactory.CreateWindow(overlay, "MonsterShopWindow", new Vector2(1000, 1680), new Color(.075f,.085f,.12f,1));
            RoomUIFactory.CreateText(window,"MonsterShopTitle","MONSTER SHOP",38,new Vector2(0,770),new Vector2(760,70),new Color(.96f,.78f,.43f));
            balance=RoomUIFactory.CreateText(window,"MonsterShopBalance","",20,new Vector2(0,700),new Vector2(850,50),Color.white);
            status=RoomUIFactory.CreateText(window,"MonsterShopStatus","Browse monsters and buy unlocked units.",18,new Vector2(0,-755),new Vector2(850,50),new Color(.82f,.85f,.9f));
            RoomUIFactory.CreateButton(window,"CloseMonsterShopButton","CLOSE",new Vector2(405,770),new Vector2(150,68),new Color(.34f,.19f,.19f),19).onClick.AddListener(Close);
            content=RoomUIFactory.CreateScrollList(window,"MonsterShopScroll",new Vector2(0,-20),new Vector2(930,1320)); overlay.gameObject.SetActive(false);
        }
        private void Refresh()
        {
            if(content==null||database==null||shop==null)return;
            for(int i=content.childCount-1;i>=0;i--)Destroy(content.GetChild(i).gameObject);
            foreach(MonsterData data in database.AllMonsters)
            {
                if(data==null)continue; GameObject card=RoomUIFactory.CreateCard(content,"MonsterCard_"+data.monsterId,240); Transform t=card.transform;
                var icon=RoomUIFactory.CreatePanel(t,"Icon",data.icon!=null?Color.white:RarityColor(data.rarity),new Vector2(-365,0),new Vector2(90,90));
                if(data.icon!=null){var img=icon.GetComponent<Image>();img.sprite=data.icon;img.preserveAspect=true;}
                RoomUIFactory.CreateText(t,"Name",data.monsterName,24,new Vector2(-220,82),new Vector2(420,42),Color.white,TextAnchor.MiddleLeft);
                RoomUIFactory.CreateText(t,"RarityRole",$"{data.rarity}  •  {data.role}",17,new Vector2(-220,42),new Vector2(420,36),RarityColor(data.rarity),TextAnchor.MiddleLeft);
                RoomUIFactory.CreateText(t,"Stats",$"HP {data.maxHP}   ATK {data.attack}   DEF {data.defense}\nRANGE {data.attackRange:0.0}   SPD {data.attackSpeed:0.0}   MOVE {data.moveSpeed:0.0}",15,new Vector2(-210,-25),new Vector2(470,72),new Color(.85f,.86f,.9f),TextAnchor.MiddleLeft);
                bool unlocked=shop.DungeonLevel>=data.unlockDungeonLevel; bool can=shop.CanPurchase(data.monsterId,out string error);
                bool affordable=currency!=null&&currency.CanAfford(data.goldCost,0);
                RoomUIFactory.CreateText(t,"Unlock",unlocked?$"{data.goldCost} GOLD":"LOCKED • DUNGEON LEVEL "+data.unlockDungeonLevel,16,new Vector2(290,38),new Vector2(220,45),unlocked?new Color(.96f,.78f,.43f):new Color(.9f,.45f,.42f));
                string btnText=!unlocked?"LOCKED":!affordable?"NOT ENOUGH GOLD":can?"BUY":"UNAVAILABLE";
                Button buy=RoomUIFactory.CreateButton(t,"BuyMonsterButton",btnText,new Vector2(335,-55),new Vector2(180,64),new Color(.4f,.27f,.14f),17); buy.interactable=unlocked; buy.onClick.AddListener(()=>Purchase(data.monsterId));
                if(!unlocked)buy.GetComponentInChildren<Text>().text="LOCKED";
            }
            Canvas.ForceUpdateCanvases();
        }
        private void Purchase(string id)
        {
            if (isPurchasing) return;
            isPurchasing = true;
            try
            {
                if (shop.TryPurchase(id, out string message)) SetStatus(message);
                else SetStatus(message);
                Refresh();
            }
            finally
            {
                isPurchasing = false;
            }
        }
        private void UpdateBalance(int gold,int soul,int crystal){if(balance!=null)balance.text=$"GOLD {gold}   SOUL {soul}";}
        private void SetStatus(string value){if(status!=null)status.text=value;}
        private static Color RarityColor(MonsterRarity rarity)=>rarity switch{MonsterRarity.Common=>new Color(.65f,.85f,.67f),MonsterRarity.Rare=>new Color(.52f,.68f,1f),MonsterRarity.Epic=>new Color(.82f,.55f,1f),_=>new Color(1f,.72f,.3f)};
    }
}
