# ZlinksPackageSystem 集成 RuoYi 系统管理与监控 设计规格

**日期**：2026-07-11
**状态**：已批准
**目标**：在 ZlinksPackageSystem 中基于 RuoYi-Vue3-ts 设计模式，完整实现系统管理与系统监控模块。

## 1. 背景

ZlinksPackageSystem 是游戏包管理系统，已有简化版的用户、权限组、通知、操作日志模块。本规格基于 RuoYi-Vue3-ts 的成熟设计模式，全面替换/扩展为标准的 RBAC 权限模型，并补齐系统监控能力。

## 2. 范围

### 包含
- **系统管理**：用户管理、角色管理、菜单管理、部门管理、岗位管理、参数设置、通知公告、操作日志、登录日志
- **系统监控**：在线用户、服务监控、缓存监控、Druid 连接池
- **前端能力**：动态路由、按钮权限指令、字典标签、分页组件、统一布局

### 不包含
- 字典管理（dict）（Zlinks 现有业务以参数设置为主，参数设置已覆盖常见需求；如需后续扩展可追加）
- 工具模块（代码生成、接口文档）—— Knife4j 已提供接口文档
- 定时任务（job）—— 当前业务不需要
- 桌面端 (desktop/) 的对应改动

## 3. 架构决策

| 决策 | 选择 | 理由 |
|---|---|---|
| 权限模型 | RBAC (user-role-menu) | 与 RuoYi 完全对齐；支撑菜单权限、按钮权限、数据范围 |
| 用户-角色存储 | sys_user_role 关联表 | RuoYi 标准 |
| 角色-菜单存储 | sys_role_menu 关联表 | RuoYi 标准 |
| 会话存储 | Redis (新增) | 在线用户、登录限制、缓存监控都依赖 |
| 系统监控采集 | OSHI | 纯 JDK 库，跨平台 |
| 操作日志 | 自定义 @Log 注解 + AOP 切面，异步线程池落库 | 不阻塞主请求 |
| 前后端分离 | 沿用现有 Vue 3 + Spring Boot 3 | 项目现状 |
| TypeScript | 沿用现有严格度 | 不引入破坏性变更 |
| 数据库迁移 | 一次性脚本迁移 + 旧表归档为 `_legacy` | 可回退 |
| Druid 账号 | admin/admin (沿用) | 已在 application.yml |

## 4. 数据模型

### 4.1 新增表清单

