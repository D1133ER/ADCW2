using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WeblogApplication.Data;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class ApiNotificationsController : ControllerBase
    {
        private readonly WeblogApplicationDbContext _context;

        public ApiNotificationsController(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/notifications
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            // We use the AlertModel as a notification proxy.
            // Return alerts related to blogs owned by this user.
            var myBlogIds = await _context.Blogs
                .Where(b => b.UserId == userId)
                .Select(b => b.Id)
                .ToListAsync();

            var alerts = await _context.Alert
                .Where(a => myBlogIds.Contains(a.BlogPostId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(alerts.Select(a => MapAlert(a, userId)));
        }

        // PUT /api/notifications/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            var myBlogIds = await _context.Blogs
                .Where(b => b.UserId == userId)
                .Select(b => b.Id)
                .ToListAsync();

            var unread = await _context.Alert
                .Where(a => myBlogIds.Contains(a.BlogPostId) && !a.isRead)
                .ToListAsync();

            foreach (var a in unread) a.isRead = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT /api/notifications/{id}/read
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var alert = await _context.Alert.FindAsync(id);
            if (alert == null) return NotFound();
            alert.isRead = true;
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

        private static object MapAlert(AlertModel a, int userId) => new
        {
            id = a.Id.ToString(),
            userId = userId.ToString(),
            type = "comment",
            title = "New comment",
            message = a.Message,
            isRead = a.isRead,
            blogId = a.BlogPostId.ToString(),
            createdAt = a.CreatedAt.ToString("o"),
        };
    }
}
