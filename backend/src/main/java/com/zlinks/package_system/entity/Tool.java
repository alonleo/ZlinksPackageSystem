package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("tool")
public class Tool extends BaseEntity {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    /** 工具名称 */
    private String name;
    /** 工具描述 */
    private String description;
    /** 工具分类 */
    private String category;
    /** 版本号 */
    private String version;
    /** 状态（如：未运行 / 运行中 / 已完成 / 失败） */
    private String status;
    /** 负责人 */
    private String manager;

    /** 运行模式：Script / LocalExecutable */
    private String runMode;
    /** 编程语言（脚本模式专用）：python / node / java / go / powershell / bash / dotnet */
    private String language;
    /** 解释器路径（脚本模式专用） */
    private String interpreterPath;
    /** 脚本绝对路径（脚本模式专用） */
    private String scriptPath;
    /** 可执行程序绝对路径（本地可执行模式专用） */
    private String executablePath;
    /** 工作目录 */
    private String workingDirectory;
    /** 额外环境变量（KEY=VALUE，一行一个） */
    private String environmentVariables;
    /** 默认参数前缀（"--"、"/" 等） */
    private String defaultArgumentPrefix;

    /** Git 仓库 URL（仅新建时填写，可选） */
    private String gitUrl;
    /** 克隆目标父目录（仅新建时填写，可选） */
    private String cloneDirectory;

    /** 参数列表 JSON（List<ToolArgument>） */
    private String argumentsJson;
    /** 通知配置 JSON（NotificationConfig） */
    private String notificationJson;
    /** 是否系统内置工具（后端管理员在后台系统中标记；1=系统内置，0=用户工具） */
    private Integer isSystemBuiltin;

    /** 是否启动前自动创建 Python 虚拟环境（仅 Language=python 生效；包装类 Boolean 让旧记录反序列化为 null） */
    private Boolean createVenv;
    /** Python 虚拟环境目录；空表示默认 {workingDirectory}/.venv */
    private String venvDirectory;
    /** requirements.txt 路径 */
    private String requirementsPath;
    /** pip 镜像源 URL */
    private String pipMirrorUrl;
}
