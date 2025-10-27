# 🔍 BrickGame 아키텍처 검토 (2025-10-19)

## 📊 현재 구조

```
Managers (싱글톤)
└── GameManager
    └── BrickGameManager (Non-MonoBehaviour)
        ├── InputManager (입력 통합)
        ├── PlankManager (패들 제어)
        ├── BallManager (공 관리)
        ├── BrickManager (벽돌 관리)
        ├── BrickGameState (상태)
        └── BrickGameSettings (설정)

GameScene
├── PhysicsPlank (GameObject)
├── PhysicsBall[] (GameObject[])
├── Brick[] (GameObject[])
├── ObjectPlacement (GameObject)
└── ScoreText (GameObject)
```

---

## 🚨 발견된 문제점

### 1. **PhysicsBall ↔ PhysicsPlank 직접 결합**

**문제:**
```csharp
// PhysicsBall.cs
[SerializeField] private PhysicsPlank plank; // Inspector 수동 할당
```

**영향:**
- Inspector에서 **수동으로 할당** 필요 → 휴먼 에러 가능
- PlankManager가 패들 제어하는데, PhysicsBall도 참조 → **중복 관리**
- 결합도 증가 → **테스트 어려움**

**해결책:**
```csharp
// PhysicsBall.cs - 패들 직접 참조 제거
// BallManager가 패들 정보를 공에게 전달
public void Initialize(Vector3 readyPosition, Collider2D plankCollider)
{
    _readyPosition = readyPosition;
    _plankCollider = plankCollider;
}
```

---

### 2. **초기화 순서 불명확**

**현재 문제:**
```
GameScene.Init()
  ↓
InitializeBrickGame()
  ↓
BrickGameManager.Initialize()
  ↓
BrickGameManager.StartGame()
  ↓
PhysicsBall.Start() 실행 시점? ← 이때 plank가 null이면 에러!
```

**해결책: 명확한 초기화 단계**
```
1. GameScene.Init()
   ↓
2. InitializeBrickGame()
   - Managers 초기화
   - 씬 오브젝트 수집 (FindFirstObjectByType)
   ↓
3. SetupGameObjects()
   - 공에게 패들 정보 주입
   - 벽돌 매니저 등록
   ↓
4. BrickGameManager.StartGame()
   - 게임 로직 시작
```

---

### 3. **공 상태 관리 이중화**

**문제:**
```csharp
// PhysicsBall.cs
private EBallState _currentState; // 공 자체가 상태 관리

// BallManager.cs
// 공 개수만 관리, 상태는 관리 안 함
```

**표준 아키텍처 (Breakout/Arkanoid):**
- **Manager가 상태 관리**
- GameObject는 **표현(렌더링)과 물리**만 담당

**개선 방향:**
- BallManager가 **모든 공의 상태** 추적
- PhysicsBall은 **명령 수행**만
```csharp
// BallManager
public void LaunchBall(PhysicsBall ball);
public bool AreAllBallsReturned();

// PhysicsBall
public void ExecuteLaunch(Vector2 direction, float force); // 명령 수행
```

---

### 4. **GameScene 초기화 로직 복잡**

**현재:**
```csharp
// GameScene.cs - 60줄
private void InitializeBrickGame()
{
    // FindFirstObjectByType 반복
    // 검증 로직 반복
    // 초기화 호출
}
```

**개선:**
```csharp
// BrickGameInitializer.cs (새 클래스)
public class BrickGameInitializer
{
    public bool Initialize()
    {
        CollectSceneObjects();
        ValidateRequirements();
        InjectDependencies();
        SetupGameObjects();
        return true;
    }
}

// GameScene.cs - 3줄
private void InitializeBrickGame()
{
    new BrickGameInitializer().Initialize();
}
```

---

## ✅ 개선된 아키텍처

### 1. **의존성 역전 (Dependency Inversion)**

**Before:**
```
PhysicsBall → PhysicsPlank (직접 참조)
```

**After:**
```
PhysicsBall ← BallManager → PlankManager → PhysicsPlank
(인터페이스를 통한 간접 참조)
```

### 2. **명확한 책임 분리**

