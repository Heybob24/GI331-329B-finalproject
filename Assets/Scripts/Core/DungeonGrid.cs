using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DreamDungeon
{
    [DefaultExecutionOrder(-100)]
    public sealed class DungeonGrid : MonoBehaviour
    {
        [SerializeField, Min(1)] private int width = 9;
        [SerializeField, Min(1)] private int height = 9;
        [SerializeField, Min(.1f)] private float cellSize = .62f;
        [SerializeField] private Vector2 worldCenter = new Vector2(0, .45f);
        [SerializeField] private Camera worldCamera;

        private DungeonCell[,] cells;
        private DungeonCell selectedCell;
        private Sprite cellSprite;
        private bool initialized;
        private bool pointerCandidate;
        private Vector2 pointerStartPosition;
        private int candidatePointerId = -1;
        private const float ClickThreshold = 12f;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector2 WorldCenter => worldCenter;
        public Camera WorldCamera => worldCamera;
        public DungeonCell SelectedCell => selectedCell;
        public event Action<DungeonCell> CellSelected;

        public void Configure(int gridWidth, int gridHeight, float size, Vector2 center, Camera cameraToUse)
        {
            width = Mathf.Max(1, gridWidth);
            height = Mathf.Max(1, gridHeight);
            cellSize = Mathf.Max(.1f, size);
            worldCenter = center;
            worldCamera = cameraToUse;
            if (Application.isPlaying) Initialize();
        }

        public void RebuildGrid(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);

            Transform existingRoot = transform.Find("GridCells");
            if (existingRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(existingRoot.gameObject);
                }
            }

            initialized = false;
            Initialize();
        }

        public void Initialize()
        {
            if (initialized) return;
            if (worldCamera == null) worldCamera = Camera.main;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "DungeonCellTexture", filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            cellSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            cellSprite.name = "DungeonCellSprite";

            cells = new DungeonCell[width, height];
            Transform gridRoot = new GameObject("GridCells").transform;
            gridRoot.SetParent(transform, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject cellObject = new GameObject($"Cell_{x}_{y}");
                    cellObject.transform.SetParent(gridRoot, false);
                    cellObject.transform.position = CellToWorld(new Vector2Int(x, y));
                    cellObject.transform.localScale = Vector3.one * (cellSize - .035f);
                    SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = cellSprite;
                    renderer.sortingOrder = 0;
                    DungeonCell cell = cellObject.AddComponent<DungeonCell>();
                    cell.Initialize(x, y);
                    cells[x, y] = cell;
                }
            }
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) Initialize();
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerCandidate = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
                pointerStartPosition = Mouse.current.position.ReadValue();
                candidatePointerId = -1;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Vector2 releasePosition = Mouse.current.position.ReadValue();
                if (pointerCandidate && (releasePosition - pointerStartPosition).sqrMagnitude <= ClickThreshold * ClickThreshold &&
                    (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) SelectAtScreen(releasePosition);
                pointerCandidate = false;
            }

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    int pointerId = touch.touchId.ReadValue();
                    pointerCandidate = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(pointerId);
                    pointerStartPosition = touch.position.ReadValue();
                    candidatePointerId = pointerId;
                }
                if (touch.press.wasReleasedThisFrame)
                {
                    Vector2 releasePosition = touch.position.ReadValue();
                    bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(candidatePointerId);
                    if (pointerCandidate && !pointerOverUi && (releasePosition - pointerStartPosition).sqrMagnitude <= ClickThreshold * ClickThreshold)
                        SelectAtScreen(releasePosition);
                    pointerCandidate = false;
                }
            }
        }

        public bool TryGetCell(Vector2Int position, out DungeonCell cell)
        {
            cell = null;
            if (!initialized || position.x < 0 || position.y < 0 || position.x >= width || position.y >= height) return false;
            cell = cells[position.x, position.y];
            return cell != null;
        }

        public bool TryGetCellAtScreen(Vector2 screenPosition, out DungeonCell cell)
        {
            cell = null;
            if (!initialized || worldCamera == null) return false;
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            Vector2Int position = WorldToCell(world);
            return TryGetCell(position, out cell);
        }

        public Vector3 CellToWorld(Vector2Int position)
        {
            float left = worldCenter.x - (width - 1) * cellSize * .5f;
            float bottom = worldCenter.y - (height - 1) * cellSize * .5f;
            return new Vector3(left + position.x * cellSize, bottom + position.y * cellSize, 0f);
        }

        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            float left = worldCenter.x - width * cellSize * .5f;
            float bottom = worldCenter.y - height * cellSize * .5f;
            return new Vector2Int(Mathf.FloorToInt((worldPosition.x - left) / cellSize), Mathf.FloorToInt((worldPosition.y - bottom) / cellSize));
        }

        public void SelectCell(DungeonCell cell)
        {
            if (selectedCell != null) selectedCell.SetSelected(false);
            selectedCell = cell;
            if (selectedCell != null) selectedCell.SetSelected(true);
            CellSelected?.Invoke(selectedCell);
        }

        public void ClearPreviews()
        {
            if (cells == null) return;
            foreach (DungeonCell cell in cells) cell.SetPreview(null);
        }

        public void SetPreview(Vector2Int position, Color color)
        {
            if (TryGetCell(position, out DungeonCell cell)) cell.SetPreview(color);
        }

        public void RefreshCellStates(DungeonLayoutData layout)
        {
            Initialize();
            foreach (DungeonCell cell in cells) cell.ClearOccupant();
            if (layout?.rooms == null) return;

            foreach (DungeonRoomInstanceData room in layout.rooms)
            {
                int roomWidth = room.rotation % 180 == 0 ? room.width : room.height;
                int roomHeight = room.rotation % 180 == 0 ? room.height : room.width;
                for (int y = room.position.y; y < room.position.y + roomHeight; y++)
                    for (int x = room.position.x; x < room.position.x + roomWidth; x++)
                        if (TryGetCell(new Vector2Int(x, y), out DungeonCell cell)) cell.SetOccupant(room.roomType, room.instanceId, room.upgradeLevel);
            }
        }

        private void SelectAtScreen(Vector2 screenPosition)
        {
            if (TryGetCellAtScreen(screenPosition, out DungeonCell cell)) SelectCell(cell);
        }

        private void OnDestroy()
        {
            if (cellSprite != null)
            {
                if (cellSprite.texture != null) Destroy(cellSprite.texture);
                Destroy(cellSprite);
            }
        }
    }
}
