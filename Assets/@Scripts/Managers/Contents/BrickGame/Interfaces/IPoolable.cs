using UnityEngine;

/// <summary>
/// 오브젝트 풀링이 가능한 오브젝트 인터페이스
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 풀에서 가져올 때 호출 (초기화)
    /// </summary>
    void OnSpawnFromPool();
    
    /// <summary>
    /// 풀에 반환할 때 호출 (정리)
    /// </summary>
    void OnReturnToPool();
    
    /// <summary>
    /// 이 오브젝트를 풀에 반환
    /// </summary>
    void ReturnToPool();
}

