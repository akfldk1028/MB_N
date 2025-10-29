using System;
using MB.Infrastructure.Messages;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 전역 입력 관리 매니저
/// - 모든 하드웨어 입력(키보드, 마우스, 터치)을 수집
/// - ActionMessageBus를 통해 입력 이벤트 발행
/// - 게임 모드와 독립적으로 작동
/// </summary>
public class InputManager
{
    #region 게임 모드
    public enum GameMode
    {
        None,           // 입력 비활성화
        BrickGame,      // 벽돌깨기 (방향키, 마우스)
        ThreeDGame,     // 3D 게임 (WASD, 카메라, 상호작용)
        RhythmGame,     // 리듬게임 (Space, Tab, Esc)
        MapEditor       // 맵 에디터 (마우스, 셀 드래그)
    }
    
    private GameMode _currentGameMode = GameMode.None;
    
    /// <summary>
    /// 현재 게임 모드 설정 (각 게임 시작 시 호출)
    /// </summary>
    public void SetGameMode(GameMode mode)
    {
        _currentGameMode = mode;
        GameLogger.Info("InputManager", $"게임 모드 변경: {mode}");
    }
    #endregion
    
    #region 초기화
    private IDisposable _updateSubscription;
    private bool _initialized = false;
    
    // 3D 게임용 Player Transform
    private Transform _playerTransform;
    public float moveSpeed = 7f;
    public float rotationSpeed = 200f;
    public float interactionDistance = 5f;
    
    // 마우스 드래그 상태
    private bool _isDragging = false;
    private Vector3Int _lastDragCell = Vector3Int.zero;
    
