using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<bool> ConfirmEmailAsync(string email, string token);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}

public class AuthService(AppDbContext context, IConfiguration config, IEmailService emailService) : IAuthService
{
    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email)) return false;

        var verifyToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailConfirmationToken = verifyToken,
            EmailConfirmed = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var verifyUrl = $"{config["FrontendUrl"]}/account/confirm-email?token={verifyToken}&email={user.Email}";
        await emailService.SendEmailAsync(user.Email, "Verify Your Account",
            $"<h1>Welcome!</h1><p>Please <a href='{verifyUrl}'>click here</a> to verify your email.</p>");

        return true;
    }

    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email && u.EmailConfirmationToken == token);
        if (user == null) return false;

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        if (!user.EmailConfirmed) throw new Exception("Please verify your email before logging in.");

        user.LastLoginAt = DateTime.UtcNow;
        return await GenerateAuthResponse(user);
    }

    // FIXED: Added missing RefreshTokenAsync implementation
    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null) return null;

        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return null;

        var user = await context.Users.FindAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return null;

        return await GenerateAuthResponse(user);
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;
        user.RefreshToken = null;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash)) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return true;

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        await context.SaveChangesAsync();

        var resetUrl = $"{config["FrontendUrl"]}/account/reset-password?token={resetToken}&email={user.Email}";
        await emailService.SendEmailAsync(user.Email, "Reset Your Password",
            $"<p>Click <a href='{resetUrl}'>here</a> to reset your password. Valid for 1 hour.</p>");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == request.Email &&
            u.PasswordResetToken == request.Token &&
            u.ResetTokenExpires > DateTime.UtcNow);

        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;

        await context.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var token = CreateToken(user);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = refresh;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await context.SaveChangesAsync();
        return new AuthResponse(token, refresh, user.Username, user.Email);
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            ValidateLifetime = false
        }, out _);
        return principal;
    }
}