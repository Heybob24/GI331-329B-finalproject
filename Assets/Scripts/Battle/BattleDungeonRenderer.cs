using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [DisallowMultipleComponent]
    public sealed class BattleDungeonRenderer : MonoBehaviour
    {
        [Header("Grid References")]
        [SerializeField] private DungeonGrid grid;
        [SerializeField, Min(0.1f)] private float cellSize = 0.62f;
        [SerializeField] private Vector2 worldCenter = new Vector2(0f, 0.45f);

        private readonly List<GameObject> visualObjects = new List<GameObject>();
        private Sprite whiteSprite;
        private DungeonLayoutData currentLayout;

        public DungeonGrid Grid => grid;
        public float CellSize => cellSize;
        public Vector2 WorldCenter => worldCenter;
        public DungeonLayoutData Layout => currentLayout;

        private void Awake()
        {
            EnsureSprite();
        }

        private void EnsureSprite()
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "BattleDungeonTexture",
                    filterMode = FilterMode.Point
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                whiteSprite.name = "BattleDungeonSprite";
            }
        }

        public void Render(DungeonLayoutData layout, float size, Vector2 center, Camera cameraToUse)
        {
            Clear();
            EnsureSprite();

            currentLayout = layout;
            cellSize = Mathf.Max(0.1f, size);
            worldCenter = center;

            // 1. Initialize or Configure DungeonGrid
            if (grid == null)
            {
                grid = GetComponentInChildren<DungeonGrid>();
                if (grid == null)
                {
                    GameObject gridGo = new GameObject("BattleDungeonGrid");
                    gridGo.transform.SetParent(transform, false);
                    grid = gridGo.AddComponent<DungeonGrid>();
                }
            }

            grid.Configure(layout.gridWidth, layout.gridHeight, cellSize, worldCenter, cameraToUse);
            grid.Initialize();
            grid.RefreshCellStates(layout);

            // 2. Spawn Outer Perimeter Bedrock Walls (Surrounding 9x9 grid)
            Transform wallRoot = new GameObject("PerimeterWalls").transform;
            wallRoot.SetParent(transform, false);
            visualObjects.Add(wallRoot.gameObject);

            int width = layout.gridWidth;
            int height = layout.gridHeight;
            Color perimeterColor = new Color(0.12f, 0.15f, 0.18f);

            for (int x = -1; x <= width; x++)
            {
                for (int y = -1; y <= height; y++)
                {
                    // Only render boundary cells outside the interior grid
                    if (x >= 0 && x < width && y >= 0 && y < height) continue;

                    Vector3 pos = CellToWorld(new Vector2Int(x, y));
                    GameObject wallCell = new GameObject($"OuterWall_{x}_{y}");
                    wallCell.transform.SetParent(wallRoot, false);
                    wallCell.transform.position = pos;
                    wallCell.transform.localScale = Vector3.one * (cellSize - 0.035f);

                    SpriteRenderer sr = wallCell.AddComponent<SpriteRenderer>();
                    sr.sprite = whiteSprite;
                    sr.color = perimeterColor;
                    sr.sortingOrder = 0;
                }
            }

            // 3. Spawn Room Outlines & Subtle Floor Frames (SortingOrder = 1)
            Transform outlineRoot = new GameObject("RoomBorders").transform;
            outlineRoot.SetParent(transform, false);
            visualObjects.Add(outlineRoot.gameObject);

            if (layout.rooms != null)
            {
                foreach (DungeonRoomInstanceData room in layout.rooms)
                {
                    if (room == null || room.roomType == DungeonRoomType.Path) continue;

                    int roomW = room.rotation % 180 == 0 ? room.width : room.height;
                    int roomH = room.rotation % 180 == 0 ? room.height : room.width;

                    Vector3 roomCenter = CellToWorld(room.position) + new Vector3((roomW - 1) * cellSize * 0.5f, (roomH - 1) * cellSize * 0.5f, 0f);

                    // Add subtle border quad
                    GameObject border = new GameObject($"Border_{room.roomType}_{room.instanceId}");
                    border.transform.SetParent(outlineRoot, false);
                    border.transform.position = roomCenter;
                    border.transform.localScale = new Vector3(roomW * cellSize - 0.02f, roomH * cellSize - 0.02f, 1f);

                    SpriteRenderer bsr = border.AddComponent<SpriteRenderer>();
                    bsr.sprite = whiteSprite;
                    Color frameColor = room.roomType switch
                    {
                        DungeonRoomType.Entrance => new Color(1f, 0.78f, 0.35f, 0.30f),
                        DungeonRoomType.DungeonCore => new Color(0.75f, 0.40f, 0.95f, 0.30f),
                        DungeonRoomType.MonsterRoom => new Color(0.85f, 0.30f, 0.25f, 0.25f),
                        DungeonRoomType.TreasureRoom => new Color(1f, 0.85f, 0.25f, 0.25f),
                        DungeonRoomType.TrapRoom => new Color(0.25f, 0.65f, 0.55f, 0.25f),
                        DungeonRoomType.MagicRoom => new Color(0.35f, 0.55f, 0.90f, 0.25f),
                        DungeonRoomType.BossRoom => new Color(0.70f, 0.15f, 0.18f, 0.30f),
                        _ => new Color(0.40f, 0.45f, 0.50f, 0.20f)
                    };
                    bsr.color = frameColor;
                    bsr.sortingOrder = 1;
                }
            }
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            if (grid != null) return grid.CellToWorld(cell);

            int width = currentLayout != null ? currentLayout.gridWidth : 9;
            int height = currentLayout != null ? currentLayout.gridHeight : 9;
            float left = worldCenter.x - (width - 1) * cellSize * 0.5f;
            float bottom = worldCenter.y - (height - 1) * cellSize * 0.5f;
            return new Vector3(left + cell.x * cellSize, bottom + cell.y * cellSize, 0f);
        }

        public Vector3 GetUnitWorldPosition(DungeonRoomInstanceData room, Vector2 localPos, float z = 0f)
        {
            if (room == null) return Vector3.zero;

            int width = room.rotation % 180 == 0 ? room.width : room.height;
            int height = room.rotation % 180 == 0 ? room.height : room.width;

            Vector3 center = CellToWorld(room.position) + new Vector3((width - 1) * cellSize * 0.5f, (height - 1) * cellSize * 0.5f, z);
            return center + new Vector3(localPos.x * width * cellSize, localPos.y * height * cellSize, 0f);
        }

        public void Clear()
        {
            foreach (GameObject obj in visualObjects)
            {
                if (obj != null) Destroy(obj);
            }
            visualObjects.Clear();
        }

        private void OnDestroy()
        {
            Clear();
            if (whiteSprite != null)
            {
                if (whiteSprite.texture != null) Destroy(whiteSprite.texture);
                Destroy(whiteSprite);
            }
        }
    }
}
