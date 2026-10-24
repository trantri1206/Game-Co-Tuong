using System.Text.RegularExpressions;
using GameCoTuong.Data;
using GameCoTuong.DTOs;
using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Services;

public class AuthService : IAuthService
{
    private readonly CoTuongDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    // Regex kiểm tra tên đăng nhập: 3-30 ký tự, chỉ chứa chữ cái, chữ số và dấu gạch dưới
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,30}$", RegexOptions.Compiled);

    // Regex kiểm tra email chuẩn
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AuthService(CoTuongDbContext context, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        // 1. Kiểm tra tính hợp lệ của thông tin đầu vào (Validation)
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return RegisterResult.Failure(validationErrors, "Dữ liệu đăng ký không hợp lệ.");
        }

        // 2. Chuẩn hóa tên và email (Trim khoảng trắng, chuyển về chữ thường để đảm bảo Case-Insensitive)
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 3. Kiểm tra trùng lặp trước (Pre-check)
        var isUsernameTaken = await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername);
        if (isUsernameTaken)
        {
            return RegisterResult.Failure(new[] { "Tên đăng nhập đã tồn tại trong hệ thống (kể cả khác hoa/thường)." });
        }

        var isEmailTaken = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (isEmailTaken)
        {
            return RegisterResult.Failure(new[] { "Email đã tồn tại trong hệ thống (kể cả khác hoa/thường)." });
        }

        // 4. Băm mật khẩu bằng BCrypt
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 5. Chuẩn bị đối tượng User mới
        var newUser = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // 6. Lưu vào cơ sở dữ liệu với xử lý Concurrency (Chặn trùng kể cả gửi đồng thời)
        try
        {
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Bắt lỗi vi phạm chỉ mục duy nhất nếu có 2 yêu cầu đến cùng một thời điểm (Race Condition)
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            if (innerMessage.Contains("UQ_Users_Username"))
            {
                return RegisterResult.Failure(new[] { "Tên đăng nhập đã được đăng ký bởi yêu cầu khác vừa hoàn thành." });
            }
            if (innerMessage.Contains("UQ_Users_Email"))
            {
                return RegisterResult.Failure(new[] { "Email đã được đăng ký bởi yêu cầu khác vừa hoàn thành." });
            }

            return RegisterResult.Failure(new[] { "Không thể tạo tài khoản do xung đột dữ liệu trên máy chủ." });
        }

        // 7. Trả về kết quả: TUYỆT ĐỐI KHÔNG chứa Password hoặc PasswordHash
        var responseDto = new UserResponse
        {
            UserId = newUser.UserId,
            Username = newUser.Username,
            Email = newUser.Email,
            DisplayName = newUser.DisplayName,
            CreatedAt = newUser.CreatedAt
        };

        return RegisterResult.Success(responseDto, "Đăng ký tài khoản thành công.");
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrEmpty(request.Password))
        {
            return LoginResult.InvalidCredentials();
        }

        // Chuẩn hóa tên/email đầu vào
        var normalized = request.UsernameOrEmail.Trim().ToLowerInvariant();

        // Tìm người dùng theo Username hoặc Email (Case-insensitive)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);
        if (user == null)
        {
            // Trả thông báo lỗi thống nhất để chống User Enumeration attack
            return LoginResult.InvalidCredentials();
        }

        // Kiểm tra mật khẩu đã băm bằng BCrypt
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            // Trả cùng một thông báo lỗi thống nhất như khi không tìm thấy tài khoản
            return LoginResult.InvalidCredentials();
        }

        // Cấp phiên truy cập (JWT token) có danh tính, chữ ký HmacSha256 và thời hạn
        var (token, expiresAt) = _jwtService.GenerateToken(user);

        var userResponse = new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt
        };

        return LoginResult.Success(token, expiresAt, userResponse);
    }

    private static List<string> ValidateRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        // Kiểm tra Username
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add("Tên đăng nhập không được để trống.");
        }
        else
        {
            var trimmedUsername = request.Username.Trim();
            if (trimmedUsername.Length < 3 || trimmedUsername.Length > 30)
            {
                errors.Add("Tên đăng nhập phải có độ dài từ 3 đến 30 ký tự.");
            }
            if (!UsernameRegex.IsMatch(trimmedUsername))
            {
                errors.Add("Tên đăng nhập chỉ được chứa các chữ cái, chữ số và dấu gạch dưới (_), không chứa khoảng trắng.");
            }
        }

        // Kiểm tra Email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email không được để trống.");
        }
        else
        {
            var trimmedEmail = request.Email.Trim();
            if (trimmedEmail.Length > 100)
            {
                errors.Add("Email không được vượt quá 100 ký tự.");
            }
            if (!EmailRegex.IsMatch(trimmedEmail))
            {
                errors.Add("Email không đúng định dạng hợp lệ (ví dụ: user@example.com).");
            }
        }

        // Kiểm tra Password
        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add("Mật khẩu không được để trống.");
        }
        else
        {
            if (request.Password.Length < 8)
            {
                errors.Add("Mật khẩu phải có độ dài tối thiểu 8 ký tự.");
            }
            if (request.Password.Length > 100)
            {
                errors.Add("Mật khẩu không được dài quá 100 ký tự.");
            }
            if (!request.Password.Any(char.IsUpper))
            {
                errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).");
            }
            if (!request.Password.Any(char.IsLower))
            {
                errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái in thường (a-z).");
            }
            if (!request.Password.Any(char.IsDigit))
            {
                errors.Add("Mật khẩu phải chứa ít nhất 1 chữ số (0-9).");
            }
            if (!request.Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errors.Add("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (ví dụ: !@#$%^&*).");
            }
        }

        // Kiểm tra xác nhận mật khẩu (nếu có truyền ConfirmPassword)
        if (!string.IsNullOrEmpty(request.ConfirmPassword) && request.Password != request.ConfirmPassword)
        {
            errors.Add("Mật khẩu xác nhận không khớp với mật khẩu.");
        }

        return errors;
    }
}
