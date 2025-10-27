# 패들 방향키 제어 가이드

## 📋 문제 해결

### ❌ 기존 문제
```
1. ObjectPlacementAdapter 없음 → 초기화 실패
2. PhysicsPlank.enabled = false → 전체 비활성화 (Collider까지!)
3. 패들이 방향키로 움직이지 않음
```

### ✅ 해결 방법
```
1. ObjectPlacementAdapter를 선택적(optional)으로 변경
2. PhysicsPlank.Update() 주석처리 (enabled는 true 유지)
3. PlankManager가 전적으로 입력 처리
```

---

## 🎮 입력 시스템 구조

```
Managers.Update()
    ↓
PublishAction(ActionId.System_Update)
    ↓
BrickGameManager.OnUpdate()
    ↓
InputManager.UpdateInput()
    ├─ ProcessKeyboardInput()  ← 방향키 감지!
    ├─ ProcessMouseInput()
    └─ ProcessTouchInput()
    ↓
PlankManager.UpdateMovement(deltaTime)
    ↓
ProcessKeyboardMovement(deltaTime)
    ├─ HorizontalInput 읽기
    ├─ 새 위치 계산
    ├─ 경계 제한 (Clamp)
    └─ Rigidbody2D.MovePosition() or Transform.position
```

---

## 🔧 핵심 수정 사항

### 1. BrickGameInitializer.cs
```csharp
// ObjectPlacementAdapter를 선택적으로 변경
if (objects.ObjectPlacementAdapter == null)
{
    GameLogger.Warning("BrickGameInitializer", 
        "ObjectPlacementAdapter를 찾을 수 없습니다. 벽돌 자동 생성 불가 (1인 테스트는 OK)");
}
```

### 2. BrickGameManager.cs
```csharp
// brickPlacer와 scoreDisplay는 null 허용
_brickPlacer = brickPlacer;  // throw 제거
_scoreDisplay = scoreDisplay; // throw 제거

if (_brickPlacer == null)
{
    GameLogger.Warning("BrickGameManager", "BrickPlacer가 null입니다. 벽돌 자동 생성 불가");
}
```

### 3. PlankManager.cs
```csharp
// PhysicsPlank.enabled = false 제거!
// (enabled = false 하면 Collider도 비활성화됨)

// PhysicsPlank의 Update는 그대로 두고 PlankManager가 추가 제어
// (_plank.enabled = false 하면 Collider도 비활성화됨!)
```

### 4. PhysicsPlank.cs
```csharp
// Update() 주석처리 (PlankManager가 전담)
// PlankManager가 입력 처리하므로 Update() 비활성화
// void Update()
// {
//     // PlankManager.UpdateMovement()가 모든 입력 처리를 담당합니다.
//     // 이 Update()는 사용하지 않습니다.
// }
```

---

## 🎯 입력 처리 플로우

### InputManager.ProcessKeyboardInput()
```csharp
if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
{
    horizontal = -1f;
}
else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
{
    horizontal = 1f;
}

if (horizontal != 0f)
{
    _currentInputType = InputType.Keyboard;
    _horizontalInput = horizontal;
    OnHorizontalInput?.Invoke(horizontal);
    GameLogger.DevLog("InputManager", $"⌨️ 방향키 입력: {(horizontal > 0 ? "→" : "←")}");
}
```

### PlankManager.ProcessKeyboardMovement()
```csharp
float horizontal = _inputManager.HorizontalInput;
if (Mathf.Abs(horizontal) < 0.01f) return;

Vector3 currentPosition = _plank.transform.position;
float targetX = currentPosition.x + (horizontal * _keyboardMoveSpeed * deltaTime);

// 경계 제한
targetX = Mathf.Clamp(targetX, _plank.leftEnd.position.x, _plank.rightEnd.position.x);

Vector3 newPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

// Rigidbody2D로 이동
Rigidbody2D rb = _plank.GetComponent<Rigidbody2D>();
if (rb != null && rb.isKinematic)
{
    rb.MovePosition(newPosition);
    GameLogger.DevLog("PlankManager", $"🎮 패들 이동 (Kinematic): {currentPosition.x:F2} → {newPosition.x:F2}");
}
```

---

## 🔍 디버깅 로그

### 정상 작동 시 로그
```
[InputManager] ⌨️ 방향키 입력: → (1.0)
[PlankManager] 🎮 패들 이동 (Kinematic): 0.00 → 0.15
[InputManager] ⌨️ 방향키 입력: ← (-1.0)
[PlankManager] 🎮 패들 이동 (Kinematic): 0.15 → 0.00
```

### 문제 발생 시 체크리스트
```
1. ✅ BrickGameManager.StartGame() 호출되었는가?
   → GameScene.InitializeBrickGame() 확인

2. ✅ InputManager.Enabled = true 인가?
   → BrickGameManager.StartGame()에서 설정

3. ✅ PlankManager.Enabled = true 인가?
   → BrickGameManager.StartGame()에서 설정

4. ✅ PhysicsPlank.enabled = true 인가?
   → PlankManager.Initialize()에서 더 이상 false 설정 안 함

5. ✅ PhysicsPlank.Update()가 주석처리되었는가?
   → PlankManager와 충돌 방지

6. ✅ PhysicsPlank.leftEnd, rightEnd가 할당되었는가?
   → Inspector에서 확인
```

---

## 🎮 지원 키

### 방향키
- `←` (LeftArrow) or `A` - 왼쪽 이동
- `→` (RightArrow) or `D` - 오른쪽 이동

### 마우스
- 왼쪽 버튼 클릭 + 드래그 - 패들 위치로 이동

### 터치 (모바일)
- 터치 + 드래그 - 패들 위치로 이동

---

## ⚙️ 설정 값

### PlankManager 설정
```csharp
private float _keyboardMoveSpeed = 15f;  // 방향키 이동 속도
```

### PhysicsPlank 설정
```csharp
public float smoothSpeed = 20f;  // 마우스 이동 부드러움 (사용 안 함)
```

---

## 📝 핵심 요약

### 작동 원리
1. **InputManager**: 방향키/마우스/터치 입력 통합 감지
2. **PlankManager**: InputManager로부터 입력 받아 패들 이동 처리
3. **PhysicsPlank**: Collider와 물리 제공, Update()는 비활성화

### 왜 이렇게?
- ✅ **중앙집중식 관리**: 모든 입력을 InputManager가 처리
- ✅ **유지보수 용이**: 입력 로직이 한 곳에 집중
- ✅ **테스트 가능**: Non-MonoBehaviour 클래스로 분리
- ✅ **플랫폼 독립**: 키보드/마우스/터치 모두 지원

---

**작성일**: 2025-10-20  
**마지막 수정**: 2025-10-20

