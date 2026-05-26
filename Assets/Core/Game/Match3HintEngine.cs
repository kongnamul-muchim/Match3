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
        private readonly Board _originalBoard;

        // 힌트 점수 테이블
        private const int ScorePerMatchTile = 10;
        private const int ScorePerChainLevel = 50;
        private const int ScoreFourMatch = 30;   // 4-in-a-row 보너스
        private const int ScoreFiveMatch = 80;   // 5-in-a-row 보너스

        public Match3HintEngine(Board board)
        {
            _originalBoard = board;
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

        /// <summary>특정 스왑을 평가하여 힌트 결과 반환 (복제 보드에서 안전하게 시뮬레이션)</summary>
        private HintResult EvaluateSwap(TilePosition a, TilePosition b)
        {
            var gemType = _originalBoard[a.Row, a.Col].Type;
            var result = new HintResult(a, b, gemType);

            // 🔥 원본 보드는 절대 수정하지 않음! 복제본에서 시뮬레이션
            var simBoard = _originalBoard.Clone();
            var simFinder = new MatchFinder(simBoard);
            var simCascade = new CascadeHandler(simBoard);

            // 1. 스왑 시뮬레이션
            simBoard.Swap(a, b);

            // 2. 직접 매치 확인
            var matches = simFinder.FindAllMatches();

            if (matches.Count == 0)
                return result; // 유효하지 않음

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

            // 3. 매치 크기별 보너스
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

            // 4. 연쇄 예측 (복제 보드에서 안전하게)
            int predictedChain = SimulateCascade(simBoard, simFinder, simCascade, allMatchedPositions, out int additionalDrops);

            result.ChainCount = predictedChain;
            result.CascadeDropCount = additionalDrops;

            // 5. 총 점수 계산
            result.TotalScore = totalMatchCount * ScorePerMatchTile
                              + (predictedChain - 1) * ScorePerChainLevel
                              + bonus;

            // 6. 랭크 조정
            if (predictedChain >= 3 && additionalDrops >= 10)
                result.Rank = HintRank.Excellent;
            else if (predictedChain >= 2 && additionalDrops >= 6)
                result.Rank = HintRank.Great;
            else if (predictedChain >= 2)
                result.Rank = HintRank.Great;

            // 7. 힌트 설명 생성
            result.Description = GenerateHint(result);

            // 복제본은 버려짐 (GC) — 원본은 안전!
            return result;
        }

        /// <summary>복제된 보드에서 연쇄 시뮬레이션 (원본 영향 없음)</summary>
        private int SimulateCascade(Board simBoard, MatchFinder simFinder, CascadeHandler simCascade,
            HashSet<(int, int)> removedPositions, out int totalAdditionalDrops)
        {
            int chainLevel = 0;
            totalAdditionalDrops = 0;

            // 이미 제거된 위치를 복제 보드에 반영
            foreach (var (r, c) in removedPositions)
            {
                if (!simBoard.IsEmpty(r, c))
                    simBoard.ClearTile(r, c);
            }

            int safetyCounter = 0;
            while (true)
            {
                simCascade.ApplyGravity();
                simCascade.FillEmptySpaces();

                var newMatches = simFinder.FindAllMatches();
                if (newMatches.Count == 0) break;

                chainLevel++;

                foreach (var group in newMatches)
                {
                    foreach (var pos in group.Tiles)
                    {
                        if (!simBoard.IsEmpty(pos.Row, pos.Col))
                        {
                            simBoard.ClearTile(pos.Row, pos.Col);
                            totalAdditionalDrops++;
                        }
                    }
                }

                safetyCounter++;
                if (safetyCounter > 20) break;
            }

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
