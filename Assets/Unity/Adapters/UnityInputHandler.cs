using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>IInputHandler Unity 구현 — Input System API 사용</summary>
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
        private bool _wasPressed; // 이전 프레임 상태 추적용
        private bool _isPressed;

        private float _cameraZDist = 10f; // Camera z=-10, game plane z=0

        private Vector3 ScreenToWorldPos(Vector2 screenPos)
        {
            var pos = new Vector3(screenPos.x, screenPos.y, _cameraZDist);
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
            // 마우스 상태 읽기
            var mouse = Mouse.current;
            if (mouse == null) return;

            _wasPressed = _isPressed;
            _isPressed = mouse.leftButton.isPressed;
            bool justPressed = _isPressed && !_wasPressed;
            bool justReleased = !_isPressed && _wasPressed;

            if (!_enabled)
            {
                if (justPressed)
                    FileLogger.Log("[Input] _enabled == false — 입력 무시됨");
                return;
            }

            Vector2 screenPos = mouse.position.ReadValue();

            if (justPressed)
            {
                _dragStartWorld = ScreenToWorldPos(screenPos);
                FileLogger.Log($"[Input] MouseDown — screen={screenPos} world=({_dragStartWorld.x:F2},{_dragStartWorld.y:F2})");

                _startTile = WorldToTile(_dragStartWorld);
                FileLogger.Log($"[Input] WorldToTile → {(_startTile?.ToString() ?? "null")}");
                _isDragging = false;
            }

            if (_isPressed && _startTile.HasValue)
            {
                Vector2 currentWorld = ScreenToWorldPos(screenPos);
                float dragDist = Vector2.Distance(currentWorld, _dragStartWorld);

                if (dragDist >= _dragThreshold && !_isDragging)
                {
                    FileLogger.Log($"[Input] Drag started — dist={dragDist:F2}");
                    _isDragging = true;
                }
            }

            if (justReleased && _startTile.HasValue)
            {
                FileLogger.Log($"[Input] MouseUp — isDragging={_isDragging}");

                if (_isDragging)
                {
                    Vector2 endWorld = ScreenToWorldPos(screenPos);
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
                int dc = direction.x > 0 ? 1 : -1;
                int nc = from.Col + dc;
                if (IsInBounds(from.Row, nc))
                    return new TilePosition(from.Row, nc);
            }
            else
            {
                int dr = direction.y > 0 ? 1 : -1;
                int nr = from.Row + dr;
                if (IsInBounds(nr, from.Col))
                    return new TilePosition(nr, from.Col);
            }

            return null;
        }
    }
}
