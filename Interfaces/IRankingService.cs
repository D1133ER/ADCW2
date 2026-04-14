using WeblogApplication.Models;

namespace WeblogApplication.Interfaces
{
    public interface IRankingService
    {
        Task<(int Like, int Dislike)> ModifyRankAsync(int postId, string action, string type, int userId);
        Task<int> GetTotalLikesAsync();
        Task<int> GetTotalDislikesAsync();
    }
}
