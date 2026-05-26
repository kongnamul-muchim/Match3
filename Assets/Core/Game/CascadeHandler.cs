using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// 매치 제거 → 중력 낙하 → 새 타일 생성 체인 처리
    /// </summary>
    public class CascadeHandler
    {
        private readonly Board _board;
        private readonly Random _rng = new Random();

        public CascadeHandler(Board board)
        {
            _board = board;
        }

        /// <summary>매치 그룹 제거 → 낙하 → 생성 (더 이상 매치 없을 때까지 반복)</summary>
        public CascadeResult ProcessCascade(List<MatchGroup> initialMatches)
        {
            int totalRemoved = 0;
            int chainCount = 0;
            var allRemovedPositions = new List<TilePosition>();

            var currentMatches = initialMatches;

            while (currentMatches.Count > 0)
            {
                chainCount++;

                // 1. 매치된 타일들 제거
                var removedPositions = RemoveMatches(currentMatches);
                totalRemoved += removedPositions.Count;
                allRemovedPositions.AddRange(removedPositions);

                // 2. 중력 낙하
                var drops = ApplyGravity();

                // 3. 빈 공간에 새 타일 생성
                var newTiles = FillEmptySpaces();

                // 4. 새로운 매치 확인
                var finder = new MatchFinder(_board);
                currentMatches = finder.FindAllMatches();
            }

            return new CascadeResult
            {
                TotalRemoved = totalRemoved,
                ChainCount = chainCount,
                AllRemovedPositions = allRemovedPositions
            };
        }

        /// <summary>매치된 타일들을 빈 칸으로 표시</summary>
        private List<TilePosition> RemoveMatches(List<MatchGroup> matches)
        {
            var removed = new HashSet<(int, int)>();
            foreach (var group in matches)
            {
                foreach (var pos in group.Tiles)
                {
                    if (!_board.IsEmpty(pos.Row, pos.Col))
                    {
                        _board.ClearTile(pos.Row, pos.Col);
                        removed.Add((pos.Row, pos.Col));
                    }
                }
            }

            var result = new List<TilePosition>();
            foreach (var (r, c) in removed)
                result.Add(new TilePosition(r, c));
            return result;
        }

        /// <summary>중력 낙하: 타일들이 아래(row 0)로 내려감</summary>
        public List<DropInfo> ApplyGravity()
        {
            var drops = new List<DropInfo>();

            for (int c = 0; c < Board.Cols; c++)
            {
                int writeRow = 0; // row 0 = bottom of screen

                // 위에서 아래로 스캔하면서 타일을 아래로 당김
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (!_board[r, c].IsEmpty)
                    {
                        if (r != writeRow)
                        {
                            // r > writeRow, 타일이 아래로 이동
                            int dropDistance = r - writeRow;
                            _board.Swap(new TilePosition(r, c), new TilePosition(writeRow, c));
                            drops.Add(new DropInfo(
                                new TilePosition(r, c),     // 원래 위치
                                new TilePosition(writeRow, c), // 도착 위치 (더 아래)
                                dropDistance
                            ));
                        }
                        writeRow++;
                    }
                }
            }

            return drops;
        }

        // ── 단계별 캐스케이드 지원 ──

        private List<(TilePosition pos, GemType type)> _pendingNewTiles;

        /// <summary>비어있는 칸의 새 타일 생성 정보를 미리 생성 (보드에는 반영 안 함)</summary>
        public void PrepareNewTileDrops()
        {
            _pendingNewTiles = new List<(TilePosition, GemType)>();
            // 위에서 아래로 스캔 (먼저 채워지는 순서대로)
            for (int c = 0; c < Board.Cols; c++)
            {
                for (int r = Board.Rows - 1; r >= 0; r--)
                {
                    if (_board[r, c].IsEmpty)
                    {
                        var type = (GemType)_rng.Next((int)GemType.Count);
                        _pendingNewTiles.Add((new TilePosition(r, c), type));
                    }
                }
            }
        }

        /// <summary>PrepareNewTileDrops() 후 호출. 새 타일들의 드롭 정보 반환</summary>
        public List<(DropInfo drop, GemType type)> GetNewTileDropData()
        {
            var result = new List<(DropInfo, GemType)>();
            if (_pendingNewTiles == null) return result;

            foreach (var (pos, type) in _pendingNewTiles)
            {
                int fromRow = Board.Rows; // 보드 위 가상의 시작 위치
                int distance = fromRow - pos.Row;
                result.Add((
                    new DropInfo(new TilePosition(fromRow, pos.Col), pos, distance),
                    type
                ));
            }
            return result;
        }

        /// <summary>PrepareNewTileDrops()에서 생성한 정보로 보드에 타일 채우기</summary>
        public void CommitNewTileDrops()
        {
            if (_pendingNewTiles == null) return;
            foreach (var (pos, type) in _pendingNewTiles)
            {
                _board.SetTile(pos.Row, pos.Col, type);
            }
            _pendingNewTiles = null;
        }

        /// <summary>빈 공간에 새 타일 생성 (기존 단일 호출 방식)</summary>
        public List<TilePosition> FillEmptySpaces()
        {
            var filled = new List<TilePosition>();
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    if (_board[r, c].IsEmpty)
                    {
                        var type = (GemType)_rng.Next((int)GemType.Count);
                        _board.SetTile(r, c, type);
                        filled.Add(new TilePosition(r, c));
                    }
                }
            }
            return filled;
        }
    }

    /// <summary>낙하 정보 (Unity 애니메이션용)</summary>
    public struct DropInfo
    {
        public TilePosition From;
        public TilePosition To;
        public int Distance;

        public DropInfo(TilePosition from, TilePosition to, int distance)
        {
            From = from;
            To = to;
            Distance = distance;
        }
    }

    /// <summary>캐스케이드 처리 결과</summary>
    public class CascadeResult
    {
        public int TotalRemoved { get; set; }
        public int ChainCount { get; set; }
        public List<TilePosition> AllRemovedPositions { get; set; }
    }
}
