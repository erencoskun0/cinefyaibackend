using CinefyAiServer.Entities;
using System.Security.Claims;

namespace CinefyAiServer.Services;

/// <summary>
/// JWT token işlemleri için servis interface'i
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Kullanıcı için JWT token oluşturur
    /// </summary>
    /// <param name="user">Kullanıcı bilgileri</param>
    /// <param name="roles">Kullanıcı rolleri</param>
    /// <returns>Token bilgileri</returns>
    Task<(string token, DateTime expiresAt)> GenerateTokenAsync(User user, IList<string> roles);

    /// <summary>
    /// Refresh token oluşturur
    /// </summary>
    /// <returns>Refresh token</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// JWT token'dan kullanıcı ID'sini alır
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Kullanıcı ID'si</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// JWT token'ın geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Token geçerli mi?</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// JWT token'dan claims'leri alır
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims listesi</returns>
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}