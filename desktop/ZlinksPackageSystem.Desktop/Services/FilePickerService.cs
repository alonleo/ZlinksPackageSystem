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
            return await PickFileAsync("选择背景图片",
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" }
                });
        }

        public async Task<string?> PickFontFileAsync()
        {
            return await PickFileAsync("选择字体文件",
                new FilePickerFileType("字体文件")
                {
                    Patterns = new[] { "*.ttf", "*.otf" }
                });
        }

        public async Task<string?> PickScriptFileAsync()
        {
            return await PickFileAsync("选择脚本",
                new FilePickerFileType("脚本文件")
                {
                    Patterns = new[] { "*.py", "*.js", "*.ts", "*.java", "*.go", "*.ps1", "*.sh", "*.bat", "*.cmd" }
                });
        }

        public async Task<string?> PickDirectoryAsync()
        {
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var topLevel = lifetime?.MainWindow;
            if (topLevel == null) return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择目录",
                AllowMultiple = false
            });
            return folders?.FirstOrDefault()?.Path?.LocalPath;
        }

        public async Task<string?> PickFileAsync(string title, string pattern)
        {
            return await PickFileAsync(title, new FilePickerFileType("文件")
            {
                Patterns = new[] { pattern }
            });
        }

        private static async Task<string?> PickFileAsync(string title, FilePickerFileType fileType)
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
