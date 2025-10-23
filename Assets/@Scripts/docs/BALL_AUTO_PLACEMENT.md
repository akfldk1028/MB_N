# 공 자동 배치 시스템 가이드

## 📋 개요

**공이 자동으로 패들 위에 안착됩니다!**
- ✅ Scene에서 수동 배치 불필요
- ✅ 패들 이동 시 공도 자동으로 따라감
- ✅ 마우스 클릭 또는 스페이스바로 발사

---

## 🎯 핵심 기능

### 1. 자동 안착
```
GameScene 로드
    ↓
BrickGameInitializer.SetupBalls()
    ↓
공에 패들 자동 할당 (Reflection 사용)
    ↓
PhysicsBall.Start()
    ↓
ResetBallToReadyState()
    ↓
SetBallPositionAbovePlank() 호출
    ↓
공이 패들 위에 자동 배치! ✅
```

### 2. 실시간 추적
```
공 상태: Ready
    ↓
Update() → UpdateStateMachine()
    ↓
UpdateReadyState() 호출
    ↓
패들 이동 감지
    ↓
CalculateBallPositionAbovePlank()
    ↓
공 위치 자동 업데이트! ✅
```

### 3. 발사
```
Ready 상태
    ↓
마우스 클릭 또는 스페이스바
    ↓
LaunchBall(launchDirection)
    ↓
상태 변경: Ready → Launching → Moving
    ↓
자유롭게 이동! ✅
```

---

## 🔧 구현 상세

### PhysicsBall.cs

#### 1. 위치 계산 (CalculateBallPositionAbovePlank)
```csharp
private Vector3 CalculateBallPositionAbovePlank()
{
    // 패들의 Collider 높이 계산
    float plankHalfHeight = _plankCollider.bounds.extents.y;
    
    // 공의 Collider 높이 계산
    float ballHalfHeight = objectCollider.bounds.extents.y;
    
    // 패들 위에 공 배치
    float ballY = plank.transform.position.y 
                + plankHalfHeight 
                + ballHalfHeight 
                + SPAWN_OFFSET_Y;
                
    float ballX = plank.transform.position.x;
    
    return new Vector3(ballX, ballY, transform.position.z);
}
```

#### 2. Ready 상태 업데이트 (UpdateReadyState)
```csharp
private void UpdateReadyState()
{
    if (plank != null)
    {
        // 패들 위에 공 위치 유지
        Vector3 targetPosition = CalculateBallPositionAbovePlank();
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = targetPosition;
        }
        
        // 입력 감지
        bool launchInput = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        
        if (launchInput)
        {
            LaunchBall(launchDirection);
            CurrentState = EBallState.Launching;
        }
    }
}
```

#### 3. 상태 진입 (OnEnterState)
```csharp
case EBallState.Ready:
    SetBallPositionAbovePlank(); // 위치 설정
    if (rb != null)
    {
        rb.isKinematic = true; // 물리 비활성화
    }
    break;
```

---

### BrickGameInitializer.cs

#### 자동 패들 할당
```csharp
private void SetupBalls(SceneObjects objects)
{
    foreach (var ball in objects.Balls)
    {
        // Reflection으로 private 필드 접근
        var plankField = typeof(PhysicsBall).GetField("plank", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (plankField != null && objects.Plank != null)
        {
            plankField.SetValue(ball, objects.Plank);
            GameLogger.Success("BrickGameInitializer", 
                $"{ball.name}에 패들 자동 할당 완료!");
        }
    }
}
```

---

## 📊 상태 다이어그램

```
None (초기)
    ↓
Ready (패들 위 대기)
    ↓ [마우스 클릭 / 스페이스바]
Launching (발사 중)
    ↓
Moving (자유 이동)
    ↓ [바닥 도달]
Ready (다시 준비)
```

---

## 🎮 Scene 설정 방법

### 1. PhysicsBall GameObject
- Inspector에서 패들 할당 **불필요!** (자동 할당)
- `Launch Direction`: (0, 1) (위쪽)
- `Launch Force`: 1.0
- `SPAWN_OFFSET_Y`: 0.05 (공과 패들 사이 간격)

### 2. PhysicsPlank GameObject
- Collider2D 필수 (공 위치 계산에 사용)
- `leftEnd`, `rightEnd` 설정 (이동 범위)

### 3. 자동 초기화
```csharp
// GameScene.cs
private void InitializeBrickGame()
{
    var initializer = new BrickGameInitializer();
    if (initializer.Initialize())
    {
        Managers.Game.BrickGame?.StartGame();
    }
}
```

---

## 🔍 디버깅

### 로그 확인
```
[PhysicsBall] {ball.name} 패들 위로 이동: (x, y, z)
[BrickGameInitializer] {ball.name}에 패들 자동 할당 완료!
[BrickGameInitializer] 3개의 공 설정 완료
```

### 경고 메시지
```
⚠️ [PhysicsBall] {ball.name}: 패들이 할당되지 않았습니다!
→ BrickGameInitializer가 실행되지 않았거나 패들이 Scene에 없음

⚠️ [PhysicsBall] {ball.name}: 패들에 Collider2D가 없습니다!
→ PhysicsPlank에 Collider2D 추가 필요
```

---

## 📝 핵심 요약

### 개발자가 할 일
1. Scene에 `PhysicsPlank` GameObject 배치
2. Scene에 `PhysicsBall` GameObject 배치
3. 끝! (나머지는 자동)

### 시스템이 자동 처리
1. ✅ 공에 패들 자동 할당
2. ✅ 공을 패들 위에 자동 배치
3. ✅ 패들 이동 시 공 자동 추적
4. ✅ 입력 감지 및 발사
5. ✅ 바닥 도달 시 자동 리셋

---

## 🚀 장점

### Before (수동 배치)
- ❌ Scene마다 공 위치 수동 조정
- ❌ 패들 이동 시 공이 떨어짐
- ❌ 복잡한 초기화 로직

### After (자동 배치)
- ✅ Scene에 그냥 배치만 하면 끝
- ✅ 패들 이동 자동 추적
- ✅ 코드로 모든 것 자동 처리

---

**작성일**: 2025-10-20  
**마지막 수정**: 2025-10-20

