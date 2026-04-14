using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using WeblogApplication.Data;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/blogs")]
    public class ApiBlogsController : ControllerBase
    {
        private readonly WeblogApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ApiBlogsController(WeblogApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET /api/blogs?published=true
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool? published)
        {
            var query = _context.Blogs
                .Include(b => b.User)
                .AsQueryable();

            var currentUserId = TryGetUserId();
            var currentUser = currentUserId.HasValue
                ? await _context.Users.FindAsync(currentUserId.Value)
                : null;

            if (published == false)
            {
                if (!currentUserId.HasValue)
                    return Unauthorized();

                query = currentUser?.Role == UserRole.Admin
                    ? query.Where(b => !b.Published)
                    : query.Where(b => !b.Published && b.UserId == currentUserId.Value);
            }
            else
            {
                query = query.Where(b => b.Published);
            }

            var blogs = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(blogs.Select(MapBlog));
        }

        // GET /api/blogs/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var blog = await _context.Blogs
                .Include(b => b.User)
                .Include(b => b.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (blog == null) return NotFound();
            if (!await CanAccessBlogAsync(blog)) return NotFound();

            var commentVoteLookup = await GetCommentVoteLookupAsync(blog.Comments.Select(c => c.Id));
            return Ok(MapBlogDetail(blog, commentVoteLookup));
        }

        // POST /api/blogs
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertBlogDto dto)
        {
            var userId = TryGetUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized();

            var blog = new BlogModel
            {
                Title = dto.Title,
                Description = dto.Content,
                ShortDescription = BuildExcerpt(dto.Excerpt, dto.Content),
                ImagePath = dto.CoverImageUrl ?? "",
                CreatedBy = user.Username,
                CreatedAt = DateTime.UtcNow,
                UserId = userId.Value,
                Published = dto.Published,
                Popularity = 0,
            };
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();
            await _context.Entry(blog).Reference(b => b.User).LoadAsync();
            return Ok(MapBlog(blog));
        }

        // PUT /api/blogs/{id}
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertBlogDto dto)
        {
            var userId = TryGetUserId();
            if (!userId.HasValue) return Unauthorized();

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null) return NotFound();
            if (blog.UserId != userId.Value) return Forbid();

            blog.Title = dto.Title;
            blog.Description = dto.Content;
            blog.ShortDescription = BuildExcerpt(dto.Excerpt, dto.Content);
            if (dto.CoverImageUrl != null) blog.ImagePath = dto.CoverImageUrl;
            blog.Published = dto.Published;
            blog.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _context.Entry(blog).Reference(b => b.User).LoadAsync();
            return Ok(MapBlog(blog));
        }

        // DELETE /api/blogs/{id}
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = TryGetUserId();
            if (!userId.HasValue) return Unauthorized();

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            var user = await _context.Users.FindAsync(userId.Value);
            var isAdmin = user?.Role == UserRole.Admin;

            if (blog.UserId != userId.Value && !isAdmin) return Forbid();

            var commentIds = await _context.Comments
                .Where(c => c.BlogId == id)
                .Select(c => c.Id)
                .ToListAsync();

            var relatedRankings = await _context.Ranking
                .Where(r =>
                    (r.Type == "blog" && r.TypeId == id) ||
                    ((r.Type == "comment" || r.Type == "comments") && commentIds.Contains(r.TypeId)))
                .ToListAsync();

            var relatedAlerts = await _context.Alert
                .Where(a => a.BlogPostId == id)
                .ToListAsync();

            _context.Ranking.RemoveRange(relatedRankings);
            _context.Alert.RemoveRange(relatedAlerts);

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/blogs/{id}/comments
        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
            if (blog == null) return NotFound();
            if (!await CanAccessBlogAsync(blog)) return NotFound();

            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.BlogId == id)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            var commentVoteLookup = await GetCommentVoteLookupAsync(comments.Select(c => c.Id));
            return Ok(comments.Select(c => MapComment(c, commentVoteLookup)));
        }

        // POST /api/blogs/{id}/comments
        [Authorize]
        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
        {
            var userId = TryGetUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized();

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null) return NotFound();
            if (!await CanAccessBlogAsync(blog)) return NotFound();

            var comment = new CommentModel
            {
                BlogId = id,
                UserId = userId.Value,
                Text = dto.Content,
                CreatedBy = user.Username,
                CreatedDate = DateTime.UtcNow,
            };
            _context.Comments.Add(comment);

            // Create notification for blog author if different user
            if (blog.UserId != userId.Value)
            {
                _context.Alert.Add(new AlertModel
                {
                    BlogPostId = id,
                    Message = $"New comment on \"{blog.Title}\"",
                    CreatedAt = DateTime.UtcNow,
                    isRead = false,
                });
            }

            await _context.SaveChangesAsync();
            comment.User = user;
            return Ok(MapComment(comment, new Dictionary<int, List<object>>()));
        }

        // GET /api/users/me/blogs
        [Authorize]
        [HttpGet("/api/users/me/blogs")]
        public async Task<IActionResult> MyBlogs()
        {
            var userId = TryGetUserId();
            if (!userId.HasValue) return Unauthorized();

            var blogs = await _context.Blogs
                .Include(b => b.User)
                .Where(b => b.UserId == userId.Value)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return Ok(blogs.Select(MapBlog));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static string BuildExcerpt(string? excerpt, string content)
        {
            if (!string.IsNullOrWhiteSpace(excerpt))
                return excerpt.Trim();

            return content.Length > 200 ? content[..200] : content;
        }

        private static object MapBlog(BlogModel b) => new
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
                avatarUrl = (string?)null,
            },
        };

        private static object MapBlogDetail(BlogModel b, IReadOnlyDictionary<int, List<object>> commentVoteLookup) => new
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
                avatarUrl = (string?)null,
            },
            comments = b.Comments?.Select(c => MapComment(c, commentVoteLookup)).ToList(),
        };

        private static object MapComment(CommentModel c, IReadOnlyDictionary<int, List<object>> commentVoteLookup) => new
        {
            id = c.Id.ToString(),
            blogId = c.BlogId.ToString(),
            userId = c.UserId.ToString(),
            content = c.Text,
            parentId = (string?)null,
            createdAt = c.CreatedDate.ToString("o"),
            updatedAt = (c.LastModifiedDate ?? c.CreatedDate).ToString("o"),
            profiles = c.User == null ? null : new
            {
                displayName = c.User.Username,
                avatarUrl = (string?)null,
            },
            commentVotes = commentVoteLookup.TryGetValue(c.Id, out var votes) ? votes : new List<object>(),
        };

        private async Task<bool> CanAccessBlogAsync(BlogModel blog)
        {
            if (blog.Published)
                return true;

            var currentUserId = TryGetUserId();
            if (!currentUserId.HasValue)
                return false;

            if (blog.UserId == currentUserId.Value)
                return true;

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            return currentUser?.Role == UserRole.Admin;
        }

        private async Task<Dictionary<int, List<object>>> GetCommentVoteLookupAsync(IEnumerable<int> commentIds)
        {
            var ids = commentIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<object>>();

            var votes = await _context.Ranking
                .Where(r => r.Type == "comment" && ids.Contains(r.TypeId))
                .Select(r => new
                {
                    r.Id,
                    r.TypeId,
                    r.UserId,
                    VoteType = r.Like == 1 ? 1 : -1,
                })
                .ToListAsync();

            return votes
                .GroupBy(v => v.TypeId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(v => (object)new
                        {
                            id = v.Id.ToString(),
                            commentId = v.TypeId.ToString(),
                            userId = v.UserId.ToString(),
                            voteType = v.VoteType,
                        })
                        .ToList());
        }

        private int? TryGetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(claim, out var userId) ? userId : null;
        }
    }

    // ─── DTOs ──────────────────────────────────────────────────────────────────

    public record UpsertBlogDto(
        string Title,
        string Content,
        string? Excerpt,
        string? CoverImageUrl,
        bool Published);

    public record AddCommentDto(string Content);
}
