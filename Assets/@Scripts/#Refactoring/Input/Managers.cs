// /*
//  * 매니저 시스템 (Managers)
//  * 
//  * 역할:
//  * 1. 모든 매니저 클래스의 통합 접점 - 싱글톤 패턴으로 구현된 중앙 관리 시스템
//  * 2. 게임 전체에서 사용되는 핵심 매니저들에 대한 전역 접근 제공
//  * 3. ActionMessage를 통한 이벤트 기반 통신 시스템 제공
//  */

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System;
// using System.Threading.Tasks;

// public class Managers : MonoBehaviour
// {
//     public static bool Initialized { get; set; } = false;

//     private static Managers s_instance;
//     private static Managers Instance { get { Init(); return s_instance; } }

//     #region Contents
//     private MessageManager<ActionType> _action_message = new MessageManager<ActionType>();
//     private GameManager _game = new GameManager();
//     private PlacementManager _placement = new PlacementManager();
//     private InputManager _input = new InputManager();
//     private ObjectManager _object = new ObjectManager();
//     private MapManager _map = new MapManager();
//     private InGameManager _Ingame = new InGameManager();
//     //private RythmGameManager _Rythm;

//     // 메시지 채널 접근자
//     public static MessageManager<ActionType> ActionMessage { get { return Instance?._action_message; } }
//     public static GameManager Game { get { return Instance?._game; } }
//     public static PlacementManager Placement { get { return Instance?._placement; } }
//     public static ObjectManager Object { get { return Instance?._object; } }
//     public static MapManager Map { get { return Instance?._map; } }
//     public static InputManager Input { get { Instance?._input.Init();  return Instance?._input; } }

//     public static InGameManager Ingame { get {Instance?._Ingame.Init(); return Instance?._Ingame; } }
//     //public static RythmGameManager RythmGame { get { return Instance?._Rythm; } }
//     #endregion

//     #region Core
//     private DataManager _data = new DataManager();
//     private PoolManager _pool = new PoolManager();
//     private ResourceManager _resource = new ResourceManager();
//     private SceneManagerEx _scene = new SceneManagerEx();
//     	private SoundManager _sound = new SoundManager();

//     private UIManager _ui = new UIManager();

//     // 메시지 채널들

//     public static DataManager Data { get { return Instance?._data; } }
//     public static PoolManager Pool { get { return Instance?._pool; } }
//     public static ResourceManager Resource { get { return Instance?._resource; } }
//     public static SceneManagerEx Scene { get { return Instance?._scene; } }
//     public static SoundManager Sound { get { Instance?._sound.Init(); return Instance?._sound; } }

//     public static UIManager UI { get { return Instance?._ui; } }

//     // 메시지 채널 접근자
//     #endregion

//     #region Unity Lifecycle

//     // public static Action UpdateHandler;  // static으로 변경

//     // private void Update()
//     // {
//     //     UpdateHandler?.Invoke();
//     // }

 

//     void Update()
//     {
//         _action_message.Publish(ActionType.Managers_Update);
//     }
    
    
//     #endregion

//     #region Public Methods
    
//     public static IDisposable Subscribe(ActionType actionType, Action handler)
//     {
//         return ActionMessage?.Subscribe(type => 
//         {
//             if (type == actionType)
//                 handler?.Invoke();
//         });
//     }

//     public static IDisposable SubscribeMultiple(Action<ActionType> handler, params ActionType[] actionTypes)
//     {
//         return ActionMessage?.Subscribe(type => 
//         {
//             foreach (var actionType in actionTypes)
//             {
//                 if (type == actionType)
//                 {
//                     handler?.Invoke(type);
//                     break;
//                 }
//             }
//         });
//     }
    
//     public static void PublishAction(ActionType actionType)
//     {
//         ActionMessage?.Publish(actionType);
//     }
//     #endregion

//     public static void Init()
//     {
//         if (s_instance == null && Initialized == false)
//         {
//             Initialized = true;

//             GameObject go = GameObject.Find("@Managers");
//             if (go == null)
//             {
//                 go = new GameObject { name = "@Managers" };
//                 go.AddComponent<Managers>();
//             }

//             DontDestroyOnLoad(go);

//             s_instance = go.GetComponent<Managers>();
//             // s_instance._Ingame = go.AddComponent<InGameManager>();
//             //s_instance._Ingame = go.AddComponent<RythmGameManager>();
//         }
//     }

//     void OnDestroy()
//     {
//         _action_message?.Dispose();
//     }
// }