using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Implementation
{
    public class UserService : IUserService
    {
        private readonly WeblogApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public UserService(WeblogApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<UserModel?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                return null;
            return user;
        }

        public async Task<UserModel> RegisterAsync(string email, string username, string password)
        {
            var user = new UserModel
            {
                Email = email,
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Blogger, // Default role for public signup
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<UserModel?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string username, string? bio)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Username = username;
            user.Bio = bio;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            user.passwordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            // Note: In a real app, the URL would be passed in or constructed from config. 
            // For now, we rely on the controller to construct the email body if it needs more context, 
            // but we can provide a basic implementation or just return true and let the controller handle content.
            // Let's keep the actual email body construction in the controller for URL flexibility, 
            // or we could add a ResetUrl parameter to this method.
            
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.passwordResetToken == token && u.PasswordResetExpiry > DateTime.UtcNow);
            if (user == null) return false;

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.passwordResetToken = null;
            user.PasswordResetExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Comments)
                .Include(u => u.Blogs)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            // Remove related ranking entries that reference this user
            var userCommentIds = user.Comments.Select(c => c.Id).ToList();
            var userBlogIds = user.Blogs.Select(b => b.Id).ToList();
            var blogCommentIds = await _context.Comments
                .Where(c => userBlogIds.Contains(c.BlogId))
                .Select(c => c.Id)
                .ToListAsync();
            var rankingCommentIds = userCommentIds.Concat(blogCommentIds).Distinct().ToList();

            var userRankings = _context.Ranking.Where(r =>
                r.UserId == userId ||
                (r.Type == "blog" && userBlogIds.Contains(r.TypeId)) ||
                ((r.Type == "comment" || r.Type == "comments") && rankingCommentIds.Contains(r.TypeId)));
            _context.Ranking.RemoveRange(userRankings);

            var relatedAlerts = _context.Alert.Where(a => userBlogIds.Contains(a.BlogPostId));
            _context.Alert.RemoveRange(relatedAlerts);

            var userComments = _context.Comments.Where(c => c.UserId == userId);
            _context.Comments.RemoveRange(userComments);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
