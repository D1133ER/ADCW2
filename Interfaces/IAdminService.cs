using WeblogApplication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeblogApplication.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync(string filterType);
        Task<object> GetTotalStatsAsync();
        Task<IEnumerable<UserModel>> GetRecentUsersAsync(int limit);
        Task<IEnumerable<BlogModel>> GetRecentBlogsAsync(int limit);
    }
}
