using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class DIContainer
{
    // 싱글톤 인스턴스
    public static DIContainer Instance { get; } = new DIContainer();

    // 서비스 타입(인터페이스)과 구현 타입을 매핑하는 딕셔너리
    private readonly Dictionary<Type, Type> _registeredTypes = new Dictionary<Type, Type>();
    // 싱글톤 인스턴스를 저장하는 딕셔너리
    private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
    // 서비스 타입과 생명주기를 매핑하는 딕셔너리
    private readonly Dictionary<Type, ServiceLifetime> _lifetimes = new Dictionary<Type, ServiceLifetime>();

    private DIContainer() { }

    /// <summary>
    /// 특정 어셈블리에서 [Injectable] 어트리뷰트가 붙은 모든 클래스를 찾아 자동으로 등록합니다.
    /// </summary>
    public void DiscoverAndRegisterServices(Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.GetCustomAttribute<InjectableAttribute>() != null);

        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttribute<InjectableAttribute>();
            // 어트리뷰트에 인터페이스가 지정되었으면 인터페이스로, 아니면 자기 자신 타입으로 등록
            Type serviceType = attribute.ServiceType ?? type;
            
            if (_registeredTypes.ContainsKey(serviceType))
            {
                Console.WriteLine($"WARNING: Service type {serviceType.Name} is already registered.");
                continue;
            }

            _registeredTypes.Add(serviceType, type);
            _lifetimes.Add(serviceType, attribute.Lifetime);
            Console.WriteLine($"Registered '{type.Name}' as '{serviceType.Name}' with {attribute.Lifetime} lifetime.");
        }
    }

    /// <summary>
    /// 요청된 타입의 인스턴스를 반환합니다. 의존성을 자동으로 주입합니다.
    /// </summary>
    public T GetInstance<T>()
    {
        return (T)GetInstance(typeof(T));
    }

    private object GetInstance(Type serviceType)
    {
        // 1. 등록된 타입인지 확인
        if (!_registeredTypes.ContainsKey(serviceType))
        {
            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        // 2. 싱글톤이고 이미 인스턴스가 생성되었다면 즉시 반환
        if (_lifetimes[serviceType] == ServiceLifetime.Singleton && _singletonInstances.ContainsKey(serviceType))
        {
            return _singletonInstances[serviceType];
        }

        // 3. 실제 구현 타입을 가져옴
        Type implementationType = _registeredTypes[serviceType];

        // 4. 생성자를 찾고, 의존성을 해결 (재귀 호출)
        ConstructorInfo constructor = implementationType.GetConstructors().First(); // 가장 첫 번째 생성자를 사용
        ParameterInfo[] constructorParameters = constructor.GetParameters();

        // 의존성이 없는 경우
        if (!constructorParameters.Any())
        {
            object instance = Activator.CreateInstance(implementationType);
            if (_lifetimes[serviceType] == ServiceLifetime.Singleton)
            {
                _singletonInstances[serviceType] = instance;
            }
            return instance;
        }
        
        // 의존성이 있는 경우, 각 의존성을 재귀적으로 해결
        var dependencies = constructorParameters
            .Select(p => GetInstance(p.ParameterType))
            .ToArray();
            
        // 5. 의존성을 주입하여 최종 인스턴스 생성
        object createdInstance = constructor.Invoke(dependencies);

        // 6. 싱글톤이라면 인스턴스를 저장
        if (_lifetimes[serviceType] == ServiceLifetime.Singleton)
        {
            _singletonInstances[serviceType] = createdInstance;
        }

        return createdInstance;
    }
}