using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Define;

public class UI_StartUpScene : UI_Scene
{
	enum GameObjects
	{
		StartImage
	}

	enum Texts
	{
		DisplayText
	}

	enum Buttons
	{
		StartButton,
		RecipeButton,
		ExitButton,
	}

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindObjects(typeof(GameObjects));
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

		GetObject((int)GameObjects.StartImage).BindEvent((evt) =>
		{
			Debug.Log("ChangeScene");
			// Managers.Scene.LoadScene(EScene.GameScene);
		});

	GetObject((int)GameObjects.StartImage).gameObject.SetActive(false);
	GetButton((int)Buttons.RecipeButton).gameObject.BindEvent(OnClickRecipeButton);
	GetButton((int)Buttons.ExitButton).gameObject.BindEvent(OnClickExitButton);
	GetButton((int)Buttons.StartButton).gameObject.BindEvent(OnClickStartButton);
	
	// GetText((int)Texts.DisplayText).text = $"StartUpScene";
	Debug.Log($"<color=cyan>[UI_StartUpScene]</color> Asset Load 합니다.");
		StartLoadAssets();

		return true;
    }
    async void OnClickStartButton(PointerEventData evt)
    {
        GameLogger.SystemStart("UI_StartUpScene", "게임 시작 버튼 클릭!");
        
        // 1. 멀티플레이어 세션 시작
        await StartMultiplayerSession();
        
        // 2. 게임 씬으로 전환
        GameLogger.Success("UI_StartUpScene", "게임 씬으로 전환");
        Managers.Scene.LoadScene(EScene.GameScene);
    }

    /// <summary>
    /// 멀티플레이어 세션 시작 및 랜덤 매치 시도
    /// </summary>
    private async Task StartMultiplayerSession()
    {
        GameLogger.Progress("UI_StartUpScene", "멀티플레이어 세션 시작 준비");

        try
        {
            // 1. 인증 확인
            bool isAuthenticated = await Managers.Auth.EnsurePlayerIsAuthorized();
            if (!isAuthenticated)
            {
                GameLogger.Error("UI_StartUpScene", "플레이어 인증 실패");
                return;
            }
            GameLogger.Success("UI_StartUpScene", "플레이어 인증 완료");

            // 2. 랜덤 매치 시도 (QuickJoin)
            GameLogger.Info("UI_StartUpScene", "랜덤 매치 시도 중...");
            bool quickJoinSuccess = await Managers.Lobby.QuickJoinAsync();
            
            if (quickJoinSuccess)
            {
                GameLogger.Network("UI_StartUpScene", "랜덤 매치 성공! 기존 세션에 참가");
                return;
            }

            // 3. 기존 세션이 없으면 새 세션 생성 (Host 역할)
            GameLogger.Progress("UI_StartUpScene", "기존 세션 없음, 새 세션 생성 중...");
            string sessionName = $"AutoSession_{UnityEngine.Random.Range(1000, 9999)}";
            bool createSuccess = await Managers.Lobby.CreateLobbyAsync(sessionName, 4);
            
            if (createSuccess)
            {
                GameLogger.Network("UI_StartUpScene", "새 세션 생성 성공! 다른 플레이어 대기 중");
                
                // LocalUser를 Host로 설정
                Managers.LocalUser.IsHost = true;
                Managers.LocalUser.DisplayName = $"Player_{UnityEngine.Random.Range(100, 999)}";
                
                // ConnectionManager를 통해 Host 시작
                Managers.Connection.StartHostLobby(Managers.LocalUser.DisplayName);
                GameLogger.Success("UI_StartUpScene", $"Host로 세션 시작: {Managers.LocalUser.DisplayName}");
            }
            else
            {
                GameLogger.Error("UI_StartUpScene", "세션 생성 실패");
            }
        }
        catch (Exception e)
        {
            GameLogger.Error("UI_StartUpScene", $"멀티플레이어 세션 시작 중 오류: {e.Message}");
        }
    }

    void OnClickRecipeButton(PointerEventData evt)
    {

    }
    void OnClickExitButton(PointerEventData evt)
    {
	    Debug.Log("ExitButton");
	    Application.Quit();
    }
	void StartLoadAssets()
	{
		Managers.Resource.LoadAllAsync<UnityEngine.Object>("PreLoad", (key, count, totalCount) =>
		{
			Debug.Log($"<color=cyan>[UI_StartUpScene]</color> {key} {count}/{totalCount}");

			if (count == totalCount)
			{
				Managers.Data.Init();

				// // 데이터 있는지 확인
				// if (Managers.Game.LoadGame() == false)
				// {
				// 	Managers.Game.InitGame();
				// 	Managers.Game.SaveGame();
				// }

				GetObject((int)GameObjects.StartImage).gameObject.SetActive(true);
				// GetText((int)Texts.DisplayText).text = "Touch To Start";
			}
		});
	}
}
