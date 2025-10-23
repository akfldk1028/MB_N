# 🚀 멀티플레이어 테스트 빠른 시작 가이드
*2025년 9월 29일*

## ⚠️ Unity 6 사용자 주의!
ParrelSync가 Unity 6에서 호환성 문제가 있을 수 있습니다.
👉 **대안: `Alternative_Testing_Methods_20250929.md` 참고**

## ⚡ 5분만에 테스트 시작하기

### 1️⃣ ParrelSync 설치 (2분)
```
Unity에서:
Window → Package Manager → "+" → Add package from git URL

이 주소 복사해서 붙여넣기:
https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync

"Add" 클릭하고 기다리기
```

### 2️⃣ Clone 만들기 (2분)
```
ParrelSync → Clones Manager
"Add new clone" → 이름 입력 → "Create"
"Open in New Editor" 클릭
```

### 3️⃣ 테스트 씬 준비 (1분)
```
1. 새 씬 생성
2. 빈 GameObject → LocalNetworkTestManager 컴포넌트 추가
3. NetworkManager 프리팹 씬에 추가 (없으면 생성)
```

### 4️⃣ 테스트 실행 (즉시!)
```
원본 Unity: Play 버튼 → Host 자동 시작
Clone Unity: Play 버튼 → Client 자동 연결
```

## 🎯 확인해야 할 것들

### ✅ 성공 신호
- Host GUI: "Host 시작 성공!"
- Host GUI: "연결된 클라이언트: 1"
- Client GUI: "서버에 성공적으로 연결됨!"

### ❌ 실패 시 체크
- NetworkManager가 씬에 있나?
- UnityTransport 컴포넌트가 NetworkManager에 있나?
- 방화벽에서 7777 포트 허용됐나?

## 🎮 플레이어 테스트 (선택사항)

### 플레이어 프리팹 만들기
```
1. Cube 생성 → DummyPlayer 스크립트 추가
2. NetworkObject 컴포넌트 추가
3. 프리팹으로 저장
4. DummyGameManager 씬에 추가
5. Player Prefab 필드에 할당
```

### 결과 확인
- 빨간색/파란색 플레이어 자동 스폰
- 원형으로 자동 움직임
- WASD로 Host 플레이어 조작 가능

## 🚨 문제 발생시

### ParrelSync 설치 안됨
- Unity 2021.3+ 사용하세요
- Git 설치되어 있나요?
- 인터넷 연결 확인

### 연결 안됨
- Host 먼저 시작했나요?
- 방화벽 7777 포트 허용
- NetworkManager 설정 확인

### 더 자세한 가이드
📖 `NetworkTestingGuide_20240929.md` 참고

---
**5분이면 멀티플레이어 테스트 준비 끝! 🎉**