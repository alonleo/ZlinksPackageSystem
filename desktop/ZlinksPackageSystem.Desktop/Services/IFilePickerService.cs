using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IFilePickerService
    {
        Task<string?> PickImageFileAsync();
        Task<string?> PickFontFileAsync();
        Task<string?> PickScriptFileAsync();
        Task<string?> PickDirectoryAsync();
        Task<string?> PickFileAsync(string title, string pattern);
    }
}
