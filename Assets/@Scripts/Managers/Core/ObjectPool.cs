using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제네릭 오브젝트 풀 시스템
/// GameObject를 재사용하여 GC Allocation 감소
/// </summary>
/// <typeparam name="T">풀링할 컴포넌트 타입 (MonoBehaviour 상속)</typeparam>
public class ObjectPool<T> where T : Component
{
    #region 풀 설정
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly int _initialSize;
    private readonly int _maxSize;
    private readonly bool _allowExpansion;
    #endregion
    
    #region 풀 관리
    private readonly Stack<T> _availableObjects = new Stack<T>();
    private readonly HashSet<T> _activeObjects = new HashSet<T>();
    private int _totalCreated = 0;
    #endregion
    
    #region 통계
    /// <summary>
    /// 현재 사용 가능한 오브젝트 수
    /// </summary>
    public int AvailableCount => _availableObjects.Count;
    
    /// <summary>
    /// 현재 활성화된 오브젝트 수
    /// </summary>
    public int ActiveCount => _activeObjects.Count;
    
    /// <summary>
    /// 총 생성된 오브젝트 수
    /// </summary>
    public int TotalCreated => _totalCreated;
    #endregion
    
    #region 생성자
    /// <summary>
    /// ObjectPool 생성자
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="parent">부모 Transform (정리용)</param>
    /// <param name="initialSize">초기 풀 크기</param>
    /// <param name="maxSize">최대 풀 크기 (0 = 무제한)</param>
    /// <param name="allowExpansion">풀 확장 허용 여부</param>
    public ObjectPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 100, bool allowExpansion = true)
    {
        _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        _parent = parent;
        _initialSize = initialSize;
        _maxSize = maxSize;
        _allowExpansion = allowExpansion;
        
        // 초기 풀 생성
        for (int i = 0; i < _initialSize; i++)
        {
            CreateNewObject();
        }
        
        GameLogger.Success("ObjectPool", $"{typeof(T).Name} 풀 생성 완료 (초기: {_initialSize}, 최대: {_maxSize})");
    }
    #endregion
    
    #region 오브젝트 생성
    /// <summary>
    /// 새 오브젝트 생성 (내부용)
    /// </summary>
    private T CreateNewObject()
    {
        if (_maxSize > 0 && _totalCreated >= _maxSize)
        {
            GameLogger.Warning("ObjectPool", $"{typeof(T).Name} 풀이 최대 크기에 도달했습니다 ({_maxSize})");
            return null;
        }
        
        T newObj = UnityEngine.Object.Instantiate(_prefab, _parent);
        newObj.gameObject.SetActive(false);
        _availableObjects.Push(newObj);
        _totalCreated++;
        
        return newObj;
    }
    #endregion
    
    #region 오브젝트 가져오기/반환
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="rotation">생성 회전</param>
    /// <returns>활성화된 오브젝트</returns>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = null;
        
        // 1. 사용 가능한 오브젝트가 있으면 재사용
        if (_availableObjects.Count > 0)
        {
            obj = _availableObjects.Pop();
        }
        // 2. 없으면 새로 생성 (확장 허용 시)
        else if (_allowExpansion)
        {
            obj = CreateNewObject();
            if (obj != null)
            {
                _availableObjects.Pop(); // CreateNewObject가 Stack에 넣으므로 다시 꺼냄
            }
        }
        
        if (obj == null)
        {
            GameLogger.Error("ObjectPool", $"{typeof(T).Name} 풀에서 오브젝트를 가져올 수 없습니다");
            return null;
        }
        
        // 3. 오브젝트 활성화
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        _activeObjects.Add(obj);
        
        return obj;
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기 (위치만)
    /// </summary>
    public T Get(Vector3 position) => Get(position, Quaternion.identity);
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기 (기본 위치)
    /// </summary>
    public T Get() => Get(Vector3.zero, Quaternion.identity);
    
    /// <summary>
    /// 오브젝트를 풀에 반환
    /// </summary>
    /// <param name="obj">반환할 오브젝트</param>
    public void Return(T obj)
    {
        if (obj == null)
        {
            GameLogger.Warning("ObjectPool", "null 오브젝트 반환 시도");
            return;
        }
        
        if (!_activeObjects.Contains(obj))
        {
            GameLogger.Warning("ObjectPool", $"{obj.name}은(는) 이 풀에서 가져온 오브젝트가 아닙니다");
            return;
        }
        
        // 오브젝트 비활성화 및 풀에 반환
        obj.gameObject.SetActive(false);
        _activeObjects.Remove(obj);
        _availableObjects.Push(obj);
    }
    #endregion
    
    #region 풀 관리
    /// <summary>
    /// 모든 활성 오브젝트를 풀에 반환
    /// </summary>
    public void ReturnAll()
    {
        // ToArray()로 복사본 생성 (컬렉션 수정 방지)
        T[] activeObjects = new T[_activeObjects.Count];
        _activeObjects.CopyTo(activeObjects);
        
        foreach (var obj in activeObjects)
        {
            Return(obj);
        }
        
        GameLogger.Info("ObjectPool", $"{typeof(T).Name} 모든 오브젝트 반환 완료");
    }
    
    /// <summary>
    /// 풀 초기화 (모든 오브젝트 파괴)
    /// </summary>
    public void Clear()
    {
        // 활성 오브젝트 파괴
        foreach (var obj in _activeObjects)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        
        // 사용 가능한 오브젝트 파괴
        while (_availableObjects.Count > 0)
        {
            var obj = _availableObjects.Pop();
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        
        _activeObjects.Clear();
        _totalCreated = 0;
        
        GameLogger.Info("ObjectPool", $"{typeof(T).Name} 풀 초기화 완료");
    }
    
    /// <summary>
    /// 풀 상태 정보
    /// </summary>
    public override string ToString()
    {
        return $"[ObjectPool<{typeof(T).Name}>] Total: {_totalCreated}, Active: {ActiveCount}, Available: {AvailableCount}";
    }
    #endregion
}

