using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using CinefyAiServer.Services;
using CinefyAiServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Kimlik doğrulama ve kullanıcı yönetimi controller'ı
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Yeni kullanıcı kaydı
    /// </summary>
    /// <param name="registerDto">Kayıt bilgileri</param>
    /// <returns>Kayıt sonucu ve token</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        try
        {
            // Email kontrolü
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Bu email adresi zaten kullanılıyor"
                });
            }

            // Yeni kullanıcı oluştur
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.Phone,
                UserRole = registerDto.Role,
                CompanyName = registerDto.CompanyName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Kullanıcı oluşturulurken hata oluştu",
                    User = new UserDto()
                });
            }

            // JWT token oluştur
            var (token, expiresAt) = await _jwtService.GenerateTokenAsync(user, new List<string> { registerDto.Role.ToString() });
            var refreshToken = _jwtService.GenerateRefreshToken();

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                Message = "Kayıt başarılı",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Role = user.UserRole,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration: {Email}", registerDto.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Sunucu hatası oluştu",
                User = new UserDto()
            });
        }
    }

    /// <summary>
    /// Kullanıcı giriş işlemi
    /// </summary>
    /// <param name="loginDto">Giriş bilgileri</param>
    /// <returns>Giriş sonucu ve token</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        try
        {
            // Kullanıcıyı email ile bul
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Email veya şifre hatalı",
                    User = new UserDto()
                });
            }

            // Şifre kontrolü
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Email veya şifre hatalı",
                    User = new UserDto()
                });
            }

            // Son giriş tarihini güncelle
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // JWT token oluştur
            var (token, expiresAt) = await _jwtService.GenerateTokenAsync(user, new List<string> { user.UserRole.ToString() });
            var refreshToken = _jwtService.GenerateRefreshToken();

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                Message = "Giriş başarılı",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Role = user.UserRole,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login: {Email}", loginDto.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Sunucu hatası oluştu",
                User = new UserDto()
            });
        }
    }

    /// <summary>
    /// Mevcut kullanıcı bilgilerini getirir
    /// </summary>
    /// <returns>Kullanıcı bilgileri</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var user = await _userManager.FindByIdAsync(userGuid.ToString());
            if (user == null || !user.IsActive)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Role = user.UserRole,
                Avatar = user.Avatar,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Sunucu hatası oluştu" });
        }
    }

    /// <summary>
    /// Token yenileme işlemi
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token bilgileri</param>
    /// <returns>Yeni token</returns>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            // Token'dan kullanıcı bilgilerini al
            var principal = _jwtService.GetPrincipalFromToken(refreshTokenDto.Token);
            if (principal == null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Geçersiz token",
                    User = new UserDto()
                });
            }

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Geçersiz token",
                    User = new UserDto()
                });
            }

            var user = await _userManager.FindByIdAsync(userGuid.ToString());
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı",
                    User = new UserDto()
                });
            }

            // Yeni token oluştur
            var (newToken, expiresAt) = await _jwtService.GenerateTokenAsync(user, new List<string> { user.UserRole.ToString() });
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                Message = "Token yenilendi",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Role = user.UserRole,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Sunucu hatası oluştu",
                User = new UserDto()
            });
        }
    }

    /// <summary>
    /// Kullanıcı çıkış işlemi (opsiyonel)
    /// </summary>
    /// <returns>Çıkış sonucu</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Çıkış başarılı" });
    }

    /// <summary>
    /// Kullanılabilir rolleri getir (Admin hariç)
    /// </summary>
    /// <returns>Rol listesi</returns>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<object>>> GetRoles()
    {
        try
        {
            var roles = await _context.Roles
                .Where(r => r.Name != "Admin") // Admin rolünü hariç tut
                .Select(r => new { id = r.Id, name = r.Name })
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { message = "Sunucu hatası oluştu" });
        }
    }
}