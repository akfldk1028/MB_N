# Input System Refactoring 설계 문서

## 📋 목표
BrickGame 전용 InputManager를 **전역 InputManager**로 리팩토링하여 모든 게임 모드에서 재사용 가능하도록 만듭니다.

## 🚨 현재 문제

### 구조적 문제
```
Managers.Update()
  ↓
ActionBus.Publish(System_Update)
  ↓
BrickGameManager.OnUpdate()
  ↓ (IsGameActive == false 체크로 막힘 ❌)
InputManager.UpdateInput() ← 여기서 중단!
```

### 모듈성 문제
1. **InputManager가 BrickGameManager 안에 갇혀있음**
   - 다른 게임(RhythmGame, MapEditor 등)에서 재사용 불가
   - BrickGame이 비활성화되면 모든 입력이 차단됨

2. **책임 혼재**
   - 이전: 모든 게임 입력 통합 관리 (Player, Camera, Rhythm, BrickGame)
   - 현재: BrickGame 전용 입력만 처리

3. **이벤트 흐름 불명확**
   - GameScene → BrickGameInitializer → BrickGameManager → InputManager
   - 너무 깊은 계층 구조

## ✅ 이전 작동하던 구조 (#Refactoring)

```
Managers (MonoBehaviour)
├── InputManager (전역) ✅
│   ├── Init() → Subscribe(Managers_Update)
│   ├── OnUpdate() → HandleKeyboardInput() + HandleMouseInput()
│   └── 이벤트 발행:
│       ├── OnMouseClickWorld
│       ├── OnMouseClickCell
│       ├── OnBackViewKey
│       └── ActionMessage.Publish(...)
├── GameManager
│   └── MoveDir (Player 이동)
├── PlacementManager
├── InGameManager (RhythmGame)
└── Core (Data, Resource, UI...)
```

**핵심: InputManager가 Managers 직속! 게임과 독립적!**

## 🎯 새로운 설계

### 계층 구조
```
Managers (MonoBehaviour)
├── Update() → Publish(System_Update)
│
├── InputManager (전역, static singleton) ✅
│   ├── Init() → Subscribe(System_Update)
│   ├── OnUpdate() → 하드웨어 입력 수집
│   ├── 게임 모드별 이벤트 발행:
│   │   ├── OnArrowKey(horizontal)      → BrickGame
│   │   ├── OnPlayerMove(moveDir)       → 3D 게임
│   │   ├── OnMouseWorldClick(pos)      → MapEditor
│   │   ├── OnRhythmGameKey(keyCode)    → RhythmGame
│   │   └── OnInteract()                → 공통
│   └── CurrentGameMode 확인 (BrickGame? Rhythm? Map?)
│
├── GameManager (static singleton)
│   └── BrickGameManager
│       └── PlankManager (OnArrowKey 구독)
│
├── ObjectManager
└── MapManager
```

### 입력 처리 플로우
```
1. Managers.Update()
   ↓
2. ActionBus.Publish(System_Update)
   ↓
3. InputManager.OnUpdate() ✅ 직접 구독!
   ↓
4. CurrentGameMode 확인
   ├── BrickGame → OnArrowKey 발행
   ├── RhythmGame → OnRhythmGameKey 발행
   └── MapEditor → OnMouseWorldClick 발행
   ↓
5. 각 게임 Manager가 필요한 이벤트만 구독
   └── PlankManager.OnArrowKey 구독 → MoveByKeyboard()
```

## 📐 설계 원칙

### 1. 단일 책임 원칙 (SRP)
- **InputManager**: 하드웨어 입력 수집 + 이벤트 발행
- **PlankManager**: 패들 이동 로직
- **BrickGameManager**: 게임 로직 통합

### 2. 개방-폐쇄 원칙 (OCP)
- 새 게임 추가 시 InputManager 수정 불필요
- 새 이벤트만 추가하면 됨

### 3. 의존성 역전 원칙 (DIP)
- 게임 → InputManager 이벤트 구독 (추상화)
- InputManager는 구체적인 게임을 알 필요 없음

## 🔧 구현 단계

### Phase 1: InputManager 이동
1. `Assets/@Scripts/Managers/Contents/BrickGame/InputManager.cs` 삭제
2. `Assets/@Scripts/#Refactoring/Input/InputManager.cs` 복사
3. `Assets/@Scripts/Managers/InputManager.cs`로 이동 (Managers 직속)

### Phase 2: Managers.cs 통합
1. `private static InputManager s_input = null;`
2. `public static InputManager Input { get { ... } }`
3. `s_input.Init()` 호출 (System_Update 구독)

### Phase 3: BrickGame 연결
1. BrickGameManager에서 InputManager 제거
2. PlankManager가 `Managers.Input.OnArrowKey` 구독
3. InputManager에 BrickGame 전용 이벤트 추가:
   ```csharp
   public event Action<float> OnArrowKey;      // 방향키 (←/→)
   public event Action<Vector3> OnMouseMove;   // 마우스 위치
   ```

### Phase 4: 게임 모드 감지
1. `CurrentGameMode` enum 추가:
   ```csharp
   public enum GameMode
   {
       None,
       BrickGame,
       RhythmGame,
       MapEditor
   }
   ```
2. `SetGameMode()` 메서드 추가
3. 각 게임 시작 시 `Managers.Input.SetGameMode(GameMode.BrickGame)` 호출

### Phase 5: 이벤트 통합
1. 기존 이벤트 유지:
   - `OnMouseClickWorld` → MapEditor
   - `OnBackViewKey` → Camera
   - `OnPlayerMove` → 3D 게임
2. 새 이벤트 추가:
   - `OnArrowKey` → BrickGame
   - `OnRhythmGameKey` → RhythmGame

## 🎯 기대 효과

### 1. 모듈성 향상
- ✅ InputManager 재사용 가능
- ✅ 게임별 입력 로직 분리

### 2. 유지보수성 향상
- ✅ 입력 처리 한 곳에서 관리
- ✅ 디버깅 용이

### 3. 확장성 향상
- ✅ 새 게임 추가 쉬움
- ✅ 이벤트만 추가하면 됨

## 🔍 검증 방법

### 1. BrickGame 작동 확인
```
방향키 입력
→ InputManager.OnUpdate() 호출
→ OnArrowKey 이벤트 발행
→ PlankManager 구독 수신
→ PhysicsPlank.MoveByKeyboard() 호출
→ 패들 이동 ✅
```

### 2. 로그 확인
```
[InputManager] ⌨️ 방향키 입력: → (1.0)
[PlankManager] 키보드 입력 처리 중 (horizontal: 1.0)
[PhysicsPlank] MoveByKeyboard 호출: horizontal=1.00
```

### 3. 다른 게임 영향 없음
- RhythmGame, MapEditor 정상 작동
- 기존 이벤트 유지

## 📝 마이그레이션 체크리스트

- [ ] Phase 1: InputManager 파일 이동
- [ ] Phase 2: Managers.cs 통합
- [ ] Phase 3: BrickGameManager 리팩토링
- [ ] Phase 4: 게임 모드 감지 추가
- [ ] Phase 5: 이벤트 통합
- [ ] 테스트: BrickGame 방향키 작동
- [ ] 테스트: 마우스 입력 작동
- [ ] 회귀 테스트: 다른 게임 정상 작동

