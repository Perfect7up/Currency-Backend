using Backend.Models;
using Backend.Services;
using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, AppDbContext context) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var success = await authService.RegisterAsync(request);
        return success
            ? Ok(new { message = "Registration successful. Please check your email to verify your account." })
            : BadRequest(new { message = "Email already in use." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var response = await authService.LoginAsync(request);
            return response != null ? Ok(response) : Unauthorized(new { message = "Invalid email or password." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string email, string token)
    {
        var success = await authService.ConfirmEmailAsync(email, token);
        return success
            ? Ok(new { message = "Email confirmed! You can now log in." })
            : BadRequest(new { message = "Invalid or expired confirmation link." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            await authService.ForgotPasswordAsync(request);
            return Ok(new { message = "If an account matches that email, a reset link has been sent." });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Terminal could not dispatch email. Please contact support." });
        }
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var success = await authService.ResetPasswordAsync(request);
        return success
            ? Ok(new { message = "Password has been successfully reset." })
            : BadRequest(new { message = "Invalid or expired reset token." });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var response = await authService.RefreshTokenAsync(request);
        return response != null ? Ok(response) : BadRequest(new { message = "Invalid token." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null) return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        await authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null) return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var user = await context.Users.FindAsync(userId);
        return user == null ? NotFound() : Ok(new UserProfileResponse(user.Id, user.Username, user.Email, user.CreatedAt));
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null) return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var success = await authService.ChangePasswordAsync(userId, request);

        return success
            ? Ok(new { message = "Password updated successfully." })
            : BadRequest(new { message = "Invalid old password." });
    }
}