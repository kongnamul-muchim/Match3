using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>IBoardRenderer Unity 구현 — DOTween 없이 코루틴 기반 애니메이션</summary>
    public class UnityBoardRenderer : MonoBehaviour, IBoardRenderer
    {
        [Header("Settings")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private float _removeDuration = 0.25f;
        [SerializeField] private float _dropDurationPerUnit = 0.08f;
        [SerializeField] private float _newTileDuration = 0.3f;
        [SerializeField] private float _hintPulseSpeed = 2f;

        [Header("Gem Sprites (5개 — Red/Blue/Green/Yellow/Purple)")]
        [SerializeField] private Sprite[] _gemSprites = new Sprite[5];

        private Gem[,] _grid;
        private GemPool _pool;
        private Transform _gemContainer;
        private int _rows, _cols;

        // ── Placeholder colors (스프라이트 없을 때 사용) ──
        private static readonly Color[] GemColors = new[]
        {
            new Color(1f, 0.2f, 0.2f),   // Red
            new Color(0.2f, 0.4f, 1f),   // Blue
            new Color(0.2f, 0.85f, 0.2f),// Green
            new Color(1f, 0.9f, 0.1f),   // Yellow
            new Color(0.7f, 0.2f, 0.9f)  // Purple
        };

        private Sprite[] _placeholderSprites;
        private bool _hasCustomSprites;
        private Coroutine _hintCoroutine;

        // ──────────────────────────────────────────────
        //  IBoardRenderer
        // ──────────────────────────────────────────────

        public void Initialize(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;

            // 기존 그리드 정리
            ClearAll();

            if (_gemContainer == null)
            {
                _gemContainer = new GameObject("Gems").transform;
                _gemContainer.SetParent(transform);
            }

            // 템플릿 Gem 생성
            var templateGO = new GameObject("GemTemplate");
            templateGO.transform.SetParent(_gemContainer);
            templateGO.SetActive(false);
            var template = templateGO.AddComponent<Gem>();
            template.SpriteRenderer.sortingOrder = 1;

            _grid = new Gem[rows, cols];
            _pool = new GemPool(template, _gemContainer, rows * cols);

            // 플레이스홀더 스프라이트 준비
            GeneratePlaceholderSprites();

            // 메인 카메라 위치/크기 조정
            AdjustCamera();
        }

        public void UpdateTile(int row, int col, GemType type)
        {
            if (!IsInBounds(row, col)) return;

            var gem = _grid[row, col];
            if (gem != null)
            {
                gem.Type = type;
                gem.SetSprite(GetSprite(type));
                gem.SetColor(GetGemColor(type));
            }
            else
            {
                gem = _pool.Get();
                gem.transform.position = GridToWorld(row, col);
                gem.Type = type;
                gem.Row = row;
                gem.Col = col;
                gem.SetSprite(GetSprite(type));
                gem.SetColor(GetGemColor(type));
                _grid[row, col] = gem;
            }
        }

        public void AnimateSwap(TilePosition a, TilePosition b, Action onComplete)
        {
            StartCoroutine(AnimateSwapRoutine(a, b, onComplete));
        }

        public void AnimateRemove(List<TilePosition> positions, Action onComplete)
        {
            StartCoroutine(AnimateRemoveRoutine(positions, onComplete));
        }

        public void AnimateDrop(List<DropInfo> drops, Action onComplete)
        {
            StartCoroutine(AnimateDropRoutine(drops, onComplete));
        }

        public void AnimateNewTile(TilePosition pos, GemType type, Action onComplete)
        {
            StartCoroutine(AnimateNewTileRoutine(pos, type, onComplete));
        }

        public void ShowHint(List<TilePosition> positions)
        {
            if (_hintCoroutine != null)
                StopCoroutine(_hintCoroutine);
            _hintCoroutine = StartCoroutine(HintPulseRoutine(positions));
        }

        public void ClearHighlights()
        {
            if (_hintCoroutine != null)
            {
                StopCoroutine(_hintCoroutine);
                _hintCoroutine = null;
            }

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (_grid[r, c] != null)
                        _grid[r, c].SetColor(GetGemColor(_grid[r, c].Type));
                }
            }
        }

        // ──────────────────────────────────────────────
        //  내부
        // ──────────────────────────────────────────────

        private void ClearAll()
        {
            if (_pool != null)
            {
                _pool.ReleaseAll();
                _pool = null;
            }
            _grid = null;
        }

        private bool IsInBounds(int r, int c) =>
            r >= 0 && r < _rows && c >= 0 && c < _cols;

        private Vector3 GridToWorld(int row, int col) =>
            new Vector3(col * _cellSize, row * _cellSize, 0f);

        private void AdjustCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float cx = (_cols - 1) * _cellSize * 0.5f;
            float cy = (_rows - 1) * _cellSize * 0.5f;
            cam.transform.position = new Vector3(cx, cy, -10f);

            float h = _rows * _cellSize * 0.5f + 1f;
            float w = _cols * _cellSize * 0.5f / cam.aspect + 1f;
            cam.orthographicSize = Mathf.Max(h, w);
        }

        // ── 스프라이트 ──

        private void GeneratePlaceholderSprites()
        {
            // 커스텀 스프라이트가 전부 할당됐는지 확인
            _hasCustomSprites = true;
            for (int i = 0; i < 5; i++)
            {
                if (_gemSprites == null || i >= _gemSprites.Length || _gemSprites[i] == null)
                {
                    _hasCustomSprites = false;
                    break;
                }
            }
            if (_hasCustomSprites) return;

            _placeholderSprites = new Sprite[5];
            int size = 64;

            for (int i = 0; i < 5; i++)
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float radius = size / 2f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - radius + 0.5f;
                        float dy = y - radius + 0.5f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        float alpha;
                        if (dist <= radius - 1.5f)
                            alpha = 1f;
                        else if (dist <= radius)
                            alpha = radius - dist + 0.5f; // 안티앨리어싱
                        else
                            alpha = 0f;

                        // 광택 효과
                        float highlight = 1f - (dx * dx + dy * dy) / (radius * radius) * 0.25f;
                        var color = GemColors[i] * highlight;
                        color.a = Mathf.Clamp01(alpha);
                        tex.SetPixel(x, y, color);
                    }
                }
                tex.Apply();
                _placeholderSprites[i] = Sprite.Create(
                    tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size
                );
            }
        }

        private Sprite GetSprite(GemType type)
        {
            int idx = (int)type;
            if (_hasCustomSprites && _gemSprites != null && idx < _gemSprites.Length)
                return _gemSprites[idx];
            if (_placeholderSprites != null && idx < _placeholderSprites.Length)
                return _placeholderSprites[idx];
            return null;
        }

        private Color GetGemColor(GemType type)
        {
            int idx = (int)type;
            if (idx >= 0 && idx < GemColors.Length)
                return GemColors[idx];
            return Color.white;
        }

        // ── 애니메이션 코루틴 ──

        private IEnumerator AnimateSwapRoutine(TilePosition a, TilePosition b, Action onComplete)
        {
            var gemA = _grid[a.Row, a.Col];
            var gemB = _grid[b.Row, b.Col];

            Vector3 posA = GridToWorld(a.Row, a.Col);
            Vector3 posB = GridToWorld(b.Row, b.Col);

            // Swap 애니 중 더 위에 있는 Gem이 앞에 보이도록
            if (gemA != null) gemA.SpriteRenderer.sortingOrder = 10;
            if (gemB != null) gemB.SpriteRenderer.sortingOrder = 10;

            float elapsed = 0f;
            while (elapsed < _swapDuration)
            {
                float t = elapsed / _swapDuration;
                float s = t * t * (3f - 2f * t); // smoothstep

                if (gemA != null) gemA.transform.position = Vector3.Lerp(posA, posB, s);
                if (gemB != null) gemB.transform.position = Vector3.Lerp(posB, posA, s);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 최종 위치 보정
            if (gemA != null) { gemA.transform.position = posB; gemA.SpriteRenderer.sortingOrder = 1; }
            if (gemB != null) { gemB.transform.position = posA; gemB.SpriteRenderer.sortingOrder = 1; }

            // 그리드 참조 교체
            _grid[a.Row, a.Col] = gemB;
            _grid[b.Row, b.Col] = gemA;
            if (gemA != null) { gemA.Row = b.Row; gemA.Col = b.Col; }
            if (gemB != null) { gemB.Row = a.Row; gemB.Col = a.Col; }

            onComplete?.Invoke();
        }

        private IEnumerator AnimateRemoveRoutine(List<TilePosition> positions, Action onComplete)
        {
            if (positions.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            var startScales = new Dictionary<Gem, Vector3>();
            foreach (var pos in positions)
            {
                var gem = _grid[pos.Row, pos.Col];
                if (gem != null)
                    startScales[gem] = gem.transform.localScale;
            }

            float elapsed = 0f;
            while (elapsed < _removeDuration)
            {
                float t = elapsed / _removeDuration;
                float scale = 1f - t * t; // ease-in

                foreach (var gem in startScales.Keys)
                {
                    if (gem != null)
                        gem.transform.localScale = startScales[gem] * scale;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 제거된 Gem 풀에 반환
            foreach (var pos in positions)
            {
                var gem = _grid[pos.Row, pos.Col];
                if (gem != null)
                {
                    gem.transform.localScale = Vector3.one;
                    _pool.Release(gem);
                    _grid[pos.Row, pos.Col] = null;
                }
            }

            onComplete?.Invoke();
        }

        private IEnumerator AnimateDropRoutine(List<DropInfo> drops, Action onComplete)
        {
            if (drops.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // 드롭 정보 정리 + 그리드 참조 업데이트
            var moving = new List<(Gem gem, Vector3 from, Vector3 to)>();
            float maxDistance = 0f;

            foreach (var drop in drops)
            {
                var gem = _grid[drop.From.Row, drop.From.Col];
                if (gem == null) continue;

                Vector3 fromPos = GridToWorld(drop.From.Row, drop.From.Col);
                Vector3 toPos = GridToWorld(drop.To.Row, drop.To.Col);
                moving.Add((gem, fromPos, toPos));

                _grid[drop.To.Row, drop.To.Col] = gem;
                _grid[drop.From.Row, drop.From.Col] = null;
                gem.Row = drop.To.Row;
                gem.Col = drop.To.Col;

                if (drop.Distance > maxDistance)
                    maxDistance = drop.Distance;
            }

            float duration = Mathf.Clamp(maxDistance * _dropDurationPerUnit, 0.15f, 0.5f);

            // 약간의 바운스 효과 (중력)
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // ease-out + bounce
                float s = 1f - Mathf.Pow(1f - t, 2f) * (1f - t * 0.3f);

                foreach (var (gem, from, to) in moving)
                {
                    if (gem != null)
                        gem.transform.position = Vector3.Lerp(from, to, s);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var (gem, _, to) in moving)
            {
                if (gem != null)
                    gem.transform.position = to;
            }

            onComplete?.Invoke();
        }

        private IEnumerator AnimateNewTileRoutine(TilePosition pos, GemType type, Action onComplete)
        {
            var gem = _pool.Get();
            gem.transform.position = GridToWorld(pos.Row, pos.Col);
            gem.Type = type;
            gem.Row = pos.Row;
            gem.Col = pos.Col;
            gem.SetSprite(GetSprite(type));
            gem.SetColor(GetGemColor(type));
            _grid[pos.Row, pos.Col] = gem;

            // Pop-in 애니메이션
            gem.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < _newTileDuration)
            {
                float t = elapsed / _newTileDuration;
                // overshoot bounce
                float s = t < 0.7f
                    ? (t / 0.7f) * (t / 0.7f)
                    : 1f + 0.15f * Mathf.Sin((t - 0.7f) / 0.3f * Mathf.PI);

                gem.transform.localScale = Vector3.one * Mathf.Min(s, 1.15f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            gem.transform.localScale = Vector3.one;

            onComplete?.Invoke();
        }

        private IEnumerator HintPulseRoutine(List<TilePosition> positions)
        {
            var hintedGems = new List<Gem>();
            foreach (var pos in positions)
            {
                var gem = _grid[pos.Row, pos.Col];
                if (gem != null)
                    hintedGems.Add(gem);
            }

            if (hintedGems.Count == 0) yield break;

            float t = 0f;
            while (true)
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(t * _hintPulseSpeed);
                foreach (var gem in hintedGems)
                {
                    if (gem != null)
                        gem.SpriteRenderer.color = Color.Lerp(Color.white, GetGemColor(gem.Type), pulse);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
