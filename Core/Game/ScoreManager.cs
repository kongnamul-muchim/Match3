using System;

namespace Match3.Core
{
    /// <summary>점수 및 콤보 관리</summary>
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int ComboMultiplier { get; private set; }
        public int TotalMoves { get; private set; }

        // 점수 테이블
        private static readonly int[] MatchScore = { 0, 0, 0, 30, 60, 100, 150, 200, 300 };
        private const int ComboBonus = 50;

        public event Action<int, int> OnScoreChanged; // (newScore, delta)

        public void AddMatchScore(int matchCount, int chainLevel)
        {
            int baseScore = matchCount < MatchScore.Length
                ? MatchScore[matchCount]
                : 300 + (matchCount - 8) * 100;

            int chainBonus = (chainLevel - 1) * ComboBonus;
            int totalPoints = baseScore + chainBonus;

            Score += totalPoints; // combo multiplier 적용 안 함 (선택사항)
            TotalMoves++;

            OnScoreChanged?.Invoke(Score, totalPoints);
        }

        public void Reset()
        {
            Score = 0;
            ComboMultiplier = 0;
            TotalMoves = 0;
        }
    }
}
