using System;
using System.Collections.Generic;

namespace Match3.Core
{
    public class Board
    {
        public const int Rows = 8;
        public const int Cols = 8;

        private Tile[,] _grid;
        private readonly Random _rng = new Random();

        public Tile this[int row, int col] => _grid[row, col];

        public Board()
        {
            _grid = new Tile[Rows, Cols];
        }

        /// <summary>초기 보드 생성 (매치 없이)</summary>
        public void Initialize()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var pos = new TilePosition(r, c);
                    _grid[r, c] = new Tile(GetRandomGemType(), pos);
                }
            }

            // 초기 매치 제거 (빈자리 다시 채우기)
            ResolveInitialMatches();
        }

        private void ResolveInitialMatches()
        {
            bool hasMatches = true;
            while (hasMatches)
            {
                hasMatches = false;
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 0; c < Cols; c++)
                    {
                        if (IsPartOfMatch(r, c))
                        {
                            _grid[r, c] = new Tile(GetRandomGemType(), new TilePosition(r, c));
                            hasMatches = true;
                        }
                    }
                }
            }
        }

        /// <summary>해당 위치가 3연속 매치에 포함되는지 확인</summary>
        private bool IsPartOfMatch(int row, int col)
        {
            var type = _grid[row, col].Type;

            // 가로 체크 (왼쪽으로 2개 연속?)
            if (col >= 2 &&
                _grid[row, col - 1].Type == type &&
                _grid[row, col - 2].Type == type)
                return true;

            // 세로 체크 (위로 2개 연속?)
            if (row >= 2 &&
                _grid[row - 1, col].Type == type &&
                _grid[row - 2, col].Type == type)
                return true;

            return false;
        }

        /// <summary>두 타일 위치 교환</summary>
        public void Swap(TilePosition a, TilePosition b)
        {
            var temp = _grid[a.Row, a.Col];
            _grid[a.Row, a.Col] = _grid[b.Row, b.Col];
            _grid[b.Row, b.Col] = temp;

            // 위치 정보 업데이트
            _grid[a.Row, a.Col].Position = a;
            _grid[b.Row, b.Col].Position = b;
        }

        /// <summary>모든 타일의 Position 정보를 현재 그리드 위치와 동기화</summary>
        public void SyncPositions()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    _grid[r, c].Position = new TilePosition(r, c);
                }
            }
        }

        /// <summary>타일을 새 타입으로 설정</summary>
        public void SetTile(int row, int col, GemType type)
        {
            _grid[row, col] = new Tile(type, new TilePosition(row, col));
        }

        /// <summary>타일 비움 (제거 표시)</summary>
        public void ClearTile(int row, int col)
        {
            _grid[row, col] = new Tile(GemType.Count, new TilePosition(row, col));
        }

        /// <summary>해당 위치가 비었는지</summary>
        public bool IsEmpty(int row, int col) => _grid[row, col].IsEmpty;

        /// <summary>보드 범위 체크</summary>
        public static bool IsInBounds(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Cols;

        private GemType GetRandomGemType()
        {
            return (GemType)_rng.Next((int)GemType.Count);
        }

        /// <summary>디버그용 보드 출력</summary>
        public void Print()
        {
            for (int r = 0; r < Rows; r++)
            {
                var line = "";
                for (int c = 0; c < Cols; c++)
                {
                    line += (int)_grid[r, c].Type + " ";
                }
                Console.WriteLine(line);
            }
        }
    }
}