| 表名 | 说明 | 关键字段 |
|---|---|---|
| `sys_user` | 用户 | user_id, dept_id, user_name, nick_name, password, phonenumber, email, avatar, status, login_ip, login_date |
| `sys_role` | 角色 | role_id, role_name, role_key, role_sort, data_scope(1-5), status |
| `sys_menu` | 菜单/权限 | menu_id, menu_name, parent_id, path, component, menu_type(M/C/F), perms, icon, visible |
| `sys_user_role` | 用户-角色 | user_id, role_id (PK) |
| `sys_role_menu` | 角色-菜单 | role_id, menu_id (PK) |
| `sys_dept` | 部门 | dept_id, parent_id, ancestors, dept_name, order_num, leader |
| ` `sys_post` | 岗位 | post_id, post_code, post_name, post_sort, status |
| `sys_config` | 参数 | config_id, config_name, config_key, config_value, config_type, is_builtin |
| `sys_notice` | 通知公告 | notice_id, notice_title, notice_type(1通知/2公告), notice_content, status |
| `sys_oper_log` | 操作日志 | oper_id, title, business_type, method, request_method, operator_type, oper_name, dept_name, oper_url, oper_ip, oper_param, json_result, status, error_msg |
| `sys_logininfor` | 登录日志 | info_id, user_name, ipaddr, login_location, browser, os, status, msg, login_time |

### 4.2 替换/废弃表

| 旧表 | 新表 | 迁移策略 |
|---|---|---|
| `users` | `sys_user` | 字段映射，迁移 username/password/real_name→nick_name，新增默认字段 |
| `permission_group` | `sys_role` + `sys_menu` | 旧权限组的 permissions 拆分为菜单权限字符串 |
| `user_group` | `sys_user_role` | 直接迁移 user_id/group_id → user_id/role_id |
| `notification` | `sys_notice` | 字段映射，title→notice_title，content→notice_content |
| `operation_log` | `sys_oper_log` | 字段映射，扩展 RuoYi 字段 |

旧表在迁移脚本中保留为 `users_legacy` / `permission_group_legacy` / `user_group_legacy` / `notification_legacy` / `operation_log_legacy`。

### 4.3 菜单树初始化

`DataInitializer` 启动时插入默认菜单：
```
系统管理 (M system)
├── 用户管理 (C system:user:list)
├── 角色管理 (C system:role:list)
├── 菜单管理 (C system:menu:list)
├── 部门管理 (C system:dept:list)
├── 岗位管理 (C system:post:list)
├── 参数设置 (C system:config:list)
└── 通知公告 (C system:notice:list)
系统监控 (M monitor)
├── 在线用户 (C monitor:online:list)
├── 操作日志 (C monitor:operlog:list)
├── 登录日志 (C monitor:logininfor:list)
├── 服务监控 (C monitor:server:list)
├── 缓存监控 (C monitor:cache:list)
└── Druid 监控 (C monitor:druid:list)
```
每个 C 节点附带按钮权限 F：新增/修改/删除/导出/查询 等。

## 5. 后端架构

### 5.1 包结构

```
com.zlinks.package_system/
├── annotation/
│   └── Log.java                       # 操作日志注解
├── aspect/
│   └── LogAspect.java                 # 操作日志切面
├── config/
│   ├── SecurityConfig.java            # 重构
│   ├── RedisConfig.java               # 新增
│   ├── MybatisPlusConfig.java
│   └── DataInitializer.java           # 重构（默认菜单+角色）
├── constant/
│   ├── UserConstants.java             # 用户常量
│   ├── CacheConstants.java            # 缓存 key 前缀
│   └── CommonConstants.java
├── controller/
│   ├── system/                        # 新增
│   │   ├── SysUserController.java
│   │   ├── SysRoleController.java
│   │   ├── SysMenuController.java
│   │   ├── SysDeptController.java
│   │   ├── SysPostController.java
│   │   ├── SysConfigController.java
│   │   └── SysNoticeController.java
│   ├── monitor/                       # 新增
│   │   ├── SysOperlogController.java
│   │   ├── SysLogininforController.java
│   │   ├── SysUserOnlineController.java
│   │   ├── ServerController.java
│   │   └── CacheController.java
│   └── AuthController.java            # 改造（login 返回 role + permissions）
├── dto/
│   ├── system/
│   └── monitor/
├── entity/
│   ├── system/                        # 新增
│   ├── monitor/                       # 新增
│   └── BaseEntity.java
├── enums/
│   ├── BusinessType.java
│   └── OperatorType.java
├── exception/
│   └── ServiceException.java          # 新增（替代 BusinessException）
├── mapper/
├── security/
│   ├── LoginUser.java                 # 新增（含 permissions 列表）
│   ├── JwtAuthenticationFilter.java   # 改造
│   ├── JwtAuthenticationEntryPoint.java
│   ├── UserDetailsImpl.java           # 改造
│   └── UserDetailsServiceImpl.java    # 改造
├── service/
│   ├── system/                        # 新增 ISys*Service + impl
│   └── monitor/                       # 新增
└── util/
    ├── RedisUtils.java                # 新增
    ├── IpUtils.java                   # 新增
    ├── ServletUtils.java
    ├── SecurityUtils.java             # 新增
    └── ...
```

### 5.2 安全流程

1. 用户登录 → AuthController 验证密码 → 生成 token → 返回 user + role + permissions
2. JwtAuthenticationFilter 解析 token → 加载 LoginUser → 设置到 SecurityContext
3. Controller 方法上 @PreAuthorize("hasAuthority('system:user:list')") 校验
4. 异步线程池 LogAspect 捕获 @Log 注解的方法调用，记录操作日志

### 5.3 通用响应

- `Result<T>` 沿用现有（code/msg/data）
- `PageResult<T>` 沿用现有（rows/total/size/current）
- `TableDataInfo<T>` 新增（兼容 RuoYi 风格：total/rows/code/msg），用于 `/list` 列表接口

### 5.4 关键 API 路径

```
POST   /login                                       # 登录（已存在，改造）
GET    /getInfo                                     # 获取当前用户信息+角色+权限（新增）
GET    /getRouters                                  # 获取动态路由（新增）

