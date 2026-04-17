using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeblogApplication.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly WeblogApplicationDbContext _context;

        public AdminService(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync(string filterType)
        {
            var totalBlogs = await _context.Blogs.CountAsync();
            var totalComments = await _context.Comments.CountAsync();

            var totalLikes = await _context.Ranking
                .Where(r => r.Type == "blog")
                .SumAsync(r => (int?)r.Like) ?? 0;

            var totalDislikes = await _context.Ranking
                .Where(r => r.Type == "blog")
                .SumAsync(r => (int?)r.Dislike) ?? 0;

            var blogsQuery = _context.Blogs.AsQueryable();
            blogsQuery = ApplyMonthFilter(blogsQuery, filterType);

            var blogDetails = await blogsQuery
                .Select(blog => new BlogViewModel
                {
                    BlogTitle = blog.Title,
                    ImagePath = blog.ImagePath,
                    TotalComments = blog.Comments.Count,
                    TotalLikes = _context.Ranking
                        .Where(v => v.Type == "blog" && v.TypeId == blog.Id)
                        .Sum(v => (int?)v.Like) ?? 0,
                    TotalDislikes = _context.Ranking
                        .Where(v => v.Type == "blog" && v.TypeId == blog.Id)
                        .Sum(v => (int?)v.Dislike) ?? 0,
                    Popularity = blog.Popularity
                })
                .OrderByDescending(blog => blog.Popularity)
                .Take(10)
                .ToListAsync();

            var bloggersQuery = _context.Users.Where(u => u.Role != UserRole.Admin);
            if (filterType == "thisMonth")
            {
                var currentMonth = DateTime.Today.Month;
                var currentYear = DateTime.Today.Year;
                bloggersQuery = bloggersQuery.Where(u =>
                    u.Blogs.Any(b => b.CreatedAt.Year == currentYear && b.CreatedAt.Month == currentMonth));
            }
            else if (filterType != "all")
            {
                int? month = ParseMonth(filterType);
                if (month.HasValue)
                {
                    bloggersQuery = bloggersQuery.Where(u =>
                        u.Blogs.Any(b => b.CreatedAt.Year == DateTime.Today.Year && b.CreatedAt.Month == month.Value));
                }
            }

            var topBloggers = await bloggersQuery
                .Select(user => new TopBloggerViewModel
                {
                    Username = user.Username,
                    Email = user.Email,
                    TotalPopularity = user.Blogs.Sum(b => (int?)b.Popularity) ?? 0
                })
                .OrderByDescending(u => u.TotalPopularity)
                .Take(10)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                TotalBlogs = totalBlogs,
                TotalLikes = totalLikes,
                TotalDislikes = totalDislikes,
                TotalComments = totalComments,
                PostDetails = blogDetails,
                TopBloggers = topBloggers
            };
        }

        public async Task<object> GetTotalStatsAsync()
        {
            return new
            {
                users = await _context.Users.CountAsync(),
                blogs = await _context.Blogs.CountAsync(),
                comments = await _context.Comments.CountAsync()
            };
        }

        public async Task<IEnumerable<UserModel>> GetRecentUsersAsync(int limit)
        {
            return await _context.Users
                .OrderByDescending(u => u.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogModel>> GetRecentBlogsAsync(int limit)
        {
            return await _context.Blogs
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        private static IQueryable<BlogModel> ApplyMonthFilter(IQueryable<BlogModel> query, string filterType)
        {
            if (string.IsNullOrEmpty(filterType) || filterType == "all")
                return query;

            if (filterType == "thisMonth")
            {
                var currentMonth = DateTime.Today.Month;
                var currentYear = DateTime.Today.Year;
                return query.Where(b => b.CreatedAt.Year == currentYear && b.CreatedAt.Month == currentMonth);
            }

            int? month = ParseMonth(filterType);
            if (month.HasValue)
            {
                return query.Where(b => b.CreatedAt.Year == DateTime.Today.Year && b.CreatedAt.Month == month.Value);
            }

            return query;
        }

        private static int? ParseMonth(string filterType)
        {
            var titleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filterType.ToLower());
            if (DateTime.TryParseExact(titleCase, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
                return parsedMonth.Month;

            if (int.TryParse(filterType, out int month) && month >= 1 && month <= 12)
                return month;

            return null;
        }
    }
}
