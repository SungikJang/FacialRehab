using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Specifications;
using UnityEngine;

namespace InGameManagers
{
    public interface IFirebaseManager
    {
        Task<FirebaseCheckResult> Init();
        Task<bool> LoadUserData();
        Task<PhoneAuthResult> SendSmsCode(string phoneNumber);
        Task<PhoneAuthResult> VerifyCode(string code);
    }


    [Injectable(typeof(IFirebaseManager), ServiceLifetime.Singleton)]
    public class FirebaseManager : IFirebaseManager
    {
        private FirebaseApp _app;
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        private FirebaseUser _currentUser;
        private string _verificationId;

        public async Task<FirebaseCheckResult> Init()
        {
            Debug.Log("LoadingManager 시작. Firebase 초기화를 시도합니다.");

            // 1. Firebase 의존성 확인 및 초기화 (비동기)
            // await는 이 작업이 끝날 때까지 기다렸다가, 메인 스레드에서 다음 코드를 실행합니다.
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            // 2. 초기화 결과 확인
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 초기화 성공
                _app = FirebaseApp.DefaultInstance;
                _auth = FirebaseAuth.DefaultInstance;
                _db = FirebaseFirestore.DefaultInstance;

                Debug.Log("Firebase 초기화 성공!");

                // 3. 초기화가 성공했으므로, 자동 로그인 체크 함수를 *호출하고 그 결과를 기다렸다가 반환*
                return CheckAutoLogin();
            }

            // Firebase 초기화 실패
            Debug.LogError($"Firebase 의존성 확인 실패: {dependencyStatus}");
            return FirebaseCheckResult.InitFailed;
            // TODO: 유저에게 심각한 오류 알림 (예: 앱 종료)
        }

        // 3. 자동 로그인 상태 확인
        private FirebaseCheckResult CheckAutoLogin()
        {
            Debug.Log("자동 로그인 상태 확인 중...");

            // 4. 'auth.CurrentUser' 확인
            // 이전에 로그인을 성공했다면, Firebase SDK가 기기에 토큰을 저장해두고
            // 앱 실행 시 자동으로 'CurrentUser'에 유저 정보를 로드합니다. (토큰이 유효할 경우)
            if (_auth.CurrentUser != null)
            {
                // 5. 토큰(CurrentUser)이 있다 -> 자동 로그인 시도
                _currentUser = _auth.CurrentUser;
                Debug.Log($"자동 로그인 유저 발견: {_currentUser.Email} (UID: {_currentUser.UserId})");

                // 6. 유저 데이터를 Firestore에서 불러옵니다.
                return FirebaseCheckResult.HasCurrentUser;
            }

            // 7. 토큰(CurrentUser)이 없다 -> 로그인/회원가입 씬으로 이동
            return FirebaseCheckResult.NoCurrentUser;
            Debug.Log("자동 로그인 유저 없음. 로그인 씬으로 이동합니다.");
            // SceneManager.LoadScene("LoginScene"); // (씬 이름을 맞게 수정하세요)
        }

