using WeblogApplication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeblogApplication.Interfaces
{
    public interface IBlogService
    {
        Task<List<BlogMetaData>> GetBlogsWithMetadataAsync(string sortOrder, int page, int pageSize);
        Task<IEnumerable<BlogModel>> GetBlogsAsync(bool? published = true, int? userId = null);
        Task<int> GetTotalBlogCountAsync();
        Task<BlogModel?> GetBlogByIdAsync(int id);
        Task<BlogModel?> GetBlogDetailAsync(int id);
        Task<List<BlogModel>> GetBlogsByUserIdAsync(int userId);
        Task CreateBlogAsync(BlogModel blog);
        Task UpdateBlogAsync(BlogModel blog);
        Task DeleteBlogAsync(int id);
    }
}
