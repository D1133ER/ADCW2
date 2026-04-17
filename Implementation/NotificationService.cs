using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly WeblogApplicationDbContext _context;

        public NotificationService(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<dynamic>> GetUnreadAlertsForUserAsync(int userId)
        {
            return await _context.Blogs
                .Join(_context.Alert,
                    blog => blog.Id,
                    notification => notification.BlogPostId,
                    (blog, notification) => new { Blog = blog, Alert = notification })
                .Where(joinResult => joinResult.Blog.UserId == userId && !joinResult.Alert.isRead)
                .Select(joinResult => new
                {
                    Id = joinResult.Alert.Id,
                    BlogTitle = joinResult.Blog.Title,
                    Message = joinResult.Alert.Message,
                    isRead = joinResult.Alert.isRead,
                    CreatedAt = joinResult.Alert.CreatedAt
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int alertId, int userId)
        {
            var alert = await _context.Alert
                .Include(a => a.BlogPost)
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null || alert.BlogPost?.UserId != userId)
                return false;

            alert.isRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadAlerts = await _context.Alert
                .Include(a => a.BlogPost)
                .Where(a => a.BlogPost != null && a.BlogPost.UserId == userId && !a.isRead)
                .ToListAsync();

            if (!unreadAlerts.Any()) return true;

            foreach (var alert in unreadAlerts)
            {
                alert.isRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateAlertAsync(int blogPostId, string message)
        {
            var notification = new AlertModel
            {
                BlogPostId = blogPostId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                isRead = false
            };

            _context.Alert.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