| 클래스 | 역할 | 책임 |
|--------|------|------|
| **BrickGameManager** | 게임 흐름 제어 | 시작, 일시정지, 종료 |
| **BallManager** | 공 생명주기 관리 | 생성, 발사, 상태 추적 |
| **PlankManager** | 패들 제어 | 이동, 위치 제공 |
| **BrickManager** | 벽돌 관리 | 등록, 파괴 추적 |
| **InputManager** | 입력 통합 | 키보드, 마우스, 터치 |
| **PhysicsBall** | 물리 시뮬레이션 | 충돌, 반사, 렌더링 |
| **PhysicsPlank** | 물리 시뮬레이션 | 충돌, 렌더링 |

### 3. **초기화 순서 표준화**

```
1. GameScene.Init()
   ↓
2. BrickGameInitializer.Initialize()
   a. CollectSceneObjects()    // 씬에서 오브젝트 수집
   b. ValidateRequirements()   // 필수 컴포넌트 검증
   c. InjectDependencies()     // Manager에 의존성 주입
   d. SetupGameObjects()       // GameObject에 매니저 연결
   ↓
3. BrickGameManager.StartGame()
   - 게임 로직 시작
```

---

## 🎯 핵심 개선 사항

### ✅ 1. **공 초기화 개선**

```csharp
// BallManager.cs
public void SetupBall(PhysicsBall ball, Vector3 readyPosition, Collider2D plankCollider)
{
    ball.Initialize(readyPosition, plankCollider);
    RegisterBall(ball);
}

// PhysicsBall.cs
public void Initialize(Vector3 readyPosition, Collider2D plankCollider)
{
    _readyPosition = readyPosition;
    _plankCollider = plankCollider;
    // plank 직접 참조 제거!
}
```

### ✅ 2. **초기화 클래스 분리**

```csharp
// BrickGameInitializer.cs
public class BrickGameInitializer
{
    public bool Initialize()
    {
        var sceneObjects = CollectSceneObjects();
        if (!ValidateRequirements(sceneObjects)) return false;
        
        InjectDependencies(sceneObjects);
        SetupGameObjects(sceneObjects);
        
        return true;
    }
    
    private void SetupGameObjects(SceneObjects objects)
    {
        // 모든 공 초기화
        foreach (var ball in objects.Balls)
        {
            var readyPos = objects.Plank.transform.position + Vector3.up * 0.5f;
            Managers.Game.BrickGame.Ball.SetupBall(
                ball, 
                readyPos, 
                objects.Plank.GetComponent<Collider2D>()
            );
        }
        
        // 모든 벽돌 등록
        foreach (var brick in objects.Bricks)
        {
            Managers.Game.BrickGame.Brick.RegisterBrick(brick);
        }
    }
}
```

### ✅ 3. **GameScene 단순화**

```csharp
// GameScene.cs
private void InitializeBrickGame()
{
    var initializer = new BrickGameInitializer();
    
    if (initializer.Initialize())
    {
        Managers.Game.BrickGame.StartGame();
        GameLogger.Success("GameScene", "BrickGame 초기화 완료!");
    }
    else
    {
        GameLogger.Error("GameScene", "BrickGame 초기화 실패!");
    }
}
```

---

## 📋 Todo: 단계별 개선 작업

### Phase 1: 의존성 분리 ⏳
- [ ] PhysicsBall에서 PhysicsPlank 직접 참조 제거
- [ ] BallManager.SetupBall() 메서드 추가
- [ ] PhysicsBall.Initialize() 메서드 추가

### Phase 2: 초기화 개선 ⏳
- [ ] BrickGameInitializer 클래스 생성
- [ ] SceneObjects 구조체 정의
- [ ] GameScene.cs 단순화

### Phase 3: 상태 관리 통합 (선택) 📅
- [ ] BallManager에 공 상태 관리 추가
- [ ] PhysicsBall 상태 관리 제거
- [ ] 명령 패턴 적용

---

## 🎓 참고: 표준 Breakout 아키텍처

```
GameController (Manager)
├── InputHandler → Paddle
├── BallController
│   └── Ball[] (Physics Objects)
├── BrickController
│   └── Brick[] (Physics Objects)
└── GameState
    ├── Score
    ├── Lives
    └── Level
```

**핵심 원칙:**
1. **Manager가 로직**, GameObject가 **표현**
2. **의존성 주입**으로 결합도 감소
3. **명확한 초기화 순서**
4. **단일 책임 원칙** 준수

---

**작성일:** 2025-10-19  
**상태:** 검토 완료, 개선 작업 대기

