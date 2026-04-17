using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeblogApplication.Implementation
{
    public class BlogService : IBlogService
    {
        private readonly WeblogApplicationDbContext _context;

        public BlogService(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BlogMetaData>> GetBlogsWithMetadataAsync(string sortOrder, int page, int pageSize)
        {
            var blogsQuery = _context.Blogs.Where(b => b.Published).AsQueryable();

            switch (sortOrder)
            {
                case "date":
                    blogsQuery = blogsQuery.OrderByDescending(b => b.CreatedAt);
                    break;
                case "popularity":
                    blogsQuery = blogsQuery.OrderByDescending(b => b.Popularity);
                    break;
                default: // "random"
                    blogsQuery = blogsQuery.OrderBy(_ => Guid.NewGuid());
                    break;
            }

            var result = await blogsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BlogMetaData
                {
                    Blog = b,
                    CommentsCount = b.Comments.Count,
                    Like = _context.Ranking
                        .Where(v => v.TypeId == b.Id && v.Type == "blog")
                        .Sum(v => (int?)v.Like) ?? 0,
                    Dislike = _context.Ranking
                        .Where(v => v.TypeId == b.Id && v.Type == "blog")
                        .Sum(v => (int?)v.Dislike) ?? 0
                })
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<BlogModel>> GetBlogsAsync(bool? published = true, int? userId = null)
        {
            var query = _context.Blogs.Include(b => b.User).AsQueryable();

            if (published.HasValue)
            {
                query = query.Where(b => b.Published == published.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(b => b.UserId == userId.Value);
            }

            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<int> GetTotalBlogCountAsync()
        {
            return await _context.Blogs.CountAsync(b => b.Published);
        }

        public async Task<BlogModel?> GetBlogByIdAsync(int id)
        {
            return await _context.Blogs.FindAsync(id);
        }

        public async Task<BlogModel?> GetBlogDetailAsync(int id)
        {
            return await _context.Blogs
                .Include(b => b.User)
                .Include(b => b.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<BlogModel>> GetBlogsByUserIdAsync(int userId)
        {
            return await _context.Blogs.Where(b => b.UserId == userId).ToListAsync();
        }

        public async Task CreateBlogAsync(BlogModel blog)
        {
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBlogAsync(BlogModel blog)
        {
            _context.Blogs.Update(blog);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBlogAsync(int id)
        {
            var blogPost = await _context.Blogs.FindAsync(id);
            if (blogPost == null) return;

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
            _context.Blogs.Remove(blogPost);
            
            await _context.SaveChangesAsync();
        }
    }
}
