using System.Collections;
using InGameManagers;
using Specifications;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    public class InitialLoadingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private float interval = 2f; // 점이 늘어나는 간격(초)
        [SerializeField] private int maxDots = 5; // 점 최대 개수
        [SerializeField] private string baseText; // 점 최대 개수
        private int _currentDots = 0;

        private GameObject _initFailPopup;
        [SerializeField]private GameObject loginGroup;
        [SerializeField]private GameObject loadingObj;
        [SerializeField]private GameObject signUpPopup;
        [SerializeField]private GameObject smsCodeVerifyPopup;
        [SerializeField]private Button loginButton;
        [SerializeField]private Button signUpButton;
        [SerializeField]private Button smsCodeVerifyButton;
        [SerializeField]private TMP_Text phoneNumberText;
        [SerializeField]private TMP_Text loginStatusText;    
        [SerializeField]private TMP_Text smsCodeText;
        [SerializeField]private TMP_Text smsCodeStatusText;
        private bool _stopLoading;

        // Start is called before the first frame update
        void Start()
        {
            loginButton.onClick.AddListener(() =>
            {
                OnClickRequestVerification(phoneNumberText.text);
            });
            smsCodeVerifyButton.onClick.AddListener(VerifySMSCode);
            signUpButton.onClick.AddListener(() =>
            {
                signUpPopup.SetActive(true);
            });
            _stopLoading = false;
            StartCoroutine(DotAnimation());
            CheckFirebase();
        }

        private async void VerifySMSCode()
        {
            var result = await Manager.Firebase.VerifyCode(smsCodeText.text);
            switch (result)
            {
                case PhoneAuthResult.InvalidVerificationCode:
                    Debug.Log("InvalidVerificationCode");
                    break;
                case PhoneAuthResult.SessionExpired:
                    Debug.Log("SessionExpired");
                    break;
                case PhoneAuthResult.AutoLoginSuccess:
                    Debug.Log("AutoLoginSuccess");
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            
        }
        // 버튼 1: [인증번호 받기] 버튼에 연결
        private async void OnClickRequestVerification(string phoneNum)
        {
        
            // 1. 번호 변환 (010 -> +8210)
            string e164Number = FormatPhoneNumber(phoneNum);
        
            if (e164Number == null)
            {
                loginStatusText.text = "올바른 전화번호를 입력해주세요.";
                return;
            }

            // 2. SMS 발송 요청
            var result = await Manager.Firebase.SendSmsCode(e164Number);
            Debug.Log("--------------------");
            Debug.Log(result);
            Debug.Log("--------------------");
            switch (result)
            {
                case PhoneAuthResult.AutoLoginSuccess:
                    Debug.Log("AutoLoginSuccess");
                    break;
                case PhoneAuthResult.CodeSent:
                    smsCodeVerifyPopup.SetActive(true);
                    Debug.Log("CodeSent");
                    break;
                case PhoneAuthResult.Failed:
                    Debug.Log("Failed");
                    break;
                case PhoneAuthResult.Timeout:
                    Debug.Log("Timeout");
                    break;
                case PhoneAuthResult.SessionExpired:
                    break;
                case PhoneAuthResult.InvalidVerificationCode:
                    break;
            }
        }
        private async void CheckFirebase()
        {
            FirebaseCheckResult result = await Manager.Firebase.Init();

            switch (result)
            {
                case FirebaseCheckResult.HasCurrentUser:
                    bool userDataLoadResult = await Manager.Firebase.LoadUserData();
                    if (userDataLoadResult)
                    {
                        StartCoroutine(StopLoading());
                        // 메인 화면으로 넘어가기
                    }
                    else
                    {
                        //어떡하지 데이터가 없는건가
                    }
                    break;
                case FirebaseCheckResult.InitFailed:
                    _initFailPopup.SetActive(true);
                    break;
                case FirebaseCheckResult.NoCurrentUser:
                    loadingObj.SetActive(false);
                    _stopLoading = true;
                    loginGroup.SetActive(true);
                    //로그인 또는 회원가입 시키기
                    break;
            }
        }
        IEnumerator DotAnimation()
        {
            while (true)
            {
                // 점 개수에 따라 텍스트 갱신
                loadingText.text = baseText + new string('.', _currentDots);

                // 다음 단계로
                _currentDots++;
                if (_currentDots > maxDots)
                    _currentDots = 0;

                if (_stopLoading)
                {
                    break;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        IEnumerator StopLoading()
        {
            yield return new WaitForSeconds(1);
            _stopLoading = true;
        }
        
        
        // 번호 변환 헬퍼 함수
        private string FormatPhoneNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return null;
        
            // 하이픈 제거
            number = number.Replace("-", "");

            // 한국 번호(010...)인 경우 맨 앞 '0' 떼고 '+82' 붙이기
            if (number.StartsWith("0"))
            {
                return "+82" + number.Substring(1); 
            }
        
            return null; // 형식이 안 맞음
        }
    }
}
