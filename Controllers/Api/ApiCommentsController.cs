using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/comments")]
    [Authorize]
    public class ApiCommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IRankingService _rankingService;
        private readonly IAuthorizationService _authorizationService;

        public ApiCommentsController(
            ICommentService commentService,
            IRankingService rankingService,
            IAuthorizationService authorizationService)
        {
            _commentService = commentService;
            _rankingService = rankingService;
            _authorizationService = authorizationService;
        }

        // POST /api/comments/{id}/vote
        [HttpPost("{id:int}/vote")]
        public async Task<IActionResult> Vote(int id, [FromBody] VoteDto dto)
        {
            var userId = GetUserId();
            var action = dto.VoteType == 1 ? "like" : "dislike";
            
            var (likes, dislikes) = await _rankingService.ModifyRankAsync(id, action, "comment", userId);
            return Ok(new { likes, dislikes });
        }

        // PUT /api/comments/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto dto)
        {
            var userId = GetUserId();
            var success = await _commentService.EditCommentAsync(id, dto.Content, userId);
            
            if (!success) return NotFound();
            return NoContent();
        }

        // DELETE /api/comments/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var success = await _commentService.DeleteCommentAsync(id, userId);
            
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

    public record VoteDto(int VoteType);
    public record UpdateCommentDto(string Content);
}
