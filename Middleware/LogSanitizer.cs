using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameCoTuong.Middleware;

public static class LogSanitizer
{
    private static readonly Regex BearerTokenRegex = new(
        @"Bearer\s+[A-Za-z0-9\-_=]+\.[A-Za-z0-9\-_=]+\.?[A-Za-z0-9\-_.+/=]*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex QueryAccessTokenRegex = new(
        @"access_token=[A-Za-z0-9\-_=]+\.[A-Za-z0-9\-_=]+\.?[A-Za-z0-9\-_.+/=]*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Làm sạch chuỗi log: thay thế token bí mật bằng [REDACTED] để không lộ vào nhật ký.
    /// </summary>
    public static string Sanitize(string? rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            return string.Empty;
        }

        var sanitized = BearerTokenRegex.Replace(rawMessage, "Bearer [REDACTED]");
        sanitized = QueryAccessTokenRegex.Replace(sanitized, "access_token=[REDACTED]");
        return sanitized;
    }

    /// <summary>
    /// Làm sạch URL của request trước khi ghi vào log.
    /// </summary>
    public static string SanitizeUrl(string url)
    {
        return Sanitize(url);
    }
}

/// <summary>
/// Middleware đảm bảo toàn bộ nhật ký HTTP request và SignalR query params đều được che mờ token.
/// </summary>
public class TokenSanitizingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenSanitizingMiddleware> _logger;

    public TokenSanitizingMiddleware(RequestDelegate next, ILogger<TokenSanitizingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Tạo log an toàn không chứa token
        var path = context.Request.Path;
        var sanitizedQuery = LogSanitizer.Sanitize(context.Request.QueryString.Value);
        var authHeader = context.Request.Headers.Authorization.ToString();
        var sanitizedAuth = LogSanitizer.Sanitize(authHeader);

        _logger.LogInformation("HTTP Request: {Method} {Path}{Query} | Auth: {Auth}",
            context.Request.Method,
            path,
            sanitizedQuery,
            string.IsNullOrEmpty(sanitizedAuth) ? "None" : sanitizedAuth);

        await _next(context);
    }
}
