using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class DungeonPathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        public bool TryFindRoute(DungeonLayoutData layout, out List<Vector2Int> route)
        {
            route = new List<Vector2Int>();
            if (layout?.rooms == null) return false;

            DungeonRoomInstanceData entrance = Find(layout, DungeonRoomType.Entrance);
            DungeonRoomInstanceData core = Find(layout, DungeonRoomType.DungeonCore);
            if (entrance == null || core == null) return false;

            Vector2Int start = entrance.position;
            Vector2Int goal = core.position;
            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == goal)
                {
                    route = Reconstruct(previous, start, goal);
                    return true;
                }

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (!InBounds(next, layout) || visited.Contains(next) || !IsWalkable(next, layout.rooms)) continue;
                    visited.Add(next);
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        public bool IsWalkable(Vector2Int position, IReadOnlyList<DungeonRoomInstanceData> rooms)
        {
            foreach (DungeonRoomInstanceData room in rooms)
            {
                if (!Contains(room, position)) continue;
                return room.roomType == DungeonRoomType.Path || room.roomType == DungeonRoomType.Entrance || room.roomType == DungeonRoomType.DungeonCore;
            }
            return false;
        }

        public static bool Contains(DungeonRoomInstanceData room, Vector2Int position)
        {
            int w = room.rotation % 180 == 0 ? room.width : room.height;
            int h = room.rotation % 180 == 0 ? room.height : room.width;
            return position.x >= room.position.x && position.x < room.position.x + w &&
                   position.y >= room.position.y && position.y < room.position.y + h;
        }

        private static bool InBounds(Vector2Int position, DungeonLayoutData layout) =>
            position.x >= 0 && position.y >= 0 && position.x < layout.gridWidth && position.y < layout.gridHeight;

        private static DungeonRoomInstanceData Find(DungeonLayoutData layout, DungeonRoomType type)
        {
            foreach (DungeonRoomInstanceData room in layout.rooms)
                if (room.roomType == type) return room;
            return null;
        }

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> previous, Vector2Int start, Vector2Int goal)
        {
            var path = new List<Vector2Int> { goal };
            Vector2Int current = goal;
            while (current != start)
            {
                current = previous[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
