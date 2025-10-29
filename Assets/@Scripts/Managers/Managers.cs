using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MB.Infrastructure.Messages;
using MB.Infrastructure.State;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

// Main Managers class - Service Locator Pattern
public class Managers : MonoBehaviour
{
    public static bool Initialized { get; private set; } = false;

    private static Managers s_instance;
    private static Managers Instance
    {
        get
        {
            if (s_instance == null)
            {
                Init();
            }
            return s_instance;
        }
    }

    #region Network Components
    // 네트워크 관련 컴포넌트들
    private NetworkManager _networkManager;
    private ConnectionManagerEx _connectionManager;
    private AuthManager _authManager;
    private UpdateRunnerEx _updateRunner;
    private LobbyServiceFacadeEx _lobbyServiceFacade;
    private LocalLobbyEx _localLobby;
    private LocalLobbyUserEx _localUser;
    private SessionManager _sessionManager;
    private DebugClassFacadeEx _debugFacade;
    
    // 게임 모드 및 로비 관리 서비스
    private GameModeService _gameModeService = new GameModeService();
    private LobbyCleanupService _lobbyCleanupService = new LobbyCleanupService();

    // 네트워크 컴포넌트 public 접근자
    public static NetworkManager Network => Instance?._networkManager;
    public static ConnectionManagerEx Connection => Instance?._connectionManager;
    public static AuthManager Auth => Instance?._authManager;
    public static UpdateRunnerEx UpdateRunner => Instance?._updateRunner;
    public static LobbyServiceFacadeEx Lobby => Instance?._lobbyServiceFacade;
    public static LocalLobbyEx LocalLobby => Instance?._localLobby;
    public static LocalLobbyUserEx LocalUser => Instance?._localUser;
    public static SessionManager Session => Instance?._sessionManager;
    public static DebugClassFacadeEx Debug => Instance?._debugFacade;
    public static GameModeService GameMode => Instance?._gameModeService;
    public static LobbyCleanupService LobbyCleanup => Instance?._lobbyCleanupService;
    #endregion

    #region Contents
    // ✅ static singleton으로 변경 (Managers 재생성 시에도 유지)
    private static InputManager s_input = null;
    private static GameManager s_game = null;
    private static ObjectManager s_object = null;
    private static MapManager s_map = null;

    /// <summary>
    /// 전역 입력 매니저 (모든 게임 모드에서 공유)
    /// </summary>
    public static InputManager Input { 
        get { 
            if (s_input == null) {
                s_input = new InputManager();
                GameLogger.Info("Managers", "InputManager 생성됨 (static singleton)");
            }
            return s_input; 
        } 
    }
    
    public static GameManager Game { 
        get { 
            if (s_game == null) {
                s_game = new GameManager();
                GameLogger.Info("Managers", "GameManager 생성됨 (static singleton)");
            }
            return s_game; 
        } 
    }
    public static ObjectManager Object { 
        get { 
            if (s_object == null) {
                s_object = new ObjectManager();
            }
            return s_object; 
        } 
    }
    public static MapManager Map { 
        get { 
            if (s_map == null) {
                s_map = new MapManager();
            }
            return s_map; 
        } 
    }
    #endregion

    #region Core
    // ✅ ActionBus를 static으로 변경하여 모든 Managers 인스턴스가 공유
    private static MB.Infrastructure.Messages.ActionMessageBus s_actionBus = null;
    private MB.Infrastructure.Messages.ActionDispatcher _actionDispatcher;
    private StateMachine _stateMachine;

    private DataManager _data = new DataManager();
    private PoolManager _pool = new PoolManager();
    private ResourceManager _resource = new ResourceManager();
    private SceneManagerEx _scene = new SceneManagerEx();
    private UIManager _ui = new UIManager();

