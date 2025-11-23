using Specifications;
using UnityEngine;

namespace InGameManagers
{
    public interface IDataManager
    {
        void Init();
        void GetPlayerToken();
        void SetUserData(UserData loadedData);
        void ClearUserData();
        
    }
    
    [Injectable(typeof(IDataManager), ServiceLifetime.Singleton)]
    public class DataManager: IDataManager
    {
        private IDataManager _dataManagerImplementation;

        public UserData CurrentUserData { get; private set; }
        
        public void SetUserData(UserData data)
        {
            this.CurrentUserData = data;
            Debug.Log($"유저 데이터 설정 완료. 레벨: {data.level}, 골드: {data.gold}");
        }

        // 5. 데이터 클리어 함수 (로그아웃 시)
        public void Init()
        {
            _dataManagerImplementation.Init();
        }

        public void GetPlayerToken()
        {
            _dataManagerImplementation.GetPlayerToken();
        }

        public void ClearUserData()
        {
            this.CurrentUserData = null;
            Debug.Log("유저 데이터 초기화 (로그아웃)");
        }
    }
}