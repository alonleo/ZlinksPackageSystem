using System.Collections.Generic;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 编程语言运行时环境检测服务
    /// </summary>
    public interface IRuntimeEnvironmentService
    {
        /// <summary>
        /// 检测系统上所有受支持的运行时环境
        /// </summary>
        Task<List<RuntimeEnvironment>> DetectAllAsync();

        /// <summary>
        /// 检测指定语言的运行时环境
        /// </summary>
        Task<RuntimeEnvironment> DetectAsync(string language);

        /// <summary>
        /// 重新检测某个语言（用于新建工具时实时检测）
        /// </summary>
        Task<RuntimeEnvironment> ReDetectAsync(string language);
    }
}
