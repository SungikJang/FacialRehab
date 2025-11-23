using System.Reflection;
using UnityEngine;

namespace InGameManagers
{
    public class Manager : MonoBehaviour
    {
        private static Manager s_instance;
        private static Manager Instance { get { return s_instance; } set { s_instance = value; } }

        private readonly DIContainer _container = DIContainer.Instance;
        
        public static IUIManager UI { get; private set; }
        public static IFirebaseManager Firebase { get; private set; }
        public static IDataManager Data { get; private set; }
        
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            // 씬 로드 전에 Managers 오브젝트가 생성되도록 보장
            if (Instance == null)   
            {
                GameObject go = new GameObject { name = "@Manager" };
                Debug.Log($"Manager Assembly: {typeof(Manager).Assembly.FullName}");
                Instance = go.AddComponent<Manager>();
                DontDestroyOnLoad(go);
            }
        }
        
        private void Awake()
        {
            // if (s_instance != null && s_instance != this)
            // {
            //     Destroy(gameObject);
            //     return;
            // }
            //
            // s_instance = this;
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        
            // --- 여기가 핵심 로직 ---

            // 1. 현재 프로젝트의 모든 코드(.dll)에서 [Injectable] 클래스들을 스캔하고 컨테이너에 등록
            _container.DiscoverAndRegisterServices(Assembly.GetExecutingAssembly());

            // 2. 컨테이너에게 각 매니저의 인스턴스를 요청하여 static 프로퍼티에 할당
            //    이때 DI 컨테이너가 의존성을 재귀적으로 해결해줍니다.
            //    (SoundManager를 요청하면 DataManager가 먼저 생성되어 주입됨)
            UI = _container.GetInstance<IUIManager>();
            Firebase = _container.GetInstance<IFirebaseManager>();
            Data = _container.GetInstance<IDataManager>();
            // Resource = _container.GetInstance<IResourceManager>();
            // UI = _container.GetInstance<IUIManager>();

            // 3. 모든 매니저의 인스턴스가 생성되고 주입이 완료된 후, 초기화 함수 호출
            UI.Init();
        
            Debug.Log("--- All Managers Initialized ---");
        }
    }
}
