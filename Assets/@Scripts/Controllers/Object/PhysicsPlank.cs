using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PhysicsPlank : PhysicsObject
{
    // ✅ 네트워크 위치 동기화 (Inspector 없이 코드로만 처리)
    private NetworkVariable<Vector3> _syncedPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner // Owner가 위치 업데이트
    );
    // public bool movable; // 이 변수는 MainBall.cs에서 공의 상태에 따라 제어할 수 있습니다. (선택적)
                           // 여기서는 항상 움직일 수 있다고 가정하고 진행합니다.
                           // 만약 공이 발사되기 전에는 움직이지 않게 하려면 MainBall.cs에서 이 값을 조절해야 합니다.

    public Transform leftEnd = null;  // 왼쪽 이동 한계점 Transform
    public Transform rightEnd = null; // 오른쪽 이동 한계점 Transform

    [Tooltip("플랭크가 마우스를 따라가는 속도. 값이 클수록 빠르게 반응합니다.")]
    [Range(1f, 20f)] // Inspector에서 슬라이더로 조절 가능
    public float smoothSpeed = 20f; // 따라가는 속도 조절 변수

    public Camera mainCamera = null; // 카메라 (자동 설정됨)
    private Plane plankPlane; // Raycast를 위한 평면

    // ✅ 멀티플레이어 자체 입력 처리를 위한 필드
    private InputManager _inputManager;
    private Rigidbody2D _rb;
    private float _keyboardMoveSpeed = 15f;

    void Start()
    {
        // ✅ 자동 초기화: Inspector 없이 코드로 모든 참조 설정
        AutoInitializeReferences();

        // 필수 참조 체크
        if (mainCamera == null)
        {
            GameLogger.Error("PhysicsPlank", "Camera를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        if (leftEnd == null || rightEnd == null)
        {
            GameLogger.Error("PhysicsPlank", "leftEnd 또는 rightEnd를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            GameLogger.Warning("PhysicsPlank", $"leftEnd({leftEnd.position.x})가 rightEnd({rightEnd.position.x})보다 큽니다!");
        }

        plankPlane = new Plane(Vector3.forward, transform.position);
        _rb = GetComponent<Rigidbody2D>();

        GameLogger.Success("PhysicsPlank", $"{gameObject.name} 초기화 완료");
    }

    /// <summary>
    /// Inspector 없이 모든 참조 자동 설정
    /// </summary>
    private void AutoInitializeReferences()
    {
        // 1. leftEnd/rightEnd 자동 탐색
        if (leftEnd == null)
        {
            GameObject leftObj = GameObject.Find("LeftEnd");
            if (leftObj != null)
            {
                leftEnd = leftObj.transform;
                GameLogger.Info("PhysicsPlank", "LeftEnd 자동 탐색 완료");
            }
        }

        if (rightEnd == null)
        {
            GameObject rightObj = GameObject.Find("RightEnd");
            if (rightObj != null)
            {
                rightEnd = rightObj.transform;
                GameLogger.Info("PhysicsPlank", "RightEnd 자동 탐색 완료");
            }
        }

        // 2. Camera 자동 설정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                GameLogger.Info("PhysicsPlank", "Camera.main 자동 설정 완료");
            }
        }

        // 3. InputManager 자동 참조 (Managers 싱글톤)
        if (_inputManager == null)
        {
            _inputManager = Managers.Game?.BrickGame?.Input;
            if (_inputManager != null)
            {
                GameLogger.Info("PhysicsPlank", "InputManager 자동 참조 완료");
            }
            else
            {
                GameLogger.Warning("PhysicsPlank", "InputManager를 찾을 수 없습니다 (멀티플레이어 입력 불가)");
            }
        }
    }

    // ✅ 네트워크 생명주기
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // Owner: 초기 위치 동기화
            _syncedPosition.Value = transform.position;
        }
        else
        {
            // 다른 플레이어: 서버 위치로 즉시 이동
            transform.position = _syncedPosition.Value;
            _syncedPosition.OnValueChanged += OnPositionChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsOwner)
        {
            _syncedPosition.OnValueChanged -= OnPositionChanged;
        }
    }

    private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        // 다른 플레이어의 패들 위치 업데이트는 Update()에서 Lerp로 처리
    }

    private void Update()
    {
        // ✅ 멀티플레이어 모드: IsSpawned일 때만 네트워크 로직 실행
        if (IsSpawned)
        {
            if (IsOwner)
            {
                // Owner: 입력 처리 + 위치 동기화
                ProcessMultiplayerInput();
                SyncPositionToServer();
            }
            else
            {
                // 다른 플레이어: 서버 위치로 부드럽게 보간
                InterpolateToServerPosition();
            }
        }
        // ✅ 싱글플레이어 모드: PlankManager가 UpdateMovement()로 입력 처리
        // (IsSpawned == false일 때는 PlankManager.UpdateMovement()가 처리)
    }

    private void SyncPositionToServer()
    {
        // 위치가 크게 변했을 때만 업데이트 (최적화)
        float positionDiff = Vector3.Distance(transform.position, _syncedPosition.Value);
        if (positionDiff > 0.01f)
        {
            _syncedPosition.Value = transform.position;
        }
    }

    private void InterpolateToServerPosition()
    {
        // 부드럽게 보간
        float interpolationSpeed = 15f;
        transform.position = Vector3.Lerp(
            transform.position,
            _syncedPosition.Value,
            Time.deltaTime * interpolationSpeed
        );
    }

    /// <summary>
    /// 플랭크와 공의 충돌 시 튕겨나갈 속도를 계산하여 반환합니다.
    /// </summary>
    /// <param name="ballRb">충돌한 공의 Rigidbody2D</param>
    /// <param name="collision">충돌 정보</param>
    /// <returns>계산된 반사 속도 벡터</returns>
    public Vector2 CalculateBounceVelocity(Rigidbody2D ballRb, Collision2D collision)
    {
            if (ballRb == null) return Vector2.zero; // 공 Rigidbody 없으면 처리 불가

            Vector2 hitPoint = collision.contacts[0].point;
            Transform plankTransform = collision.transform; // 플랭크 자신의 Transform
            Collider2D plankCollider = collision.collider; // 플랭크 자신의 Collider

            float xOffset = hitPoint.x - plankTransform.position.x;
            float normalizedOffset = xOffset / (plankCollider.bounds.size.x / 2f);
            normalizedOffset = Mathf.Clamp(normalizedOffset, -1f, 1f);

            float bounceAngle = normalizedOffset * 75f; // 최대 반사각 (75도)
            float bounceAngleRad = bounceAngle * Mathf.Deg2Rad;
            Vector2 bounceDirection = new Vector2(Mathf.Sin(bounceAngleRad), Mathf.Cos(bounceAngleRad)).normalized;

            // 공의 현재 속력 사용
            float currentSpeed = ballRb.linearVelocity.magnitude;
            float targetSpeed = currentSpeed;

            if (targetSpeed < 5f) targetSpeed = 10f; // 최소 속도 보정

            Vector2 bounceVelocity = bounceDirection * targetSpeed;
            // Debug.Log($"[PhysicsPlank] Calculated Bounce: Offset={normalizedOffset:F2}, Angle={bounceAngle:F1}, Dir={bounceDirection}, Speed={targetSpeed:F2}");

            return bounceVelocity;
    }

    #region ✅ 멀티플레이어 입력 처리 (PlankManager 로직 복사)
    /// <summary>
    /// 멀티플레이어 모드에서 Owner가 직접 입력 처리
    /// PlankManager와 동일한 로직
    /// </summary>
    private void ProcessMultiplayerInput()
    {
        if (_inputManager == null) return;

        // 현재 입력 타입에 따라 이동 처리
        switch (_inputManager.CurrentInputType)
        {
            case InputManager.InputType.Keyboard:
                ProcessKeyboardInput();
                break;

            case InputManager.InputType.Mouse:
            case InputManager.InputType.Touch:
                ProcessPointerInput();
                break;
        }
    }

    private void ProcessKeyboardInput()
    {
        float horizontal = _inputManager.HorizontalInput;

        if (Mathf.Abs(horizontal) < 0.01f) return;

        Vector3 currentPosition = transform.position;
        float targetX = currentPosition.x + (horizontal * _keyboardMoveSpeed * Time.deltaTime);

        // 경계 제한
        if (leftEnd != null && rightEnd != null)
        {
            targetX = Mathf.Clamp(targetX, leftEnd.position.x, rightEnd.position.x);
        }

        Vector3 newPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

        // Rigidbody2D로 이동
        if (_rb != null && _rb.isKinematic)
        {
            _rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }

    private void ProcessPointerInput()
    {
        if (!_inputManager.IsPointerActive) return;

        Vector3 pointerPosition = _inputManager.PointerPosition;

        // 1. 마우스 위치로 Ray 생성
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        // 2. Ray와 플랭크 평면의 교차점 계산
        float enterDistance;
        if (plankPlane.Raycast(ray, out enterDistance))
        {
            // 교차점 월드 좌표 얻기
            Vector3 worldPosition = ray.GetPoint(enterDistance);
            float targetX = worldPosition.x;

            // 3. 경계 제한
            if (leftEnd != null && rightEnd != null)
            {
                targetX = Mathf.Clamp(targetX, leftEnd.position.x, rightEnd.position.x);
            }

            // 4. 부드럽게 이동
            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

            Vector3 smoothedPosition = Vector3.MoveTowards(currentPosition, targetPosition, smoothSpeed * Time.deltaTime);

            // Rigidbody2D로 이동
            if (_rb != null && _rb.isKinematic)
            {
                _rb.MovePosition(smoothedPosition);
            }
            else
            {
                transform.position = smoothedPosition;
            }
        }
    }
    #endregion

    // 기존 PlankBallCollision 메서드는 CalculateBounceVelocity로 대체되었으므로 제거 또는 주석 처리
    // public void PlankBallCollision(Collision2D collision)
    // {
    //     // ... 기존 코드 ...
    // }
}
