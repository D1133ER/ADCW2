using System.Threading.Tasks;

namespace WeblogApplication.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
