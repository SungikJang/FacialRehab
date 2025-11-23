namespace Specifications
{
    public enum UIType
    {
        InitialLoadingUI,
        MainUI,
    }

    public enum FirebaseCheckResult
    {
        HasCurrentUser,
        InitFailed,
        NoCurrentUser
    }
    public enum PhoneAuthResult
    {
        // 1. verificationCompleted (자동 로그인 성공)
        AutoLoginSuccess, 
    
        // 2. codeSent (SMS 코드 전송 완료 -> 수동 입력 필요)
        CodeSent,
    
        // 3. verificationFailed (인증 실패)
        Failed,
    
        // 4. codeAutoRetrievalTimeOut (타임아웃)
        Timeout,
        
        InvalidVerificationCode,
        
        SessionExpired
    }
    //
    // public enum UserDataLoadCheckResult
    // {
    //     HasCurrentUser,
    //     InitFailed,
    //     NoCurrentUser
    // }
}