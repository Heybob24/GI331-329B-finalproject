using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class MainQuestManager : MonoBehaviour
    {
        public static MainQuestManager Instance { get; private set; }

        private QuestManager questManager;

        public event Action<QuestData> OnMainQuestUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(QuestManager manager)
        {
            questManager = manager;
            EvaluateMainQuestSequence(notifyNewUnlocks: false);
        }

        public IReadOnlyList<QuestData> GetMainQuests()
        {
            if (questManager == null || questManager.Database == null) return Array.Empty<QuestData>();
            return questManager.Database.GetByType(QuestType.Main);
        }

        public bool IsMainQuestUnlocked(QuestData quest)
        {
            if (quest == null || questManager == null) return false;
            if (string.IsNullOrWhiteSpace(quest.unlockRequirement)) return true;

            QuestProgressEntry prereq = questManager.GetProgressEntry(quest.unlockRequirement);
            if (prereq == null) return false;
            return prereq.status == QuestStatus.Completed || prereq.status == QuestStatus.Claimed;
        }

        public void EvaluateMainQuestSequence(bool notifyNewUnlocks = true)
        {
            if (questManager == null || questManager.Database == null) return;
            IReadOnlyList<QuestData> mainQuests = questManager.Database.GetByType(QuestType.Main);

            foreach (QuestData quest in mainQuests)
            {
                if (quest == null || !quest.isActive) continue;
                QuestProgressEntry entry = questManager.GetOrCreateEntry(quest);
                bool unlocked = IsMainQuestUnlocked(quest);

                if (!unlocked)
                {
                    if (entry.status != QuestStatus.Completed && entry.status != QuestStatus.Claimed)
                    {
                        entry.status = QuestStatus.Locked;
                    }
                }
                else
                {
                    bool wasLocked = entry.status == QuestStatus.Locked;
                    questManager.MarkMainQuestUnlocked(quest.questId);
                    if (wasLocked)
                    {
                        entry.status = entry.currentProgress >= quest.targetAmount
                            ? QuestStatus.Completed
                            : QuestStatus.InProgress;
                        if (notifyNewUnlocks)
                        {
                            OnMainQuestUnlocked?.Invoke(quest);
                        }
                    }
                }
            }
        }
    }
}
