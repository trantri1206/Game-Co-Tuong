namespace GameCoTuong.Services;

public interface ITokenBlacklistService
{
    void RevokeToken(string token, DateTime expiresAt);
    bool IsTokenRevoked(string token);
}
