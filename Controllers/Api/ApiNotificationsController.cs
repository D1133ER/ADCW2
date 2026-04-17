using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class ApiNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public ApiNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /api/notifications
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            var alerts = await _notificationService.GetUnreadAlertsForUserAsync(userId);
            
            return Ok(alerts.Select(a => new
            {
                id = a.Id.ToString(),
                userId = userId.ToString(),
                type = "comment",
                title = "New comment",
                message = a.Message,
                isRead = a.isRead,
                createdAt = a.CreatedAt.ToString("o"),
            }));
        }

        // PUT /api/notifications/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        // PUT /api/notifications/{id}/read
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (!success) return NotFound();
            return NoContent();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.Parse(claim!);
        }
    }
}
