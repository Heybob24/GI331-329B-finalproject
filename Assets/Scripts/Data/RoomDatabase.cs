using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Room Database", fileName = "RoomDatabase")]
    public sealed class RoomDatabase : ScriptableObject
    {
        [SerializeField] private List<RoomData> rooms = new List<RoomData>();

        private Dictionary<string, RoomData> byId;
        private Dictionary<DungeonRoomType, List<RoomData>> byType;
        private RoomData[] allRooms;

        public IReadOnlyList<RoomData> AllRooms
        {
            get { EnsureCache(); return allRooms; }
        }

        public RoomData FindById(string roomId)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(roomId)) return null;
            return byId.TryGetValue(roomId.Trim(), out RoomData data) ? data : null;
        }

        public IReadOnlyList<RoomData> GetByType(DungeonRoomType type)
        {
            EnsureCache();
            return byType.TryGetValue(type, out List<RoomData> matches) ? matches : System.Array.Empty<RoomData>();
        }

        public bool ValidateDatabase(out string error)
        {
            EnsureCache();
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomData data = rooms[i];
                if (data == null) { error = $"Room Database entry {i} is missing."; return false; }
                if (string.IsNullOrWhiteSpace(data.roomId)) { error = $"Room '{data.name}' has an empty roomId."; return false; }
                for (int j = 0; j < i; j++)
                    if (rooms[j] != null && rooms[j].roomId == data.roomId)
                    {
                        error = $"Duplicate roomId '{data.roomId}' in Room Database.";
                        return false;
                    }
            }
            error = string.Empty;
            return true;
        }

        private void OnEnable() => RebuildCache();

        private void OnValidate() => RebuildCache();

        private void EnsureCache()
        {
            if (byId == null || byType == null || allRooms == null) RebuildCache();
        }

        private void RebuildCache()
        {
            if (rooms == null) rooms = new List<RoomData>();
            byId = new Dictionary<string, RoomData>();
            byType = new Dictionary<DungeonRoomType, List<RoomData>>();
            var validRooms = new List<RoomData>();
            foreach (RoomData data in rooms)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.roomId)) continue;
                string id = data.roomId.Trim();
                if (byId.ContainsKey(id))
                {
                    Debug.LogError($"Room Database '{name}' contains duplicate roomId '{id}'.", this);
                    continue;
                }
                byId.Add(id, data);
                validRooms.Add(data);
                if (!byType.TryGetValue(data.roomType, out List<RoomData> list))
                {
                    list = new List<RoomData>();
                    byType.Add(data.roomType, list);
                }
                list.Add(data);
            }
            allRooms = validRooms.ToArray();
        }
    }
}
