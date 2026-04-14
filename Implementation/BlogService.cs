using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

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

            // Single query with aggregation via a left join — avoids N+1
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

        public async Task<int> GetTotalBlogCountAsync()
        {
            return await _context.Blogs.CountAsync(b => b.Published);
        }

        public async Task<BlogModel?> GetBlogByIdAsync(int id)
        {
            return await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
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

        public async Task DeleteBlogAsync(BlogModel blog)
        {
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();
        }
    }
}
