using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>AI 힌트 평가 결과</summary>
    public class HintResult
    {
        public TilePosition From;
        public TilePosition To;
        public GemType SwappedGemType; // 힌트에서 바꿀 타일의 타입
        public int MatchCount;         // 현재 매치된 타일 수
        public int ChainCount;         // 예측된 연쇄 체인 수
        public int TotalScore;         // 예상 점수
        public int CascadeDropCount;   // 연쇄로 제거될 추가 타일 수
        public string Description;     // 자연어 힌트
        public HintRank Rank;

        public HintResult(TilePosition from, TilePosition to, GemType type)
        {
            From = from;
            To = to;
            SwappedGemType = type;
            MatchCount = 0;
            ChainCount = 0;
            TotalScore = 0;
            CascadeDropCount = 0;
            Description = "";
            Rank = HintRank.None;
        }

        public bool IsValid => MatchCount >= 3;
    }

    public enum HintRank
    {
        None,       // 유효하지 않은 이동
        Normal,     // 3-match
        Good,       // 4-match 이상 or 특수보석 가능
        Great,      // 연쇄 2단계 이상
        Excellent   // 연쇄 3단계 이상 or 대량 제거
    }

    /// <summary>
    /// Match-3 AI 힌트 엔진.
    /// 모든 가능한 스왑을 시뮬레이션하고 점수를 매겨 최적의 수를 추천.
    /// </summary>
    public class Match3HintEngine
    {
        private readonly Board _board;

        // 힌트 점수 테이블
        private const int ScorePerMatchTile = 10;
        private const int ScorePerChainLevel = 50;
        private const int ScoreFourMatch = 30;   // 4-in-a-row 보너스
        private const int ScoreFiveMatch = 80;   // 5-in-a-row 보너스

        public Match3HintEngine(Board board)
        {
            _board = board;
        }

        /// <summary>모든 가능한 스왑을 평가하여 정렬된 힌트 목록 반환</summary>
        public List<HintResult> EvaluateAllMoves()
        {
            var results = new List<HintResult>();

            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    var pos = new TilePosition(r, c);

                    // 오른쪽 스왑
                    if (c + 1 < Board.Cols)
                    {
                        var result = EvaluateSwap(pos, new TilePosition(r, c + 1));
                        if (result.IsValid)
                            results.Add(result);
                    }

                    // 아래쪽 스왑
                    if (r + 1 < Board.Rows)
                    {
                        var result = EvaluateSwap(pos, new TilePosition(r + 1, c));
                        if (result.IsValid)
                            results.Add(result);
                    }
                }
            }

            // 점수 기준 내림차순 정렬
            results.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));
            return results;
        }

        /// <summary>가장 좋은 힌트 하나 반환</summary>
        public HintResult GetBestMove()
        {
            var allMoves = EvaluateAllMoves();
            return allMoves.Count > 0 ? allMoves[0] : null;
        }

        /// <summary>특정 스왑을 평가하여 힌트 결과 반환</summary>
        private HintResult EvaluateSwap(TilePosition a, TilePosition b)
        {
            var gemType = _board[a.Row, a.Col].Type;
            var result = new HintResult(a, b, gemType);

            // 스왑 시뮬레이션
            _board.Swap(a, b);

            // 1. 직접 매치 확인
            var finder = new MatchFinder(_board);
            var matches = finder.FindAllMatches();

            if (matches.Count == 0)
            {
                _board.Swap(a, b); // 복원
                return result;     // 유효하지 않음
            }

            // 매치 정보 수집
            int totalMatchCount = 0;
            int maxMatchGroup = 0;
            var allMatchedPositions = new HashSet<(int, int)>();

            foreach (var group in matches)
            {
                int groupSize = group.Tiles.Count;
                totalMatchCount += groupSize;
                if (groupSize > maxMatchGroup) maxMatchGroup = groupSize;

                foreach (var pos in group.Tiles)
                    allMatchedPositions.Add((pos.Row, pos.Col));
            }

            result.MatchCount = totalMatchCount;
            result.Rank = HintRank.Normal;

            // 2. 매치 크기별 보너스
            int bonus = 0;
            if (totalMatchCount >= 5)
            {
                bonus = ScoreFiveMatch;
                result.Rank = HintRank.Good;
            }
            else if (totalMatchCount >= 4)
            {
                bonus = ScoreFourMatch;
                result.Rank = HintRank.Good;
            }

            // 3. 연쇄(캐스케이드) 예측 시뮬레이션
            int predictedChain = SimulateCascade(allMatchedPositions, out int additionalDrops);

            result.ChainCount = predictedChain;
            result.CascadeDropCount = additionalDrops;

            // 4. 총 점수 계산
            result.TotalScore = totalMatchCount * ScorePerMatchTile
                              + (predictedChain - 1) * ScorePerChainLevel
                              + bonus;

            // 5. 랭크 조정 (연쇄 기반)
            if (predictedChain >= 3 && additionalDrops >= 10)
                result.Rank = HintRank.Excellent;
            else if (predictedChain >= 2 && additionalDrops >= 6)
                result.Rank = HintRank.Great;
            else if (predictedChain >= 2)
                result.Rank = HintRank.Great;

            // 6. 힌트 설명 생성
            result.Description = GenerateHint(result);

            // 스왑 복원
            _board.Swap(a, b);
            return result;
        }

        /// <summary>매치 후 캐스케이드 시뮬레이션 (연쇄 예측)</summary>
        private int SimulateCascade(HashSet<(int, int)> removedPositions, out int totalAdditionalDrops)
        {
            // 이 함수를 위해 보드의 복사본에서 시뮬레이션해야 하지만,
            // C# struct 복사보다는 임시 Clear/복원 방식 사용

            int chainLevel = 0;
            totalAdditionalDrops = 0;

            // 이미 제거된 위치를 보드에 반영
            foreach (var (r, c) in removedPositions)
            {
                if (!_board.IsEmpty(r, c))
                    _board.ClearTile(r, c);
            }

            var cascadeFinder = new MatchFinder(_board);
            var cascadeHandler = new CascadeHandler(_board);

            int safetyCounter = 0;
            while (true)
            {
                // 중력 적용
                cascadeHandler.ApplyGravity();

                // 새 타일 채우기
                cascadeHandler.FillEmptySpaces();

                // 매치 확인
                var newMatches = cascadeFinder.FindAllMatches();
                if (newMatches.Count == 0) break;

                chainLevel++;

                foreach (var group in newMatches)
                {
                    foreach (var pos in group.Tiles)
                    {
                        if (!_board.IsEmpty(pos.Row, pos.Col))
                        {
                            _board.ClearTile(pos.Row, pos.Col);
                            totalAdditionalDrops++;
                        }
                    }
                }

                safetyCounter++;
                if (safetyCounter > 20) break; // 무한루프 방지
            }

            // 보드 복원은 호출한 쪽에서 Swap 원복할 때 같이 복원됨 (스왑 전 상태로 돌아감)
            return chainLevel;
        }

        /// <summary>자연어 힌트 생성</summary>
        private string GenerateHint(HintResult result)
        {
            string direction = result.From.Col != result.To.Col
                ? (result.To.Col > result.From.Col ? "오른쪽" : "왼쪽")
                : (result.To.Row > result.From.Row ? "위쪽" : "아래쪽");

            string gemName = result.SwappedGemType switch
            {
                GemType.Red => "빨간",
                GemType.Blue => "파란",
                GemType.Green => "초록",
                GemType.Yellow => "노란",
                GemType.Purple => "보라",
                _ => "보석"
            };

            return result.Rank switch
            {
                HintRank.Excellent =>
                    $"🔥 {gemName} 보석을 {direction}으로 바꾸면 {result.ChainCount}연쇄! 대박 매치!",
                HintRank.Great =>
                    $"✨ {gemName} 보석을 {direction}으로 바꾸면 {result.MatchCount}개 매치 + 연쇄!",
                HintRank.Good =>
                    $"💎 {gemName} 보석을 {direction}으로 바꾸면 {result.MatchCount}개 매치! 특수보석 생성 가능!",
                HintRank.Normal =>
                    $"👆 {gemName} 보석을 {direction}으로 바꾸면 {result.MatchCount}개 매치!",
                _ => $"👆 여기를 {direction}으로 바꿔보세요!"
            };
        }
    }
}
