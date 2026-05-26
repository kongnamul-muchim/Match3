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

        /// <summary>중력 낙하: 빈 칸 위에 있는 타일들을 아래로 내림</summary>
        public List<DropInfo> ApplyGravity()
        {
            var drops = new List<DropInfo>();

            for (int c = 0; c < Board.Cols; c++)
            {
                int writeRow = Board.Rows - 1;

                // 아래에서 위로 스캔하면서 타일을 아래로 당김
                for (int r = Board.Rows - 1; r >= 0; r--)
                {
                    if (!_board[r, c].IsEmpty)
                    {
                        if (r != writeRow)
                        {
                            int dropDistance = writeRow - r;
                            _board.Swap(new TilePosition(r, c), new TilePosition(writeRow, c));
                            drops.Add(new DropInfo(
                                new TilePosition(r, c),
                                new TilePosition(writeRow, c),
                                dropDistance
                            ));
                        }
                        writeRow--;
                    }
                }
            }

            return drops;
        }

        /// <summary>빈 공간에 새 타일 생성</summary>
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
