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
    private InputManager _inputManager;
    private Camera _mainCamera;
    private Plane _plankPlane; // Raycast용 평면
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
    public void Initialize(PhysicsPlank plank, InputManager inputManager, Camera mainCamera)
    {
        _plank = plank;
        _inputManager = inputManager;
        _mainCamera = mainCamera;
        
        if (_plank == null)
        {
            GameLogger.Error("PlankManager", "PhysicsPlank가 null입니다!");
            return;
        }
        
        if (_inputManager == null)
        {
            GameLogger.Error("PlankManager", "InputManager가 null입니다!");
            return;
        }
        
        if (_mainCamera == null)
        {
            GameLogger.Error("PlankManager", "Camera가 null입니다!");
            return;
        }
        
        // PhysicsPlank의 Update는 그대로 두고 PlankManager가 추가 제어
        // (_plank.enabled = false 하면 Collider도 비활성화됨!)
        
        // Raycast용 평면 생성
        _plankPlane = new Plane(Vector3.forward, _plank.transform.position);
        
        GameLogger.Success("PlankManager", "초기화 완료 (의존성 주입됨)");
    }
    
    /// <summary>
    /// 매 프레임 패들 이동 처리
    /// 싱글플레이어 전용 - 멀티플레이어일 때는 PhysicsPlank.Update()에서 자체 처리
    /// </summary>
    public void UpdateMovement(float deltaTime)
    {
        if (!_enabled)
        {
            GameLogger.DevLog("PlankManager", "PlankManager가 비활성화됨 (_enabled == false)");
            return;
        }

        if (_plank == null)
        {
            GameLogger.DevLog("PlankManager", "PhysicsPlank가 null");
            return;
        }

        if (_inputManager == null)
        {
            GameLogger.DevLog("PlankManager", "InputManager가 null");
            return;
        }

        // ✅ 멀티플레이어 모드 체크: NetworkObject로 스폰되었으면 PhysicsPlank.Update()에서 처리
        var baseObject = _plank as Unity.Assets.Scripts.Objects.BaseObject;
        if (baseObject != null && baseObject.IsSpawned)
        {
            // NetworkObject로 스폰된 경우, PhysicsPlank.Update()에서 입력 처리하므로 여기서는 return
            GameLogger.DevLog("PlankManager", $"멀티플레이어 모드 (IsSpawned=true, IsOwner={baseObject.IsOwner}) - PhysicsPlank.Update()에서 처리");
            return;
        }

        // ✅ 싱글플레이어 모드: PlankManager가 입력 처리
        // 현재 입력 타입에 따라 이동 처리
        switch (_inputManager.CurrentInputType)
        {
            case InputManager.InputType.Keyboard:
                ProcessKeyboardMovement(deltaTime);
                break;

            case InputManager.InputType.Mouse:
            case InputManager.InputType.Touch:
                ProcessPointerMovement();
                break;
        }
    }
    
    #region 이동 처리
    private void ProcessKeyboardMovement(float deltaTime)
    {
        float horizontal = _inputManager.HorizontalInput;
        
        if (Mathf.Abs(horizontal) < 0.01f) return;
        
        Vector3 currentPosition = _plank.transform.position;
        float targetX = currentPosition.x + (horizontal * _keyboardMoveSpeed * deltaTime);
        
        // 경계 제한
        if (_plank.leftEnd != null && _plank.rightEnd != null)
        {
            targetX = Mathf.Clamp(targetX, _plank.leftEnd.position.x, _plank.rightEnd.position.x);
        }
        
        Vector3 newPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);
        
        // Rigidbody2D로 이동
        Rigidbody2D rb = _plank.GetComponent<Rigidbody2D>();
        if (rb != null && rb.isKinematic)
        {
            rb.MovePosition(newPosition);
            GameLogger.DevLog("PlankManager", $"🎮 패들 이동 (Kinematic): {currentPosition.x:F2} → {newPosition.x:F2}");
        }
        else
        {
            _plank.transform.position = newPosition;
            GameLogger.DevLog("PlankManager", $"🎮 패들 이동 (Transform): {currentPosition.x:F2} → {newPosition.x:F2}");
        }
        
        OnPlankMoved?.Invoke(newPosition);
    }
    
    private void ProcessPointerMovement()
    {
        if (!_inputManager.IsPointerActive) return;
        
        Vector3 pointerPosition = _inputManager.PointerPosition;
        
        // 1. 마우스 위치로 Ray 생성
        Ray ray = _mainCamera.ScreenPointToRay(pointerPosition);
        
        // 2. Ray와 플랭크 평면의 교차점 계산
        float enterDistance;
        if (_plankPlane.Raycast(ray, out enterDistance))
        {
            // 교차점 월드 좌표 얻기
            Vector3 worldPosition = ray.GetPoint(enterDistance);
            float targetX = worldPosition.x;
            
            // 3. 경계 제한
            if (_plank.leftEnd != null && _plank.rightEnd != null)
            {
                targetX = Mathf.Clamp(targetX, _plank.leftEnd.position.x, _plank.rightEnd.position.x);
            }
            
            // 4. 부드럽게 이동
            Vector3 currentPosition = _plank.transform.position;
            Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);
            
            float smoothSpeed = _plank.smoothSpeed;
            Vector3 smoothedPosition = Vector3.MoveTowards(currentPosition, targetPosition, smoothSpeed * Time.deltaTime);
            
            // Rigidbody2D로 이동
            Rigidbody2D rb = _plank.GetComponent<Rigidbody2D>();
            if (rb != null && rb.isKinematic)
            {
                rb.MovePosition(smoothedPosition);
            }
            else
            {
                _plank.transform.position = smoothedPosition;
            }
            
            OnPlankMoved?.Invoke(smoothedPosition);
        }
    }
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

