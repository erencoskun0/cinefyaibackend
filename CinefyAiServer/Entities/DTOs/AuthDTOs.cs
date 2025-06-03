using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs
{
    /// <summary>
    /// Kullanıcı kayıt DTO'su
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ad gereklidir")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string? Phone { get; set; }

        public UserRole Role { get; set; } = UserRole.User;
        public string? CompanyName { get; set; }
    }

    /// <summary>
    /// Kullanıcı giriş DTO'su
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Authentication response DTO'su
    /// </summary>
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
    }

    /// <summary>
    /// Kullanıcı bilgileri DTO'su
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public string? Avatar { get; set; }
        public string? CompanyName { get; set; }
        public string? Position { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "Türkiye";
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// Token yenileme DTO'su
    /// </summary>
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Token gereklidir")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token gereklidir")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}