using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class TrapDetailsUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot; [SerializeField] private TrapDatabase database; [SerializeField] private RoomManager rooms;
        [SerializeField] private TrapUpgradeManager upgrades; [SerializeField] private TrapPlacementManager placement;
        private RectTransform overlay; private Text title,body,status; private Button upgrade; private string currentId;
        public void Configure(RectTransform safe,TrapDatabase db,RoomManager rm,TrapUpgradeManager up,TrapPlacementManager place){safeAreaRoot=safe;database=db;rooms=rm;upgrades=up;placement=place;}
        private void Awake(){if(safeAreaRoot==null&&GameObject.Find("SafeAreaRoot")!=null)safeAreaRoot=GameObject.Find("SafeAreaRoot").GetComponent<RectTransform>();Build();}
        public void Show(TrapInstanceData instance){if(instance==null)return;if(overlay==null){if(safeAreaRoot==null&&GameObject.Find("SafeAreaRoot")!=null)safeAreaRoot=GameObject.Find("SafeAreaRoot").GetComponent<RectTransform>();Build();}if(overlay==null)return;currentId=instance.instanceId;Refresh();overlay.gameObject.SetActive(true);}
        public void Close(){currentId=null;if(overlay!=null)overlay.gameObject.SetActive(false);}
        private void Build(){if(safeAreaRoot==null||overlay!=null)return;overlay=RoomUIFactory.CreateOverlay(safeAreaRoot,"TrapDetailsPanel");var window=RoomUIFactory.CreateWindow(overlay,"TrapDetailsWindow",new Vector2(930,1050),new Color(.075f,.085f,.12f,1));title=RoomUIFactory.CreateText(window,"TrapDetailsTitle","TRAP",36,new Vector2(0,410),new Vector2(780,70),new Color(.96f,.78f,.43f));body=RoomUIFactory.CreateText(window,"TrapDetailsStats","",21,new Vector2(0,25),new Vector2(820,610),Color.white);status=RoomUIFactory.CreateText(window,"TrapDetailsStatus","",17,new Vector2(0,-300),new Vector2(800,55),new Color(.86f,.84f,.77f));upgrade=RoomUIFactory.CreateButton(window,"UpgradeTrapButton","UPGRADE",new Vector2(-290,-395),new Vector2(210,74),new Color(.42f,.29f,.12f),18);Button move=RoomUIFactory.CreateButton(window,"MoveTrapButton","MOVE",new Vector2(-65,-395),new Vector2(170,74),new Color(.16f,.30f,.36f),18);Button remove=RoomUIFactory.CreateButton(window,"RemoveTrapButton","REMOVE",new Vector2(145,-395),new Vector2(190,74),new Color(.37f,.18f,.21f),18);Button close=RoomUIFactory.CreateButton(window,"CloseTrapDetailsButton","CLOSE",new Vector2(0,-485),new Vector2(260,68),new Color(.20f,.22f,.27f),18);upgrade.onClick.AddListener(Upgrade);move.onClick.AddListener(Move);remove.onClick.AddListener(Remove);close.onClick.AddListener(Close);overlay.gameObject.SetActive(false);}
        private void Refresh(){TrapInstanceData unit=rooms!=null?rooms.FindTrap(currentId):null;if(unit==null){Close();return;}TrapData data=database.FindById(unit.trapId);if(data==null){status.text="Trap data is missing.";return;}title.text=data.trapName;bool can=upgrades.CanUpgrade(unit.instanceId,out int gold,out int soul,out string error);int roomIndex=unit.roomInstanceId==null?-1:rooms.Layout.rooms.FindIndex(r=>r.instanceId==unit.roomInstanceId);body.text=$"{data.description}\n\nType: {data.trapType}\nLevel: {unit.level} / {data.maxLevel}\nRoom: {roomIndex+1}\nDamage: {unit.Damage(data)}\nDamage per second: {unit.DamagePerSecond(data):0.#}\nDuration: {unit.Duration(data):0.#} sec\nCooldown: {unit.Cooldown(data):0.#} sec\nTrigger Radius: {unit.TriggerRadius(data):0.#}\nSlow: {unit.SlowPercentage(data):P0}\n\nNext upgrade: {(can?$"{gold} Gold, {soul} Soul":error)}";upgrade.interactable=can;}
        private void Upgrade(){if(upgrades.TryUpgrade(currentId,out string message))status.text=message;else status.text=message;Refresh();}
        private void Move(){placement.BeginMove(currentId);Close();}
        private void Remove(){if(placement.Remove(currentId,out string message))Close();else status.text=message;}
    }
}
