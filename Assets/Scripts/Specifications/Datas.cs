using Firebase.Firestore;

namespace Specifications
{
    [FirestoreData]
    public class UserData
    {
        // Firestore는 C#의 속성(Property)을 인식합니다.
        // { get; set; }을 붙여주세요.
        // [FirestoreProperty] // 속성별로 지정할 수도 있습니다.
        [FirestoreProperty]
        public string nickname { get; set; }
        [FirestoreProperty]
        public int level { get; set; }
        [FirestoreProperty]
        public int gold { get; set; }
        [FirestoreProperty]
        public long lastLoginTimestamp { get; set; }
    }
}