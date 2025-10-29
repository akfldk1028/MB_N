/*
 * 게임 매니저 (GameManager)
 * 
 * 역할:
 * 1. 게임의 핵심 데이터와 게임 진행 상태 관리
 * 2. 게임 세이브 데이터(리소스, 영웅 등) 저장 및 로드
 * 3. 플레이어 진행 상황 추적 및 저장
 * 4. 영웅 소유 상태 및 레벨, 경험치 등의 데이터 관리
 * 5. JSON 형식으로 게임 데이터를 저장하고 로드하는 기능 제공
 * 6. 게임 초기화 및 데이터 설정 제어
 * 7. Managers 클래스를 통해 전역적으로 접근 가능한 게임 데이터 제공
 */

using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using MB.Infrastructure.Messages;


public class GameManager
{
	#region BrickGame
	private BrickGameManager _brickGame;
	private IDisposable _brickGameUpdateSubscription; // ✅ GC 방지용 구독 저장

	/// <summary>
	/// 벽돌깨기 게임 매니저 접근자
	/// Managers.Game.BrickGame 형태로 사용
	/// </summary>
	public BrickGameManager BrickGame => _brickGame;
	#endregion
	
	public GameManager()
	{
		GameLogger.Progress("GameManager", "GameManager 생성됨");
		
		// BrickGameManager 인스턴스 생성
		_brickGame = new BrickGameManager();
	}
	
	/// <summary>
	/// BrickGame 초기화 (의존성 주입)
	/// 씬에서 필요한 컴포넌트들을 찾아서 주입
	/// </summary>
	public void InitializeBrickGame(
		IBrickPlacer brickPlacer,
		IScoreDisplay scoreDisplay,
		PhysicsPlank plank,
		Camera mainCamera,
		BrickGameSettings settings = null)
	{
		if (_brickGame == null)
		{
			GameLogger.Error("GameManager", "BrickGame이 null입니다.");
			return;
		}
		
		// ✅ 기존 구독 해제 (중복 구독 방지)
		if (_brickGameUpdateSubscription != null)
		{
			GameLogger.Warning("GameManager", "기존 BrickGame 구독 해제 중...");
			_brickGameUpdateSubscription.Dispose();
			_brickGameUpdateSubscription = null;
		}
		
		// 설정이 없으면 기본값 사용
		if (settings == null)
		{
			settings = BrickGameSettings.CreateDefault();
		}
		
		// TimeProvider는 Unity 기본 제공
		var timeProvider = new UnityTimeProvider();
		
		// BrickGame 초기화
		_brickGame.Initialize(brickPlacer, scoreDisplay, timeProvider, plank, mainCamera, settings);

		// ActionBus에 Update 구독 (✅ GC 방지를 위해 필드에 저장!)
		GameLogger.Info("GameManager", "ActionBus에 BrickGame.OnUpdate 구독 시작...");
		_brickGameUpdateSubscription = Managers.Subscribe(ActionId.System_Update, _brickGame.OnUpdate);
		if (_brickGameUpdateSubscription != null)
		{
			GameLogger.Success("GameManager", "✅ ActionBus 구독 성공! (필드에 저장하여 GC 방지)");
		}
		else
		{
			GameLogger.Error("GameManager", "❌ ActionBus 구독 실패! BrickGame이 업데이트되지 않습니다.");
		}

		GameLogger.Success("GameManager", "BrickGame 초기화 완료!");
	}

	#region Save & Load	
	public string Path { get { return Application.persistentDataPath + "/SaveData.json"; } }

	#endregion

	#region Action
	#endregion
}
