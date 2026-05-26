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

        private readonly SwapHandler _swapHandler;
        private readonly CascadeHandler _cascadeHandler;

        // Unity 어댑터 (외부에서 주입)
        public IBoardRenderer Renderer { get; set; }
        public IInputHandler Input { get; set; }

        public event Action<int> OnChainCombo;
        public event Action OnGameOver;

        public GameController()
        {
            Board = new Board();
            Score = new ScoreManager();
            State = new GameStateMachine();
            MatchFinder = new MatchFinder(Board);
            _swapHandler = new SwapHandler(Board, MatchFinder);
            _cascadeHandler = new CascadeHandler(Board);
        }

        /// <summary>게임 시작</summary>
        public void StartGame()
        {
            Board.Initialize();
            Score.Reset();
            State.Reset();
            Renderer?.Initialize(Board.Rows, Board.Cols);

            // 초기 보드 렌더
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    Renderer?.UpdateTile(r, c, Board[r, c].Type);

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

                // 게임오버 체크
                if (!MatchFinder.HasAnyValidMove())
                {
                    State.ChangeState(GameState.GameOver);
                    OnGameOver?.Invoke();
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

        /// <summary>힌트 요청 (선택 가능한 이동 중 하나 반환)</summary>
        public bool TryGetHint(out TilePosition a, out TilePosition b)
        {
            a = new TilePosition(0, 0);
            b = new TilePosition(0, 0);

            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    var pos = new TilePosition(r, c);

                    // 오른쪽 스왑
                    if (c + 1 < Board.Cols)
                    {
                        Board.Swap(pos, new TilePosition(r, c + 1));
                        bool valid = MatchFinder.FindAllMatches().Count > 0;
                        Board.Swap(pos, new TilePosition(r, c + 1));
                        if (valid)
                        {
                            a = pos;
                            b = new TilePosition(r, c + 1);
                            return true;
                        }
                    }

                    // 아래쪽 스왑
                    if (r + 1 < Board.Rows)
                    {
                        Board.Swap(pos, new TilePosition(r + 1, c));
                        bool valid = MatchFinder.FindAllMatches().Count > 0;
                        Board.Swap(pos, new TilePosition(r + 1, c));
                        if (valid)
                        {
                            a = pos;
                            b = new TilePosition(r + 1, c);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
