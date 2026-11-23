using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum CharacterCategory
    {
        Adventurer,
        Monster,
        Boss
    }

    [Serializable]
    public sealed class CharacterAnimationEntry
    {
        public string characterId;
        public string displayName;
        public CharacterCategory characterType;
        public string movementType;
        public string attackType;
        public string gameplayEntityId;
        public Sprite idleSprite;
        public RuntimeAnimatorController animatorController;
        public GameObject prefab;
    }

    [CreateAssetMenu(fileName = "CharacterAnimationDatabase", menuName = "Dream Dungeon/Character Animation Database")]
    public sealed class CharacterAnimationDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterAnimationEntry> entries = new List<CharacterAnimationEntry>();

        public IReadOnlyList<CharacterAnimationEntry> Entries => entries;

        public void SetEntries(List<CharacterAnimationEntry> list)
        {
            entries = list ?? new List<CharacterAnimationEntry>();
        }

        public CharacterAnimationEntry FindById(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && string.Equals(entries[i].characterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    return entries[i];
                }
            }
            return null;
        }

        public CharacterAnimationEntry FindByGameplayId(string gameplayId)
        {
            if (string.IsNullOrWhiteSpace(gameplayId) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && string.Equals(entries[i].gameplayEntityId, gameplayId, StringComparison.OrdinalIgnoreCase))
                {
                    return entries[i];
                }
            }
            return null;
        }
    }
}
