using System;
using UnityEngine;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>IInputHandler Unity 구현 — 마우스/터치 드래그</summary>
    public class UnityInputHandler : MonoBehaviour, IInputHandler
    {
        [Header("Settings")]
        [SerializeField] private float _dragThreshold = 0.3f;
        [SerializeField] private float _boardCellSize = 1f;

        public const int BoardSize = 8;

        // ── IInputHandler ──

        public event Action<TilePosition, TilePosition> OnTileSwapped;

        private bool _enabled;

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        // ── 내부 상태 ──

        private Vector2 _dragStartWorld;
        private TilePosition? _startTile;
        private bool _isDragging;

        private float _cameraZDist = 10f; // Camera z=-10, game plane z=0

        private Vector3 ScreenToWorldPos()
        {
            var pos = Input.mousePosition;
            pos.z = _cameraZDist;
            return Camera.main.ScreenToWorldPoint(pos);
        }

        private void Start()
        {
            string camInfo = Camera.main != null
                ? $"pos={Camera.main.transform.position} ortho={Camera.main.orthographic}"
                : "NULL";
            FileLogger.Log($"[UnityInputHandler] Start() — Camera.main: {camInfo}");
        }

        private void Update()
        {
            if (!_enabled)
            {
                if (Input.GetMouseButtonDown(0))
                    FileLogger.Log("[UnityInputHandler] _enabled == false — 입력 무시됨");
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _dragStartWorld = ScreenToWorldPos();
                FileLogger.Log($"[Input] MouseDown — screen={Input.mousePosition} world=({_dragStartWorld.x:F2},{_dragStartWorld.y:F2})");

                _startTile = WorldToTile(_dragStartWorld);
                FileLogger.Log($"[Input] WorldToTile → {(_startTile?.ToString() ?? "null")}");
                _isDragging = false;
            }

            if (Input.GetMouseButton(0) && _startTile.HasValue)
            {
                Vector2 currentWorld = ScreenToWorldPos();
                float dragDist = Vector2.Distance(currentWorld, _dragStartWorld);

                if (dragDist >= _dragThreshold)
                {
                    if (!_isDragging)
                        FileLogger.Log($"[Input] Drag started — dist={dragDist:F2}");
                    _isDragging = true;
                }
            }

            if (Input.GetMouseButtonUp(0) && _startTile.HasValue)
            {
                FileLogger.Log($"[Input] MouseUp — isDragging={_isDragging}");

                if (_isDragging)
                {
                    Vector2 endWorld = ScreenToWorldPos();
                    Vector2 dragDir = endWorld - _dragStartWorld;
                    FileLogger.Log($"[Input] Drag dir=({dragDir.x:F2},{dragDir.y:F2}) mag={dragDir.magnitude:F2}");

                    var endTile = GetAdjacentInDirection(_startTile.Value, dragDir);
                    FileLogger.Log($"[Input] Adjacent tile → {endTile?.ToString() ?? "null"}");

                    if (endTile.HasValue && IsInBounds(endTile.Value.Row, endTile.Value.Col))
                    {
                        FileLogger.Log($"[Input] Firing OnTileSwapped: {_startTile.Value} → {endTile.Value}");
                        OnTileSwapped?.Invoke(_startTile.Value, endTile.Value);
                    }
                }

                _startTile = null;
                _isDragging = false;
            }
        }

        // ── 유틸 ──

        private static bool IsInBounds(int row, int col) =>
            row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;

        private TilePosition? WorldToTile(Vector2 worldPos)
        {
            int col = Mathf.RoundToInt(worldPos.x / _boardCellSize);
            int row = Mathf.RoundToInt(worldPos.y / _boardCellSize);

            if (IsInBounds(row, col))
                return new TilePosition(row, col);
            return null;
        }

        private TilePosition? GetAdjacentInDirection(TilePosition from, Vector2 direction)
        {
            float ax = Mathf.Abs(direction.x);
            float ay = Mathf.Abs(direction.y);

            if (ax > ay)
            {
                // 가로 방향
                int dc = direction.x > 0 ? 1 : -1;
                int nc = from.Col + dc;
                if (IsInBounds(from.Row, nc))
                    return new TilePosition(from.Row, nc);
            }
            else
            {
                // 세로 방향
                int dr = direction.y > 0 ? 1 : -1;
                int nr = from.Row + dr;
                if (IsInBounds(nr, from.Col))
                    return new TilePosition(nr, from.Col);
            }

            return null;
        }
    }
}
