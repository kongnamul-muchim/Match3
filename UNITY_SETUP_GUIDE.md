# 🎮 Match3 — Unity Shell 설치 가이드

Core C# 코드는 순수 C#이라 Unity가 없어도 컴파일됨.
아래 내용은 네가 학원 Unity 프로젝트에서 할 작업이야.

## 1. Unity 프로젝트 생성
- Unity 6, 2D URP 프로젝트 생성
- `Assets/` 구조:
```
Assets/
├── Core/                    ← 내가 짠 코드 여기 복붙
│   └── Game/ + Interfaces/
├── Unity/                   ← 네가 만들 부분
│   ├── Adapters/
│   ├── UI/
│   └── Prefabs/
├── Sprites/                 ← 게임에 쓸 이미지
└── Editor/
```

## 2. 필요한 것들
- **스프라이트:** 5가지 색상의 보석 이미지 (32×32 or 64×64)
- **폰트:** 점수 표시용
- **파티클:** 매치 이펙트 (선택)

## 3. Unity 어댑터 구현 목록

### UnityBoardRenderer.cs (IBoardRenderer 구현)
```csharp
// Board의 각 칸에 Gem GameObject 배치
// 타입 변경 시 스프라이트 교체
// 스왑/제거/낙하 DOTween 애니메이션
```
→ 팁: Object Pool로 Gem 재사용 (Instantiate 최소화)

### UnityInputHandler.cs (IInputHandler 구현)
```csharp
// 마우스/터치 드래그 입력
// 드래그 방향 → 인접 타일 계산 → OnTileSwapped 이벤트 발생
```

### Match3Bootstrapper.cs (DI 등록)
```csharp
// GameController 생성 + Renderer/Input 주입
// StartGame() 호출
```

## 4. 추천 라이브러리
- **DOTween** — 애니메이션 필수
- 유료 에셋 안 사도 됨, DOTween 무료면 충분

## 5. 빠른 시작 코드

```csharp
using UnityEngine;
using Match3.Core;

public class Match3Bootstrapper : MonoBehaviour
{
    private GameController _game;

    void Start()
    {
        _game = new GameController();
        _game.Renderer = GetComponent<IBoardRenderer>();
        _game.Input = GetComponent<IInputHandler>();
        _game.OnGameOver += () => Debug.Log("게임오버!");
        _game.StartGame();
    }
}
```

## 6. 애니메이션 흐름
```
플레이어 입력 → Swap 애니메이션 (0.2초)
    → 매치 확인 (즉시)
    → Remove 애니메이션 (0.3초)
    → Drop 애니메이션 (0.3초) 
    → 새 타일 Fill (즉시)
    → 추가 매치? → 반복
    → 없음 → Idle 상태 (입력 가능)
```