    public InputManager()
    {
        GameLogger.SystemStart("InputManager", "전역 입력 매니저 생성됨");
    }
    
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        
        // ✅ ActionBus의 System_Update 구독
        if (Managers.ActionBus != null)
        {
            _updateSubscription = Managers.ActionBus.Subscribe(ActionId.System_Update, OnUpdate);
            GameLogger.Success("InputManager", "System_Update 구독 완료!");
        }
        else
        {
            GameLogger.Error("InputManager", "ActionBus가 null입니다!");
        }
    }
    
    ~InputManager()
    {
        _updateSubscription?.Dispose();
    }
    #endregion
    
    #region Update (매 프레임 호출)
    private void OnUpdate()
    {
        if (_currentGameMode == GameMode.None) return;
        
        // 1. 키보드 입력 (게임 모드별 처리)
        HandleKeyboardInput();
        
        // 2. 마우스 입력 (UI 위가 아닐 때만)
        if (!IsMouseOverUI())
        {
            HandleMouseInput();
        }
        else
        {
            _isDragging = false;
        }
    }
    
    private bool IsMouseOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    #endregion
    
    #region 키보드 입력 처리
    private void HandleKeyboardInput()
    {
        switch (_currentGameMode)
        {
            case GameMode.BrickGame:
                HandleBrickGameKeyboard();
                break;
                
            case GameMode.ThreeDGame:
                HandleThreeDGameKeyboard();
                break;
                
            case GameMode.RhythmGame:
                HandleRhythmGameKeyboard();
                break;
                
            case GameMode.MapEditor:
                // MapEditor는 마우스 중심
                break;
        }
    }
    
    /// <summary>
    /// 벽돌깨기 게임 키보드 입력 (방향키)
    /// </summary>
    private void HandleBrickGameKeyboard()
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
            // ✅ ActionBus로 방향키 입력 발행
            var payload = new ArrowKeyPayload(horizontal);
            var message = ActionMessage.From(ActionId.Input_ArrowKey, payload);
            Managers.ActionBus.Publish(message);
            
            GameLogger.DevLog("InputManager", $"⌨️ 방향키 입력: {(horizontal > 0 ? "→" : "←")} ({horizontal})");
        }
    }
    
    /// <summary>
    /// 3D 게임 키보드 입력 (WASD, 카메라, 상호작용)
    /// </summary>
    private void HandleThreeDGameKeyboard()
    {
        // Player Transform 찾기 (지연 로딩)
        if (_playerTransform == null)
        {
            FindPlayerTransform();
        }
        
        // WASD 이동
        Vector2 moveDir = Vector2.zero;
        float turn = 0f;
        
        if (Input.GetKey(KeyCode.W)) moveDir.y += 2f;
        if (Input.GetKey(KeyCode.S)) moveDir.y -= 2f;
        if (Input.GetKey(KeyCode.A)) 
        {
            moveDir.x -= 4f;
            turn = -4f;
        }
        if (Input.GetKey(KeyCode.D)) 
        {
            moveDir.x += 4f;
            turn = 4f;
        }
        
        if (moveDir != Vector2.zero)
        {
            var payload = new PlayerMovePayload(moveDir, turn);
            var message = ActionMessage.From(ActionId.Input_PlayerMove, payload);
            Managers.ActionBus.Publish(message);
            
            // 백업: InputManager에서 직접 회전 처리
            if (_playerTransform != null && turn != 0f)
            {
                _playerTransform.Rotate(0f, turn * rotationSpeed * Time.deltaTime, 0f);
            }
        }
        
        // 카메라 전환
        if (Input.GetKeyDown(KeyCode.B))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_CameraBackView));
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_CameraTopView));
        }
        
        // 상호작용 (F키: 음식 서빙 등)
        if (Input.GetKeyDown(KeyCode.F))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_Interact));
        }
    }
    
    /// <summary>
    /// 리듬게임 키보드 입력 (Space, Tab, Esc)
    /// </summary>
    private void HandleRhythmGameKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_RhythmGameStart));
        }
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_RhythmGameSkip));
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_RhythmGameExit));
        }
    }
    
    private void FindPlayerTransform()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }
        
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            
            // TODO: PlayerMove 컴포넌트 추가 시 활성화
            // var playerMove = playerObj.GetComponent<PlayerMove>();
            // if (playerMove != null)
            // {
            //     moveSpeed = playerMove.moveSpeed;
            //     rotationSpeed = playerMove.rotationSpeed;
            // }
            
            GameLogger.Info("InputManager", $"Player 찾음: {playerObj.name}");
        }
    }
    #endregion
    
    #region 마우스 입력 처리
    private void HandleMouseInput()
    {
        switch (_currentGameMode)
        {
            case GameMode.BrickGame:
                // BrickGame은 마우스를 PlankManager에서 직접 처리 (기존 방식 유지)
                break;
                
            case GameMode.MapEditor:
            case GameMode.ThreeDGame:
                HandleMouseWorldInteraction();
                break;
        }
    }
    
    /// <summary>
    /// 월드/셀 기반 마우스 상호작용 (MapEditor, 3D Game)
    /// </summary>
    private void HandleMouseWorldInteraction()
    {
        // 마우스 클릭 (Down)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                // 월드 좌표 클릭
                var worldPayload = new WorldPositionPayload(hit.point);
                Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_MouseWorldClick, worldPayload));
                
                // TODO: MapManager에 World2Cell 메서드 추가 시 활성화
                // 셀 좌표 클릭 (그리드 시스템이 있을 때)
                // if (Managers.Map?.CellGrid != null)
                // {
                //     Vector3Int cellPos = Managers.Map.World2Cell(hit.point);
                //     var cellPayload = new CellPositionPayload(cellPos);
                //     Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_MouseCellClick, cellPayload));
                //     
                //     _isDragging = true;
                //     _lastDragCell = cellPos;
                // }
            }
        }
        
        // TODO: MapManager에 World2Cell 메서드 추가 시 활성화
        // 마우스 드래그 (Hold)
        // if (Input.GetMouseButton(0) && _isDragging)
        // {
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     if (Physics.Raycast(ray, out var hit))
        //     {
        //         if (Managers.Map?.CellGrid != null)
        //         {
        //             Vector3Int cellPos = Managers.Map.World2Cell(hit.point);
        //             if (cellPos != _lastDragCell)
        //             {
        //                 var cellPayload = new CellPositionPayload(cellPos);
        //                 Managers.ActionBus.Publish(ActionMessage.From(ActionId.Input_MouseCellDrag, cellPayload));
        //                 _lastDragCell = cellPos;
        //             }
        //         }
        //     }
        // }
        
        // 마우스 드래그 종료 (Up)
        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _isDragging = false;
        }
    }
    #endregion
}

