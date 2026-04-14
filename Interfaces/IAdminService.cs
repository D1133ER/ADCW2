using WeblogApplication.Models;

namespace WeblogApplication.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync(string filterType);
    }
}
