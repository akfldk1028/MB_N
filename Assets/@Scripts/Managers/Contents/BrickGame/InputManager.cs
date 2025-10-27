using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임의 입력 관리 매니저
/// - 방향키, 마우스, 터치 입력 통합 처리
/// - 플랫폼 독립적 입력 추상화
/// </summary>
public class InputManager
{
    #region 입력 타입
    public enum InputType
    {
        Keyboard,   // 방향키 (←/→)
        Mouse,      // 마우스 위치
        Touch       // 터치 입력
    }
    
    private InputType _currentInputType = InputType.Keyboard;
    public InputType CurrentInputType => _currentInputType;
    #endregion
    
    #region 입력 상태
    private float _horizontalInput = 0f;  // -1 (왼쪽) ~ 1 (오른쪽)
    private Vector3 _pointerPosition = Vector3.zero;
    private bool _isPointerActive = false;
    
    /// <summary>
    /// 방향키 입력값 (-1: 왼쪽, 0: 정지, 1: 오른쪽)
    /// </summary>
    public float HorizontalInput => _horizontalInput;
    
    /// <summary>
    /// 마우스/터치 위치 (스크린 좌표)
    /// </summary>
    public Vector3 PointerPosition => _pointerPosition;
    
    /// <summary>
    /// 마우스/터치가 활성화되어 있는지
    /// </summary>
    public bool IsPointerActive => _isPointerActive;
    #endregion
    
    #region 이벤트
    public event Action<float> OnHorizontalInput;      // 방향키 입력 시
    public event Action<Vector3> OnPointerInput;       // 마우스/터치 입력 시
    public event Action OnPointerDown;                 // 클릭/터치 시작
    public event Action OnPointerUp;                   // 클릭/터치 종료
    #endregion
    
    #region 설정
    private bool _enabled = true;
    
    /// <summary>
    /// 입력 활성화 여부 (게임 일시정지 시 비활성화)
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!_enabled)
            {
                ResetInput();
            }
        }
    }
    #endregion
    
    public InputManager()
    {
        GameLogger.SystemStart("InputManager", "입력 매니저 생성됨");
    }
    
    public void Initialize()
    {
        _enabled = true;
        ResetInput();
        GameLogger.Success("InputManager", "초기화 완료");
    }
    
    /// <summary>
    /// 매 프레임 입력 처리 (BrickGameManager.OnUpdate에서 호출)
    /// </summary>
    public void UpdateInput()
    {
        if (!_enabled) return;
        
        // 1. 방향키 입력 (키보드)
        ProcessKeyboardInput();
        
        // 2. 마우스 입력
        ProcessMouseInput();
        
        // 3. 터치 입력 (모바일)
        ProcessTouchInput();
    }
    
    #region 입력 처리
    private void ProcessKeyboardInput()
    {
        float horizontal = 0f;
        
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
            GameLogger.DevLog("InputManager", $"⌨️ 방향키 입력: {(horizontal > 0 ? "→" : "←")} ({horizontal:F1})");
        }
        else
        {
            _horizontalInput = 0f;
        }
    }
    
    private void ProcessMouseInput()
    {
        if (Input.GetMouseButton(0)) // 왼쪽 버튼 클릭 중
        {
            _currentInputType = InputType.Mouse;
            _pointerPosition = Input.mousePosition;
            _isPointerActive = true;
            OnPointerInput?.Invoke(_pointerPosition);
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown?.Invoke();
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            _isPointerActive = false;
            OnPointerUp?.Invoke();
        }
    }
    
    private void ProcessTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            _currentInputType = InputType.Touch;
            _pointerPosition = touch.position;
            _isPointerActive = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            
            OnPointerInput?.Invoke(_pointerPosition);
            
            if (touch.phase == TouchPhase.Began)
            {
                OnPointerDown?.Invoke();
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isPointerActive = false;
                OnPointerUp?.Invoke();
            }
        }
    }
    #endregion
    
    #region 유틸리티
    private void ResetInput()
    {
        _horizontalInput = 0f;
        _pointerPosition = Vector3.zero;
        _isPointerActive = false;
    }
    
    /// <summary>
    /// 특정 입력 타입 강제 설정 (테스트용)
    /// </summary>
    public void SetInputType(InputType type)
    {
        _currentInputType = type;
        GameLogger.Info("InputManager", $"입력 타입 변경: {type}");
    }
    #endregion
}

