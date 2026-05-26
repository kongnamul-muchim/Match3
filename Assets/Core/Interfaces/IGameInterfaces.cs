using System.Collections.Generic;

namespace Match3.Core
{
    /// <summary>보드 렌더링 인터페이스 (Unity 어댑터)</summary>
    public interface IBoardRenderer
    {
        void Initialize(int rows, int cols);
        void UpdateTile(int row, int col, GemType type);
        void AnimateSwap(TilePosition a, TilePosition b, System.Action onComplete);
        void AnimateRemove(List<TilePosition> positions, System.Action onComplete);
        void AnimateDrop(List<DropInfo> drops, List<(TilePosition pos, GemType type)> newTiles, System.Action onComplete);
        void AnimateNewTile(TilePosition pos, GemType type, System.Action onComplete);
        void ShowHint(List<TilePosition> positions);
        void ClearHighlights();
        void AnimateReshuffle(int rows, int cols, System.Action onComplete);
    }

    /// <summary>입력 처리 인터페이스 (Unity 어댑터)</summary>
    public interface IInputHandler
    {
        event System.Action<TilePosition, TilePosition> OnTileSwapped;
        void SetEnabled(bool enabled);
    }

    /// <summary>게임 이벤트 (UI 업데이트용)</summary>
    public interface IGameEvents
    {
        event System.Action<int> OnScoreUpdated;
        event System.Action<int> OnChainCombo;
        event System.Action OnGameOver;
        event System.Action OnBoardStable; // 캐스케이드 종료 → 입력 가능
    }
}
