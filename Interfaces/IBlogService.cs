using WeblogApplication.Models;

namespace WeblogApplication.Interfaces
{
    public interface IBlogService
    {
        Task<List<BlogMetaData>> GetBlogsWithMetadataAsync(string sortOrder, int page, int pageSize);
        Task<int> GetTotalBlogCountAsync();
        Task<BlogModel?> GetBlogByIdAsync(int id);
        Task<List<BlogModel>> GetBlogsByUserIdAsync(int userId);
        Task CreateBlogAsync(BlogModel blog);
        Task UpdateBlogAsync(BlogModel blog);
        Task DeleteBlogAsync(BlogModel blog);
    }
}
