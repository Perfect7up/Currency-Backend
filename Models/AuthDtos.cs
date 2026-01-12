namespace Backend.Models;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record AuthResponse(string AccessToken, string RefreshToken, string Username, string Email);
public record UserProfileResponse(Guid Id, string Username, string Email, DateTime CreatedAt);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);