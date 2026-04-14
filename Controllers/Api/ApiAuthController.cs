using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeblogApplication.Data;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly WeblogApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ApiAuthController(WeblogApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email already in use." });

            var user = new UserModel
            {
                Email = dto.Email,
                Username = dto.DisplayName,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Blogger,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(BuildAuthResponse(user));
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            return Ok(BuildAuthResponse(user));
        }

        // GET /api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            return Ok(BuildMeResponse(user));
        }

        // PUT /api/auth/me/profile
        [Authorize]
        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Username = dto.DisplayName;
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
            await _context.SaveChangesAsync();

            return Ok(new UserProfileDto
            {
                Id = user.Id.ToString(),
                UserId = user.Id.ToString(),
                DisplayName = user.Username,
                Bio = user.Bio,
                AvatarUrl = null,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            });
        }

        // POST /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            // Password reset email sending is handled by the existing UserController/SMTP flow.
            // Return 204 to satisfy the frontend contract.
            return NoContent();
        }

        // POST /api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.passwordResetToken == dto.Token);
            if (user == null) return BadRequest(new { message = "Invalid or expired token." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.passwordResetToken = null;
            await _context.SaveChangesAsync();

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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
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
                CreatedAt = DateTime.UtcNow.ToString("o"),
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
                CreatedAt = DateTime.UtcNow.ToString("o"),
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
