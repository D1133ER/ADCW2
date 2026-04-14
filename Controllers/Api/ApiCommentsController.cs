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
    [Route("api/comments")]
    [Authorize]
    public class ApiCommentsController : ControllerBase
    {
        private readonly WeblogApplicationDbContext _context;

        public ApiCommentsController(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        // POST /api/comments/{id}/vote
        [HttpPost("{id:int}/vote")]
        public async Task<IActionResult> Vote(int id, [FromBody] VoteDto dto)
        {
            // Vote type tracking uses the RankingModel.
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var existing = await _context.Ranking
                .FirstOrDefaultAsync(r => r.TypeId == id && r.Type == "comment" && r.UserId == userId);

            if (existing != null)
            {
                // Toggle off if same vote
                if ((dto.VoteType == 1 && existing.Like == 1) || (dto.VoteType == -1 && existing.Dislike == 1))
                {
                    _context.Ranking.Remove(existing);
                }
                else
                {
                    existing.Like = dto.VoteType == 1 ? 1 : 0;
                    existing.Dislike = dto.VoteType == -1 ? 1 : 0;
                }
            }
            else
            {
                _context.Ranking.Add(new RankingModel
                {
                    TypeId = id,
                    Type = "comment",
                    UserId = userId,
                    Like = dto.VoteType == 1 ? 1 : 0,
                    Dislike = dto.VoteType == -1 ? 1 : 0,
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.Parse(claim!);
        }
    }

    public record VoteDto(int VoteType);
}
