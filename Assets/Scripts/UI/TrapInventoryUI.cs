using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class TrapInventoryUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot; [SerializeField] private TrapInventoryManager inventory; [SerializeField] private TrapDatabase database;
        [SerializeField] private TrapPlacementManager placement; [SerializeField] private RoomManager rooms; [SerializeField] private TrapDetailsUI details;
        private RectTransform overlay; private Transform content; private Text status;
        public void Configure(RectTransform safe,TrapInventoryManager inv,TrapDatabase db,TrapPlacementManager place,RoomManager rm,TrapDetailsUI detail){safeAreaRoot=safe;inventory=inv;database=db;placement=place;rooms=rm;details=detail;}
        private void Awake()
        {
            EnsureConfigured();
        }
        private void EnsureConfigured()
        {
            if(inventory==null)inventory=FindAnyObjectByType<TrapInventoryManager>();
            if(database==null&&inventory!=null)database=inventory.Database;
            if(database==null)database=Resources.Load<TrapDatabase>("DreamDungeon/TrapDatabase")??Resources.Load<TrapDatabase>("TrapDatabase");
            if(placement==null)placement=FindAnyObjectByType<TrapPlacementManager>();
            if(rooms==null)rooms=FindAnyObjectByType<RoomManager>();
            if(details==null)details=FindAnyObjectByType<TrapDetailsUI>();
            if(safeAreaRoot==null&&GameObject.Find("SafeAreaRoot")!=null)safeAreaRoot=GameObject.Find("SafeAreaRoot").GetComponent<RectTransform>();
            if(overlay==null&&safeAreaRoot!=null)
            {
                Build();
                if(inventory!=null)inventory.InventoryChanged+=Refresh;
                if(placement!=null)placement.StatusChanged+=SetStatus;
            }
        }
        private void OnDestroy(){if(inventory!=null)inventory.InventoryChanged-=Refresh;if(placement!=null)placement.StatusChanged-=SetStatus;}
        public void Open(){EnsureConfigured();if(overlay==null)return;overlay.gameObject.SetActive(true);Refresh();}
        public void Close(){if(overlay!=null)overlay.gameObject.SetActive(false);}
        private void Build(){if(safeAreaRoot==null||overlay!=null)return;overlay=RoomUIFactory.CreateOverlay(safeAreaRoot,"TrapInventoryPanel");var window=RoomUIFactory.CreateWindow(overlay,"TrapInventoryWindow",new Vector2(1000,1680),new Color(.075f,.085f,.12f,1));RoomUIFactory.CreateText(window,"TrapInventoryTitle","TRAP INVENTORY",36,new Vector2(0,770),new Vector2(800,70),new Color(.96f,.78f,.43f));status=RoomUIFactory.CreateText(window,"TrapInventoryStatus","Select a trap, then tap a Trap Room.",18,new Vector2(0,-755),new Vector2(850,50),new Color(.82f,.85f,.9f));RoomUIFactory.CreateButton(window,"CloseTrapInventoryButton","CLOSE",new Vector2(405,770),new Vector2(150,68),new Color(.34f,.19f,.19f),19).onClick.AddListener(Close);content=RoomUIFactory.CreateScrollList(window,"TrapInventoryScroll",new Vector2(0,-20),new Vector2(930,1320));overlay.gameObject.SetActive(false);}
        private void Refresh(){if(content==null||database==null||inventory==null)return;for(int i=content.childCount-1;i>=0;i--)Destroy(content.GetChild(i).gameObject);foreach(TrapData data in database.AllTraps){if(data==null)continue;int owned=inventory.GetOwned(data.trapId),placed=inventory.GetPlaced(data.trapId),available=inventory.GetAvailable(data.trapId);GameObject card=RoomUIFactory.CreateCard(content,"TrapInventory_"+data.trapId,Mathf.Max(160,154+placed*58));Transform t=card.transform;var icon=RoomUIFactory.CreatePanel(t,"Icon",data.icon!=null?Color.white:Color.gray,new Vector2(-360,12),new Vector2(58,58));if(data.icon!=null){Image image=icon.GetComponent<Image>();image.sprite=data.icon;image.preserveAspect=true;}RoomUIFactory.CreateText(t,"Name",data.trapName,23,new Vector2(-210,55),new Vector2(440,40),Color.white,TextAnchor.MiddleLeft);RoomUIFactory.CreateText(t,"Counts",$"Owned {owned}   Placed {placed}   Available {available}",17,new Vector2(-210,10),new Vector2(440,36),new Color(.84f,.85f,.9f),TextAnchor.MiddleLeft);Button select=RoomUIFactory.CreateButton(t,"SelectTrapButton",available>0?"PLACE":"NONE AVAILABLE",new Vector2(340,38),new Vector2(190,60),new Color(.18f,.34f,.30f),16);select.interactable=available>0;select.onClick.AddListener(()=>Select(data.trapId));int row=0;if(rooms?.Layout?.traps!=null)foreach(TrapInstanceData unit in rooms.Layout.traps)if(unit.trapId==data.trapId){Button inspect=RoomUIFactory.CreateButton(t,"ManageTrap_"+unit.instanceId,$"MANAGE L{unit.level}",new Vector2(280,-35-row*56),new Vector2(180,48),new Color(.22f,.26f,.37f),15);inspect.onClick.AddListener(()=>Inspect(unit.instanceId));row++;}}Canvas.ForceUpdateCanvases();}
        private void Select(string id){if(placement.SelectTrap(id))Close();else SetStatus("Trap is unavailable. Check quantity and ownership.");}
        private void Inspect(string id){TrapInstanceData unit=rooms.FindTrap(id);if(unit==null)return;Close();details.Show(unit);}
        private void SetStatus(string value){if(status!=null)status.text=value;}
    }
}
