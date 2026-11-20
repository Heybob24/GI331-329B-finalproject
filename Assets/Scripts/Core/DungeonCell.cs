using UnityEngine;

namespace DreamDungeon
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class DungeonCell : MonoBehaviour
    {
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;
        [SerializeField] private DungeonCellType cellType;
        [SerializeField] private bool occupied;
        [SerializeField] private string roomId;
        [SerializeField, Min(1)] private int roomLevel = 1;

        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private Color? previewColor;
        private bool selected;

        public Vector2Int Position => new Vector2Int(gridX, gridY);
        public DungeonCellType CellType => cellType;
        public bool Occupied => occupied;
        public string RoomId => roomId;

        public void Initialize(int x, int y)
        {
            gridX = x;
            gridY = y;
            cellType = DungeonCellType.Empty;
            occupied = false;
            roomId = string.Empty;
            spriteRenderer = GetComponent<SpriteRenderer>();
            RefreshColor();
        }

        public void SetOccupant(DungeonRoomType type, string associatedRoomId, int level = 1)
        {
            occupied = true;
            roomId = associatedRoomId;
            roomLevel = Mathf.Max(1, level);
            cellType = type switch
            {
                DungeonRoomType.Entrance => DungeonCellType.Entrance,
                DungeonRoomType.DungeonCore => DungeonCellType.DungeonCore,
                DungeonRoomType.MonsterRoom => DungeonCellType.MonsterRoom,
                DungeonRoomType.TreasureRoom => DungeonCellType.TreasureRoom,
                DungeonRoomType.TrapRoom => DungeonCellType.TrapRoom,
                DungeonRoomType.MagicRoom => DungeonCellType.MagicRoom,
                DungeonRoomType.BossRoom => DungeonCellType.BossRoom,
                DungeonRoomType.Path => DungeonCellType.Path,
                _ => DungeonCellType.Empty
            };
            RefreshColor();
        }

        public void ClearOccupant()
        {
            occupied = false;
            roomId = string.Empty;
            roomLevel = 1;
            cellType = DungeonCellType.Empty;
            RefreshColor();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            RefreshColor();
        }

        public void SetPreview(Color? color)
        {
            previewColor = color;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = cellType switch
            {
                DungeonCellType.Entrance => new Color(.91f, .66f, .26f),
                DungeonCellType.DungeonCore => new Color(.66f, .31f, .82f),
                DungeonCellType.MonsterRoom => new Color(.72f, .25f, .21f),
                DungeonCellType.TreasureRoom => new Color(.88f, .71f, .19f),
                DungeonCellType.TrapRoom => new Color(.22f, .48f, .42f),
                DungeonCellType.MagicRoom => new Color(.28f, .42f, .75f),
                DungeonCellType.BossRoom => new Color(.55f, .12f, .15f),
                DungeonCellType.Path => new Color(.48f, .43f, .38f),
                _ => new Color(.20f, .25f, .29f)
            };

            if (occupied && roomLevel > 1)
                baseColor = Color.Lerp(baseColor, Color.white, Mathf.Min(.28f, (roomLevel - 1) * .06f));

            spriteRenderer.color = previewColor ?? (selected ? Color.Lerp(baseColor, Color.white, .55f) : baseColor);
        }
    }
}
