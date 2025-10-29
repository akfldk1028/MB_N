# 블록깨기 게임 아키텍처 문서

## 🎮 게임 구조 (정리 완료)

### Infrastructure Layer (메시지 시스템)
```
ActionMessageBus (static singleton)
├── ActionId (enum)
│   ├── System_Update
│   ├── Input_ArrowKey
│   └── ...
├── Payload Types
│   ├── ArrowKeyPayload
│   ├── WorldPositionPayload
│   └── ...
└── Subscribe/Publish 패턴
```

### Managers Layer (전역 관리)
```
Managers (MonoBehaviour, DontDestroyOnLoad)
├── InputManager (static singleton) ✅ 전역 입력
│   ├── GameMode (BrickGame, ThreeDGame...)
│   ├── HandleKeyboardInput()
│   └── ActionBus.Publish(Input_ArrowKey)
│
├── GameManager (static singleton)
│   └── BrickGameManager
│       ├── PlankManager
│       │   ├── Subscribe(Input_ArrowKey)
│       │   └── UpdateMovement()
│       ├── BallManager
│       │   ├── RegisterBall()
│       │   ├── UnregisterBall()
│       │   └── CurrentPower
│       └── BrickManager
│
├── DataManager
├── ResourceManager
└── UIManager
```

### Game Objects Layer (씬 오브젝트)
```
GameScene
├── Plank (PhysicsPlank)
│   └── PlankManager에서 제어
│
├── Ball (PhysicsBall)
│   ├── EBallState (Ready, Launching, Moving)
│   ├── UpdateReadyState() → Space/Click으로 발사
│   ├── LaunchBall(direction)
│   └── BallManager에 자동 등록
│
└── Bricks (Brick)
    ├── HP 시스템
    ├── OnBallCollision()
    └── BrickManager에 등록
```

---

## 🔄 게임 플로우 (완전 정리)

### 1. 게임 시작
```
1. GameScene.Init()
   ↓
2. Managers.Input.SetGameMode(BrickGame)
   ↓
3. BrickGameInitializer.Initialize()
   ↓
4. BrickGameManager.Initialize()
   ├─ PlankManager.Initialize(plank, camera)
   │   └─ ActionBus.Subscribe(Input_ArrowKey)
   ├─ BallManager.Initialize()
   └─ BrickManager.Initialize()
   ↓
5. BrickGameManager.StartGame()
   ├─ _state.Reset()
   ├─ _state.CurrentPhase = Playing ✅
   └─ _plankManager.ResetPosition()
```

### 2. 메인 게임 루프
```
매 프레임:

1. Managers.Update()
   ↓
2. ActionBus.Publish(System_Update)
   ↓
3. InputManager.OnUpdate() ✅ 방향키 감지
   ↓ (방향키 입력 시)
4. ActionBus.Publish(Input_ArrowKey, ArrowKeyPayload)
   ↓
5. PlankManager.OnArrowKeyInput()
   └─ _currentHorizontalInput = payload.Horizontal
   ↓
6. BrickGameManager.OnUpdate()
   └─ if (_state.IsGameActive) ✅
       ├─ _plankManager.UpdateMovement()
       │   └─ PhysicsPlank.MoveByKeyboard()
       ├─ _ballManager.UpdatePowerTimer()
       └─ SpawnNewRow() (시간 체크)
   ↓
7. PhysicsBall.Update()
   └─ UpdateStateMachine()
       ├─ Ready: UpdateReadyState()
       │   ├─ 패들 따라 이동
       │   └─ Space/Click → LaunchBall()
       ├─ Launching: UpdateLaunchingState()
       └─ Moving: UpdateMovingPhysics()
```

### 3. 공 발사
```
1. PhysicsBall (Ready 상태)
   ↓
2. UpdateReadyState()
   ├─ if (Space || MouseClick) ✅
   ↓
3. LaunchBall(direction)
   ├─ rb.isKinematic = false
   └─ Launch(direction, force)
   ↓
4. CurrentState = Launching
   ↓
5. CurrentState = Moving
```

### 4. 벽돌 충돌
```
1. PhysicsBall.OnCollisionEnter2D(brick)
   ↓
2. Brick.HandleBallCollision()
   ├─ HP -= ball.AttackPower
   ├─ if (HP <= 0) → Destroy()
   └─ BrickManager.OnBrickDestroyed()
   ↓
3. BrickGameManager.AddScore(points)
   ↓
4. if (AllBricksDestroyed)
   └─ LevelUp()
```

