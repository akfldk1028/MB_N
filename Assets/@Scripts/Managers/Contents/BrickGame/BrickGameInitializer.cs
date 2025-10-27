using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// BrickGame 초기화 전담 클래스
/// - 씬 오브젝트 수집
/// - 의존성 검증
/// - Manager 초기화
/// - GameObject 설정
/// </summary>
public class BrickGameInitializer
{
    #region Scene Objects Container
    private class SceneObjects
    {
        public ObjectPlacementAdapter ObjectPlacementAdapter;
        public ScoreDisplayAdapter ScoreDisplayAdapter;
        public PhysicsPlank Plank;
        public Camera MainCamera;
        public PhysicsBall[] Balls;
        public Brick[] Bricks;
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// BrickGame 초기화 메인 진입점
    /// </summary>
    public bool Initialize()
    {
        GameLogger.SystemStart("BrickGameInitializer", "BrickGame 초기화 시작");
        
        // 1. 씬 오브젝트 수집
        var sceneObjects = CollectSceneObjects();
        
        // 2. 필수 컴포넌트 검증
        if (!ValidateRequirements(sceneObjects))
        {
            return false;
        }
        
        // 3. Manager에 의존성 주입
        InjectDependencies(sceneObjects);
        
        // 4. GameObject 설정 (공, 벽돌 등)
        SetupGameObjects(sceneObjects);
        
        GameLogger.Success("BrickGameInitializer", "BrickGame 초기화 완료!");
        return true;
    }
    #endregion
    
    #region Private Methods - Collection
    /// <summary>
    /// 씬에서 필요한 모든 오브젝트 수집
    /// </summary>
    private SceneObjects CollectSceneObjects()
    {
        GameLogger.Progress("BrickGameInitializer", "씬 오브젝트 수집 중...");
        
        var objects = new SceneObjects
        {
            ObjectPlacementAdapter = Object.FindFirstObjectByType<ObjectPlacementAdapter>(),
            ScoreDisplayAdapter = Object.FindFirstObjectByType<ScoreDisplayAdapter>(),
            Plank = Object.FindFirstObjectByType<PhysicsPlank>(),
            MainCamera = Camera.main,
            Balls = Object.FindObjectsByType<PhysicsBall>(FindObjectsSortMode.None),
            Bricks = Object.FindObjectsByType<Brick>(FindObjectsSortMode.None)
        };
        
        GameLogger.Info("BrickGameInitializer", 
            $"수집 완료: 공 {objects.Balls?.Length ?? 0}개, 벽돌 {objects.Bricks?.Length ?? 0}개");
        
        return objects;
    }
    #endregion
    
    #region Private Methods - Validation
    /// <summary>
    /// 필수 컴포넌트 검증
    /// </summary>
    private bool ValidateRequirements(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "필수 컴포넌트 검증 중...");
        
        bool isValid = true;
        
        // 선택: ObjectPlacementAdapter (1인 테스트에서는 불필요)
        if (objects.ObjectPlacementAdapter == null)
        {
            GameLogger.Warning("BrickGameInitializer", "ObjectPlacementAdapter를 찾을 수 없습니다. 벽돌 자동 생성 불가 (1인 테스트는 OK)");
        }
        
        // 선택: ScoreDisplayAdapter (경고만)
        if (objects.ScoreDisplayAdapter == null)
        {
            GameLogger.Warning("BrickGameInitializer", "ScoreDisplayAdapter를 찾을 수 없습니다. 점수 표시 불가");
        }
        
        // 필수: PhysicsPlank
        if (objects.Plank == null)
        {
            GameLogger.Error("BrickGameInitializer", "PhysicsPlank를 찾을 수 없습니다!");
            isValid = false;
        }
        
        // 필수: Camera
        if (objects.MainCamera == null)
        {
            GameLogger.Error("BrickGameInitializer", "Camera를 찾을 수 없습니다!");
            isValid = false;
        }
        
        // 선택: PhysicsBall (게임 시작 후 생성 가능)
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            GameLogger.Warning("BrickGameInitializer", "PhysicsBall이 씬에 없습니다. 동적 생성 필요");
        }
        
        if (isValid)
        {
            GameLogger.Success("BrickGameInitializer", "모든 필수 컴포넌트 검증 완료");
        }
        
        return isValid;
    }
    #endregion
    
    #region Private Methods - Dependency Injection
    /// <summary>
    /// Manager에 의존성 주입
    /// </summary>
    private void InjectDependencies(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "의존성 주입 중...");
        
        // GameManager를 통해 BrickGame 초기화
        Managers.Game.InitializeBrickGame(
            objects.ObjectPlacementAdapter,
            objects.ScoreDisplayAdapter,
            objects.Plank,
            objects.MainCamera,
            null  // 기본 설정 사용
        );
        
        GameLogger.Success("BrickGameInitializer", "의존성 주입 완료");
    }
    #endregion
    
    #region Private Methods - Setup
    /// <summary>
    /// GameObject 설정 (공, 벽돌 등)
    /// </summary>
    private void SetupGameObjects(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "GameObject 설정 중...");
        
        // 1. 공 설정
        SetupBalls(objects);
        
        // 2. 벽돌 설정
        SetupBricks(objects);
        
        GameLogger.Success("BrickGameInitializer", "GameObject 설정 완료");
    }
    
    /// <summary>
    /// 공(Ball) 초기화 및 Manager 연결
    /// </summary>
    private void SetupBalls(SceneObjects objects)
    {
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            GameLogger.Info("BrickGameInitializer", "공이 없어 설정 생략");
            return;
        }
        
        var ballManager = Managers.Game?.BrickGame?.Ball;
        if (ballManager == null)
        {
            GameLogger.Error("BrickGameInitializer", "BallManager가 null입니다!");
            return;
        }
        
        foreach (var ball in objects.Balls)
        {
            if (ball == null) continue;
            
            // 🔧 자동으로 패들 할당 (Scene에서 수동 할당 불필요!)
            var plankField = typeof(PhysicsBall).GetField("plank", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (plankField != null && objects.Plank != null)
            {
                plankField.SetValue(ball, objects.Plank);
                GameLogger.Success("BrickGameInitializer", $"{ball.name}에 패들 자동 할당 완료!");
            }
            else
            {
                GameLogger.Warning("BrickGameInitializer", $"{ball.name}에 패들 할당 실패!");
            }
        }
        
        GameLogger.Info("BrickGameInitializer", $"{objects.Balls.Length}개의 공 설정 완료");
    }
    
    /// <summary>
    /// 벽돌(Brick) 초기화 및 Manager 연결
    /// </summary>
    private void SetupBricks(SceneObjects objects)
    {
        if (objects.Bricks == null || objects.Bricks.Length == 0)
        {
            GameLogger.Info("BrickGameInitializer", "벽돌이 없어 설정 생략 (게임 시작 후 생성 예정)");
            return;
        }
        
        var brickManager = Managers.Game?.BrickGame?.Brick;
        if (brickManager == null)
        {
            GameLogger.Error("BrickGameInitializer", "BrickManager가 null입니다!");
            return;
        }
        
        // 벽돌은 Start()에서 자동 등록되므로 여기서는 검증만
        GameLogger.Info("BrickGameInitializer", $"{objects.Bricks.Length}개의 벽돌 발견 (자동 등록 예정)");
    }
    #endregion
}

