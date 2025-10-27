# BrickGame 시스템 사용 가이드

## 📁 구조 개요

```
Assets/@Scripts/Managers/Contents/BrickGame/
├── BrickGameManager.cs          # 핵심 게임 로직 (Non-MonoBehaviour)
├── BrickGameSettings.cs         # 게임 설정값
├── BrickGameState.cs            # 게임 상태 관리
├── BrickGameBootstrap.cs        # 씬 초기화 스크립트
├── Interfaces/
│   ├── IBrickPlacer.cs          # 벽돌 배치 인터페이스
│   ├── IScoreDisplay.cs         # 점수 표시 인터페이스
│   └── ITimeProvider.cs         # 시간 제공 인터페이스
└── Adapters/
    ├── ObjectPlacementAdapter.cs # ObjectPlacement 어댑터
    ├── ScoreDisplayAdapter.cs    # Score UI 어댑터
    └── UnityTimeProvider.cs      # Unity Time 래퍼
```

---

## 🎯 설계 원칙

### 1. **MonoBehaviour 의존성 제거**
- BrickGameManager는 순수 C# 클래스 (Non-MonoBehaviour)
- 테스트 가능, 네트워크 동기화 용이

### 2. **인터페이스 추상화**
- IBrickPlacer: 벽돌 생성 로직 분리
- IScoreDisplay: UI 의존성 제거
- ITimeProvider: Unity Time API 의존성 제거

### 3. **계층적 구조**
```csharp
Managers (최상위)
  └─ GameManager
      ├─ BrickGameManager (벽돌깨기)
      ├─ NetworkGameManager (네트워크 게임) // 나중에 추가
      └─ PuzzleGameManager (퍼즐 게임) // 나중에 추가
```

### 4. **ActionBus 통합**
- Update 로직은 `Managers.Subscribe(ActionId.System_Update, OnUpdate)` 형태로 구독
- MonoBehaviour의 Update()에 의존하지 않음

---

## 🚀 씬 설정 방법

### Step 1: Bootstrap 추가
1. 씬에 빈 GameObject 생성
2. 이름: `@BrickGameBootstrap`
3. `BrickGameBootstrap.cs` 컴포넌트 추가

### Step 2: Adapter 컴포넌트 추가
1. **ObjectPlacementAdapter**
   - ObjectPlacement가 있는 GameObject에 추가
   - 또는 별도 GameObject 생성 후 추가
   
2. **ScoreDisplayAdapter**
   - Score 텍스트(TextMeshPro)가 있는 GameObject에 추가

### Step 3: Bootstrap 설정
- Inspector에서 `BrickGameBootstrap` 컴포넌트 찾기
- `Object Placement Adapter` 슬롯에 ObjectPlacementAdapter 드래그
- `Score Display Adapter` 슬롯에 ScoreDisplayAdapter 드래그
- `Game Settings` 조정 (선택사항, 비워두면 기본값 사용)
- `Auto Start Game` 체크 (자동 시작 원하면)

### Step 4: 기존 BrickGameManager 제거
- 씬에 기존 BrickGameManager (MonoBehaviour) 있으면 제거
- 이제 `BrickGameManager_Old.cs`로 백업되어 있음

---

## 💻 코드 사용법

### 게임 시작
```csharp
Managers.Game.BrickGame.StartGame();
```

### 게임 일시정지
```csharp
Managers.Game.BrickGame.PauseGame();
```

### 게임 재개
```csharp
Managers.Game.BrickGame.ResumeGame();
```

### 점수 추가 (벽돌 파괴 시)
```csharp
Managers.Game.BrickGame.AddScore(10);
```

### 게임 상태 확인
```csharp
bool isActive = Managers.Game.BrickGame.IsGameActive();
int currentScore = Managers.Game.BrickGame.GetCurrentScore();
int currentLevel = Managers.Game.BrickGame.GetCurrentLevel();
```

### 이벤트 구독
```csharp
// 게임 시작 이벤트
Managers.Game.BrickGame.OnGameStart += () =>
{
    Debug.Log("게임 시작!");
};

// 레벨업 이벤트
Managers.Game.BrickGame.OnLevelUp += (level) =>
{
    Debug.Log($"레벨업! 현재 레벨: {level}");
};

// 점수 변경 이벤트
Managers.Game.BrickGame.OnScoreChanged += (score) =>
{
    Debug.Log($"점수: {score}");
};
```

---

## ⚙️ 설정 커스터마이징

### BrickGameSettings 수정
```csharp
var settings = new BrickGameSettings
{
    initialSpawnDelay = 3f,      // 초기 딜레이 3초
    spawnInterval = 4f,          // 기본 간격 4초
    spawnIntervalDecreaseRate = 0.9f,  // 90%로 감소
    minSpawnInterval = 1.0f,     // 최소 1초
    maxLevel = 100,              // 최대 레벨 100
    initialLevel = 5,            // 레벨 5부터 시작
    initialRowCount = 5          // 초기 5행 생성
};

// GameManager 초기화 시 전달
Managers.Game.InitializeBrickGame(brickPlacer, scoreDisplay, settings);
```

---

## 🔧 확장 가능성

### 1. 다른 게임 모드 추가
```csharp
public class GameManager
{
    private BrickGameManager _brickGame;
    private NetworkGameManager _networkGame;  // 추가
    private PuzzleGameManager _puzzleGame;    // 추가
    
    public BrickGameManager BrickGame => _brickGame;
    public NetworkGameManager NetworkGame => _networkGame;
    public PuzzleGameManager PuzzleGame => _puzzleGame;
}
```

### 2. 커스텀 Placer 구현
```csharp
public class CustomBrickPlacer : MonoBehaviour, IBrickPlacer
{
    public void PlaceMultipleRows(int rowCount)
    {
        // 커스텀 벽돌 생성 로직
    }
}
```

### 3. 네트워크 동기화
```csharp
// BrickGameManager는 Non-MonoBehaviour이므로
// NetworkVariable 대신 이벤트로 동기화
Managers.Game.BrickGame.OnScoreChanged += (score) =>
{
    if (IsServer)
    {
        SyncScoreClientRpc(score);
    }
};
```

---

## 📝 주의사항

1. **반드시 Bootstrap 사용**
   - 직접 `new BrickGameManager()` 하지 말고
   - `Managers.Game.BrickGame` 사용

2. **초기화 순서**
   - `Managers.Init()` → `Managers.Game.InitializeBrickGame()` → `StartGame()`

3. **기존 코드 마이그레이션**
   - 기존에 `BrickGameManager` 참조했던 코드는
   - `Managers.Game.BrickGame`으로 변경

---

## 🎉 장점 요약

✅ **테스트 가능**: Mock 객체로 단위 테스트 작성 가능  
✅ **결합도 낮음**: MonoBehaviour, UI, 네트워크와 분리  
✅ **재사용성**: 다른 씬이나 게임 모드에서도 사용 가능  
✅ **유지보수**: 설정/상태/로직 명확히 분리  
✅ **확장성**: 새로운 게임 모드 추가 용이  
✅ **일관성**: Managers 패턴과 동일한 구조

