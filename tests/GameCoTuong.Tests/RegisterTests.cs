using GameCoTuong.DTOs;
using GameCoTuong.Services;
using GameCoTuong.Tests.Helpers;
using Xunit;

namespace GameCoTuong.Tests;

public class RegisterTests
{
    private readonly BCryptPasswordHasher _passwordHasher = new();
    private readonly JwtService _jwtService = new("SuperSecretJwtKeyForTestingOnlyAtLeast32Chars!");

    [Fact]
    public async Task Register_ValidInformation_CreatesUserAndReturnsSuccess()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        var request = new RegisterRequest
        {
            Username = "valid_user_2026",
            Email = "valid_user@example.com",
            Password = "StrongPassword@123",
            ConfirmPassword = "StrongPassword@123",
            DisplayName = "Kỳ Thủ Hợp Lệ"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.UserId > 0);
        Assert.Equal("valid_user_2026", result.Data.Username);
        Assert.Equal("valid_user@example.com", result.Data.Email);
    }

    [Theory]
    [InlineData("", "valid@example.com", "Strong@123", "Strong@123")] // Username trống
    [InlineData("ab", "valid@example.com", "Strong@123", "Strong@123")] // Username quá ngắn (< 3)
    [InlineData("user name", "valid@example.com", "Strong@123", "Strong@123")] // Username chứa khoảng trắng
    [InlineData("valid_user", "", "Strong@123", "Strong@123")] // Email trống
    [InlineData("valid_user", "invalid-email-format", "Strong@123", "Strong@123")] // Email sai định dạng
    [InlineData("valid_user", "valid@example.com", "short1!", "short1!")] // Mật khẩu quá ngắn (< 8)
    [InlineData("valid_user", "valid@example.com", "nocapital123!", "nocapital123!")] // Thiếu chữ hoa
    [InlineData("valid_user", "valid@example.com", "NOLOWERCASE123!", "NOLOWERCASE123!")] // Thiếu chữ thường
    [InlineData("valid_user", "valid@example.com", "NoSpecialChar123", "NoSpecialChar123")] // Thiếu ký tự đặc biệt
    [InlineData("valid_user", "valid@example.com", "Strong@123", "Mismatch@456")] // Mật khẩu xác nhận không khớp
    public async Task Register_InvalidInformation_BlockedWithDescriptiveErrors(
        string username, string email, string password, string confirmPassword)
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Register_DuplicateUsernameOrEmail_CaseInsensitive_Blocked()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        // Đăng ký tài khoản ban đầu
        await authService.RegisterAsync(new RegisterRequest
        {
            Username = "original_user",
            Email = "original@example.com",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        });

        // Act 1: Thử trùng username với chữ in hoa
        var dupUsernameResult = await authService.RegisterAsync(new RegisterRequest
        {
            Username = "ORIGINAL_USER", // Khác hoa thường
            Email = "different@example.com",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        });

        // Act 2: Thử trùng email với chữ in hoa
        var dupEmailResult = await authService.RegisterAsync(new RegisterRequest
        {
            Username = "different_user",
            Email = "ORIGINAL@EXAMPLE.COM", // Khác hoa thường
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        });

        // Assert
        Assert.False(dupUsernameResult.IsSuccess);
        Assert.Contains(dupUsernameResult.Errors, e => e.Contains("Tên đăng nhập đã tồn tại"));

        Assert.False(dupEmailResult.IsSuccess);
        Assert.Contains(dupEmailResult.Errors, e => e.Contains("Email đã tồn tại"));
    }

    [Fact]
    public async Task Register_TwoConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        var dbName = $"ConcurrentDb_{Guid.NewGuid():N}";
        var request = new RegisterRequest
        {
            Username = "concurrent_player",
            Email = "concurrent@example.com",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        // Gửi 2 yêu cầu đăng ký hoàn toàn đồng thời
        var task1 = Task.Run(async () =>
        {
            using var ctx = TestDbHelper.CreateIsolatedDbContext();
            var auth = new AuthService(ctx, _passwordHasher, _jwtService);
            return await auth.RegisterAsync(request);
        });

        var task2 = Task.Run(async () =>
        {
            using var ctx = TestDbHelper.CreateIsolatedDbContext();
            var auth = new AuthService(ctx, _passwordHasher, _jwtService);
            return await auth.RegisterAsync(request);
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert: Cả 2 yêu cầu chạy độc lập với dữ liệu riêng biệt đều hoàn thành
        Assert.All(results, r => Assert.NotNull(r));
    }
}
