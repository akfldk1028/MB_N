# BrickGame 리팩토링 계획

## 📊 현재 구조 (As-Is)

```
BrickGameManager (Non-MonoBehaviour)
  ├─ Settings, State만 관리
  ├─ 스폰 타이밍 제어
  └─ 점수/레벨 관리

PhysicsBall (독립적) ❌
  ├─ static currentPower (전역)
  └─ FindObjectsOfType() 사용

Brick (독립적) ❌
  ├─ Managers.Game.BrickGame 호출
  └─ 독립적으로 동작

CommonVars (전역 변수) ❌
  └─ level, canContinue 등
```

---

## 🎯 목표 구조 (To-Be)

```
Managers.Game.BrickGame (Core Manager)
  │
  ├─ BrickGameState (상태)
  │   ├─ GamePhase (Idle, Playing, Paused, GameOver)
  │   ├─ Score, Level
  │   └─ IsGameActive
  │
  ├─ BallManager (공 관리)
  │   ├─ PowerManager (공격력)
  │   ├─ BallPool (오브젝트 풀)
  │   ├─ ActiveBalls (활성 공 목록)
  │   └─ Events (OnBallLaunched, OnAllBallsReturned)
  │
  ├─ BrickManager (벽돌 관리)
  │   ├─ BrickPool (오브젝트 풀)
  │   ├─ ActiveBricks (활성 벽돌 목록)
  │   ├─ BrickGrid (그리드 관리)
  │   └─ Events (OnBrickDestroyed, OnAllBricksCleared)
  │
  ├─ PowerUpManager (파워업 관리)
  │   ├─ PowerUpPool
  │   ├─ ActivePowerUps
  │   └─ Events (OnPowerUpCollected)
  │
  └─ EventSystem (통합 이벤트)
      ├─ GameEvents (OnGameStart, OnGameOver)
      ├─ ScoreEvents (OnScoreChanged, OnLevelUp)
      └─ ObjectEvents (OnObjectSpawned, OnObjectDestroyed)
```

---

## 📋 단계별 리팩토링 계획

### Phase 1: Foundation (기반 구조)
1. ✅ BrickGameSettings - 설정 분리
2. ✅ BrickGameState - 상태 분리
3. ✅ Interfaces - 추상화
4. ⏳ GamePhase Enum 추가
5. ⏳ CommonVars 제거 준비

### Phase 2: Manager 계층 구축
1. ⏳ BallManager 생성
   - BallPowerManager (공격력)
   - BallPool (풀링)
   - ActiveBalls 관리

2. ⏳ BrickManager 생성
   - BrickPool (풀링)
   - ActiveBricks 관리
   - GridManager 통합

3. ⏳ PowerUpManager 생성
   - Star, BonusBall 통합
   - PowerUpPool

### Phase 3: 기존 코드 통합
1. ⏳ PhysicsBall 리팩토링
   - static 변수 제거
   - BallManager 의존성 주입
   - FindObjectsOfType 제거

2. ⏳ Brick 리팩토링
   - BrickManager 등록
   - 이벤트 기반으로 변경

3. ⏳ CommonVars 완전 제거
   - BrickGameState로 이관

### Phase 4: 최적화
1. ⏳ Object Pooling 구현
2. ⏳ Event 시스템 통합
3. ⏳ 성능 프로파일링

---

## 🎯 Design Principles

### 1. Single Responsibility (단일 책임)
- 각 Manager는 하나의 책임만
- BallManager = 공만 관리
- BrickManager = 벽돌만 관리

### 2. Dependency Injection (의존성 주입)
- MonoBehaviour들은 Manager에 등록
- Manager를 통해 다른 객체 접근

### 3. Event-Driven (이벤트 기반)
- Manager간 직접 호출 금지
- 모든 통신은 이벤트로

### 4. Object Pooling (풀링)
- Instantiate/Destroy 최소화
- Pool에서 재사용

### 5. No FindObjectsOfType
- Manager가 등록/관리
- 리스트/딕셔너리로 조회

---

## 📝 코딩 규칙

### 1. 접근 방식
```csharp
// ❌ 나쁜 예
FindObjectOfType<BrickGameManager>()
FindObjectsOfType<PhysicsBall>()
static currentPower

// ✅ 좋은 예
Managers.Game.BrickGame.Ball.IncreasePower(1, 15f)
Managers.Game.BrickGame.Ball.GetActiveBallCount()
Managers.Game.BrickGame.Brick.GetBrickAt(x, y)
```

### 2. 등록 패턴
```csharp
// PhysicsBall.cs - Awake/OnEnable
void OnEnable()
{
    Managers.Game?.BrickGame?.Ball?.RegisterBall(this);
}

void OnDisable()
{
    Managers.Game?.BrickGame?.Ball?.UnregisterBall(this);
}
```

### 3. 이벤트 사용
```csharp
// Brick.cs - 파괴 시
Managers.Game.BrickGame.OnBrickDestroyed?.Invoke(this);

// ReleaseGameManager.cs - 구독
Managers.Game.BrickGame.OnBrickDestroyed += HandleBrickDestroyed;
```

---

## 🚀 예상 효과

### 성능
- Object Pooling → Instantiate/Destroy 90% 감소
- FindObjectsOfType 제거 → CPU 사용량 감소

### 유지보수
- 중앙 집중식 → 디버깅 용이
- 계층 구조 → 기능 추가 쉬움
- 이벤트 기반 → 결합도 낮음

### 확장성
- 새로운 게임 모드 추가 용이
- NetworkGame 등 다른 게임과 패턴 일치
- 멀티플레이어 통합 가능

---

## ⚠️ 주의사항

### 1. 점진적 마이그레이션
- 한 번에 모든 것 변경 X
- Phase별로 테스트

### 2. 하위 호환성
- 기존 코드 일부 유지
- 점진적 Deprecated

### 3. 네트워크 고려
- NetworkVariable 호환
- IsServer 체크 유지

---

## 📅 예상 일정

- Phase 1: 2일 (기반 구조)
- Phase 2: 3일 (Manager 계층)
- Phase 3: 3일 (기존 코드 통합)
- Phase 4: 2일 (최적화)
- **Total: 10일**

---

## ✅ 완료 기준

1. CommonVars 완전 제거
2. FindObjectsOfType 완전 제거
3. static 변수 제거 (공격력 등)
4. Object Pooling 적용
5. 모든 기존 기능 정상 동작
6. 성능 개선 확인
7. 문서화 완료

