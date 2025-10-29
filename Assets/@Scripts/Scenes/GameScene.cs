using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
	private static bool _isGameInitialized = false; // ✅ static으로 변경하여 모든 인스턴스 공유
	private static GameScene _instance = null; // ✅ 싱글톤 인스턴스
	
	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		// ✅ 중복 인스턴스 제거 (싱글톤 패턴)
		if (_instance != null && _instance != this)
		{
			GameLogger.Warning("GameScene", $"중복된 GameScene 발견! 파괴합니다. (기존: {_instance.name}, 중복: {this.name})");
			Destroy(gameObject);
			return false;
		}
		
		_instance = this;
		SceneType = EScene.GameScene;

		// ✅ 중복 초기화 방지
		if (_isGameInitialized)
		{
			GameLogger.Warning("GameScene", "이미 초기화되었습니다. 스킵합니다.");
			return true;
		}

		// ✅ ObjectPlacement GameObject 자동 생성 (Inspector 불필요)
		EnsureObjectPlacementExists();

		// 벽돌깨기 게임 초기화 (씬 오브젝트 자동 탐색)
		InitializeBrickGame();

		_isGameInitialized = true; // ✅ 초기화 완료 플래그
		return true;
	}

	/// <summary>
	/// ObjectPlacement GameObject가 없으면 자동 생성
	/// </summary>
	private void EnsureObjectPlacementExists()
	{
		// 이미 존재하는지 확인
		ObjectPlacement existing = FindFirstObjectByType<ObjectPlacement>();
		if (existing != null)
		{
			GameLogger.Info("GameScene", "ObjectPlacement가 이미 존재합니다.");
			return;
		}

		// 없으면 새로 생성
		GameObject placerObj = new GameObject("ObjectPlacement");
		placerObj.AddComponent<ObjectPlacement>();
		GameLogger.Success("GameScene", "ObjectPlacement GameObject 자동 생성 완료!");
	}

	private void InitializeBrickGame()
	{
		GameLogger.SystemStart("GameScene", "InitializeBrickGame() 호출됨");
		
		// ✅ 전역 InputManager를 BrickGame 모드로 설정
		Managers.Input.SetGameMode(InputManager.GameMode.BrickGame);
		GameLogger.Success("GameScene", "InputManager 게임 모드 설정: BrickGame");
		
		// BrickGameInitializer가 모든 초기화 담당
		var initializer = new BrickGameInitializer();
		
		bool initResult = initializer.Initialize();
		GameLogger.Info("GameScene", $"initializer.Initialize() 결과: {initResult}");
		
		if (initResult)
		{
			// BrickGame null 체크
			if (Managers.Game == null)
			{
				GameLogger.Error("GameScene", "Managers.Game이 null입니다!");
				return;
			}
			
			if (Managers.Game.BrickGame == null)
			{
				GameLogger.Error("GameScene", "Managers.Game.BrickGame이 null입니다!");
				return;
			}
			
			GameLogger.Progress("GameScene", "StartGame() 호출 직전...");
			
			// 게임 시작 (공 준비 상태로 전환)
			Managers.Game.BrickGame.StartGame();
			
			GameLogger.Success("GameScene", "StartGame() 호출 완료!");
		}
		else
		{
			GameLogger.Error("GameScene", "BrickGame 초기화 실패!");
		}
	}

	public override void Clear()
	{

	}
}
