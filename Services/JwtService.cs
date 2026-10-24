using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameCoTuong.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameCoTuong.Services;

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationInMinutes;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"]
            ?? "GameCoTuong_Default_Secret_Key_At_Least_32_Characters_Long!";
        _issuer = configuration["Jwt:Issuer"] ?? "GameCoTuongServer";
        _audience = configuration["Jwt:Audience"] ?? "GameCoTuongClient";
        _expirationInMinutes = int.TryParse(configuration["Jwt:ExpirationInMinutes"], out var exp) ? exp : 60;
    }

    public JwtService(string secretKey, string issuer = "GameCoTuongServer", string audience = "GameCoTuongClient", int expirationInMinutes = 60)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expirationInMinutes = expirationInMinutes;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user, TimeSpan? customLifetime = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        DateTime issuedAt;
        DateTime expiresAt;

        if (customLifetime.HasValue && customLifetime.Value <= TimeSpan.Zero)
        {
            // Token được tạo ra trong quá khứ để phục vụ kiểm thử token hết hạn
            issuedAt = now.Add(customLifetime.Value).AddMinutes(-30);
            expiresAt = now.Add(customLifetime.Value);
        }
        else
        {
            issuedAt = now;
            expiresAt = now.Add(customLifetime ?? TimeSpan.FromMinutes(_expirationInMinutes));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("displayName", user.DisplayName ?? user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = issuedAt,
            IssuedAt = issuedAt,
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    public bool ValidateToken(string token, out ClaimsPrincipal? principal, out string? errorMessage)
    {
        principal = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            errorMessage = "Token không được để trống.";
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = !string.IsNullOrEmpty(_issuer),
            ValidIssuer = _issuer,
            ValidateAudience = !string.IsNullOrEmpty(_audience),
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Không cho phép độ trễ thời gian nhằm từ chối ngay khi hết hạn
        };

        try
        {
            principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                // Đảm bảo thuật toán ký chính xác là HmacSha256
                if (!jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    errorMessage = "Thuật toán chữ ký không hợp lệ.";
                    principal = null;
                    return false;
                }
            }

            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            errorMessage = "Phiên truy cập đã hết hạn (Token expired).";
            return false;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            errorMessage = "Chữ ký số không hợp lệ hoặc token đã bị sửa đổi (Signature validation failed).";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Token không hợp lệ: {ex.Message}";
            return false;
        }
    }
}
