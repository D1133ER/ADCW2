using WeblogApplication.Models;

namespace WeblogApplication.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentModel>> GetCommentsByBlogIdAsync(int blogId);
        Task<CommentModel> PostCommentAsync(int blogId, string text, int userId, string username);
        Task<bool> EditCommentAsync(int commentId, string newText, int userId);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
    }
}
