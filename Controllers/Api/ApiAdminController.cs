using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class ApiAdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public ApiAdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET /api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var stats = await _adminService.GetTotalStatsAsync();
            return Ok(stats);
        }

        // GET /api/admin/users?limit=10
        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] int limit = 10)
        {
            var users = await _adminService.GetRecentUsersAsync(limit);

            return Ok(users.Select(u => new
            {
                id = u.Id.ToString(),
                userId = u.Id.ToString(),
                displayName = u.Username,
                bio = u.Bio,
                avatarUrl = (string?)null,
                createdAt = u.CreatedAt.ToString("o"),
                updatedAt = DateTime.UtcNow.ToString("o"),
            }));
        }

        // GET /api/admin/blogs?limit=10
        [HttpGet("blogs")]
        public async Task<IActionResult> Blogs([FromQuery] int limit = 10)
        {
            var blogs = await _adminService.GetRecentBlogsAsync(limit);

            return Ok(blogs.Select(b => new
            {
                id = b.Id.ToString(),
                userId = b.UserId.ToString(),
                title = b.Title,
                content = b.Description,
                excerpt = b.ShortDescription,
                coverImageUrl = string.IsNullOrEmpty(b.ImagePath) ? (string?)null : b.ImagePath,
                published = b.Published,
                isFeatured = false,
                createdAt = b.CreatedAt.ToString("o"),
                updatedAt = (b.LastUpdatedAt ?? b.CreatedAt).ToString("o"),
                profiles = b.User == null ? null : new
                {
                    displayName = b.User.Username,
                },
            }));
        }
    }
}
