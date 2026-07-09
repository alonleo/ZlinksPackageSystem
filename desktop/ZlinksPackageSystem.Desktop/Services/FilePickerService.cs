using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class FilePickerService : IFilePickerService
    {
        public async Task<string?> PickImageFileAsync()
        {
            return await PickAsync("选择背景图片",
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" }
                });
        }

        public async Task<string?> PickFontFileAsync()
        {
            return await PickAsync("选择字体文件",
                new FilePickerFileType("字体文件")
                {
                    Patterns = new[] { "*.ttf", "*.otf" }
                });
        }

        private static async Task<string?> PickAsync(string title, FilePickerFileType fileType)
        {
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var topLevel = lifetime?.MainWindow;

            if (topLevel == null) return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { fileType }
            });

            return files?.FirstOrDefault()?.Path?.LocalPath;
        }
    }
}