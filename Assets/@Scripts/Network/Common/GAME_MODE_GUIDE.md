# 🎮 게임 모드 시스템 가이드

## 📊 개요

MB 프로젝트는 **2가지 게임 모드**를 지원합니다:
1. **Multiplayer Mode** (2인 랜덤 매칭)
2. **Local Test Mode** (1인 로컬 테스트)

---

## 🏗️ 아키텍처

### 모듈 구조

```
Managers
├── GameModeService (게임 모드 관리)
│   ├── Multiplayer (2인)
│   └── LocalTest (1인)
│
├── LobbyCleanupService (로비 정리)
│   ├── LeaveCurrentLobby()
│   ├── CleanupAllLobbies()
│   └── DeleteLobby()
│
└── ConnectionManagerEx
    └── MaxConnectedPlayers (동적 연결)
```

### 의존성 흐름

```
UI_StartUpScene
    ↓
GameModeService.SetMode()
    ↓
LobbyCleanupService.Cleanup()
    ↓
ConnectionManagerEx.StartHost/Client()
    ↓
GameScene
```

---

## 🎯 사용 방법

### 1. 멀티플레이어 모드 (2인 매칭)

**StartUpScene:**
1. "Start" 버튼 클릭
2. 자동으로 멀티플레이어 모드 설정
3. 이전 로비 정리
4. 랜덤 매칭 시작

**플로우:**
```csharp
// UI_StartUpScene.cs
async void OnClickStartButton()
{
    // 1. 멀티플레이어 모드 설정
    Managers.GameMode.SetMultiplayerMode();
    
    // 2. 이전 로비 정리
    await Managers.LobbyCleanup.CleanupAllLobbiesAsync();
    
    // 3. 랜덤 매칭
    bool success = await Managers.Lobby.QuickJoinAsync();
    
    // 4. Host/Client 시작
    if (isHost)
        Managers.Connection.StartHostLobby(playerName);
    else
        Managers.Connection.StartClientLobby(playerName);
}
```

**특징:**
- MaxConnectedPlayers = 2
- MinPlayersToStart = 2
- Unity Lobby 서비스 사용
- 자동 매칭 & 씬 전환

---

### 2. 로컬 테스트 모드 (1인 플레이)

**StartUpScene:**
1. "LocalTest" 버튼 클릭 (또는 추가)
2. 자동으로 로컬 테스트 모드 설정
3. 네트워크 없이 즉시 GameScene 전환

**플로우:**
```csharp
// UI_StartUpScene.cs
async void OnClickLocalTestButton()
{
    // 1. 로컬 테스트 모드 설정
    Managers.GameMode.SetLocalTestMode();
    
    // 2. 이전 로비 정리 (혹시 남은 로비)
    await Managers.LobbyCleanup.CleanupAllLobbiesAsync();
    
    // 3. 직접 GameScene 전환 (네트워크 없음)
    Managers.Scene.LoadScene(EScene.GameScene);
}
```

**특징:**
- MaxConnectedPlayers = 1
- MinPlayersToStart = 1
- Unity Lobby 사용 안 함
- 즉시 게임 시작

---

## 🔧 GameModeService API

### 모드 설정

```csharp
// 멀티플레이어 모드
Managers.GameMode.SetMultiplayerMode();

// 로컬 테스트 모드
Managers.GameMode.SetLocalTestMode();
```

### 상태 확인

```csharp
// 현재 모드
GameModeService.GameMode mode = Managers.GameMode.CurrentMode;

// 최대 플레이어
int maxPlayers = Managers.GameMode.MaxPlayers;

// 최소 플레이어 (게임 시작 조건)
int minPlayers = Managers.GameMode.MinPlayersToStart;

// 로컬 테스트인지
bool isLocalTest = Managers.GameMode.IsLocalTest;

// 멀티플레이어인지
bool isMultiplayer = Managers.GameMode.IsMultiplayer;

// 게임 시작 가능 여부
bool canStart = Managers.GameMode.CanStartGame(currentPlayerCount);
```

---

## 🧹 LobbyCleanupService API

### 로비 정리

```csharp
// 현재 로비 나가기
await Managers.LobbyCleanup.LeaveCurrentLobbyAsync();

// 모든 로비 정리
await Managers.LobbyCleanup.CleanupAllLobbiesAsync();

// 특정 로비 삭제 (Host만)
await Managers.LobbyCleanup.DeleteLobbyAsync(lobbyId);

// 상태 초기화
Managers.LobbyCleanup.ResetLobbyState();
```

### 로비 정보 설정

