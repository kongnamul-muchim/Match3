using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// 3연속 이상 매치 감지 엔진.
    /// 가로/세로 스캔으로 매치 그룹을 찾는다.
    /// </summary>
    public class MatchFinder
    {
        private readonly Board _board;

        public MatchFinder(Board board)
        {
            _board = board;
        }

        /// <summary>현재 보드에서 모든 매치 그룹 찾기</summary>
        public List<MatchGroup> FindAllMatches()
        {
            var matches = new List<MatchGroup>();

            // 가로 스캔
            for (int r = 0; r < Board.Rows; r++)
            {
                FindHorizontalMatches(r, matches);
            }

            // 세로 스캔
            for (int c = 0; c < Board.Cols; c++)
            {
                FindVerticalMatches(c, matches);
            }

            return matches;
        }

        /// <summary>특정 행에서 가로 매치 찾기</summary>
        private void FindHorizontalMatches(int row, List<MatchGroup> matches)
        {
            int start = 0;
            for (int c = 1; c <= Board.Cols; c++)
            {
                if (c < Board.Cols &&
                    !_board[row, c].IsEmpty &&
                    _board[row, c].Type == _board[row, start].Type)
                {
                    continue;
                }

                int length = c - start;
                if (length >= 3)
                {
                    var tiles = new List<TilePosition>();
                    for (int i = start; i < c; i++)
                        tiles.Add(new TilePosition(row, i));
                    matches.Add(new MatchGroup(tiles, MatchDirection.Horizontal));
                }
                start = c;
            }
        }

        /// <summary>특정 열에서 세로 매치 찾기</summary>
        private void FindVerticalMatches(int col, List<MatchGroup> matches)
        {
            int start = 0;
            for (int r = 1; r <= Board.Rows; r++)
            {
                if (r < Board.Rows &&
                    !_board[r, col].IsEmpty &&
                    _board[r, col].Type == _board[start, col].Type)
                {
                    continue;
                }

                int length = r - start;
                if (length >= 3)
                {
                    var tiles = new List<TilePosition>();
                    for (int i = start; i < r; i++)
                        tiles.Add(new TilePosition(i, col));
                    matches.Add(new MatchGroup(tiles, MatchDirection.Vertical));
                }
                start = r;
            }
        }

        /// <summary>스왑 후 유효한 매치가 있는지 확인</summary>
        public bool HasMatchAt(TilePosition pos, GemType type)
        {
            int row = pos.Row, col = pos.Col;

            // 가로 체크 (좌우 연속 개수)
            int horizontalCount = 1;
            for (int c = col - 1; c >= 0 && _board[row, c].Type == type; c--)
                horizontalCount++;
            for (int c = col + 1; c < Board.Cols && _board[row, c].Type == type; c++)
                horizontalCount++;

            if (horizontalCount >= 3) return true;

            // 세로 체크 (상하 연속 개수)
            int verticalCount = 1;
            for (int r = row - 1; r >= 0 && _board[r, col].Type == type; r--)
                verticalCount++;
            for (int r = row + 1; r < Board.Rows && _board[r, col].Type == type; r++)
                verticalCount++;

            return verticalCount >= 3;
        }

        /// <summary>유효한 이동이 하나라도 있는지 확인 (게임오버 체크용)</summary>
        public bool HasAnyValidMove()
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    var pos = new TilePosition(r, c);

                    // 오른쪽 스왑 시도
                    if (c + 1 < Board.Cols)
                    {
                        _board.Swap(pos, new TilePosition(r, c + 1));
                        bool valid = FindAllMatches().Count > 0;
                        _board.Swap(pos, new TilePosition(r, c + 1));
                        if (valid) return true;
                    }

                    // 아래쪽 스왑 시도
                    if (r + 1 < Board.Rows)
                    {
                        _board.Swap(pos, new TilePosition(r + 1, c));
                        bool valid = FindAllMatches().Count > 0;
                        _board.Swap(pos, new TilePosition(r + 1, c));
                        if (valid) return true;
                    }
                }
            }
            return false;
        }
    }

    public enum MatchDirection
    {
        Horizontal,
        Vertical
    }

    /// <summary>매치 그룹 (연결된 타일들)</summary>
    public class MatchGroup
    {
        public List<TilePosition> Tiles { get; }
        public MatchDirection Direction { get; }
        public int Count => Tiles.Count;

        public MatchGroup(List<TilePosition> tiles, MatchDirection direction)
        {
            Tiles = tiles;
            Direction = direction;
        }

        /// <summary>매치 그룹의 타입 (첫 타일 기준)</summary>
        public GemType Type => GemType.Count; // 실제론 외부에서 결정
    }
}
