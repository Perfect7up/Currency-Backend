using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Username { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool EmailConfirmed { get; set; } = false;

    public string? EmailConfirmationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? ResetTokenExpires { get; set; }
}