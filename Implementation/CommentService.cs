using Microsoft.EntityFrameworkCore;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Implementation
{
    public class CommentService : ICommentService
    {
        private readonly WeblogApplicationDbContext _context;

        public CommentService(WeblogApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommentModel>> GetCommentsByBlogIdAsync(int blogId)
        {
            return await _context.Comments
                .Where(c => c.BlogId == blogId)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<CommentModel> PostCommentAsync(int blogId, string text, int userId, string username)
        {
            var comment = new CommentModel
            {
                BlogId = blogId,
                Text = text,
                CreatedBy = username,
                UserId = userId,
                CreatedDate = DateTime.Now,
            };

            var blog = await _context.Blogs.FindAsync(blogId);
            if (blog != null)
            {
                blog.Popularity++;
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> EditCommentAsync(int commentId, string newText, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null || comment.UserId != userId)
                return false;

            comment.Text = newText;
            comment.LastModifiedDate = DateTime.Now;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null || comment.UserId != userId)
                return false;

            var relatedRanks = await _context.Ranking
                .Where(v => v.TypeId == commentId && v.Type == "comment")
                .ToListAsync();

            var blog = await _context.Blogs.FindAsync(comment.BlogId);
            if (blog != null)
            {
                blog.Popularity--;
            }

            _context.Comments.Remove(comment);
            _context.Ranking.RemoveRange(relatedRanks);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
