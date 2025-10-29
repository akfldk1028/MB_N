using System;

namespace MB.Infrastructure.Messages
{
    public enum ActionId
    {
        // 시스템 이벤트
        System_Update,
        System_LateUpdate,
        System_FixedUpdate,
        
        // UI 이벤트
        UI_OpenView,
        UI_CloseView,
        
        // 게임플레이 이벤트
        Gameplay_StartSession,
        Gameplay_EndSession,
        
        // 네트워크 이벤트
        Network_ClientConnected,
        Network_ClientDisconnected,
        
        // ✅ 입력 이벤트 (전역 InputManager가 발행)
        Input_ArrowKey,           // BrickGame 방향키 (←/→)
        Input_PlayerMove,         // 3D 게임 WASD 이동
        Input_MouseWorldClick,    // MapEditor 월드 클릭
        Input_MouseCellClick,     // MapEditor 셀 클릭
        Input_MouseCellDrag,      // MapEditor 셀 드래그
        Input_Interact,           // F키 상호작용 (음식 서빙)
        Input_CameraBackView,     // B키 카메라
        Input_CameraTopView,      // T키 카메라
        Input_RhythmGameStart,    // Space키 리듬게임 시작
        Input_RhythmGameSkip,     // Tab키 리듬게임 스킵
        Input_RhythmGameExit      // Esc키 리듬게임 종료
    }

    public interface IActionPayload { }

    public readonly struct NoPayload : IActionPayload
    {
        public static readonly NoPayload Instance = new NoPayload();
    }

    // ✅ 입력 관련 Payload 타입들
    /// <summary>
    /// 방향키 입력 (-1: 왼쪽, 0: 정지, 1: 오른쪽)
    /// </summary>
    public readonly struct ArrowKeyPayload : IActionPayload
    {
        public float Horizontal { get; }
        
        public ArrowKeyPayload(float horizontal)
        {
            Horizontal = horizontal;
        }
    }

    /// <summary>
    /// 플레이어 이동 입력 (WASD)
    /// </summary>
    public readonly struct PlayerMovePayload : IActionPayload
    {
        public UnityEngine.Vector2 MoveDirection { get; }
        public float Turn { get; }
        
        public PlayerMovePayload(UnityEngine.Vector2 moveDirection, float turn)
        {
            MoveDirection = moveDirection;
            Turn = turn;
        }
    }

    /// <summary>
    /// 월드 좌표 입력 (마우스 클릭, 터치)
    /// </summary>
    public readonly struct WorldPositionPayload : IActionPayload
    {
        public UnityEngine.Vector3 Position { get; }
        
        public WorldPositionPayload(UnityEngine.Vector3 position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// 셀 좌표 입력 (그리드 기반)
    /// </summary>
    public readonly struct CellPositionPayload : IActionPayload
    {
        public UnityEngine.Vector3Int CellPosition { get; }
        
        public CellPositionPayload(UnityEngine.Vector3Int cellPosition)
        {
            CellPosition = cellPosition;
        }
    }

    public readonly struct ActionMessage
    {
        public ActionId Id { get; }
        public IActionPayload Payload { get; }

        ActionMessage(ActionId id, IActionPayload payload)
        {
            Id = id;
            Payload = payload ?? NoPayload.Instance;
        }

        public static ActionMessage From(ActionId id) =>
            new ActionMessage(id, NoPayload.Instance);

        public static ActionMessage From(ActionId id, IActionPayload payload) =>
            new ActionMessage(id, payload ?? NoPayload.Instance);

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : IActionPayload
        {
            if (Payload is TPayload typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }

    public interface IAction
    {
        ActionId Id { get; }
        void Execute(ActionMessage message);
    }
}
