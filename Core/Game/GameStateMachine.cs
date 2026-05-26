using System;

namespace Match3.Core
{
    public enum GameState
    {
        Idle,           // 대기 중 (플레이어 입력 가능)
        Swapping,       // 타일 교환 애니메이션 중
        Matching,       // 매치 확인 중
        Cascading,      // 제거 + 낙하 + 생성 중
        GameOver        // 더 이상 이동 불가
    }

    /// <summary>게임 상태 기계 (이벤트 기반)</summary>
    public class GameStateMachine
    {
        public GameState CurrentState { get; private set; } = GameState.Idle;

        public event Action<GameState, GameState> OnStateChanged;

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        public bool CanInput => CurrentState == GameState.Idle;

        public void Reset()
        {
            ChangeState(GameState.Idle);
        }
    }
}
