# Object Pool 시스템 통합 가이드

## 개요

벽돌깨기 게임에서 Ball과 Brick은 자주 생성/파괴되므로 Object Pool을 사용하면 **GC Allocation을 크게 줄일 수 있습니다**.

## 구축된 인프라

### 1. ObjectPool<T> 클래스
- 위치: `Assets/@Scripts/Managers/Core/ObjectPool.cs`
- 제네릭 오브젝트 풀 시스템
- Stack 기반의 효율적인 재사용

### 2. IPoolable 인터페이스
- 위치: `Assets/@Scripts/Managers/Contents/BrickGame/Interfaces/IPoolable.cs`
- 풀링 가능한 오브젝트가 구현해야 하는 인터페이스

## 통합 방법 (향후 작업)

### Phase 1: Brick 풀링

#### 1. Brick.cs 수정
```csharp
public class Brick : PhysicsObject, IPoolable
{
    private bool _isPooled = false;
    
    public void OnSpawnFromPool()
    {
        _isPooled = true;
        // 초기화 로직 (Start 내용 이동)
    }
    
    public void OnReturnToPool()
    {
        // 정리 로직
        wave = 1;
        isGameOverTriggered = false;
    }
    
    public void ReturnToPool()
    {
        if (_isPooled)
        {
            var brickManager = Managers.Game?.BrickGame?.Brick;
            brickManager?.ReturnBrick(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Destroy(gameObject) → ReturnToPool() 변경
}
```

#### 2. BrickManager에 Pool 추가
```csharp
public class BrickManager
{
    private ObjectPool<Brick> _brickPool;
    
    public void Initialize(Brick brickPrefab, Transform poolParent)
    {
        _brickPool = new ObjectPool<Brick>(
            brickPrefab, 
            poolParent, 
            initialSize: 50, 
            maxSize: 200
        );
    }
    
    public Brick SpawnBrick(Vector3 position)
    {
        var brick = _brickPool.Get(position);
        brick.OnSpawnFromPool();
        RegisterBrick(brick);
        return brick;
    }
    
    public void ReturnBrick(Brick brick)
    {
        brick.OnReturnToPool();
        UnregisterBrick(brick);
        _brickPool.Return(brick);
    }
}
```

### Phase 2: Ball 풀링

#### 1. PhysicsBall.cs 수정
```csharp
public class PhysicsBall : PhysicsObject, IPoolable
{
    private bool _isPooled = false;
    
    public void OnSpawnFromPool()
    {
        _isPooled = true;
        // 상태 초기화
        CurrentState = EBallState.Ready;
    }
    
    public void OnReturnToPool()
    {
        // 정리
        CurrentState = EBallState.None;
    }
    
    public void ReturnToPool()
    {
        if (_isPooled)
        {
            var ballManager = Managers.Game?.BrickGame?.Ball;
            ballManager?.ReturnBall(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

#### 2. BallManager에 Pool 추가
```csharp
public class BallManager
{
    private ObjectPool<PhysicsBall> _ballPool;
    
    public void Initialize(PhysicsBall ballPrefab, Transform poolParent)
    {
        _ballPool = new ObjectPool<PhysicsBall>(
            ballPrefab, 
            poolParent, 
            initialSize: 20, 
            maxSize: 100
        );
    }
    
    public PhysicsBall SpawnBall(Vector3 position)
    {
        var ball = _ballPool.Get(position);
        ball.OnSpawnFromPool();
        RegisterBall(ball);
        return ball;
    }
    
    public void ReturnBall(PhysicsBall ball)
    {
        ball.OnReturnToPool();
        UnregisterBall(ball);
        _ballPool.Return(ball);
    }
}
```

## 주의사항

### 1. NetworkObject와의 호환성
- Unity Netcode의 NetworkObject는 기본적으로 풀링을 지원하지 않습니다
- NetworkObject 풀링은 별도의 NetworkObjectPool 구현이 필요합니다
- **현재 프로젝트는 네트워크 게임이므로 NetworkObjectPool 고려 필요**

### 2. 상태 초기화
- OnReturnToPool()에서 모든 상태를 초기화해야 합니다
- 이벤트 구독 해제 필수
- Transform, Rigidbody 상태 리셋

### 3. 성능 측정
- 풀링 전후 프로파일러로 GC Allocation 비교
- 풀 크기 최적화 (너무 크면 메모리 낭비)

## 테스트 체크리스트

- [ ] Brick 생성/파괴가 풀로 동작하는지 확인
- [ ] Ball 생성/파괴가 풀로 동작하는지 확인
- [ ] 풀에서 가져온 오브젝트 상태가 올바르게 초기화되는지 확인
- [ ] 메모리 누수가 없는지 확인 (Profiler)
- [ ] 네트워크 동기화가 정상 작동하는지 확인

## 예상 성능 개선

- **GC Allocation**: 80~90% 감소 예상
- **프레임 드랍**: 벽돌 대량 파괴 시 개선
- **메모리 사용량**: 약간 증가 (풀 유지 비용)

## 참고 자료

- Unity 공식 문서: Object Pooling
- Unity Netcode for GameObjects: NetworkObjectPool

