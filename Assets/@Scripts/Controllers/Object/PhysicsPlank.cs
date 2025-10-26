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

    public Camera mainCamera = null; // Public으로 변경하고 Inspector에서 할당
    private Plane plankPlane; // Raycast를 위한 평면

    void Start()
    {
        // mainCamera = Camera.main; // 더 이상 Camera.main 사용 안 함 (삭제)
        if (mainCamera == null)
        {
            // 오류 메시지를 Inspector 할당 확인으로 수정
            Debug.LogError("Main Camera가 Inspector에서 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd == null || rightEnd == null)
        {
            Debug.LogError("Plank의 leftEnd 또는 rightEnd가 설정되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            Debug.LogWarning($"Plank 경고: leftEnd({leftEnd.position.x})의 x좌표가 rightEnd({rightEnd.position.x})보다 크거나 같습니다!", this);
        }

        plankPlane = new Plane(Vector3.forward, transform.position);
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
        if (!IsSpawned) return;

        if (IsOwner)
        {
            // Owner: 위치 변경 시 NetworkVariable 업데이트
            SyncPositionToServer();
        }
        else
        {
            // 다른 플레이어: 서버 위치로 부드럽게 보간
            InterpolateToServerPosition();
        }
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

    // PlankManager가 입력 처리하므로 Update() 비활성화
    // void Update()
    // {
    //     // PlankManager.UpdateMovement()가 모든 입력 처리를 담당합니다.
    //     // 이 Update()는 사용하지 않습니다.
    // }

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

    // 기존 PlankBallCollision 메서드는 CalculateBounceVelocity로 대체되었으므로 제거 또는 주석 처리
    // public void PlankBallCollision(Collision2D collision)
    // {
    //     // ... 기존 코드 ...
    // }
}
