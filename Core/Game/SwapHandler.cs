using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// 타일 스왑 + 유효성 검증 처리
    /// </summary>
    public class SwapHandler
    {
        private readonly Board _board;
        private readonly MatchFinder _matchFinder;

        public SwapHandler(Board board, MatchFinder matchFinder)
        {
            _board = board;
            _matchFinder = matchFinder;
        }

        /// <summary>스왑 결과</summary>
        public enum SwapResult
        {
            InvalidSwap,        // 인접하지 않은 타일
            NoMatch,            // 스왑했지만 매치 없음 (되돌림)
            ValidMatch,         // ✅ 매치 발생!
            GameOver            // 더 이상 가능한 이동 없음
        }

        /// <summary>두 타일 스왑 시도 → 결과 반환</summary>
        public SwapResult TrySwap(TilePosition a, TilePosition b)
        {
            // 1. 인접 체크
            if (!a.IsAdjacentTo(b))
                return SwapResult.InvalidSwap;

            // 2. 스왑
            _board.Swap(a, b);

            // 3. 매치 있는지 확인
            var matches = _matchFinder.FindAllMatches();
            if (matches.Count > 0)
                return SwapResult.ValidMatch;

            // 4. 매치 없으면 되돌리기
            _board.Swap(a, b);
            return SwapResult.NoMatch;
        }
    }
}
