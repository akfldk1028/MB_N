using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/// <summary>
/// 로비 정리 서비스
/// - 이전 세션 정리
/// - 로비 나가기
/// - 상태 초기화
/// </summary>
public class LobbyCleanupService
{
    #region Properties
    private string _currentLobbyId;
    private string _currentPlayerId;
    
    public bool IsInLobby => !string.IsNullOrEmpty(_currentLobbyId);
    #endregion
    
    #region Constructor
    public LobbyCleanupService()
    {
        GameLogger.SystemStart("LobbyCleanupService", "로비 정리 서비스 생성");
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// 현재 로비 정보 설정
    /// </summary>
    public void SetCurrentLobby(string lobbyId, string playerId)
    {
        _currentLobbyId = lobbyId;
        _currentPlayerId = playerId;
        GameLogger.Info("LobbyCleanupService", $"로비 정보 설정: {lobbyId}");
    }
    
    /// <summary>
    /// 현재 로비에서 나가기
    /// </summary>
    public async Task<bool> LeaveCurrentLobbyAsync()
    {
        if (!IsInLobby)
        {
            GameLogger.Info("LobbyCleanupService", "현재 참가 중인 로비 없음");
            return true;
        }
        
        try
        {
            GameLogger.Progress("LobbyCleanupService", $"로비 나가기 시도: {_currentLobbyId}");
            
            await LobbyService.Instance.RemovePlayerAsync(_currentLobbyId, _currentPlayerId);
            
            GameLogger.Success("LobbyCleanupService", "로비 나가기 성공");
            ResetLobbyState();
            return true;
        }
        catch (LobbyServiceException e)
        {
            // 404 에러는 이미 로비가 삭제된 것이므로 성공으로 간주
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                GameLogger.Info("LobbyCleanupService", "로비가 이미 삭제됨 (정상)");
                ResetLobbyState();
                return true;
            }
            
            GameLogger.Error("LobbyCleanupService", $"로비 나가기 실패: {e.Message}");
            return false;
        }
        catch (System.Exception e)
        {
            GameLogger.Error("LobbyCleanupService", $"로비 나가기 중 오류: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 플레이어가 참가한 모든 로비 정리 시도
    /// </summary>
    public async Task<bool> CleanupAllLobbiesAsync()
    {
        try
        {
            GameLogger.Progress("LobbyCleanupService", "모든 로비 정리 시도 중...");
            
            // 현재 로비 나가기
            if (IsInLobby)
            {
                await LeaveCurrentLobbyAsync();
            }
            
            // 추가로 참가 중인 로비가 있는지 확인 (Unity 서비스 레벨)
            // Note: Sessions API는 자동으로 세션 상태 관리하므로 추가 정리 불필요
            
            GameLogger.Success("LobbyCleanupService", "로비 정리 완료");
            return true;
        }
        catch (System.Exception e)
        {
            GameLogger.Error("LobbyCleanupService", $"로비 정리 중 오류: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 로비 상태 초기화
    /// </summary>
    public void ResetLobbyState()
    {
        _currentLobbyId = null;
        _currentPlayerId = null;
        GameLogger.Info("LobbyCleanupService", "로비 상태 초기화");
    }
    
    /// <summary>
    /// 특정 로비 삭제 (Host만 가능)
    /// </summary>
    public async Task<bool> DeleteLobbyAsync(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            GameLogger.Warning("LobbyCleanupService", "삭제할 로비 ID 없음");
            return false;
        }
        
        try
        {
            GameLogger.Progress("LobbyCleanupService", $"로비 삭제 시도: {lobbyId}");
            
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            
            GameLogger.Success("LobbyCleanupService", "로비 삭제 성공");
            
            if (_currentLobbyId == lobbyId)
            {
                ResetLobbyState();
            }
            
            return true;
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                GameLogger.Info("LobbyCleanupService", "로비가 이미 삭제됨 (정상)");
                return true;
            }
            
            GameLogger.Error("LobbyCleanupService", $"로비 삭제 실패: {e.Message}");
            return false;
        }
        catch (System.Exception e)
        {
            GameLogger.Error("LobbyCleanupService", $"로비 삭제 중 오류: {e.Message}");
            return false;
        }
    }
    #endregion
}

