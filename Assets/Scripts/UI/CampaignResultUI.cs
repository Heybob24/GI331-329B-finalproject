using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class CampaignResultUI : MonoBehaviour
    {
        private BattleManager battleManager;
        private Button worldMapButton;
        private Button nextStageButton;

        public void AttachToResultPanel(BattleManager manager, Transform resultPanelTransform)
        {
            battleManager = manager;
            if (resultPanelTransform == null) return;

            if (worldMapButton == null)
            {
                worldMapButton = CreateActionButton(resultPanelTransform, "ReturnWorldMapButton", "RETURN TO WORLD MAP",
                    new Vector2(-205f, -310f), new Vector2(380f, 82f), new Color(0.20f, 0.34f, 0.48f), OnReturnToWorldMap);
            }

            if (nextStageButton == null)
            {
                nextStageButton = CreateActionButton(resultPanelTransform, "NextStageButton", "NEXT STAGE",
                    new Vector2(205f, -310f), new Vector2(380f, 82f), new Color(0.22f, 0.56f, 0.34f), OnNextStage);
            }
        }

        public string FormatCampaignResultDetails(bool victory, int wave, int defeated, int coreHp)
        {
            if (!BattleSession.IsCampaignBattle)
            {
                return victory
                    ? $"All {wave} Waves Cleared!\nAdventurers Defeated: {defeated}\nCore Integrity: {coreHp} HP"
                    : $"Defeated on Wave {wave}\nAdventurers Defeated: {defeated}\nCore Destroyed";
            }

            string regionStr = !string.IsNullOrEmpty(BattleSession.RegionId) ? BattleSession.RegionId : "forgotten_forest";
            string stageStr = !string.IsNullOrEmpty(BattleSession.StageId) ? BattleSession.StageId : "forest_stage_1";
            string diffStr = BattleSession.Difficulty.ToString();
            string bossStr = !string.IsNullOrEmpty(BattleSession.StageBossId)
                ? (victory ? $"Boss Defeated: {BattleSession.StageBossId}" : $"Boss Encounter: {BattleSession.StageBossId}")
                : "Boss: None";
            string clearBadges = "";
            if (victory)
            {
                string fc = BattleSession.IsFirstClearResult ? "[FIRST CLEAR!] " : "[REPLAY CLEAR] ";
                string pc = BattleSession.IsPerfectClearResult ? "[PERFECT CLEAR ★]" : "";
                clearBadges = $"\n{fc}{pc}";
            }

            return $"Region: {regionStr}   •   Stage: {stageStr}\n" +
                   $"Difficulty: {diffStr}   •   Wave Reached: {wave}\n" +
                   $"Adventurers Defeated: {defeated}   •   {bossStr}\n" +
                   $"Core HP Remaining: {coreHp} HP{clearBadges}";
        }

        public void RefreshButtonStates(bool victory)
        {
            bool isCampaign = BattleSession.IsCampaignBattle;
            if (worldMapButton != null)
            {
                worldMapButton.gameObject.SetActive(isCampaign);
            }
            if (nextStageButton != null)
            {
                string nextStageId = GetNextStageId();
                nextStageButton.gameObject.SetActive(isCampaign && victory && !string.IsNullOrEmpty(nextStageId));
            }
        }

        private string GetNextStageId()
        {
            if (!BattleSession.IsCampaignBattle || string.IsNullOrEmpty(BattleSession.StageId)) return null;
            CampaignManager cm = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>();
            CampaignStageData next = cm != null ? cm.GetNextStage(BattleSession.StageId) : null;
            return next != null ? next.stageId : null;
        }

        private void OnReturnToWorldMap()
        {
            battleManager?.ReturnToWorldMap(null);
        }

        private void OnNextStage()
        {
            string nextId = GetNextStageId();
            battleManager?.ReturnToWorldMap(nextId);
        }

        private static Button CreateActionButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            RectTransform rt = RoomUIFactory.CreatePanel(parent, name, color, anchoredPos, size);
            Button btn = rt.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(action);
            RoomUIFactory.CreateText(rt, name + "Label", label, 21, Vector2.zero, size - new Vector2(16f, 10f), Color.white);
            return btn;
        }
    }
}
