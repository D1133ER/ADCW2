using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public ApiAuthController(IUserService userService, IConfiguration config, IEmailService emailService)
        {
            _userService = userService;
            _config = config;
            _emailService = emailService;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                return Conflict(new { message = "Email already in use." });

            var user = await _userService.RegisterAsync(dto.Email, dto.DisplayName, dto.Password);
            return Ok(BuildAuthResponse(user));
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });

            return Ok(BuildAuthResponse(user));
        }

        // GET /api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(BuildMeResponse(user));
        }

        // PUT /api/auth/me/profile
        [Authorize]
        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            var success = await _userService.UpdateProfileAsync(userId, dto.DisplayName, dto.Bio);
            if (!success) return NotFound();

            var user = await _userService.GetUserByIdAsync(userId);
            return Ok(new UserProfileDto
            {
                Id = user!.Id.ToString(),
                UserId = user.Id.ToString(),
                DisplayName = user.Username,
                Bio = user.Bio,
                AvatarUrl = null,
                CreatedAt = user.CreatedAt.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            });
        }

        // POST /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var success = await _userService.RequestPasswordResetAsync(dto.Email);
            if (success)
            {
                var user = await _userService.GetUserByEmailAsync(dto.Email);
                var resetUrl = Url.Action("ResetPassword", "User", new { email = user!.Email, token = user.passwordResetToken }, Request.Scheme);
                await _emailService.SendEmailAsync(user.Email, "Password Reset", $"Reset link: {resetUrl}");
            }
            // Always return 204 to prevent enumeration
            return NoContent();
        }

        // POST /api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _userService.ResetPasswordAsync(dto.Token, dto.NewPassword);
            if (!success) return BadRequest(new { message = "Invalid or expired token." });

            return NoContent();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.Parse(claim!);
        }

        private string GenerateToken(UserModel user)
        {
            var keyStr = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyStr)) throw new InvalidOperationException("JWT Key not configured.");
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private object BuildAuthResponse(UserModel user) => new
        {
            token = GenerateToken(user),
            user = new { id = user.Id.ToString(), email = user.Email },
            profile = new UserProfileDto
            {
                Id = user.Id.ToString(),
                UserId = user.Id.ToString(),
                DisplayName = user.Username,
                Bio = user.Bio,
                AvatarUrl = (string?)null,
                CreatedAt = user.CreatedAt.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            },
            isAdmin = user.Role == UserRole.Admin,
        };

        private object BuildMeResponse(UserModel user) => new
        {
            user = new { id = user.Id.ToString(), email = user.Email },
            profile = new UserProfileDto
            {
                Id = user.Id.ToString(),
                UserId = user.Id.ToString(),
                DisplayName = user.Username,
                Bio = user.Bio,
                AvatarUrl = (string?)null,
                CreatedAt = user.CreatedAt.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            },
            isAdmin = user.Role == UserRole.Admin,
        };
    }

    // ─── DTOs ──────────────────────────────────────────────────────────────────

    public record RegisterDto(string Email, string Password, string DisplayName);
    public record LoginDto(string Email, string Password);
    public record ForgotPasswordDto(string Email);
    public record ResetPasswordDto(string Token, string NewPassword);
    public record UpdateProfileDto(string DisplayName, string? Bio);

    public class UserProfileDto
    {
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string CreatedAt { get; init; } = "";
        public string UpdatedAt { get; init; } = "";
    }
}
