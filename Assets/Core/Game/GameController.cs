using System;
using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>
    /// 게임 메인 컨트롤러. Core 로직을 오케스트레이션.
    /// Unity 의존성 제로 — IBoardRenderer, IInputHandler 등 인터페이스로 분리.
    /// </summary>
    public class GameController
    {
        public Board Board { get; }
        public ScoreManager Score { get; }
        public GameStateMachine State { get; }
        public MatchFinder MatchFinder { get; }
        public Match3HintEngine HintEngine { get; }

        private readonly SwapHandler _swapHandler;
        private readonly CascadeHandler _cascadeHandler;

        // Unity 어댑터 (외부에서 주입)
        public IBoardRenderer Renderer { get; set; }
        public IInputHandler Input { get; set; }

        public event Action<int> OnChainCombo;
        public event Action OnGameOver;
        public event Action OnBoardReshuffled; // 셔플 발생 시 UI 알림용

        private int _reshuffleCount;
        private const int MaxReshuffles = 3; // 3번 연속 셔플 후에도 이동 없으면 게임오버

        public GameController()
        {
            Board = new Board();
            Score = new ScoreManager();
            State = new GameStateMachine();
            MatchFinder = new MatchFinder(Board);
            HintEngine = new Match3HintEngine(Board);
            _swapHandler = new SwapHandler(Board, MatchFinder);
            _cascadeHandler = new CascadeHandler(Board);
        }

        /// <summary>게임 시작</summary>
        public void StartGame()
        {
            Board.Initialize();
            Score.Reset();
            State.Reset();
            _reshuffleCount = 0;
            Renderer?.Initialize(Board.Rows, Board.Cols);

            // 초기 보드 렌더
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    Renderer?.UpdateTile(r, c, Board[r, c].Type);

            // 초기 보드에 유효한 이동이 없으면 자동 셔플
            if (!MatchFinder.HasAnyValidMove())
            {
                AutoReshuffle();
                return; // AutoReshuffle에서 Input을 enable함
            }

            Input?.SetEnabled(true);
        }

        /// <summary>플레이어 타일 스왑 요청 (InputHandler에서 호출)</summary>
        public void OnPlayerSwap(TilePosition a, TilePosition b)
        {
            if (!State.CanInput) return;

            State.ChangeState(GameState.Swapping);
            Input?.SetEnabled(false);

            // 스왑 시도
            var result = _swapHandler.TrySwap(a, b);

            switch (result)
            {
                case SwapHandler.SwapResult.ValidMatch:
                    Renderer?.AnimateSwap(a, b, () => ProcessMatches());
                    break;

                case SwapHandler.SwapResult.NoMatch:
                    // 스왑 애니메이션 후 되돌리기
                    Renderer?.AnimateSwap(a, b, () =>
                    {
                        Renderer?.AnimateSwap(a, b, () =>
                        {
                            State.ChangeState(GameState.Idle);
                            Input?.SetEnabled(true);
                        });
                    });
                    break;

                case SwapHandler.SwapResult.InvalidSwap:
                    State.ChangeState(GameState.Idle);
                    Input?.SetEnabled(true);
                    break;
            }
        }

        /// <summary>매치 → 캐스케이드 처리 (한 체인씩, 연쇄 보임)</summary>
        private void ProcessMatches(int chainLevel = 1)
        {
            State.ChangeState(GameState.Matching);

            var matches = MatchFinder.FindAllMatches();
            if (matches.Count == 0)
            {
                // 안정 상태
                State.ChangeState(GameState.Idle);
                Input?.SetEnabled(true);

                // 게임오버 체크 → 셔플로 대체
                if (!MatchFinder.HasAnyValidMove())
                {
                    _reshuffleCount++;
                    if (_reshuffleCount >= MaxReshuffles)
                    {
                        // N번 연속 셔플 후에도 없으면 진짜 게임오버
                        State.ChangeState(GameState.GameOver);
                        OnGameOver?.Invoke();
                    }
                    else
                    {
                        AutoReshuffle();
                    }
                }
                return;
            }

            State.ChangeState(GameState.Cascading);

            // 1. 매치된 타일 직접 제거
            var removePositions = new List<TilePosition>();
            foreach (var group in matches)
            {
                foreach (var pos in group.Tiles)
                {
                    if (!Board.IsEmpty(pos.Row, pos.Col))
                    {
                        Board.ClearTile(pos.Row, pos.Col);
                        removePositions.Add(pos);
                    }
                }
            }

            // 2. 점수 반영
            Score.AddMatchScore(removePositions.Count, chainLevel);
            if (chainLevel > 1)
                OnChainCombo?.Invoke(chainLevel);

            // 3. 제거 → 중력 → 새 타일 드롭 → 연쇄
            Renderer?.AnimateRemove(removePositions, () =>
            {
                // 중력 낙하
                var drops = _cascadeHandler.ApplyGravity();

                // 새 타일 드롭 정보 미리 생성
                _cascadeHandler.PrepareNewTileDrops();
                var newTileData = _cascadeHandler.GetNewTileDropData();
                var newTiles = newTileData.ConvertAll(d => (d.drop.To, d.type));
                var newTileDrops = newTileData.ConvertAll(d => d.drop);

                // 전체 드롭 한번에 애니메이션 (기존 타일 + 새 타일)
                Renderer?.AnimateDrop(drops, newTiles, () =>
                {
                    // 보드 데이터에 새 타일 반영
                    _cascadeHandler.CommitNewTileDrops();
                    Board.SyncPositions();

                    // 보드 전체 렌더 업데이트
                    for (int r = 0; r < Board.Rows; r++)
                        for (int c = 0; c < Board.Cols; c++)
                            Renderer?.UpdateTile(r, c, Board[r, c].Type);

                    // 추가 매치 확인 (연쇄!)
                    ProcessMatches(chainLevel + 1);
                });
            });
        }

        /// <summary>이동 가능한 보드가 나올 때까지 자동 셔플</summary>
        private void AutoReshuffle()
        {
            State.ChangeState(GameState.Cascading);
            Input?.SetEnabled(false);

            // 이동 불가 → 렌더러에 셔플 애니메이션 요청
            Renderer?.AnimateReshuffle(Board.Rows, Board.Cols, () =>
            {
                // 새 타일로 보드 채우기 (매치 없이)
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Cols; c++)
                    {
                        var type = (GemType)new Random().Next((int)GemType.Count);
                        Board.SetTile(r, c, type);
                    }
                }
                Board.SyncPositions();

                // 보드 전체 렌더 업데이트
                for (int r = 0; r < Board.Rows; r++)
                    for (int c = 0; c < Board.Cols; c++)
                        Renderer?.UpdateTile(r, c, Board[r, c].Type);

                // 셔플 후 다시 매치 제거
                // (셔플 과정에서 3연속이 생길 수 있으므로 ProcessMatches로 처리)
                OnBoardReshuffled?.Invoke();
                ProcessMatches();
            });
        }

        /// <summary>타임아웃 등 외부에서 강제 게임오버</summary>
        public void ForceGameOver()
        {
            if (State.CurrentState == GameState.GameOver) return;
            State.ChangeState(GameState.GameOver);
            Input?.SetEnabled(false);
            OnGameOver?.Invoke();
        }

        /// <summary>AI 힌트 요청 — 최적의 수 반환 (Match3HintEngine 사용)</summary>
        public bool TryGetHint(out TilePosition a, out TilePosition b, out HintResult hintResult)
        {
            hintResult = HintEngine.GetBestMove();
            if (hintResult != null)
            {
                a = hintResult.From;
                b = hintResult.To;
                return true;
            }

            a = new TilePosition(0, 0);
            b = new TilePosition(0, 0);
            return false;
        }

        /// <summary>기본 힌트 (하위호환)</summary>
        public bool TryGetHint(out TilePosition a, out TilePosition b)
        {
            var result = HintEngine.GetBestMove();
            if (result != null)
            {
                a = result.From;
                b = result.To;
                return true;
            }

            a = new TilePosition(0, 0);
            b = new TilePosition(0, 0);
            return false;
        }
    }
}
