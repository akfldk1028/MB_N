using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		SceneType = EScene.GameScene;

		// ✅ ObjectPlacement GameObject 자동 생성 (Inspector 불필요)
		EnsureObjectPlacementExists();

		// 벽돌깨기 게임 초기화 (씬 오브젝트 자동 탐색)
		InitializeBrickGame();

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
		// BrickGameInitializer가 모든 초기화 담당
		var initializer = new BrickGameInitializer();
		
		if (initializer.Initialize())
		{
			// 게임 시작 (공 준비 상태로 전환)
			Managers.Game.BrickGame?.StartGame();
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