```csharp
// 현재 로비 정보 저장
Managers.LobbyCleanup.SetCurrentLobby(lobbyId, playerId);

// 로비 참가 여부
bool isInLobby = Managers.LobbyCleanup.IsInLobby;
```

---

## 🚨 에러 해결

### 문제: "player is already a member of the lobby"

**원인:**
- 이전 실행에서 만든 로비가 남아있음
- QuickJoin이 자신의 이전 로비를 찾음
- 다시 참가하려고 해서 에러 발생

**해결:**
```csharp
// StartMultiplayerSession() 시작 부분에 추가
await Managers.LobbyCleanup.CleanupAllLobbiesAsync();
```

✅ **적용 완료:** `UI_StartUpScene.cs` Line 116-117

---

### 문제: MaxConnectedPlayers 변경 안됨

**Before:**
```csharp
public int MaxConnectedPlayers = 1; // 고정값
```

**After:**
```csharp
public int MaxConnectedPlayers => Managers.GameMode?.MaxPlayers ?? 2; // 동적
```

✅ **적용 완료:** `ConnectionManagerEx.cs` Line 83

---

## 📋 Unity Inspector 설정

### StartUpScene에 LocalTest 버튼 추가

1. **Hierarchy → Canvas → Buttons**
2. **Start 버튼 복사** (Ctrl+D)
3. **이름 변경:** "LocalTestButton"
4. **Text 수정:** "1인 테스트"
5. **위치 조정:** Start 버튼 아래

**자동 바인딩:**
```csharp
// UI_StartUpScene.cs에서 자동으로 감지
GetButton((int)Buttons.LocalTestButton).gameObject.BindEvent(OnClickLocalTestButton);
```

---

## 🎯 테스트 시나리오

### 1. 로컬 테스트 (1인)

1. StartUpScene 실행
2. **LocalTest 버튼** 클릭
3. ✅ 즉시 GameScene 전환
4. ✅ 벽돌깨기 게임 플레이
5. ✅ 네트워크 없이 동작

### 2. 멀티플레이어 (2인)

**플레이어 A:**
1. Start 버튼 클릭
2. ✅ Host로 로비 생성
3. ✅ 매칭 대기...

**플레이어 B:**
1. Start 버튼 클릭
2. ✅ Client로 참가
3. ✅ 자동으로 GameScene 전환

---

## 📊 상태 다이어그램

```
[StartUpScene]
    |
    ├─ LocalTest 버튼
    │   ├─ SetLocalTestMode()
    │   ├─ CleanupLobbies()
    │   └─ LoadScene(GameScene) ✅
    │
    └─ Start 버튼
        ├─ SetMultiplayerMode()
        ├─ CleanupLobbies()
        ├─ QuickJoin()
        │   ├─ Success: StartHost/Client
        │   └─ Fail: CreateLobby
        └─ LoadScene(GameScene) (자동)
```

---

## 🔍 코드 위치

| 파일 | 경로 | 역할 |
|------|------|------|
| **GameModeService.cs** | `Assets/@Scripts/Network/Common/` | 모드 관리 |
| **LobbyCleanupService.cs** | `Assets/@Scripts/Network/Common/` | 로비 정리 |
| **Managers.cs** | `Assets/@Scripts/Managers/` | 서비스 통합 |
| **UI_StartUpScene.cs** | `Assets/@Scripts/UI/Scene/` | UI 로직 |
| **ConnectionManagerEx.cs** | `Assets/@Scripts/Network/ConnectionManagement/` | 연결 관리 |

---

## 💡 모범 사례

### ✅ DO

```csharp
// 모드 전환 시 로비 정리
Managers.GameMode.SetMultiplayerMode();
await Managers.LobbyCleanup.CleanupAllLobbiesAsync();

// 게임 시작 전 모드 확인
if (Managers.GameMode.CanStartGame(playerCount))
{
    StartGame();
}

// 현재 모드에 따른 분기
if (Managers.GameMode.IsLocalTest)
{
    // 로컬 전용 로직
}
else
{
    // 멀티플레이어 로직
}
```

### ❌ DON'T

```csharp
// ❌ 하드코딩된 플레이어 수
public int MaxConnectedPlayers = 1;

// ❌ 로비 정리 없이 매칭 시도
await Managers.Lobby.QuickJoinAsync();

// ❌ 모드 확인 없이 네트워크 호출
Managers.Connection.StartHostLobby(name);
```

---

**작성일:** 2025-10-20  
**버전:** 1.0  
**상태:** ✅ 구현 완료