# 系统管理
/system/user/list /system/user/{id} /system/user [POST,PUT,DELETE]
/system/user/profile /system/user/profile/updatePwd /system/user/profile/avatar
/system/user/resetPwd /system/user/changeStatus /system/user/authRole /system/user/deptTree
/system/role/list /system/role/{id} /system/role [POST,PUT,DELETE]
/system/role/dataScope /system/role/changeStatus
/system/role/authUser/{allocatedList,unallocatedList,cancel,cancelAll,selectAll}
/system/menu/list /system/menu/{id} /system/menu [POST,PUT,DELETE]
/system/menu/treeselect /system/menu/roleMenuTreeselect/{roleId}
/system/dept/list /system/dept/{id} /system/dept [POST,PUT,DELETE] /system/dept/treeselect
/system/post/list /system/post/{id} /system/post [POST,PUT,DELETE]
/system/config/list /system/config/{id} /system/config [POST,PUT,DELETE] /system/config/key/{configKey}
/system/notice/list /system/notice/{id} /system/notice [POST,PUT,DELETE]

# 系统监控
/monitor/operlog/list /monitor/operlog/{id} [DELETE] /monitor/operlog/clean [DELETE]
/monitor/logininfor/list /monitor/logininfor/{id} [DELETE] /monitor/logininfor/clean [DELETE] /monitor/logininfor/unlock [PUT]
/monitor/online/list /monitor/online/{tokenId} [DELETE]
/monitor/server [GET]
/monitor/cache [GET] /monitor/cache/getNames /monitor/cache/getKeys/{cacheName} /monitor/cache/getValue/{cacheName}/{cacheKey}
/monitor/cache/clearCacheName/{cacheName} [DELETE] /monitor/cache/clearCacheKey/{cacheKey} [DELETE] /monitor/cache/clearCacheAll [DELETE]
/druid/* （已存在）
```

## 6. 前端架构

### 6.1 目录结构

```
src/
├── api/
│   ├── login.ts                      # 改造（登录返回 role + permissions）
│   ├── system/
│   │   ├── user.ts role.ts menu.ts dept.ts post.ts config.ts notice.ts
│   ├── monitor/
│   │   ├── operlog.ts logininfor.ts online.ts server.ts cache.ts
├── components/
│   ├── Pagination/index.vue          # 通用分页
│   ├── DictTag/index.vue             # 字典色块
│   ├── FileUpload/index.vue
│   └── SvgIcon/index.vue             # 图标
├── directive/
│   ├── hasPermi.ts                   # v-hasPermi
│   └── hasRole.ts                    # v-hasRole
├── layout/
│   ├── index.vue                     # 整体布局
│   ├── components/
│   │   ├── Sidebar/index.vue         # 侧边栏
│   │   ├── Navbar.vue                # 顶部
│   │   ├── TagsView.vue              # 标签页
│   │   ├── Breadcrumb.vue
│   │   ├── AppMain.vue
│   │   └── Settings/index.vue
├── plugins/
├── router/
│   └── index.ts                      # 静态 + 动态加载
├── store/
│   ├── index.ts
│   └── modules/
│       ├── user.ts                   # 改造
│       ├── permission.ts             # 新增（动态路由）
│       ├── dict.ts                   # 新增
│       ├── tagsView.ts               # 新增
│       └── settings.ts               # 新增
├── types/
│   ├── api/
│   │   ├── system.d.ts
│   │   └── monitor.d.ts
│   └── index.ts
├── utils/
│   ├── request.ts                    # 改造（注入 permissions 到 store）
│   ├── auth.ts                       # token 存储
│   ├── permission.ts                 # 权限校验工具
│   ├── dict.ts                       # 字典工具
│   ├── ruoyi.ts                      # parseStrEmpty 等
│   └── validate.ts
└── views/
    ├── system/
    │   ├── user/        {index.vue, authRole.vue, profile/*}
    │   ├── role/        {index.vue, authUser.vue, selectUser.vue}
    │   ├── menu/        {index.vue}
    │   ├── dept/        {index.vue}
    │   ├── post/        {index.vue}
    │   ├── config/      {index.vue}
    │   └── notice/      {index.vue, ReadUsers.vue}
    ├── monitor/
    │   ├── online/      {index.vue}
    │   ├── operlog/     {index.vue, detail.vue}
    │   ├── logininfor/  {index.vue}
    │   ├── server/      {index.vue}
    │   ├── cache/       {index.vue, list.vue}
    │   └── druid/       {index.vue}
    ├── login.vue        # 改造（登录后跳转 + 动态加载路由）
    └── error/
        ├── 401.vue 404.vue
```

### 6.2 关键能力

- **动态路由**：登录后调用 `/getRouters` 获取菜单树 → 生成路由 → `router.addRoute()`
- **按钮权限**：`<button v-hasPermi="['system:user:list']">` 校验
- **字典缓存**：store.dict 缓存常用字典，按需 loadDict
- **分页组件**：`<Pagination :total="total" v-model:page="queryParams.pageNum" />`
- **图标**：使用 svg-icon，按需引入

## 7. 三 Agent 任务分工（方案 ①）

### Agent 1：基础与 RBAC 核心（先行）

**后端任务**：
1. `scripts/migrate-to-ruoyi.sql` 迁移脚本（含旧表数据迁移 + 旧表归档）
2. 实体：`SysUser / SysRole / SysMenu / SysDept / SysPost / SysUserRole / SysRoleMenu`
3. `BaseEntity` 增强（createBy、updateBy 等）
4. `LoginUser` + `UserDetailsImpl` + `UserDetailsServiceImpl` 改造（返回 roles + permissions）
5. `SecurityConfig` 重构（基于 RBAC + @PreAuthorize）
6. `JwtAuthenticationFilter` 增强（解析 token 后设置完整 LoginUser）
7. `JwtUtil` 增强（claims 包含 userId、username）
8. `@Log` 注解 + `LogAspect` 切面 + 异步线程池配置
9. `BusinessType` / `OperatorType` 枚举
10. `SysUserController / SysRoleController / SysMenuController / SysDeptController / SysPostController`
11. `DataInitializer` 重构：默认菜单树 + admin 绑定超级管理员角色
12. 删除/废弃：`PermissionGroupController / UserGroupController` 及其前后端代码
13. `application.yml` 增加 Redis 配置 + 引入 `spring-boot-starter-data-redis`
14. 改造 `AuthController`：login 返回 `LoginResult { token, user, roles, permissions }`

**前端任务**：
1. `package.json` 增加 `echarts`、`pinia-plugin-persistedstate`
2. 完整布局（Sidebar、Navbar、TagsView、Breadcrumb、AppMain）
3. `pinia-plugin-persistedstate` 配置（user store 持久化 token）
4. `src/utils/request.ts` 改造：401 跳转登录、注入权限
5. `directive/hasPermi.ts` + `directive/hasRole.ts` + 在 `main.ts` 注册
6. `store/permission.ts`：动态路由生成 + addRoute
7. `router/index.ts`：拆分 constantRoutes + dynamicRoutes
8. `views/login.vue` 改造：登录后调用 `generateRoutes` 加载动态菜单
9. `views/system/user/{index.vue, authRole.vue, profile/*}` 用户管理
10. `views/system/role/{index.vue, authUser.vue, selectUser.vue}` 角色管理
11. `views/system/menu/index.vue` 菜单管理
12. `views/system/dept/index.vue` 部门管理
13. `views/system/post/index.vue` 岗位管理
14. 个人中心 + 修改密码 + 头像上传
15. 通用组件 `Pagination/index.vue`
16. 通用组件 `DictTag/index.vue`（骨架，Agent 2 完善）

**验证**：登录、按权限显示菜单、用户/角色/菜单 CRUD、权限分配、密码重置、状态切换

### Agent 2：系统管理业务（与 Agent 3 并行）

**后端任务**：
1. `SysConfigController` + `SysConfigService`（参数设置 CRUD + 按 configKey 查询 + 内置参数校验）
2. `SysNoticeController` + `SysNoticeService`（通知公告 CRUD + ReadUsers）
3. 重构 `OperationLogController` → `SysOperlogController`（字段对齐 RuoYi）
4. `SysOperLogService` + 异步落库接入 LogAspect
5. `SysLogininforController` + `SysLogininforService`（查询/清空/解锁）
6. `LoginLogService`：监听 `AuthenticationSuccessEvent` / `AuthenticationFailureEvent`
7. 数据迁移：现有 `notification`/`operation_log` → `sys_notice`/`sys_oper_log`

**前端任务**：
1. `views/system/config/index.vue` 参数设置
2. `views/system/notice/index.vue` + `ReadUsers.vue` 通知公告
3. `views/monitor/operlog/index.vue` + `detail.vue` 操作日志
4. `views/monitor/logininfor/index.vue` 登录日志
5. 完善 `<DictTag>` 组件（接入 dict store）
6. 完善 `<FileUpload>` 组件（头像、附件）

**验证**：参数 CRUD、通知公告 CRUD、日志查询/清空/解锁、字典 tag 渲染

### Agent 3：系统监控（与 Agent 2 并行）

**后端任务**：
1. `pom.xml` 增加 `oshi-core` 依赖
2. `RedisConfig` 配置（连接工厂 + RedisTemplate）
3. `RedisUtils` 工具类
4. `SysUserOnlineService`：基于 Redis 存储 token → user 映射
5. `SysUserOnlineController`：列表 + 强退（删除 token）
6. `JwtAuthenticationFilter` 改造：登录成功时写入 Redis `login_tokens:{token}` → user 信息
7. `ServerController`：OSHI 采集 CPU/内存/磁盘/JVM/系统信息，返回 `ServerInfo VO`
8. `CacheController`：Redis info + keys + get + delete + clear all
9. Druid 监控：已在 application.yml，确认可用
10. `IpUtils`：离线 IP 库解析登录地点（可选 MVP：用 mock 数据）

**前端任务**：
1. `views/monitor/online/index.vue` 在线用户（强制下线）
2. `views/monitor/server/index.vue` 服务监控（CPU/内存 ECharts 可视化）
3. `views/monitor/cache/index.vue` + `list.vue` 缓存监控（详情、命令执行、清空）
4. `views/monitor/druid/index.vue` Druid 监控（外链 iframe `/druid/index.html`）
5. `api/monitor/*.ts` 全部 API

**验证**：在线列表、强退、系统信息、缓存详情/清空、Druid 可访问

## 8. 执行顺序

```
1. Agent 1 串行执行（基础设施）
   ↓ 完成 + 两阶段审查通过
2. Agent 2 ‖ Agent 3 并行执行（业务 + 监控）
   ↓ 都完成 + 审查通过
3. 最终集成验证：登录 → 各模块 CRUD → 权限校验 → 监控数据
```

## 9. 测试与验证

- **Service 层**：每个 Service 关键方法编写单元测试
- **Controller 层**：用 @WebMvcTest 验证权限 + 响应
- **端到端**：登录 → 各菜单 → 各 CRUD 操作

## 10. 风险与回退

| 风险 | 缓解措施 |
|---|---|
| 数据库迁移失败 | 迁移脚本事务包裹 + 旧表保留 + 提供 rollback.sql |
| 现有 notification/operation_log 数据丢失 | 迁移前全量备份 |
| @PreAuthorize 漏配导致 403 | 后端单测覆盖 |
| 前端 TypeScript 编译错误 | strict + 全量 `vue-tsc --noEmit` |
| Redis 不可用 | 提供本地 fallback（仅影响在线用户） |
| OSHI 跨平台差异 | 测试 Linux/Windows |

## 11. 交付物清单

- `scripts/migrate-to-ruoyi.sql`
- `backend/src/main/java/com/zlinks/package_system/` （按包结构新增/改造文件）
- `frontend/src/` （按目录结构新增/改造文件）
- `docs/superpowers/specs/2026-07-11-zlinks-ruoyi-system-design.md`（本文件）
- `docs/superpowers/plans/2026-07-11-zlinks-ruoyi-implementation.md`
- Git commits（每 Agent 提交一次）