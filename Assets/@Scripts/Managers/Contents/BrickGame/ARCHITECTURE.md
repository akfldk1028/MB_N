# BrickGame 아키텍처 문서

## 🏗️ 전체 구조 개요

```
[Managers.cs] (최상위)
    └─ [GameManager]
        └─ [BrickGameManager] (Non-MonoBehaviour)
            ├─ [BrickGameSettings] (설정)
            ├─ [BrickGameState] (상태)
            └─ 의존성 주입 (Interfaces)
                ├─ [IBrickPlacer]
                ├─ [IScoreDisplay]
                └─ [ITimeProvider]

[씬 레벨]
    └─ [BrickGameBootstrap] (MonoBehaviour)
        ├─ [ObjectPlacementAdapter] → IBrickPlacer 구현
        ├─ [ScoreDisplayAdapter] → IScoreDisplay 구현
        └─ [UnityTimeProvider] → ITimeProvider 구현
```

---

## 📦 모듈별 의존성 분석

### 1. **Interfaces Layer** (의존성 없음)
```
IBrickPlacer.cs      ← 아무것도 의존하지 않음
IScoreDisplay.cs     ← 아무것도 의존하지 않음
ITimeProvider.cs     ← 아무것도 의존하지 않음
```

### 2. **Data Layer**
```
BrickGameSettings.cs
  └─ UnityEngine (Serializable, Range 등)

BrickGameState.cs
  ├─ UnityEngine (Mathf)
  └─ GameLogger
```

### 3. **Adapter Layer**
```
UnityTimeProvider.cs
  ├─ ITimeProvider (구현)
  └─ UnityEngine (Time)

ObjectPlacementAdapter.cs
  ├─ IBrickPlacer (구현)
  ├─ MonoBehaviour (상속)
  ├─ ObjectPlacement (참조)
  └─ GameLogger

ScoreDisplayAdapter.cs
  ├─ IScoreDisplay (구현)
  ├─ MonoBehaviour (상속)
  ├─ TMPro (TextMeshPro)
  └─ GameLogger
```

### 4. **Core Logic Layer**
```
BrickGameManager.cs
  ├─ IBrickPlacer (의존성)
  ├─ IScoreDisplay (의존성)
  ├─ ITimeProvider (의존성)
  ├─ BrickGameSettings (설정)
  ├─ BrickGameState (상태)
  ├─ CommonVars (전역 변수)
  ├─ UnityEngine (Mathf)
  └─ GameLogger
```

### 5. **Manager Layer**
```
GameManager.cs
  ├─ BrickGameManager (인스턴스)
  ├─ MB.Infrastructure.Messages (ActionId)
  ├─ Managers (ActionBus 구독)
  ├─ UnityTimeProvider (생성)
  └─ GameLogger
```

### 6. **Bootstrap Layer**
```
BrickGameBootstrap.cs
  ├─ MonoBehaviour (상속)
  ├─ ObjectPlacementAdapter (참조)
  ├─ ScoreDisplayAdapter (참조)
  ├─ BrickGameSettings (설정)
  ├─ Managers.Game (접근)
  └─ GameLogger
```

---

## 🔄 데이터 흐름

### 초기화 플로우
```
1. Unity Scene Load
   └─ @Managers GameObject 생성 (DontDestroyOnLoad)
       └─ Managers.Awake()
           └─ GameManager 생성
               └─ BrickGameManager 인스턴스 생성 (비초기화 상태)

2. BrickGame Scene Load
   └─ BrickGameBootstrap.Start()
       ├─ ObjectPlacementAdapter 찾기
       ├─ ScoreDisplayAdapter 찾기
       ├─ BrickGameSettings 준비
       └─ Managers.Game.InitializeBrickGame()
           ├─ UnityTimeProvider 생성
           ├─ BrickGameManager.Initialize() 호출
           │   └─ 의존성 주입 완료
           ├─ ActionBus에 System_Update 구독
           └─ (옵션) BrickGame.StartGame()
```

### 게임 실행 플로우
```
1. 매 프레임 (Update)
   └─ Managers.Update()
       └─ ActionBus.Publish(ActionId.System_Update)
           └─ BrickGameManager.OnUpdate() 호출
               ├─ IsGameActive 확인
               ├─ TimeProvider.CurrentTime 체크
               └─ 스폰 타이밍 도달 시
                   ├─ SpawnNewRow()
                   │   └─ IBrickPlacer.PlaceMultipleRows(1)
                   └─ AdjustDifficulty()
                       └─ BrickGameState 업데이트

2. 점수 추가 (외부 호출)
   └─ Managers.Game.BrickGame.AddScore(10)
       ├─ BrickGameState.AddScore()
       ├─ IScoreDisplay.UpdateScore()
       └─ OnScoreChanged 이벤트 발생

3. 레벨업
   └─ BrickGameManager.IncreaseLevel()
       ├─ CommonVars.level++
       ├─ AdjustDifficultyByLevel()
       └─ OnLevelUp 이벤트 발생
```

