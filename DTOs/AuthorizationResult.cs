namespace GameCoTuong.DTOs;

public class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;

    public static AuthorizationResult Success(string message = "Thao tác được phép thực hiện.")
    {
        return new AuthorizationResult
        {
            IsAuthorized = true,
            StatusCode = 200,
            Message = message
        };
    }

    public static AuthorizationResult Unauthorized(string message = "Yêu cầu xác thực phiên đăng nhập (Thiếu hoặc sai token).")
    {
        return new AuthorizationResult
        {
            IsAuthorized = false,
            StatusCode = 401,
            Message = message
        };
    }

    public static AuthorizationResult Forbidden(string message = "Từ chối truy cập: Bạn không có quyền thực hiện thao tác trong phòng này.")
    {
        return new AuthorizationResult
        {
            IsAuthorized = false,
            StatusCode = 403,
            Message = message
        };
    }
}
