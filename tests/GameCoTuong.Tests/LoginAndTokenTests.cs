using System.Security.Claims;
using GameCoTuong.DTOs;
using GameCoTuong.Entities;
using GameCoTuong.Services;
using GameCoTuong.Tests.Helpers;
using Xunit;

namespace GameCoTuong.Tests;

public class LoginAndTokenTests
{
    private readonly BCryptPasswordHasher _passwordHasher = new();
    private readonly TokenBlacklistService _blacklistService = new();
    private readonly JwtService _jwtService;

    public LoginAndTokenTests()
    {
        _jwtService = new JwtService("SuperSecretJwtKeyForTestingOnlyAtLeast32Chars!", blacklistService: _blacklistService);
    }

    [Fact]
    public async Task Login_ValidUsernameAndPassword_ReturnsValidJwt()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        var rawPassword = "StrongPassword@123";
        var user = new User
        {
            Username = "login_test_user",
            Email = "login_test@example.com",
            PasswordHash = _passwordHasher.HashPassword(rawPassword),
            DisplayName = "Test Player",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "login_test_user",
            Password = rawPassword
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);

        // Kiểm tra giải mã token
        var isValid = _jwtService.ValidateToken(result.Token, out var principal, out _);
        Assert.True(isValid);
        Assert.NotNull(principal);
        Assert.Equal(user.UserId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("login_test_user", principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public async Task Login_ValidEmail_ReturnsValidJwt()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        var rawPassword = "StrongPassword@123";
        var user = new User
        {
            Username = "email_user",
            Email = "email_user@example.com",
            PasswordHash = _passwordHasher.HashPassword(rawPassword),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act (dùng email viết hoa để test không phân biệt hoa thường)
        var result = await authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "EMAIL_USER@EXAMPLE.COM",
            Password = rawPassword
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnifiedErrorMessage()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        var user = new User
        {
            Username = "existing_user",
            Email = "existing@example.com",
            PasswordHash = _passwordHasher.HashPassword("CorrectPassword@123"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "existing_user",
            Password = "WrongPassword@123"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Token);
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", result.Message);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsSameUnifiedErrorMessage()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var authService = new AuthService(context, _passwordHasher, _jwtService);

        // Act
        var result = await authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "ghost_user_does_not_exist",
            Password = "AnyPassword@123"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Token);
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", result.Message);
    }

    [Fact]
    public void Token_TamperedSignature_IsRejected()
    {
        // Arrange
        var user = new User { UserId = 10, Username = "tamper_test", Email = "tamper@example.com" };
        var (validToken, _) = _jwtService.GenerateToken(user);

        // Act: Sửa đổi 4 ký tự cuối của chữ ký
        var tamperedToken = validToken[..^4] + "XYZ9";
        var isValid = _jwtService.ValidateToken(tamperedToken, out var principal, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Null(principal);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void Token_Expired_IsRejected()
    {
        // Arrange
        var user = new User { UserId = 11, Username = "expired_test", Email = "expired@example.com" };
        // Sinh token có thời hạn âm (đã hết hạn trong quá khứ)
        var (expiredToken, _) = _jwtService.GenerateToken(user, customLifetime: TimeSpan.FromSeconds(-10));

        // Act
        var isValid = _jwtService.ValidateToken(expiredToken, out var principal, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Null(principal);
        Assert.Contains("hết hạn", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Token_Revoked_IsRejected()
    {
        // Arrange
        var user = new User { UserId = 12, Username = "revoke_test", Email = "revoke@example.com" };
        var (token, expiresAt) = _jwtService.GenerateToken(user);

        // Thu hồi token vào blacklist
        _blacklistService.RevokeToken(token, expiresAt);

        // Act
        var isValid = _jwtService.ValidateToken(token, out _, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Contains("thu hồi", errorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
