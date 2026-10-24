namespace GameCoTuong.DTOs;

public class RegisterResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new List<string>();
    public UserResponse? Data { get; set; }

    public static RegisterResult Success(UserResponse user, string message = "Đăng ký tài khoản thành công.")
    {
        return new RegisterResult
        {
            IsSuccess = true,
            Message = message,
            Data = user
        };
    }

    public static RegisterResult Failure(params string[] errors)
    {
        return new RegisterResult
        {
            IsSuccess = false,
            Message = "Đăng ký thất bại do thông tin không hợp lệ.",
            Errors = errors.ToList()
        };
    }

    public static RegisterResult Failure(IEnumerable<string> errors, string message = "Đăng ký thất bại.")
    {
        return new RegisterResult
        {
            IsSuccess = false,
            Message = message,
            Errors = errors.ToList()
        };
    }
}
