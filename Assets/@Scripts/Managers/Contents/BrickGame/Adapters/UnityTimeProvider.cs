using UnityEngine;

/// <summary>
/// Unity Time API를 ITimeProvider 인터페이스로 래핑
/// 테스트 시 Mock 객체로 대체 가능
/// </summary>
public class UnityTimeProvider : ITimeProvider
{
    public float CurrentTime => Time.time;
    public float DeltaTime => Time.deltaTime;
}