---

## 🎯 설계 원칙 준수 확인

### ✅ SOLID 원칙

**1. Single Responsibility (단일 책임)**
- BrickGameManager: 게임 로직만
- BrickGameSettings: 설정만
- BrickGameState: 상태 관리만
- Adapters: 인터페이스 구현만

**2. Open/Closed (개방/폐쇄)**
- 인터페이스로 확장 가능
- 새로운 Placer 추가 시 BrickGameManager 수정 불필요

**3. Liskov Substitution (리스코프 치환)**
- IBrickPlacer 구현체들은 교체 가능
- Mock 객체로 테스트 가능

**4. Interface Segregation (인터페이스 분리)**
- IBrickPlacer, IScoreDisplay, ITimeProvider 각각 분리
- 필요한 것만 의존

**5. Dependency Inversion (의존성 역전)**
- BrickGameManager는 구체 클래스가 아닌 인터페이스에 의존
- 의존성 주입 (Dependency Injection) 사용

---

## 🧪 테스트 가능성

### Unit Test 예시
```csharp
[Test]
public void TestBrickGameManager_AddScore()
{
    // Arrange
    var mockPlacer = new MockBrickPlacer();
    var mockDisplay = new MockScoreDisplay();
    var mockTime = new MockTimeProvider();
    var settings = BrickGameSettings.CreateDefault();
    
    var manager = new BrickGameManager();
    manager.Initialize(mockPlacer, mockDisplay, mockTime, settings);
    manager.StartGame();
    
    // Act
    manager.AddScore(100);
    
    // Assert
    Assert.AreEqual(100, manager.GetCurrentScore());
    Assert.AreEqual(1, mockDisplay.UpdateCallCount);
}
```

---

## 🔌 확장 포인트

### 1. 새로운 게임 모드 추가
```csharp
public class GameManager
{
    private BrickGameManager _brickGame;
    private NetworkBattleGameManager _networkGame;  // 새로 추가
    
    public BrickGameManager BrickGame => _brickGame;
    public NetworkBattleGameManager NetworkGame => _networkGame;
}
```

### 2. 커스텀 벽돌 배치 알고리즘
```csharp
public class WavePatternPlacer : MonoBehaviour, IBrickPlacer
{
    public void PlaceMultipleRows(int rowCount)
    {
        // 웨이브 패턴 생성 로직
    }
}
```

### 3. 네트워크 동기화
```csharp
Managers.Game.BrickGame.OnScoreChanged += (score) =>
{
    if (IsServer)
    {
        SyncScoreToClientsRpc(score);
    }
};
```

---

## ⚠️ 주의사항

### 1. 초기화 순서 엄수
```
Managers.Init() 
  → Managers.Game.InitializeBrickGame() 
  → Managers.Game.BrickGame.StartGame()
```

### 2. ActionBus 구독 시점
- GameManager.InitializeBrickGame()에서 자동 구독됨
- 수동으로 구독하지 말 것

### 3. CommonVars 사용
- 전역 변수이므로 멀티 게임 모드 시 주의
- 게임 시작 시 RestartAllVariables() 호출 필수

---

## 📊 성능 고려사항

### 메모리
- BrickGameManager: Non-MonoBehaviour → GC 부담 적음
- 상태 객체: 재사용 (매번 생성 X)
- 이벤트: 구독 해제 불필요 (생명주기가 게임과 동일)

### CPU
- Update: ActionBus 한 번만 발동 → O(1)
- 타이밍 체크: 단순 float 비교 → 매우 빠름
- 인터페이스 호출: 가상 함수 오버헤드 극소

---

## 🎉 완성도 체크리스트

- ✅ MonoBehaviour 의존성 제거
- ✅ 인터페이스 추상화
- ✅ 의존성 주입 패턴
- ✅ 설정/상태/로직 분리
- ✅ ActionBus 통합
- ✅ Managers 패턴 준수
- ✅ 계층적 구조 (GameManager > BrickGameManager)
- ✅ 테스트 가능 구조
- ✅ 확장 가능 설계
- ✅ 린트 에러 없음
- ✅ 문서화 완료

