using UnityEngine;

/// <summary>
/// ObjectPlacement MonoBehaviour를 IBrickPlacer 인터페이스로 어댑팅
/// BrickGameManager가 MonoBehaviour에 직접 의존하지 않도록 함
/// </summary>
public class ObjectPlacementAdapter : MonoBehaviour, IBrickPlacer
{
    [Header("참조")]
    [SerializeField] private ObjectPlacement objectPlacement;
    
    private void Awake()
    {
        // ObjectPlacement 자동 참조
        if (objectPlacement == null)
        {
            objectPlacement = GetComponent<ObjectPlacement>();
            
            if (objectPlacement == null)
            {
                objectPlacement = FindObjectOfType<ObjectPlacement>();
            }
            
            if (objectPlacement == null)
            {
                GameLogger.Error("ObjectPlacementAdapter", "ObjectPlacement 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }
    
    public void PlaceMultipleRows(int rowCount)
    {
        if (objectPlacement != null)
        {
            objectPlacement.PlaceMultipleRows(rowCount);
        }
        else
        {
            GameLogger.Warning("ObjectPlacementAdapter", "ObjectPlacement가 null입니다. 행 생성 불가");
        }
    }
}

