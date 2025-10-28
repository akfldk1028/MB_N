using UnityEngine;

/// <summary>
/// Managers 상태 진단 스크립트
/// 아무 GameObject에 추가하여 @Managers의 활성 상태를 확인
/// </summary>
public class ManagersDiagnostic : MonoBehaviour
{
    private int _frameCount = 0;

    void Start()
    {
        GameLogger.SystemStart("ManagersDiagnostic", "Managers 진단 시작");
        DiagnoseManagers();
    }

    void Update()
    {
        _frameCount++;

        // 처음 10프레임만 진단
        if (_frameCount <= 10)
        {
            GameLogger.Info("ManagersDiagnostic", $"✅ ManagersDiagnostic.Update() 호출됨! (프레임: {_frameCount})");

            if (_frameCount == 1)
            {
                DiagnoseManagers();
            }
        }
    }

    private void DiagnoseManagers()
    {
        GameLogger.Progress("ManagersDiagnostic", "=== @Managers 상태 진단 ===");

        // 1. @Managers GameObject 찾기
        GameObject managersGo = GameObject.Find("@Managers");
        if (managersGo == null)
        {
            GameLogger.Error("ManagersDiagnostic", "❌ @Managers GameObject를 찾을 수 없습니다!");
            GameLogger.Error("ManagersDiagnostic", "→ Managers.Init()이 호출되지 않았습니다.");
            return;
        }

        GameLogger.Success("ManagersDiagnostic", "✅ @Managers GameObject 발견");

        // 2. GameObject 활성화 상태 확인
        if (!managersGo.activeInHierarchy)
        {
            GameLogger.Error("ManagersDiagnostic", "❌ @Managers GameObject가 비활성화되어 있습니다!");
            GameLogger.Error("ManagersDiagnostic", $"→ activeSelf: {managersGo.activeSelf}");
            GameLogger.Error("ManagersDiagnostic", $"→ activeInHierarchy: {managersGo.activeInHierarchy}");
            GameLogger.Error("ManagersDiagnostic", "→ Unity Inspector에서 @Managers를 활성화하세요!");
            return;
        }

        GameLogger.Success("ManagersDiagnostic", "✅ @Managers GameObject가 활성화되어 있습니다");

        // 3. Managers 컴포넌트 확인
        Managers managersComponent = managersGo.GetComponent<Managers>();
        if (managersComponent == null)
        {
            GameLogger.Error("ManagersDiagnostic", "❌ Managers 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        GameLogger.Success("ManagersDiagnostic", "✅ Managers 컴포넌트 발견");

        // 4. Managers 컴포넌트 활성화 상태 확인
        if (!managersComponent.enabled)
        {
            GameLogger.Error("ManagersDiagnostic", "❌ Managers 컴포넌트가 비활성화되어 있습니다!");
            GameLogger.Error("ManagersDiagnostic", "→ Unity Inspector에서 Managers 컴포넌트를 활성화하세요!");
            return;
        }

        GameLogger.Success("ManagersDiagnostic", "✅ Managers 컴포넌트가 활성화되어 있습니다");

        // 5. Managers 초기화 상태 확인
        if (!Managers.Initialized)
        {
            GameLogger.Warning("ManagersDiagnostic", "⚠️ Managers.Initialized가 false입니다!");
        }
        else
        {
            GameLogger.Success("ManagersDiagnostic", "✅ Managers.Initialized = true");
        }

        // 6. BrickGame 상태 확인
        if (Managers.Game?.BrickGame == null)
        {
            GameLogger.Warning("ManagersDiagnostic", "⚠️ BrickGame이 null입니다!");
        }
        else
        {
            GameLogger.Success("ManagersDiagnostic", "✅ BrickGame 존재함");
        }

        // 7. ActionBus 상태 확인
        if (Managers.ActionBus == null)
        {
            GameLogger.Error("ManagersDiagnostic", "❌ ActionBus가 null입니다!");
        }
        else
        {
            GameLogger.Success("ManagersDiagnostic", "✅ ActionBus 존재함");
        }

        GameLogger.Success("ManagersDiagnostic", "=== 진단 완료 ===");
    }
}
