// using System;
// using UnityEngine;
// using UnityEngine.EventSystems;

// public class InputManager
// {
//     public bool IsMouseOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

//     public Action<Vector3> OnMouseClickWorld;
//     public Action<Vector3Int> OnMouseClickCell;
//     public Action<Vector3Int> OnMouseDragCell;
//     public Action<Vector3Int> OnMouseDragEndCell;
//     public Action OnBackViewKey;
//     public Action OnTopViewKey;
//     public Action OnPlayerViewKey;
//     private bool _isDragging = false;
//     private Vector3Int _lastDragCell = Vector3Int.zero;
//     private IDisposable _updateSubscription;
//     private bool _initialized = false;

//     // PlayerMove 관련 변수들
//     private Transform _playerTransform;
//     public float moveSpeed = 7f;
//     public float rotationSpeed = 200f;
    
//     // 서빙 관련 변수
//     public float interactionDistance = 5f;  // 인터랙션 가능 거리

//     public InputManager()
//     {
//         Debug.Log("<color=cyan>[InputManager]</color> 생성됨");
//     }

//     public void Init()
//     {
//         if (_initialized) return;
//         _initialized = true;

//         if (Managers.ActionMessage != null)
//         {
//             _updateSubscription = Managers.Subscribe(ActionType.Managers_Update, OnUpdate);
//             Debug.Log("<color=cyan>[InputManager]</color> Update 구독 성공");
//         }

//         // Player Transform 찾기
//         FindPlayerTransform();
//     }

//     private void FindPlayerTransform()
//     {
//         GameObject playerObj = GameObject.FindWithTag("Player");
//         if (playerObj == null)
//         {
//             playerObj = GameObject.Find("Player");
//         }
        
//         if (playerObj != null)
//         {
//             _playerTransform = playerObj.transform;
            
//             // PlayerMove 컴포넌트가 있으면 설정값 가져오기
//             PlayerMove playerMove = playerObj.GetComponent<PlayerMove>();
//             if (playerMove != null)
//             {
//                 moveSpeed = playerMove.moveSpeed;
//                 rotationSpeed = playerMove.rotationSpeed;
//             }
            
//             Debug.Log("<color=cyan>[InputManager]</color> Player 찾음: " + playerObj.name);
//         }
//         else
//         {
//             Debug.LogWarning("<color=cyan>[InputManager]</color> Player 오브젝트를 찾을 수 없습니다.");
//         }
//     }

//     ~InputManager()
//     {
//         _updateSubscription?.Dispose();
//     }

//     private void OnUpdate()
//     {
//         // 1) 키보드 입력은 항상 체크
//         HandleKeyboardInput();

//         // 2) UI 위에 마우스가 있으면 마우스 로직을 건너뛰되, 드래그 상태는 초기화
//         if (IsMouseOverUI)
//         {
//             _isDragging = false;
//             return; // 키보드는 이미 처리했으므로, 여기서 마우스 관련은 종료
//         }

//         // 3) 마우스 클릭/드래그 처리
//         HandleMouseInput();
//     }

//     private void HandleKeyboardInput()
//     {
//         // Player가 없으면 다시 찾기 시도
//         if (_playerTransform == null)
//         {
//             FindPlayerTransform();
//         }

//         // 백뷰 3인칭 게임 조작 (탱크 컨트롤러)
//         Vector2 moveDir = Vector2.zero;
//         float turn = 0f;

//         if (Input.GetKey(KeyCode.W)) moveDir.y += 2;  // 전진
//         if (Input.GetKey(KeyCode.S)) moveDir.y -= 2;  // 후진
//         if (Input.GetKey(KeyCode.A)) 
//         {
//             moveDir.x -= 4;  // Player.cs로 회전 정보 전달
//             turn = -4f;      // InputManager에서도 직접 처리
//         }
//         if (Input.GetKey(KeyCode.D)) 
//         {
//             moveDir.x += 4f;  // Player.cs로 회전 정보 전달
//             turn = 4f;       // InputManager에서도 직접 처리
//         }

//         // GameManager로 이동+회전 정보 전달 (Player.cs에서 처리)
//         if (Managers.Game != null)
//         {
//             if (moveDir != Managers.Game.MoveDir)
//             {
//                 Managers.Game.MoveDir = moveDir;
//             }
//         }

//         // 백업 회전 처리 (InputManager에서 직접)
//         if (_playerTransform != null && turn != 0f)
//         {
//             _playerTransform.Rotate(0f, turn * rotationSpeed * Time.deltaTime, 0f);
//         }

//         if (Input.GetKeyDown(KeyCode.P))
//         {
//             Managers.Game.CustomerCreator._tableManager.DebugPrintAllTableOrders();
//         }
        
//         if (Input.GetKeyDown(KeyCode.Q))
//         {
//             Managers.Map.DebugPrintWaitingQueue();
//         }
        
//         if (Input.GetKeyDown(KeyCode.B))
//         {
//             OnBackViewKey?.Invoke();
//             Managers.PublishAction(ActionType.Camera_BackViewActivated);
//         }
//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             OnTopViewKey?.Invoke();
//             Managers.PublishAction(ActionType.Camera_TopViewActivated);
//         }
//         if (Input.GetKeyDown(KeyCode.K))
//         {
//             Managers.PublishAction(ActionType.Player_InteractKey);
//         }


