using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonValidator
    {
        private readonly DungeonPathfinder pathfinder;

        public DungeonValidator(DungeonPathfinder dungeonPathfinder) => pathfinder = dungeonPathfinder;

        public bool ValidatePlacement(DungeonLayoutData layout, DungeonRoomInstanceData candidate, string ignoredInstanceId, out string error)
        {
            if (candidate == null) { error = "No room was selected."; return false; }
            int width = candidate.rotation % 180 == 0 ? candidate.width : candidate.height;
            int height = candidate.rotation % 180 == 0 ? candidate.height : candidate.width;
            if (candidate.position.x < 0 || candidate.position.y < 0 || candidate.position.x + width > layout.gridWidth || candidate.position.y + height > layout.gridHeight)
            {
                error = "That room footprint extends outside the dungeon grid.";
                return false;
            }

            foreach (DungeonRoomInstanceData room in layout.rooms)
            {
                if (room.instanceId == ignoredInstanceId) continue;
                if (Overlaps(candidate, room))
                {
                    error = "That space is already occupied.";
                    return false;
                }
            }

            var withoutMoving = new List<DungeonRoomInstanceData>();
            foreach (DungeonRoomInstanceData room in layout.rooms)
                if (room.instanceId != ignoredInstanceId) withoutMoving.Add(room);

            if (!IsAdjacentToWalkable(candidate, withoutMoving))
            {
                error = candidate.roomType == DungeonRoomType.Path
                    ? "Path must connect to the entrance, core, or an existing connected path."
                    : "Rooms must touch a path, the entrance, or the dungeon core.";
                return false;
            }

            bool hadValidRoute = pathfinder.TryFindRoute(layout, out _);
            withoutMoving.Add(candidate);
            var testLayout = new DungeonLayoutData { gridWidth = layout.gridWidth, gridHeight = layout.gridHeight, rooms = withoutMoving };
            bool hasValidRoute = pathfinder.TryFindRoute(testLayout, out _);
            if (!hasValidRoute && !(candidate.roomType == DungeonRoomType.Path && !hadValidRoute))
            {
                error = "That change would break the route between Entrance and Dungeon Core.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool ValidateRemoval(DungeonLayoutData layout, DungeonRoomInstanceData room, out string error)
        {
            if (room == null) { error = "Select a room first."; return false; }
            if (room.roomType == DungeonRoomType.Entrance || room.roomType == DungeonRoomType.DungeonCore)
            {
                error = "Entrance and Dungeon Core are permanent.";
                return false;
            }

            var remaining = new List<DungeonRoomInstanceData>(layout.rooms);
            remaining.Remove(room);
            var testLayout = new DungeonLayoutData { gridWidth = layout.gridWidth, gridHeight = layout.gridHeight, rooms = remaining };
            if (!pathfinder.TryFindRoute(testLayout, out _))
            {
                error = "That removal would break the route between Entrance and Dungeon Core.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private bool IsAdjacentToWalkable(DungeonRoomInstanceData candidate, IReadOnlyList<DungeonRoomInstanceData> rooms)
        {
            int width = candidate.rotation % 180 == 0 ? candidate.width : candidate.height;
            int height = candidate.rotation % 180 == 0 ? candidate.height : candidate.width;
            for (int x = candidate.position.x; x < candidate.position.x + width; x++)
            {
                if (pathfinder.IsWalkable(new Vector2Int(x, candidate.position.y - 1), rooms) ||
                    pathfinder.IsWalkable(new Vector2Int(x, candidate.position.y + height), rooms)) return true;
            }
            for (int y = candidate.position.y; y < candidate.position.y + height; y++)
            {
                if (pathfinder.IsWalkable(new Vector2Int(candidate.position.x - 1, y), rooms) ||
                    pathfinder.IsWalkable(new Vector2Int(candidate.position.x + width, y), rooms)) return true;
            }
            return false;
        }

        private static bool Overlaps(DungeonRoomInstanceData a, DungeonRoomInstanceData b)
        {
            int aw = a.rotation % 180 == 0 ? a.width : a.height;
            int ah = a.rotation % 180 == 0 ? a.height : a.width;
            int bw = b.rotation % 180 == 0 ? b.width : b.height;
            int bh = b.rotation % 180 == 0 ? b.height : b.width;
            return a.position.x < b.position.x + bw && a.position.x + aw > b.position.x &&
                   a.position.y < b.position.y + bh && a.position.y + ah > b.position.y;
        }
    }
}
