# 🚀 빠른 시작 가이드

## 1인 테스트 (혼자 플레이)

### 방법 A: Inspector 설정 (1분)

1. **Unity 에디터 열기**
2. **Hierarchy → @NetworkSystems 선택**
3. **Inspector → ConnectionManagerEx**
4. **Max Connected Players = 1** 설정
5. **재생 버튼 클릭**
6. **Start 버튼 클릭**
7. ✅ 즉시 GameScene으로 전환!

### 방법 B: LocalTest 버튼 (UI 추가 필요)

**StartUpScene에 버튼 추가:**

1. **Hierarchy → Canvas → Buttons**
2. **Start 버튼 복사** (Ctrl+D)
3. **이름: LocalTestButton**
4. **Text: "1인 테스트"**
5. **코드 자동 바인딩됨**

**사용:**
- LocalTest 버튼 클릭
- ✅ 네트워크 없이 즉시 GameScene 전환

---

## 2인 매칭 (멀티플레이어)

1. **Inspector → Max Connected Players = 2** 설정
2. **두 대의 PC 또는 Unity 에디터 + 빌드**

**플레이어 A (첫 번째):**
- Start 버튼 클릭
- Host로 대기...

**플레이어 B (두 번째):**
- Start 버튼 클릭
- Client로 참가
- ✅ 자동으로 GameScene 전환

---

## 🔧 현재 상태 확인

### 콘솔 로그 확인

**정상 플로우 (1인):**
```
[GameModeService] 게임 모드 서비스 생성 (기본: Multiplayer)
[ConnectionManagerEx] 매칭 완료! (현재 1명 / 필요 1명) 즉시 게임 시작
[GameScene] BrickGame 초기화 시작
```

**정상 플로우 (2인):**
```
[ConnectionManagerEx] 매칭 대기 중... (현재 1명 / 필요 2명)
[ConnectionManagerEx] 매칭 완료! (현재 2명 / 필요 2명) 즉시 게임 시작
```

---

## 🚨 문제 해결

### 씬이 안 넘어감

**확인사항:**
1. Max Connected Players 값 확인
2. 현재 연결된 플레이어 수 확인
3. 콘솔에서 "매칭 완료" 로그 확인

**해결:**
- 1인 테스트: Max Connected Players = 1
- 2인 매칭: Max Connected Players = 2

### "player is already a member" 에러

**원인:** 이전 로비가 남아있음

**해결:**
```csharp
// UI_StartUpScene.cs에 이미 추가됨
await Managers.LobbyCleanup.CleanupAllLobbiesAsync();
```

✅ 이미 적용되어 있어서 자동으로 정리됨

---

## 📊 빠른 비교

| 항목 | 1인 테스트 | 2인 매칭 |
|------|-----------|---------|
| **설정** | Max = 1 | Max = 2 |
| **시작** | 즉시 | 대기 후 시작 |
| **네트워크** | Host만 | Host + Client |
| **용도** | 개발/테스트 | 실제 플레이 |

---

**작성일:** 2025-10-20