### 5. 공 바닥 도달
```
1. PhysicsBall.OnTriggerEnter2D(BottomBoundary)
   ↓
2. HandleTrigger()
   ├─ if (IsLastBall())
   │   └─ ResetBallToReadyState()
   │       ├─ rb.linearVelocity = 0
   │       ├─ CurrentState = Ready
   │       └─ SetBallPositionAbovePlank()
   └─ else
       └─ Destroy(ball)
```

---

## ✅ 해결된 문제들

### 1. 패들 방향키 움직임 ✅
**문제:** BrickGame 전용 InputManager가 BrickGameManager 안에 갇혀있음
**해결:** 전역 InputManager를 Managers 직속으로 승격

**플로우:**
```
InputManager.HandleBrickGameKeyboard()
→ ActionBus.Publish(Input_ArrowKey)
→ PlankManager.OnArrowKeyInput()
→ PlankManager.UpdateMovement()
→ PhysicsPlank.MoveByKeyboard()
```

### 2. IsGameActive = False 문제 ✅
**문제:** StartGame()에서 상태 초기화 순서 잘못됨
```csharp
❌ Before:
_state.CurrentPhase = Playing;
_state.Reset(); // Playing → Idle로 되돌아감!

✅ After:
_state.Reset(); // 먼저 초기화
_state.CurrentPhase = Playing; // 그 다음 설정
```

### 3. 공 발사 시스템 ✅
**현재 상태:** 정상 작동 중
- PhysicsBall이 Ready 상태에서 Space/Click 감지
- LaunchBall()로 발사
- 벽돌 충돌 정상 작동
- 바닥 도달 시 자동 복귀

---

## 🎯 게임 조작법

### 키보드
- **←/→ (화살표 키)**: 패들 이동
- **A/D**: 패들 이동 (대체)
- **Space**: 공 발사

### 마우스
- **마우스 이동**: 패들 추적 (PhysicsPlank.Update()에서 처리)
- **왼쪽 클릭**: 공 발사

---

## 📊 성능 최적화

### 1. 싱글톤 패턴 적용 ✅
- `Managers` (MonoBehaviour, DontDestroyOnLoad)
- `InputManager` (static singleton)
- `GameManager` (static singleton)
- `ActionMessageBus` (static singleton)

### 2. 이벤트 기반 통신 ✅
- `ActionBus.Subscribe/Publish` 패턴
- FindObjectOfType 제거
- BallManager/BrickManager로 등록 관리

### 3. 상태 머신 패턴 ✅
- `EBallState` (Ready, Launching, Moving)
- `GamePhase` (Idle, Playing, Paused, GameOver)

---

## 🔧 추가 개선 제안

### 1. Space 키 입력을 InputManager로 통합
현재 PhysicsBall이 직접 Input 감지 → InputManager로 이동

```csharp
// InputManager.cs - HandleBrickGameKeyboard()
if (Input.GetKeyDown(KeyCode.Space))
{
    Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_LaunchBall));
}

// PhysicsBall.cs - UpdateReadyState()
// ActionBus.Subscribe(Input_LaunchBall, OnLaunchBall) 구독
```

### 2. 게임 난이도 조절
- 공 속도 조절
- 벽돌 HP 증가
- 스폰 간격 감소

### 3. UI 피드백
- 현재 점수 표시
- 레벨 표시
- 남은 목숨 표시

---

## 📝 테스트 체크리스트

- [x] 패들 방향키 이동
- [x] 패들 마우스 추적
- [x] 공 Space/Click 발사
- [x] 공 벽돌 충돌
- [x] 벽돌 파괴
- [x] 공 바닥 도달 → 복귀
- [x] 점수 시스템
- [ ] 레벨업 시스템
- [ ] 게임 오버
- [ ] 재시작

---

## 🚀 다음 단계

1. **UI 시스템 통합**
   - 점수 표시
   - 레벨 표시
   - 일시정지 메뉴

2. **게임 밸런스 조정**
   - 공 속도 테스트
   - 벽돌 HP 밸런스
   - 난이도 곡선

3. **추가 기능**
   - 파워업 아이템
   - 멀티볼
   - 특수 벽돌

---

**작성일:** 2025-10-29  
**버전:** 1.0  
**상태:** 완료 ✅

