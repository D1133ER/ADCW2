using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class ApiAdminController : ControllerBase
    {
        private readonly WeblogApplicationDbContext _context;

        public ApiAdminController(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var users = await _context.Users.CountAsync();
            var blogs = await _context.Blogs.CountAsync();
            var comments = await _context.Comments.CountAsync();
            return Ok(new { users, blogs, comments });
        }

        // GET /api/admin/users?limit=10
        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] int limit = 10)
        {
            var users = await _context.Users
                .OrderByDescending(u => u.Id)
                .Take(limit)
                .ToListAsync();

            return Ok(users.Select(u => new
            {
                id = u.Id.ToString(),
                userId = u.Id.ToString(),
                displayName = u.Username,
                bio = u.Bio,
                avatarUrl = (string?)null,
                createdAt = DateTime.UtcNow.ToString("o"),
                updatedAt = DateTime.UtcNow.ToString("o"),
            }));
        }

        // GET /api/admin/blogs?limit=10
        [HttpGet("blogs")]
        public async Task<IActionResult> Blogs([FromQuery] int limit = 10)
        {
            var blogs = await _context.Blogs
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .ToListAsync();

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
