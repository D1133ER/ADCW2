using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeblogApplication.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<dynamic>> GetUnreadAlertsForUserAsync(int userId);
        Task<bool> MarkAsReadAsync(int alertId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task CreateAlertAsync(int blogPostId, string message);
    }
}
