using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// BrickGame 초기화 전담 클래스
/// - 씬 오브젝트 수집
/// - 의존성 검증
/// - Manager 초기화
/// </summary>
public class BrickGameInitializer
{
    #region Scene Objects Container
    private class SceneObjects
    {
        public ObjectPlacement ObjectPlacement;
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

        var sceneObjects = CollectSceneObjects();

        if (!ValidateRequirements(sceneObjects))
            return false;

        InjectDependencies(sceneObjects);
        SetupGameObjects(sceneObjects);

        GameLogger.Success("BrickGameInitializer", "BrickGame 초기화 완료!");
        return true;
    }
    #endregion

    #region Collection
    private SceneObjects CollectSceneObjects()
    {
        GameLogger.Progress("BrickGameInitializer", "씬 오브젝트 수집 중...");

        var objects = new SceneObjects
        {
            ObjectPlacement = Object.FindFirstObjectByType<ObjectPlacement>(),
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

    #region Validation
    private bool ValidateRequirements(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "필수 컴포넌트 검증 중...");

        bool isValid = true;

        // 선택: ObjectPlacement (멀티플레이어에서는 불필요)
        if (objects.ObjectPlacement == null)
        {
            GameLogger.Warning("BrickGameInitializer", "ObjectPlacement 없음 - 벽돌 자동 생성 불가 (멀티플레이어 OK)");
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

        // 선택: PhysicsBall (동적 생성 가능)
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            GameLogger.Warning("BrickGameInitializer", "PhysicsBall 없음 - 동적 생성 필요");
        }

        if (isValid)
        {
            GameLogger.Success("BrickGameInitializer", "모든 필수 컴포넌트 검증 완료");
        }

        return isValid;
    }
    #endregion

    #region Dependency Injection
    private void InjectDependencies(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "의존성 주입 중...");

        // ✅ Adapter 제거: ObjectPlacement 직접 주입
        Managers.Game.InitializeBrickGame(
            objects.ObjectPlacement,
            objects.Plank,
            objects.MainCamera,
            null  // 기본 설정 사용
        );

        GameLogger.Success("BrickGameInitializer", "의존성 주입 완료");
    }
    #endregion

    #region Setup
    private void SetupGameObjects(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "GameObject 설정 중...");

        SetupBalls(objects);
        SetupBricks(objects);

        GameLogger.Success("BrickGameInitializer", "GameObject 설정 완료");
    }

    private void SetupBalls(SceneObjects objects)
    {
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            GameLogger.Info("BrickGameInitializer", "공 없음 - 설정 생략");
            return;
        }

        var ballManager = Managers.Game?.BrickGame?.Ball;
        if (ballManager == null)
        {
            GameLogger.Error("BrickGameInitializer", "BallManager가 null!");
            return;
        }

        foreach (var ball in objects.Balls)
        {
            if (ball == null) continue;

            // 리플렉션으로 Plank 할당 (Inspector 설정 불필요)
            var plankField = typeof(PhysicsBall).GetField("plank",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (plankField != null && objects.Plank != null)
            {
                plankField.SetValue(ball, objects.Plank);
                GameLogger.Success("BrickGameInitializer", $"{ball.name}에 패들 자동 할당!");
            }
            else
            {
                GameLogger.Warning("BrickGameInitializer", $"{ball.name}에 패들 할당 실패!");
            }
        }

        GameLogger.Info("BrickGameInitializer", $"{objects.Balls.Length}개의 공 설정 완료");
    }

    private void SetupBricks(SceneObjects objects)
    {
        if (objects.Bricks == null || objects.Bricks.Length == 0)
        {
            GameLogger.Info("BrickGameInitializer", "벽돌 없음 - 게임 시작 후 생성 예정");
            return;
        }

        var brickManager = Managers.Game?.BrickGame?.Brick;
        if (brickManager == null)
        {
            GameLogger.Error("BrickGameInitializer", "BrickManager가 null!");
            return;
        }

        // 벽돌은 Start()에서 자동 등록
        GameLogger.Info("BrickGameInitializer", $"{objects.Bricks.Length}개의 벽돌 발견 (자동 등록 예정)");
    }
    #endregion
}
