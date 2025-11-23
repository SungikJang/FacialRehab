using System;

// 서비스의 생명주기를 정의 (Transient: 매번 새로 생성, Singleton: 단 하나의 인스턴스 공유)
public enum ServiceLifetime
{
    Transient,
    Singleton
}

// 클래스에 부착하여 DI 컨테이너에 자동으로 등록되도록 하는 Attribute
[AttributeUsage(AttributeTargets.Class)]
public class InjectableAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    public Type ServiceType { get; }

    /// <summary>
    /// Injectable Attribute 생성자
    /// </summary>
    /// <param name="serviceType">등록할 서비스의 인터페이스 타입. null이면 자기 자신의 타입으로 등록.</param>
    /// <param name="lifetime">서비스의 생명주기 (기본값: Transient)</param>
    public InjectableAttribute(Type serviceType = null, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
        ServiceType = serviceType;
    }
}