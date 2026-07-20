using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> ChangeAccountAsync(string oldPassword, string? newPassword, string? newUsername);
        bool IsAuthenticated { get; }
        string? Token { get; }
    }
}