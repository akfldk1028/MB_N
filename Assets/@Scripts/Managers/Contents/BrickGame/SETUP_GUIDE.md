# 벽돌깨기 게임 Unity 씬 설정 가이드

## 📋 개요

리팩토링된 BrickGame 시스템을 Unity 씬에서 설정하는 방법을 단계별로 안내합니다.

---

## 🎯 필수 컴포넌트

### 1. **@BrickGameBootstrap** (새로 추가 필요)
씬에 빈 GameObject를 생성하고 `BrickGameBootstrap` 컴포넌트를 추가합니다.

**역할:**
- 게임 시작 시 자동으로 `Managers.Game.BrickGame` 초기화
- 필요한 어댑터들을 찾아서 의존성 주입
- 자동으로 게임 시작 (옵션)

### 2. **ObjectPlacement** (기존)
벽돌 배치를 담당하는 컴포넌트입니다. (이미 씬에 존재)

**확인사항:**
- `PlaceMultipleRows(int rowCount)` 메서드가 구현되어 있어야 함

### 3. **@ObjectPlacementAdapter** (새로 추가 필요)
ObjectPlacement를 감싸는 Adapter입니다.

### 4. **@ScoreDisplayAdapter** (새로 추가 필요)
점수를 표시할 TextMeshPro를 감싸는 Adapter입니다.

---

## 🔧 단계별 설정 방법

### Step 1: Bootstrap 생성

1. Hierarchy에서 우클릭 → `Create Empty`
2. 이름을 `@BrickGameBootstrap`으로 변경
3. `BrickGameBootstrap` 컴포넌트 추가

### Step 2: ObjectPlacementAdapter 생성

**Option A: 기존 ObjectPlacement GameObject에 추가**
```
1. ObjectPlacement가 있는 GameObject 선택
2. ObjectPlacementAdapter 컴포넌트 추가
3. Inspector에서 ObjectPlacement 필드가 자동으로 채워지는지 확인
```

**Option B: 별도 GameObject 생성**
```
1. Hierarchy에서 Create Empty
2. 이름: @ObjectPlacementAdapter
3. ObjectPlacementAdapter 컴포넌트 추가
4. Inspector에서 ObjectPlacement 참조 설정
```

### Step 3: ScoreDisplayAdapter 생성

1. 점수를 표시할 **TextMeshPro** GameObject 찾기 (예: "ScoreText")
2. 해당 GameObject 또는 부모에 `ScoreDisplayAdapter` 컴포넌트 추가
3. Inspector에서 TextMeshPro 참조 설정

### Step 4: Bootstrap 설정

`@BrickGameBootstrap`를 선택하고 Inspector에서:

```
[필수 컴포넌트 참조]
- Object Placement Adapter: (드래그 앤 드롭)
- Score Display Adapter: (드래그 앤 드롭)

[게임 설정]
- Game Settings: (비워두면 기본값 사용)

[자동 시작]
- Auto Start Game: ✓ (체크하면 씬 시작 시 자동 게임 시작)
```

---

## ✅ 검증 체크리스트

씬이 제대로 설정되었는지 확인:

### 필수 GameObject
- [ ] `@BrickGameBootstrap` 존재
- [ ] `ObjectPlacement` 또는 `@ObjectPlacementAdapter` 존재
- [ ] TextMeshPro + `@ScoreDisplayAdapter` 존재

### 컴포넌트 참조
- [ ] `BrickGameBootstrap.objectPlacementAdapter` 설정됨
- [ ] `BrickGameBootstrap.scoreDisplayAdapter` 설정됨
- [ ] `ObjectPlacementAdapter.objectPlacement` 설정됨 (자동)
- [ ] `ScoreDisplayAdapter.scoreText` 설정됨 (자동 또는 수동)

### 실행 확인
- [ ] Play 버튼 클릭
- [ ] Console에 `[BrickGameBootstrap] BrickGame 초기화 완료!` 로그 확인
- [ ] Console에 `[BrickGameManager] 게임 시작!` 로그 확인
- [ ] 점수가 표시되는지 확인

---

## 🎮 실행 시 동작 흐름

```
1. Unity Play 시작
   ↓
2. BrickGameBootstrap.Start()
   ↓
3. Managers.Game.InitializeBrickGame()
   ↓
4. BrickGameManager 초기화 (DI)
   ├─ BallManager 초기화
   ├─ BrickManager 초기화
   └─ ActionBus 구독
   ↓
5. (AutoStart=true면) BrickGameManager.StartGame()
   ↓
6. 게임 시작! 🎉
```

---

## 🐛 문제 해결

### Console에 에러가 표시되는 경우

#### "ObjectPlacementAdapter를 찾을 수 없습니다!"
→ Step 2를 확인하여 ObjectPlacementAdapter를 생성하세요.

#### "ScoreDisplayAdapter를 찾을 수 없습니다. 점수 표시 불가"
→ Warning이므로 게임은 실행되지만, 점수가 표시되지 않습니다.
→ Step 3를 확인하여 ScoreDisplayAdapter를 생성하세요.

#### "BrickGame이 null입니다."
→ Managers가 제대로 초기화되지 않았습니다.
→ 씬에 `@Managers` GameObject가 있는지 확인하세요.

### 게임이 시작되지 않는 경우

1. **BrickGameBootstrap.autoStartGame이 체크되어 있는지 확인**
2. **수동 시작**: BrickGameBootstrap에서 우클릭 → `Test: Start Game`
3. **코드로 시작**: `Managers.Game.BrickGame.StartGame();`

---

## 🔄 기존 코드와의 차이점

### Before (Old)
```csharp
// MonoBehaviour 직접 사용
public class BrickGameManager : MonoBehaviour
{
    void Update() { /* ... */ }
    FindObjectOfType<BrickGameManager>();
}
```

### After (New)
```csharp
// Non-MonoBehaviour + DI
public class BrickGameManager
{
    void OnUpdate() { /* ... */ }  // ActionBus 구독
}

// 접근 방법
Managers.Game.BrickGame.StartGame();
```

---

## 📚 참고 문서

- `README.md` - 전체 아키텍처 개요
- `ARCHITECTURE.md` - 상세 설계
- `REFACTORING_SUMMARY.md` - 리팩토링 요약
- `OBJECT_POOL_GUIDE.md` - Object Pool 통합 가이드

---

## 💡 테스트 메서드 (Inspector Context Menu)

`@BrickGameBootstrap` GameObject를 선택한 상태에서 우클릭:

- **Test: Start Game** - 게임 시작
- **Test: Pause Game** - 게임 일시정지
- **Test: Resume Game** - 게임 재개
- **Test: Add Score 100** - 점수 100 추가 (테스트용)

---

**작성일**: 2025년 10월 18일
**버전**: 1.0

