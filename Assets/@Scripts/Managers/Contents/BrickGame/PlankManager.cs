using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임의 패들(플랭크) 관리 매니저
/// - 패들 이동 제어
/// - InputManager로부터 입력 받기
/// - PhysicsPlank 통합 관리
/// </summary>
public class PlankManager
{
    #region 참조
    private PhysicsPlank _plank;
    private Camera _mainCamera;
    private Plane _plankPlane; // Raycast용 평면
    private IDisposable _arrowKeySubscription; // ✅ ActionBus 구독 해제용
    #endregion
    
    #region 입력 상태 (전역 InputManager로부터 받음)
    private float _currentHorizontalInput = 0f; // ✅ 현재 방향키 입력값
    #endregion
    
    #region 설정
    private float _keyboardMoveSpeed = 15f;  // 방향키 이동 속도
    private bool _enabled = true;
    
    /// <summary>
    /// 패들 활성화 여부
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (_plank != null)
            {
                _plank.enabled = _enabled;
            }
        }
    }
    
    /// <summary>
    /// 방향키 이동 속도
    /// </summary>
    public float KeyboardMoveSpeed
    {
        get => _keyboardMoveSpeed;
        set => _keyboardMoveSpeed = Mathf.Max(1f, value);
    }
    #endregion
    
    #region 이벤트
    public event Action<Vector3> OnPlankMoved;
    #endregion
    
    public PlankManager()
    {
        GameLogger.SystemStart("PlankManager", "패들 매니저 생성됨");
    }
    
    /// <summary>
    /// 초기화 (의존성 주입)
    /// </summary>
    public void Initialize(PhysicsPlank plank, Camera mainCamera)
    {
        _plank = plank;
        _mainCamera = mainCamera;
        
        if (_plank == null)
        {
            GameLogger.Error("PlankManager", "PhysicsPlank가 null입니다!");
            return;
        }
        
        if (_mainCamera == null)
        {
            GameLogger.Error("PlankManager", "Camera가 null입니다!");
            return;
        }
        
        // ✅ 전역 InputManager의 Input_ArrowKey 이벤트 구독
        _arrowKeySubscription = Managers.ActionBus.Subscribe(
            MB.Infrastructure.Messages.ActionId.Input_ArrowKey, 
            OnArrowKeyInput
        );
        GameLogger.Success("PlankManager", "Input_ArrowKey 이벤트 구독 완료!");
        
        // PhysicsPlank의 Update는 그대로 두고 PlankManager가 추가 제어
        // (_plank.enabled = false 하면 Collider도 비활성화됨!)
        
        // Raycast용 평면 생성
        _plankPlane = new Plane(Vector3.forward, _plank.transform.position);
        
        GameLogger.Success("PlankManager", "초기화 완료 (의존성 주입됨)");
    }
    
    /// <summary>
    /// 방향키 입력 이벤트 핸들러 (ActionBus에서 호출)
    /// </summary>
    private void OnArrowKeyInput(MB.Infrastructure.Messages.ActionMessage message)
    {
        if (message.TryGetPayload<MB.Infrastructure.Messages.ArrowKeyPayload>(out var payload))
        {
            _currentHorizontalInput = payload.Horizontal;
            GameLogger.DevLog("PlankManager", $"방향키 입력 수신: {_currentHorizontalInput}");
        }
    }
    
    /// <summary>
    /// 소멸자 (ActionBus 구독 해제)
    /// </summary>
    ~PlankManager()
    {
        _arrowKeySubscription?.Dispose();
    }
    
    /// <summary>
    /// 매 프레임 패들 이동 처리
    /// ✅ 싱글/멀티플레이어 통합 - PlankManager가 모든 입력 처리 담당
    /// </summary>
    public void UpdateMovement(float deltaTime)
    {
        // ✅ 디버깅: 매 60프레임마다 상태 출력
        if (Time.frameCount % 60 == 0)
        {
            GameLogger.Info("PlankManager", $"UpdateMovement 호출! (enabled: {_enabled}, plank: {(_plank != null ? "OK" : "NULL")}, horizontal: {_currentHorizontalInput})");
        }
        
        // ✅ 디버깅: 조건 체크
        if (!_enabled)
        {
            if (Time.frameCount % 60 == 0)
                GameLogger.Warning("PlankManager", "UpdateMovement: _enabled = false");
            return;
        }
        if (_plank == null)
        {
            if (Time.frameCount % 60 == 0)
                GameLogger.Warning("PlankManager", "UpdateMovement: _plank = null");
            return;
        }

        // ✅ 멀티플레이어 Owner가 아니면 입력 처리 안 함
        if (_plank.IsNetworkMode() && !_plank.IsOwner)
        {
            GameLogger.DevLog("PlankManager", "다른 플레이어의 패들 (IsOwner=false) - 입력 처리 건너뜀");
            return;
        }

        // ✅ 싱글플레이어 또는 멀티플레이어 Owner: 방향키 입력 처리
        if (Mathf.Abs(_currentHorizontalInput) > 0.01f)
        {
            GameLogger.Info("PlankManager", $"방향키 입력 처리: {_currentHorizontalInput}");
            ProcessKeyboardMovement(deltaTime);
        }
        
        // ✅ 마우스 입력은 PhysicsPlank에서 직접 처리 (기존 방식 유지)
        // BrickGame은 마우스로 패들 추적하는 로직이 PhysicsPlank.Update()에 있음
    }
    
    #region 이동 처리
    /// <summary>
    /// 키보드 입력 처리 - PhysicsPlank.MoveByKeyboard() 위임
    /// </summary>
    private void ProcessKeyboardMovement(float deltaTime)
    {
        float horizontal = _currentHorizontalInput; // ✅ 전역 InputManager에서 받은 입력

        if (Mathf.Abs(horizontal) < 0.01f)
        {
            GameLogger.Warning("PlankManager", $"ProcessKeyboardMovement: horizontal 입력이 0입니다 ({horizontal})");
            return;
        }

        Vector3 beforePosition = _plank.transform.position;

        // ✅ PhysicsPlank에 이동 위임
        _plank.MoveByKeyboard(horizontal, deltaTime);

        Vector3 afterPosition = _plank.transform.position;

        // BB 브랜치의 Info 로그 유지
        GameLogger.Info("PlankManager", $"🎮 패들 이동 (키보드): {beforePosition.x:F2} → {afterPosition.x:F2}");
        OnPlankMoved?.Invoke(afterPosition);
    }

    /// <summary>
    /// 마우스/터치 입력 처리 - PhysicsPlank.Update()에서 자체 처리
    /// ✅ 더 이상 사용하지 않음: PhysicsPlank가 마우스 입력을 직접 처리
    /// </summary>
    // private void ProcessPointerMovement()
    // {
    //     // BrickGame은 마우스를 패들 추적용으로 사용
    //     // PhysicsPlank.Update()에서 Input.mousePosition 직접 처리
    // }
    #endregion
    
    #region 제어 메서드
    /// <summary>
    /// 패들을 중앙으로 리셋
    /// </summary>
    public void ResetPosition()
    {
        if (_plank == null) return;
        
        if (_plank.leftEnd != null && _plank.rightEnd != null)
        {
            float centerX = (_plank.leftEnd.position.x + _plank.rightEnd.position.x) / 2f;
            Vector3 centerPosition = new Vector3(centerX, _plank.transform.position.y, _plank.transform.position.z);
            
            Rigidbody2D rb = _plank.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = new Vector2(centerPosition.x, centerPosition.y);
            }
            else
            {
                _plank.transform.position = centerPosition;
            }
            
            GameLogger.Info("PlankManager", $"패들 위치 리셋: {centerPosition}");
        }
    }
    
    /// <summary>
    /// 마우스 추적 속도 변경
    /// </summary>
    public void SetMouseSpeed(float speed)
    {
        if (_plank != null)
        {
            _plank.smoothSpeed = speed;
        }
    }
    
    /// <summary>
    /// 패들 위치 가져오기
    /// </summary>
    public Vector3 GetPosition()
    {
        return _plank != null ? _plank.transform.position : Vector3.zero;
    }
    #endregion
}