    public static MB.Infrastructure.Messages.ActionMessageBus ActionBus { get { return s_actionBus; } }
    public static StateMachine StateMachine { get { return Instance?._stateMachine; } }
    public static DataManager Data { get { return Instance?._data; } }
    public static PoolManager Pool { get { return Instance?._pool; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SceneManagerEx Scene { get { return Instance?._scene; } }
    public static UIManager UI { get { return Instance?._ui; } }
    #endregion

    public static void Init()
    {
        if (s_instance == null && Initialized == false)
        {
            Initialized = true;

            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();
        }
    }

    private async void Awake()
    {
        // 중복 체크
        if (s_instance != null && s_instance != this)
        {
            GameLogger.Warning("Managers", "중복된 Managers 인스턴스 발견! 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ ActionBus를 static으로 한 번만 생성
        if (s_actionBus == null)
        {
            s_actionBus = new MB.Infrastructure.Messages.ActionMessageBus();
            GameLogger.Success("Managers", "ActionBus 생성됨 (static)");
        }

        // 메시지 시스템 초기화
        GameLogger.Progress("Managers", "Infrastructure 시스템 초기화 중...");
        if (_actionDispatcher == null)
        {
            _actionDispatcher = new MB.Infrastructure.Messages.ActionDispatcher(s_actionBus);
            GameLogger.Success("Managers", "ActionDispatcher 생성됨");
        }

        if (_stateMachine == null)
        {
            _stateMachine = new StateMachine(s_actionBus);
            GameLogger.Success("Managers", "StateMachine 생성됨");
        }

        // ✅ InputManager 초기화 (System_Update 구독)
        Input.Init();
        GameLogger.Success("Managers", "InputManager 초기화 완료 (System_Update 구독)");

        // 네트워크 컴포넌트 초기화
        await InitializeNetworkComponents();
    }

    private async Task InitializeNetworkComponents()
    {
        GameLogger.SystemStart("Managers", "네트워크 컴포넌트 초기화 시작");

        // 1. Unity Services 초기화
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            try
            {
                GameLogger.Progress("Managers", "Unity Services 초기화 중...");
                var options = new InitializationOptions()
                    .SetEnvironmentName("production");
                await UnityServices.InitializeAsync(options);
                GameLogger.Success("Managers", "Unity Services 초기화 성공");
            }
            catch (Exception e)
            {
                GameLogger.Error("Managers", $"Unity Services 초기화 실패: {e.Message}");
                return;
            }
        }
        else
        {
            GameLogger.Success("Managers", "Unity Services 이미 초기화됨");
        }

        // 2. 단일 GameObject에 모든 네트워크 컴포넌트 추가
        GameLogger.Progress("Managers", "네트워크 시스템 GameObject 생성 중...");
        var networkGo = new GameObject("@NetworkSystems");
        DontDestroyOnLoad(networkGo);
        
        _networkManager = networkGo.AddComponent<NetworkManager>();
        _connectionManager = networkGo.AddComponent<ConnectionManagerEx>();
        _authManager = networkGo.AddComponent<AuthManager>();
        _updateRunner = networkGo.AddComponent<UpdateRunnerEx>();
        
        GameLogger.Success("Managers", "모든 네트워크 컴포넌트가 @NetworkSystems에 생성됨");

        // 2-1. NetworkManager 설정 (Transport, NetworkConfig, NetworkPrefabs)
        await ConfigureNetworkManager(networkGo);
        GameLogger.Success("Managers", "NetworkManager 설정 완료 (Transport, Config, Prefabs)");

        // 3. Non-MonoBehaviour 객체들 초기화
        GameLogger.Info("Managers", "서비스 객체들 초기화 중...");
        _debugFacade = new DebugClassFacadeEx();
        _localLobby = new LocalLobbyEx();
        _localUser = new LocalLobbyUserEx();
        _lobbyServiceFacade = new LobbyServiceFacadeEx();
        _sessionManager = new SessionManager();

        // 4. LobbyServiceFacade 의존성 주입
        GameLogger.Progress("Managers", "LobbyServiceFacade 의존성 주입 중...");
        _lobbyServiceFacade.Initialize(
            _debugFacade,
            _updateRunner,
            _localLobby,
            _localUser,
            _scene,
            _networkManager
        );

        // 5. 멀티플레이어 기능 검증
        await ValidateMultiplayerCapabilities();

        GameLogger.Success("Managers", "네트워크 컴포넌트 초기화 완료!");
    }

    /// <summary>
    /// NetworkManager를 코드로 완전히 설정 (Transport, NetworkConfig, NetworkPrefabs)
    /// Addressable 시스템을 활용한 비동기 로딩
    /// </summary>
    private async Task ConfigureNetworkManager(GameObject networkGo)
    {
        GameLogger.Progress("Managers", "NetworkManager 설정 시작...");

        // 1. UnityTransport 추가 및 WebSocket 활성화
        var transport = networkGo.AddComponent<UnityTransport>();
        
        // ✅ WebSocket 모드 활성화 (Relay는 WebSocket 사용)
        transport.UseWebSockets = true;
        GameLogger.Success("Managers", "UnityTransport 설정 완료 (WebSocket 모드)");

        // 2. NetworkConfig 생성 및 설정
        var config = new NetworkConfig
        {
            NetworkTransport = transport,
            TickRate = 60,
            ClientConnectionBufferTimeout = 10,
            ConnectionApproval = true,  // ConnectionManagerEx에서 필요
            EnableSceneManagement = true,
            ForceSamePrefabs = true
        };
        GameLogger.Info("Managers", "NetworkConfig 설정 완료 (TickRate: 60, Approval: ON)");

        // 3. NetworkPrefabsList 로드 (ResourceManager 위임)
        var prefabsList = await _resource.LoadNetworkPrefabsListAsync();
        if (prefabsList != null && prefabsList.PrefabList.Count > 0)
        {
            // NetworkPrefabsList를 NetworkPrefabs로 복사
            foreach (var networkPrefab in prefabsList.PrefabList)
            {
                config.Prefabs.Add(networkPrefab);
            }
            GameLogger.Success("Managers", $"NetworkPrefabs 등록 완료 ({prefabsList.PrefabList.Count}개)");
        }
        else
        {
            GameLogger.Warning("Managers", "NetworkPrefabsList 없음. 빈 리스트로 진행");
        }

        // 4. NetworkManager에 설정 적용
        _networkManager.NetworkConfig = config;
        GameLogger.Success("Managers", "NetworkManager.NetworkConfig 할당 완료");
    }


    /// <summary>
    /// 멀티플레이어 기능이 제대로 설정되었는지 검증
    /// </summary>
    private Task ValidateMultiplayerCapabilities()
    {
        GameLogger.SystemStart("Managers", "멀티플레이어 기능 검증 시작");

        // Unity Services 상태 확인
        bool servicesReady = UnityServices.State == ServicesInitializationState.Initialized;
        if (servicesReady) GameLogger.Success("Managers", "Unity Services: 준비됨");
        else GameLogger.Error("Managers", "Unity Services: 미준비");

        // Authentication 서비스 확인
        bool authReady = _authManager != null;
        if (authReady) GameLogger.Success("Managers", "Authentication: 준비됨");
        else GameLogger.Error("Managers", "Authentication: 미준비");

        // NetworkManager 확인
        bool networkReady = _networkManager != null;
        if (networkReady) GameLogger.Success("Managers", "NetworkManager: 준비됨");
        else GameLogger.Error("Managers", "NetworkManager: 미준비");

        // ConnectionManager 확인
        bool connectionReady = _connectionManager != null;
        if (connectionReady) GameLogger.Success("Managers", "ConnectionManager: 준비됨");
        else GameLogger.Error("Managers", "ConnectionManager: 미준비");

        // Lobby 서비스 확인
        bool lobbyReady = _lobbyServiceFacade != null && _localLobby != null && _localUser != null;
        if (lobbyReady) GameLogger.Success("Managers", "Lobby System: 준비됨");
        else GameLogger.Error("Managers", "Lobby System: 미준비");

        // Session 관리 확인
        bool sessionReady = _sessionManager != null;
        if (sessionReady) GameLogger.Success("Managers", "Session Manager: 준비됨");
        else GameLogger.Error("Managers", "Session Manager: 미준비");

        // 전체 멀티플레이어 준비 상태
        bool allReady = servicesReady && authReady && networkReady && connectionReady && lobbyReady && sessionReady;
        
        if (allReady)
        {
            GameLogger.Success("Managers", "🎉 멀티플레이어 시스템 모든 준비 완료!");
            GameLogger.Network("Managers", "📡 로비 생성/참가 가능");
            GameLogger.Network("Managers", "🔗 클라이언트/호스트 연결 가능");
            GameLogger.Network("Managers", "💾 세션 데이터 동기화 가능");
        }
        else
        {
            GameLogger.Warning("Managers", "⚠️ 일부 멀티플레이어 기능이 준비되지 않았습니다");
        }

        // 인터넷 연결 상태 확인
        var internetReachability = Application.internetReachability;
        string connectionStatus = internetReachability switch
        {
            NetworkReachability.NotReachable => "인터넷 연결 없음",
            NetworkReachability.ReachableViaCarrierDataNetwork => "모바일 데이터 연결",
            NetworkReachability.ReachableViaLocalAreaNetwork => "WiFi/LAN 연결",
            _ => "연결 상태 불명"
        };
        GameLogger.Network("Managers", $"🌐 인터넷 상태: {connectionStatus}");
        
        return Task.CompletedTask;
    }

    private void Update()
    {
        // ✅ 디버깅: Update가 호출되는지 확인 (매 60프레임마다)
        if (Time.frameCount % 60 == 0)
        {
            GameLogger.Info("Managers", $"Update() 호출 중! (프레임: {Time.frameCount}, enabled: {enabled}, activeInHierarchy: {gameObject.activeInHierarchy})");
        }

        PublishAction(ActionId.System_Update);

        // ✅ 디버깅: Publish 확인 (매 60프레임마다)
        if (Time.frameCount % 60 == 0)
        {
            GameLogger.Info("Managers", $"ActionId.System_Update 발행됨!");
        }
    }

    private void LateUpdate()
    {
        PublishAction(ActionId.System_LateUpdate);
    }

    private void FixedUpdate()
    {
        PublishAction(ActionId.System_FixedUpdate);
    }

    private void OnDestroy()
    {
        // ✅ 디버깅: OnDestroy 호출 확인
        GameLogger.Warning("Managers", $"OnDestroy 호출됨! (s_instance == this: {s_instance == this})");
        
        // ✅ 이 인스턴스가 싱글톤 인스턴스인 경우에만 정리
        if (s_instance == this)
        {
            GameLogger.Error("Managers", "싱글톤 인스턴스가 파괴되고 있습니다! ActionBus는 유지됩니다.");
            _stateMachine?.Dispose();
            _actionDispatcher?.Dispose();
            // ✅ ActionBus는 static이므로 Dispose하지 않음 (다른 씬에서 재사용)
            // s_actionBus?.Dispose();
            s_instance = null;
            Initialized = false;
        }
        else
        {
            GameLogger.Info("Managers", "중복 인스턴스가 파괴되었습니다 (정상)");
        }
    }

    #region Public Methods
    public static IDisposable Subscribe(ActionId actionId, Action handler)
    {
        return ActionBus?.Subscribe(actionId, handler);
    }

    public static IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
    {
        return ActionBus?.Subscribe(actionId, handler);
    }

    public static IDisposable SubscribeMultiple(Action<ActionMessage> handler, params ActionId[] actionIds)
    {
        return ActionBus?.Subscribe(handler, actionIds);
    }

    public static void RegisterAction(IAction action)
    {
        Instance?._actionDispatcher?.Register(action);
    }

    public static void UnregisterAction(IAction action)
    {
        Instance?._actionDispatcher?.Unregister(action);
    }

    public static void PublishAction(ActionId actionId)
    {
        ActionBus?.Publish(ActionMessage.From(actionId));
    }

    public static void PublishAction(ActionId actionId, IActionPayload payload)
    {
        ActionBus?.Publish(ActionMessage.From(actionId, payload));
    }

    public static void RegisterState(IState state)
    {
        Instance?._stateMachine?.RegisterState(state);
    }

    public static void SetState(StateId stateId)
    {
        Instance?._stateMachine?.SetState(stateId);
    }

    public static StateId CurrentStateId
    {
        get { return Instance?._stateMachine?.CurrentId ?? StateId.None; }
    }
    #endregion

}
