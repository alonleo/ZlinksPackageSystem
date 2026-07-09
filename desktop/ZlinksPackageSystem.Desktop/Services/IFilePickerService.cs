using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IFilePickerService
    {
        Task<string?> PickImageFileAsync();
    }
}
