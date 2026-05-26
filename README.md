# Match3 — Unity Match-3 Puzzle Game

**8×8 보드에서 3개 이상 같은 색상의 보석을 매치시켜 제거하는 퍼즐 게임**

## Architecture

```
Core/ (Pure C#)          ← Unity 의존성 제로
├── Tile.cs              # 보석 데이터
├── Board.cs             # 8×8 그리드 + 스왑
├── MatchFinder.cs       # BFS 매치 감지
├── SwapHandler.cs       # 스왑 유효성 검증
├── CascadeHandler.cs    # 제거→낙하→생성 체인
├── ScoreManager.cs      # 점수/콤보
├── GameStateMachine.cs  # 상태 관리
└── GameController.cs    # 전체 오케스트레이션

Unity/ (Adapter Layer)   ← 네가 학원에서 구현
├── UnityBoardRenderer.cs
├── UnityInputHandler.cs
├── GemPool.cs
└── UI/
```

## Key Features
- **순수 C# Core** — BlockPuzzle과 동일한 DI/Event-driven 패턴
- **BFS 매치 감지** — 가로/세로 3연속 이상 탐색
- **캐스케이드 시스템** — 매치 → 제거 → 낙하 → 생성 무한 반복
- **게임오버 감지** — 가능한 이동이 없을 때 자동 종료
- **힌트 시스템** — 유효한 이동 자동 탐색

## How to Build (Unity)
1. Unity 6 2D URP 프로젝트 생성
2. `Core/` 폴더 통째로 복사
3. `IBoardRenderer`, `IInputHandler` 구현
4. DOTween 설치 (애니메이션)
5. `Match3Bootstrapper` → `StartGame()` 실행

## GitHub
https://github.com/kongnamul-muchim/Match3
