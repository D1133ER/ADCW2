using System.Threading.Tasks;
using WeblogApplication.Models;

namespace WeblogApplication.Interfaces
{
    public interface IUserService
    {
        Task<UserModel?> AuthenticateAsync(string email, string password);
        Task<UserModel> RegisterAsync(string email, string username, string password);
        Task<UserModel?> GetUserByIdAsync(int userId);
        Task<UserModel?> GetUserByEmailAsync(string email);
        Task<bool> UpdateProfileAsync(int userId, string username, string? bio);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> DeleteUserAsync(int userId);
    }
}
