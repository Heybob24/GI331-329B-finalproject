using UnityEngine;

namespace DreamDungeon
{
    public sealed class PlacementPreview : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        private readonly Color validColor = new Color(.18f, .9f, .32f, 1f);
        private readonly Color invalidColor = new Color(.95f, .18f, .18f, 1f);

        public void Initialize(DungeonGrid dungeonGrid) => grid = dungeonGrid;

        public void Configure(DungeonGrid dungeonGrid) => grid = dungeonGrid;

        public void Show(RoomData data, Vector2Int position, int rotation, bool valid)
        {
            if (grid == null || data == null) return;
            grid.ClearPreviews();
            int width = rotation % 180 == 0 ? data.width : data.height;
            int height = rotation % 180 == 0 ? data.height : data.width;
            Paint(position, width, height, valid);
        }

        public void Show(DungeonRoomInstanceData room, Vector2Int position, bool valid)
        {
            if (grid == null || room == null) return;
            grid.ClearPreviews();
            int width = room.rotation % 180 == 0 ? room.width : room.height;
            int height = room.rotation % 180 == 0 ? room.height : room.width;
            Paint(position, width, height, valid);
        }

        public void ShowPath(Vector2Int position, bool valid) => Paint(position, 1, 1, valid);

        public void Clear()
        {
            if (grid != null) grid.ClearPreviews();
        }

        private void Paint(Vector2Int position, int width, int height, bool valid)
        {
            Color color = valid ? validColor : invalidColor;
            for (int y = position.y; y < position.y + height; y++)
                for (int x = position.x; x < position.x + width; x++)
                    grid.SetPreview(new Vector2Int(x, y), color);
        }
    }
}
