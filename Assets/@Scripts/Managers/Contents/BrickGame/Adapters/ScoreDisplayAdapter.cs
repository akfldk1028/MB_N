using UnityEngine;
using TMPro;

/// <summary>
/// TextMeshPro UI를 IScoreDisplay 인터페이스로 어댑팅
/// BrickGameManager가 UI 컴포넌트에 직접 의존하지 않도록 함
/// </summary>
public class ScoreDisplayAdapter : MonoBehaviour, IScoreDisplay
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshPro scoreText;
    
    private void Awake()
    {
        // TextMeshPro 자동 참조
        if (scoreText == null)
        {
            scoreText = GetComponent<TextMeshPro>();
            
            if (scoreText == null)
            {
                GameLogger.Warning("ScoreDisplayAdapter", "TextMeshPro가 할당되지 않았습니다. 점수 표시 불가");
            }
        }
    }
    
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}