        // 6. Firestore에서 유저 데이터 불러오기
        public async Task<bool> LoadUserData()
        {
            Debug.Log($"UID {_currentUser.UserId}의 데이터 로드를 시도합니다...");

            // 8. DB에서 "users" 컬렉션 -> "user.UserId" 문서(Document)를 찾습니다.
            // Firestore의 경로는 [컬렉션] -> [문서] -> [컬렉션] -> [문서] ... 구조입니다.
            DocumentReference docRef = _db.Collection("users").Document(_currentUser.UserId);

            try
            {
                var result = await docRef.GetSnapshotAsync();
                if (result.Exists)
                {
                    // 11. 문서가 존재함 -> UserData 클래스 형식으로 자동 변환
                    Debug.Log("유저 데이터 발견. 데이터를 파싱합니다.");
                    UserData loadedData = result.ConvertTo<UserData>();

                    // 12. GameDataManager에 불러온 데이터를 저장
                    Manager.Data.SetUserData(loadedData);

                    // 13. 모든 로딩 완료, 메인 씬으로 이동
                    Debug.Log("데이터 로드 완료. 메인 씬으로 이동합니다.");
                    return true;
                    // SceneManager.LoadScene("MainMenuScene"); // (씬 이름을 맞게 수정하세요)/
                }

                // 14. 문서는 없는데 인증 정보(CurrentUser)만 있는 비정상 케이스
                // (예: 회원가입 중 DB 생성 실패)
                Debug.LogError("인증 정보는 있으나 Firestore에 데이터베이스 문서가 없습니다.");
                // 14. [해결책] 문서가 없으면 지금 즉시 기본 데이터로 생성한다!
                Debug.LogWarning($"DB 문서가 없어 새로 생성합니다. UID: {_currentUser.UserId}");

                // 14-1. UserData의 기본값 객체 생성
                UserData defaultData = new UserData
                {
                    nickname = _currentUser.UserId,
                    level = 1, // 초기 레벨
                    gold = 100, // 초기 재화
                    lastLoginTimestamp = GetCurrentTimestamp() // 현재 시간 (타임스탬프)
                };

                // 14-2. DB에 이 기본 데이터로 문서를 생성 (SetAsync도 비동기!)
                //      이것도 실패할 수 있으니 try-catch로 감싸는 게 좋지만,
                //      여기서는 일단 상위 try-catch가 잡아주길 기대합니다.
                await docRef.SetAsync(defaultData);

                // 14-3. 방금 생성한 데이터를 Manager에 똑같이 저장
                Manager.Data.SetUserData(defaultData);

                // 14-4. "복구 및 로드 성공"으로 간주하고 true 반환
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"데이터 로드 실패: {e}");
                return false;
            }
        }
        
        public async Task<PhoneAuthResult> SendSmsCode(string phoneNumber)
        {
            PhoneAuthProvider provider = PhoneAuthProvider.GetInstance(_auth);

            // 30초 타임아웃 설정 (선택 사항)
            uint phoneAuthTimeoutMs = 30000;

            Debug.Log($"[{phoneNumber}]로 인증 코드 발송을 요청합니다...");

            PhoneAuthOptions options = new PhoneAuthOptions
            {
                PhoneNumber = phoneNumber,
                TimeoutInMilliseconds = 30000,
                // ForceResendToken = null // (처음 보낼 땐 null 또는 생략)
            };
            var tcs = new TaskCompletionSource<PhoneAuthResult>();

            provider.VerifyPhoneNumber(
                options,

                // (콜백 1) 인증 성공 시 (예: 안드로이드 기기 자동 인증)
                // 이 콜백은 코드를 수동으로 입력할 필요 없이 바로 로그인 성공을 의미합니다.
                verificationCompleted: async (credential) =>
                {
                    Debug.Log("인증 성공: 자동 인증 완료.");
                    // 즉시 2단계(로그인)로 넘어갑니다.
                    var result = await SignInWithCredential(credential);
                    tcs.TrySetResult(result);
                },

                // (콜백 2) 인증 실패 시
                verificationFailed: (error) =>
                {
                    Debug.Log($"인증 실패: {error}");
                    tcs.TrySetResult(PhoneAuthResult.Failed);
                    // UI에 에러 메시지 표시 (예: "유효하지 않은 번호입니다.")
                },

                // (콜백 3) SMS 코드가 사용자에게 발송되었을 때 (가장 중요)
                codeSent: (verificationId, forceResendToken) =>
                {
                    Debug.Log("인증 코드 발송 성공. 사용자 입력을 기다립니다.");

                    // 2단계에서 사용하기 위해 verificationId를 반드시 저장합니다.
                    _verificationId = verificationId;
                    tcs.TrySetResult(PhoneAuthResult.CodeSent);
                    // UI에 "SMS 코드를 입력하세요" UI를 띄웁니다.
                },

                // (콜백 4) 코드 자동 타임아웃
                codeAutoRetrievalTimeOut: (verificationId) =>
                {
                    Debug.Log("인증 코드 입력 시간이 초과되었습니다.");
                    Debug.Log($"자동 SMS 감지 시간이 지났습니다. (ID: {verificationId})");
                    // tcs.TrySetResult(PhoneAuthResult.Timeout);
                }
            );
            return await tcs.Task;
        }

        // public async Task<bool> SignInWithSmsCode(string smsCode)
        // {
        //     if (string.IsNullOrEmpty(_verificationId))
        //     {
        //         Debug.LogError("인증 요청(1단계)이 먼저 완료되어야 합니다.");
        //         return false;
        //     }
        //
        //     Debug.Log("수동 입력한 코드로 로그인을 시도합니다...");
        //
        //     // 1단계에서 받은 _verificationId와 
        //     // 사용자가 입력한 smsCode로 인증 자격(Credential)을 생성합니다.
        //     // GetCredential을 호출하기 위해 PhoneAuthProvider 인스턴스를 먼저 가져옵니다.
        //     PhoneAuthProvider provider = PhoneAuthProvider.GetInstance(_auth);
        //
        //     // --- 2. (수정된 부분) ---
        //     // 클래스(PhoneAuthProvider)가 아닌 인스턴스(provider)로 메서드를 호출합니다.
        //     Credential credential = provider.GetCredential(_verificationId, smsCode);
        //     // Credential credential = PhoneAuthProvider.GetCredential(_verificationId, smsCode);
        //
        //     // 2단계-B: 이 Credential로 실제 로그인을 합니다.
        //     return await SignInWithCredential(credential);
        // }

        private async Task<PhoneAuthResult> SignInWithCredential(Credential credential)
        {
            try
            {
                // 이 함수 하나로 "로그인"과 "신규 회원가입"이 동시에 처리됩니다.
                FirebaseUser user = await _auth.SignInWithCredentialAsync(credential);

                Debug.Log($"로그인/가입 성공! UID: {user.UserId}, Phone: {user.PhoneNumber}");

                // (중요) 3단계: 이 유저가 신규 가입자인지, 기존 유저인지 확인
                await CheckUserInFirestore(user);

                return PhoneAuthResult.AutoLoginSuccess;
            }
            catch (FirebaseException e)
            {
                AuthError errorCode = (AuthError)e.ErrorCode;

                switch (errorCode)
                {
                    case AuthError.InvalidVerificationCode:
                        Debug.LogError("인증 코드가 틀렸습니다.");
                        return PhoneAuthResult.InvalidVerificationCode;
                    case AuthError.SessionExpired:
                        Debug.LogError("인증 세션이 만료되었습니다.");
                        return PhoneAuthResult.SessionExpired;
                    case AuthError.QuotaExceeded:
                        break;

                    default:
                        Debug.LogError($"로그인 실패: {e.Message}");
                        break;
                }
                return PhoneAuthResult.Failed;
            }
        }
        
        public async Task<PhoneAuthResult> VerifyCode(string code)
        {
#if UNITY_EDITOR
            Debug.LogWarning("💻 에디터 환경 감지: 전화번호 인증을 건너뛰고 '익명 로그인'으로 대체합니다.");
    
            // 에디터에서는 진짜 Phone Auth를 못 쓰므로, 테스트를 위해 '익명 로그인'을 수행합니다.
            // 이렇게 하면 가짜지만 유효한 UID를 가진 FirebaseUser가 생성되어 DB 테스트가 가능해집니다.
            try 
            {
                FirebaseUser user = (await _auth.SignInAnonymouslyAsync()).User;
                Debug.Log($"[에디터 테스트] 익명 로그인 성공! UID: {user.UserId}");
        
                // 3단계: DB 확인 로직 호출 (기존 코드 재활용)
                await CheckUserInFirestore(user);
                return PhoneAuthResult.AutoLoginSuccess;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[에디터 테스트] 익명 로그인 실패: {e}");
                return PhoneAuthResult.Failed;
            }
#else
            // 예외 처리: ID가 없거나 코드가 비어있으면 중단
            if (string.IsNullOrEmpty(_verificationId) || string.IsNullOrEmpty(code))
            {
                return PhoneAuthResult.Failed;
            }

            // 2. (핵심) '인증 ID'와 '입력된 코드'를 합쳐서 [자격 증명(Credential)]을 만듭니다.
            // *주의: 사용하시는 SDK 버전에 따라 GetCredential이 정적(static)이 아닐 수 있으므로 인스턴스를 통해 호출합니다.
            Credential credential = PhoneAuthProvider.GetInstance(_auth).GetCredential(_verificationId, code);
        
            // 3. 만들어진 자격 증명으로 최종 로그인 시도
            var result = await SignInWithCredential(credential);
            return result;
#endif
        }

        private async Task CheckUserInFirestore(FirebaseUser user)
        {
            DocumentReference docRef = _db.Collection("users").Document(user.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                // [로그인 성공]
                // 기존 유저입니다. Firestore에서 데이터를 불러옵니다.
                Debug.Log("기존 유저 로그인. 데이터를 로드합니다.");
                UserData loadedData = snapshot.ConvertTo<UserData>();
                Manager.Data.SetUserData(loadedData);

                // 메인 씬으로 이동
            }
            else
            {
                // [회원가입 성공]
                // 신규 유저입니다. 닉네임/프로필을 생성해야 합니다.
                // 1. 기본 데이터 생성
                var newData = new UserData
                {
                    nickname = "12",
                    level = 1,
                    gold = 1,
                    lastLoginTimestamp = 21
                };

                // 2. DB에 저장
                await docRef.SetAsync(newData);
                Manager.Data.SetUserData(newData);
            }
        }

        // 현재 시간을 Unix 타임스탬프(long)로 반환
        private long GetCurrentTimestamp()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}