//        if (Input.GetKeyDown(KeyCode.Space) && Managers.Ingame.isInteracting && !Managers.Ingame.isRhythmGameStarted) //Start Rhythm Game
//         {
//             Managers.Ingame.isRhythmGameStarted = true;
//             Managers.Ingame.StartText.SetActive(false);
//             Managers.Ingame.rhythmGameManager?.StartRhythmSequence();
//         }

//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             Managers.Ingame.rhythmGameManager?.ESCPressed();
//             Managers.Ingame.Resume();
            
//             // 리듬게임 강제 종료 시 메인 BGM 재시작
//             RestartMainBGM();
            
            
//             UI_GameScene sceneUI = Managers.UI.ShowSceneUI<UI_GameScene>();
//             sceneUI.GetComponent<Canvas>().sortingOrder = 100;
//             sceneUI.SetInfo();
//         }

        

//         // Tab키로 현재 레시피 건너뛰기 (리듬게임 진행 중일 때만)
//         if (Input.GetKeyDown(KeyCode.Tab) && Managers.Ingame.isInteracting && Managers.Ingame.isRhythmGameStarted)
//         {
//             // tab 사운드 재생
//             Managers.Sound.Play(Define.ESound.Effect, "tab");
            
//             Managers.Ingame.rhythmGameManager?.SkipCurrentRecipe();
//         }
        
//         // F키로 음식 서빙 (리듬게임 중이 아닐 때만)
//         if (Input.GetKeyDown(KeyCode.F) && !Managers.Ingame.isInteracting)
//         {
//             HandleFoodServing();
//         }
//     }

//     private void HandleMouseInput()
//     {
//         // -- 1) 마우스 클릭(왼쪽 버튼 Down)
//         if (Input.GetMouseButtonDown(0))
//         {
//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             if (Physics.Raycast(ray, out var hit))
//             {
//                 OnMouseClickWorld?.Invoke(hit.point);

//                 if (Managers.Map.CellGrid != null)
//                 {
//                     Vector3Int cellPos = Managers.Map.World2Cell(hit.point);
//                     OnMouseClickCell?.Invoke(cellPos);

//                     _isDragging = true;
//                     _lastDragCell = cellPos;
//                 }
//             }
//         }

//         // -- 2) 마우스 드래그 중
//         if (Input.GetMouseButton(0) && _isDragging)
//         {
//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             if (Physics.Raycast(ray, out var hit))
//             {
//                 if (Managers.Map.CellGrid != null)
//                 {
//                     Vector3Int cellPos = Managers.Map.World2Cell(hit.point);
//                     if (cellPos != _lastDragCell)
//                     {
//                         OnMouseDragCell?.Invoke(cellPos);
//                         _lastDragCell = cellPos;
//                     }
//                 }
//             }
//         }

//         // -- 3) 마우스 드래그 종료(왼쪽 버튼 Up)
//         if (Input.GetMouseButtonUp(0) && _isDragging)
//         {
//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             if (Physics.Raycast(ray, out var hit))
//             {
//                 if (Managers.Map.CellGrid != null)
//                 {
//                     Vector3Int cellPos = Managers.Map.World2Cell(hit.point);
//                     OnMouseDragEndCell?.Invoke(cellPos);
//                 }
//             }

//             _isDragging = false;
//         }
//     }

//     private void HandleFoodServing()
//     {
//         if (Managers.Game?.Player == null) return;
        
//         Vector3 playerPos = Managers.Game.Player.transform.position;
        
//         // TableManager에게 서빙 처리 위임
//         Table nearestTable = Managers.Game.CustomerCreator.TableManager.GetNearestServableTable(playerPos);
        
//         if (nearestTable != null)
//         {
//             Debug.Log($"<color=green>[InputManager] 테이블 {nearestTable.tableId}에 음식 서빙 시도!</color>");
            
//             bool serveSuccess = Managers.Game.CustomerCreator.TableManager.TryServeTable(nearestTable);
            
//             if (serveSuccess)
//             {
//                 Debug.Log($"<color=green>[InputManager] 테이블 {nearestTable.tableId} 서빙 완료!</color>");
//             }
//             else
//             {
//                 Debug.Log($"<color=yellow>[InputManager] 테이블 {nearestTable.tableId} 서빙 실패!</color>");
//             }
//         }
//         else
//         {
//             Debug.Log("<color=yellow>[InputManager] 근처에 서빙 가능한 테이블이 없습니다.</color>");
//         }
//     }

//     /// <summary>
//     /// 메인 BGM을 재시작합니다.
//     /// </summary>
//     public void RestartMainBGM()
//     {
//         try 
//         {
//             AudioClip audioClip = Managers.Resource.Load<AudioClip>("spring-day");
//             if (audioClip != null)
//             {
//                 Managers.Sound.Play(Define.ESound.Bgm, audioClip);
//                 Debug.Log("<color=green>[InputManager]</color> 메인 BGM 재시작: spring-day");
//             }
//             else
//             {
//                 Debug.LogWarning("<color=yellow>[InputManager]</color> spring-day AudioClip을 찾을 수 없습니다.");
//             }
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"<color=red>[InputManager]</color> BGM 재시작 실패: {e.Message}");
//         }
//     }
// }
