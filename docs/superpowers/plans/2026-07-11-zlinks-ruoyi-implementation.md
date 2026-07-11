# ZlinksPackageSystem 集成 RuoYi 系统管理与监控 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。
>
> **设计规格**：`docs/superpowers/specs/2026-07-11-zlinks-ruoyi-system-design.md`
> **参考代码源**：`/home/leo/文档/zl/RuoYi-Vue3-ts/`（前端） + RuoYi-Vue 后端（标准 RuoYi 实体/API 模式）
> **目标项目**：`/home/leo/文档/zl/ZlinksPackageSystem/`

**目标**：基于 RuoYi-Vue3-ts 设计模式，在 ZlinksPackageSystem 中完整实现系统管理与系统监控模块。

**架构**：后端 Spring Boot 3.3.7 + MyBatis-Plus + Spring Security + Redis + OSHI；前端 Vue 3 + TS + Element Plus + Pinia + ECharts；权限模型 user-role-menu 三表 RBAC。

**技术栈**：Java 21、Spring Boot 3.3.7、MyBatis-Plus 3.5.7、Redis (Spring Data Redis)、OSHI、Vue 3.4、TypeScript 5.3、Element Plus 2.6、Pinia 2.1、Vue Router 4.3、ECharts 5、Axios。

---

## 文件结构总览

### 后端新增/改造文件

```
backend/src/main/java/com/zlinks/package_system/
├── annotation/Log.java                              [新建]
├── aspect/LogAspect.java                            [新建]
├── config/
│   ├── SecurityConfig.java                          [改造]
│   ├── RedisConfig.java                             [新建]
│   └── DataInitializer.java                         [改造]
├── constant/
│   ├── UserConstants.java                           [新建]
│   ├── CacheConstants.java                          [新建]
│   └── CommonConstants.java                         [新建]
├── controller/
│   ├── AuthController.java                          [改造]
│   ├── system/
│   │   ├── SysUserController.java                  [新建]
│   │   ├── SysRoleController.java                  [新建]
│   │   ├── SysMenuController.java                  [新建]
│   │   ├── SysDeptController.java                  [新建]
│   │   ├── SysPostController.java                  [新建]
│   │   ├── SysConfigController.java                [新建]
│   │   └── SysNoticeController.java                [新建]
│   └── monitor/
│       ├── SysOperlogController.java               [新建]
│       ├── SysLogininforController.java            [新建]
│       ├── SysUserOnlineController.java            [新建]
│       ├── ServerController.java                   [新建]
│       └── CacheController.java                    [新建]
├── dto/
│   ├── system/                                      [新建包，多个 DTO]
│   └── monitor/                                     [新建包，多个 DTO]
├── entity/
│   ├── BaseEntity.java                              [改造]
│   ├── system/                                      [新建包，10+ 实体]
│   └── monitor/                                     [新建包，3+ 实体]
├── enums/
│   ├── BusinessType.java                            [新建]
│   └── OperatorType.java                            [新建]
├── exception/ServiceException.java                  [新建]
├── security/
│   ├── LoginUser.java                               [新建]
│   ├── JwtAuthenticationFilter.java                [改造]
│   ├── UserDetailsImpl.java                         [改造]
│   └── UserDetailsServiceImpl.java                 [改造]
├── service/
│   ├── system/                                      [新建包，ISys*Service + impl]
│   └── monitor/                                     [新建包，IMonitor*Service + impl]
└── util/
    ├── RedisUtils.java                              [新建]
    ├── IpUtils.java                                 [新建]
    ├── SecurityUtils.java                           [新建]
    └── ServletUtils.java                            [新建]

backend/src/main/resources/
├── application.yml                                  [改造：增加 Redis 配置]
└── scripts/migrate-to-ruoyi.sql                     [新建]
```

### 前端新增/改造文件

```
frontend/src/
├── api/
│   ├── login.ts                                     [改造]
│   ├── system/{user,role,menu,dept,post,config,notice}.ts   [新建]
│   └── monitor/{operlog,logininfor,online,server,cache}.ts  [新建]
├── components/
│   ├── Pagination/index.vue                         [新建]
│   ├── DictTag/index.vue                            [新建]
│   └── FileUpload/index.vue                         [新建]
├── directive/
│   ├── hasPermi.ts                                  [新建]
│   └── hasRole.ts                                   [新建]
├── layout/
│   ├── index.vue                                    [新建/改造]
│   └── components/{Sidebar,Navbar,TagsView,Breadcrumb,AppMain}/  [新建]
├── router/index.ts                                  [改造：动态路由]
├── store/
│   ├── index.ts                                     [新建]
│   └── modules/{user,permission,dict,tagsView,settings}.ts  [新建/改造]
├── types/
│   ├── api/system.d.ts                              [新建]
│   ├── api/monitor.d.ts                             [新建]
│   └── index.ts                                     [改造]
├── utils/
│   ├── request.ts                                   [改造]
│   ├── auth.ts                                      [新建]
│   ├── permission.ts                                [新建]
│   ├── dict.ts                                      [新建]
│   └── ruoyi.ts                                     [新建]
└── views/
    ├── login.vue                                    [改造]
    ├── system/
    │   ├── user/{index,authRole}.vue + profile/*    [新建]
    │   ├── role/{index,authUser,selectUser}.vue     [新建]
    │   ├── menu/index.vue                           [新建]
    │   ├── dept/index.vue                           [新建]
    │   ├── post/index.vue                           [新建]
    │   ├── config/index.vue                         [新建]
    │   └── notice/{index,ReadUsers}.vue             [新建]
    └── monitor/
        ├── online/index.vue                         [新建]
        ├── operlog/{index,detail}.vue               [新建]
        ├── logininfor/index.vue                     [新建]
        ├── server/index.vue                         [新建]
        ├── cache/{index,list}.vue                   [新建]
        └── druid/index.vue                          [新建]
```

---

## 执行顺序

```
Agent 1（基础与 RBAC 核心）── 串行
   ↓ 完成后
Agent 2（系统管理业务）── 并行 ──┐
                                ├── 整合验证
Agent 3（系统监控）    ── 并行 ──┘
```

---

# 任务 1：Agent 1 基础与 RBAC 核心（先行）

**目标**：建立 RuoYi 风格的 RBAC 数据模型、安全框架、布局与路由、个人中心、用户/角色/菜单/部门/岗位管理。完成后其他 Agent 才能并行。

**参考源**：
- 前端：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/views/system/user/`、`role/`、`menu/`、`dept/`、`post/`、`user/profile/`
- 前端 API：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/api/system/`
- 前端布局：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/layout/`
- 前端 store：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/store/modules/`
- 前端工具：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/utils/`
- 前端指令：`/home/leo/文档/zl/RuoYi-Vue3-ts/src/directive/`
- 后端模式：参考 RuoYi-Vue (Java) 的标准实现（实体继承 BaseEntity、Service 继承 ServiceImpl<Mapper, Entity>、Controller 继承 BaseController）

## 1.1 数据库迁移脚本

- [ ] **创建迁移脚本 `scripts/migrate-to-ruoyi.sql`**

包含：
1. 新建 sys_user / sys_role / sys_menu / sys_user_role / sys_role_menu / sys_dept / sys_post / sys_config / sys_notice / sys_oper_log / sys_logininfor 表
2. 数据迁移：users → sys_user（username/password/nick_name from real_name 等）
3. 数据迁移：permission_group → sys_role（每个权限组创建角色，permissions 字符串解析为菜单权限）
4. 数据迁移：user_group → sys_user_role（user_id/group_id → user_id/role_id）
5. 数据迁移：notification → sys_notice（title/content 等）
6. 数据迁移：operation_log → sys_oper_log
7. 旧表重命名为 *_legacy
8. 提供 `scripts/rollback-ruoyi.sql` 回退脚本

迁移在事务中执行，失败回滚。需考虑 PostgreSQL 和 MySQL 兼容（分别提供 `scripts/migrate-to-ruoyi-pgsql.sql` 与 `scripts/migrate-to-ruoyi-mysql.sql`）。

## 1.2 后端：实体层

- [ ] **创建 `entity/system/` 包下所有实体**（参考 RuoYi 标准字段）

```
BaseEntity（id, createBy, createTime, updateBy, updateTime, remark）
SysUser extends BaseEntity
SysRole extends BaseEntity
SysMenu extends BaseEntity
SysDept extends BaseEntity
SysPost extends BaseEntity
SysUserRole (userId, roleId) 复合主键
SysRoleMenu (roleId, menuId) 复合主键
SysUserRoleKey / SysRoleMenuKey (复合主键类)
```

每个实体：
- `@TableName("sys_xxx")`
- 字段 `@TableField`（驼峰→下划线自动映射）
- 时间字段 `@JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")`
- 逻辑删除 `@TableLogic`（sys_user.is_deleted 沿用现有 is_deleted）

## 1.3 后端：通用类

- [ ] **`enums/BusinessType.java`**：OTHER/ADD/EDIT/REMOVE/QUERY/EXPORT/IMPORT/CLEAN/LOGIN/LOGOUT
- [ ] **`enums/OperatorType.java`**：MANAGE/PC/MOBILE/OTHER
- [ ] **`constant/UserConstants.java`**：CAPTCHA_EXPIRATION、DEFAULT_PASSWORD("123456")、USER_ADMIN("admin")、ROLE_ADMIN("admin") 等
- [ ] **`constant/CacheConstants.java`**：LOGIN_TOKEN_KEY("login_tokens:")、SYS_CONFIG_KEY("sys_config:")、SYS_DICT_KEY("sys_dict:")、PWD_RETRY_CNT_KEY("pwd_err_cnt:")
- [ ] **`constant/CommonConstants.java`**：UTF-8、YES/NO 等
- [ ] **`exception/ServiceException.java`**：业务异常（替代 BusinessException 但保留兼容）
- [ ] **`annotation/Log.java`**：`@Log(title, businessType, operatorType, isSaveRequestData, isSaveResponseData)`
- [ ] **`aspect/LogAspect.java`**：环绕通知，方法成功返回后异步线程池写入 sys_oper_log
- [ ] **`util/SecurityUtils.java`**：`getLoginUser()`、`getUserId()`、`getUsername()`、`isAdmin()`、`hasPermission(perm)`
- [ ] **`util/ServletUtils.java`**：获取请求 IP、User-Agent 解析等

## 1.4 后端：安全框架重构

- [ ] **`security/LoginUser.java`**：包含 `SysUser user`、`Set<String> permissions`、`Set<String> roles`、`Long expireTime`
- [ ] **`security/UserDetailsImpl.java`**：实现 UserDetails，包装 LoginUser
- [ ] **`security/UserDetailsServiceImpl.java`**：实现 UserDetailsService，根据 username 加载 SysUser + 角色 + 菜单权限（menu.perms where menu_type='F'）
- [ ] **`security/JwtAuthenticationFilter.java`**：改造——解析 token → 从 Redis 读取 LoginUser → 设置到 SecurityContext；登录时把 LoginUser 写入 Redis（key=login_tokens:{token}, expire=JWT expiration）
- [ ] **`util/JwtUtil.java`**：改造——claims 包含 userId
- [ ] **`config/SecurityConfig.java`**：改造——
  - 禁用 CSRF、formLogin、httpBasic
  - 启用 @PreAuthorize
  - 配置 JwtAuthenticationFilter 在 UsernamePasswordAuthenticationFilter 之前
  - 配置白名单：/login、/register、/captchaImage、/druid/**、/doc.html、/swagger-ui/**
  - 401/403 → JwtAuthenticationEntryPoint 返回统一 JSON
- [ ] **`config/RedisConfig.java`**：RedisTemplate 配置 + Jackson2JsonRedisSerializer 序列化
- [ ] **`util/RedisUtils.java`**：get/set/delete/hasKeys/deleteObject/deleteAll/expire/getExpire

## 1.5 后端：Controller / Service / Mapper

每个模块按 RuoYi 标准：
- Mapper：继承 `BaseMapper<SysXxx>`
- Service 接口：`ISysXxxService extends IService<SysXxx>`
- Service 实现：继承 `ServiceImpl<SysXxxMapper, SysXxx> implements ISysXxxService`

- [ ] **SysUser 模块**：
  - `SysUserMapper extends BaseMapper<SysUser>`
  - `ISysUserService` + `SysUserServiceImpl`
  - `SysUserController`：list（含 deptId/username/status 筛选）/getInfo（当前用户+角色+权限）/getRouters（动态路由菜单树）/addUser/updateUser/delUser/resetPwd/changeStatus/authRole（分配角色）/updateProfile/updatePwd/uploadAvatar/deptTree
  - 配套 DTO：`SysUserQuery`、`SysUserAddRequest`、`SysUserUpdateRequest`、`UserProfileVO`、`LoginResult`

- [ ] **SysRole 模块**：
  - `SysRoleMapper` / `ISysRoleService` / `SysRoleServiceImpl` / `SysRoleController`
  - 端点：list（带分页+筛选）/add/edit/remove/dataScope（数据权限分配）/changeStatus/allocatedUserList/unallocatedUserList/authUserCancel/authUserCancelAll/authUserSelectAll/deptTree(roleId)
  - 角色分配：维护 sys_user_role + sys_role_menu 关联

- [ ] **SysMenu 模块**：
  - `SysMenuMapper` / `ISysMenuService` / `SysMenuServiceImpl` / `SysMenuController`
  - 端点：list/getInfo/add/edit/remove/treeselect（菜单下拉树）/roleMenuTreeselect(roleId)（带 checkedKeys）/updateSort
  - 菜单类型：M（目录）、C（菜单）、F（按钮）

- [ ] **SysDept 模块**：
  - `SysDeptMapper` / `ISysDeptService` / `SysDeptServiceImpl` / `SysDeptController`
  - 端点：list/getInfo/add/edit/remove/treeselect（部门下拉树）/listExclude(deptId)

- [ ] **SysPost 模块**：
  - `SysPostMapper` / `ISysPostService` / `SysPostServiceImpl` / `SysPostController`
  - 端点：list/getInfo/add/edit/remove

## 1.6 后端：AuthController 改造

- [ ] **改造 `AuthController` 与 `AuthService`**：
  - `POST /login`：验证 → 生成 token → Redis 写入 → 返回 `{ token, user, roles, permissions }`
  - `GET /getInfo`：返回当前用户 + roles + permissions（前端初始化用）
  - `GET /getRouters`：返回当前用户的菜单树（component/path/meta/children）

## 1.7 后端：DataInitializer 重构

- [ ] **改造 `DataInitializer.java`**：
  - 启动时检查 sys_user/sys_role/sys_menu/sys_dept/sys_post/sys_config/sys_notice 表是否为空
  - 为空时插入：
    - 默认部门：根部门"Zlinks集团"
    - 默认岗位：董事长、总经理、普通员工
    - 默认菜单树（按规格 4.3）
    - 默认角色：超级管理员(role_key=admin, data_scope=1)、普通角色(role_key=common)
    - admin 用户（如果不存在）：username=admin, password=BCrypt(admin123), nick_name=管理员, dept_id=1, role_id=1
    - 默认参数：sys.user.initPassword=123456、sys.user.captchaEnabled=false 等
    - 默认通知：欢迎页

## 1.8 后端：清理旧代码

- [ ] **删除/废弃旧文件**：
  - 删除 `PermissionGroupController / PermissionGroupService / impl / mapper / dto`
  - 删除 `UserGroupController / UserGroupService / impl / mapper`
  - 删除 `entity/UserGroup.java`、`entity/PermissionGroup.java`
  - 旧 `UserController` → 标记 @Deprecated，新建 `SysUserController`
  - 旧 `NotificationController` → 标记 @Deprecated（Agent 2 重写为 SysNoticeController）
  - 旧 `OperationLogController` → 标记 @Deprecated（Agent 2 重写为 SysOperlogController）

## 1.9 后端：application.yml 改造

- [ ] **增加 Redis 配置**：
```yaml
spring:
  data:
    redis:
      host: localhost
      port: 6379
      password:
      database: 0
      timeout: 10s
  # 保留现有 PostgreSQL/MySQL 配置
```

## 1.10 前端：依赖与配置

- [ ] **更新 `frontend/package.json`**：
  - 增加 `echarts: ^5.4.3`
  - 增加 `pinia-plugin-persistedstate: ^3.2.1`
- [ ] **`vite.config.ts`**：确认路径别名 `@`、`unplugin-auto-import`、`unplugin-vue-components` 配置

## 1.11 前端：布局

- [ ] **新建 `layout/index.vue`**：组合 Sidebar + Navbar + TagsView + AppMain + Breadcrumb
- [ ] **新建 `layout/components/Sidebar/index.vue`**：基于 el-menu，根据路由 meta.icon/title 显示，支持折叠
- [ ] **新建 `layout/components/Navbar.vue`**：面包屑、用户头像下拉、设置、全屏
- [ ] **新建 `layout/components/TagsView.vue`**：标签页（来自 store.tagsView）
- [ ] **新建 `layout/components/Breadcrumb.vue`**：面包屑导航
- [ ] **新建 `layout/components/AppMain.vue`**：router-view + keep-alive

## 1.12 前端：路由 + Store

- [ ] **改造 `router/index.ts`**：
  - `constantRoutes`：login / 401 / 404 / index（首页）
  - `dynamicRoutes`：通过 store.permission.generateRoutes() 动态生成（来自 /getRouters）
  - `meta.icon` 使用 svg 路径
  - `meta.title` 支持 i18n 占位（先用中文）
- [ ] **新建 `store/index.ts`**：创建 pinia 实例 + 注册持久化插件
- [ ] **改造 `store/modules/user.ts`**：
  - state：token、userInfo、roles、permissions
  - actions：login、logout、getInfo、getRouters
  - 持久化：token（localStorage）、userInfo（sessionStorage）
- [ ] **新建 `store/modules/permission.ts`**：
  - state：routes、addRoutes
  - actions：generateRoutes(roles) - 根据后端返回的菜单树 + 静态路由 → 生成可访问路由 → addRoute
- [ ] **新建 `store/modules/tagsView.ts`**：访问过的页面 → 标签页列表
- [ ] **新建 `store/modules/settings.ts`**：侧边栏 logo 标题等

## 1.13 前端：工具与指令

- [ ] **改造 `utils/request.ts`**：
  - request 拦截器：注入 Authorization
  - response 拦截器：401 → 登出；50000 → 提示；其他正常返回
- [ ] **新建 `utils/auth.ts`**：token 读取/写入/删除
- [ ] **新建 `utils/permission.ts`**：`hasPermission(perm)`、`hasRole(role)`
- [ ] **新建 `utils/ruoyi.ts`**：`parseStrEmpty` 等
- [ ] **新建 `directive/hasPermi.ts`**：`v-hasPermi="['system:user:list']"` → 校验当前用户 permissions
- [ ] **新建 `directive/hasRole.ts`**：`v-hasRole="['admin']"` → 校验角色
- [ ] **`main.ts`**：注册指令、全局组件

## 1.14 前端：API 模块

- [ ] **`api/login.ts`**：改造 login + getInfo + logout + getRouters
- [ ] **`api/system/user.ts`**：listUser / getUser / addUser / updateUser / delUser / resetUserPwd / changeUserStatus / getUserProfile / updateUserProfile / updateUserPwd / uploadAvatar / getAuthRole / updateAuthRole / deptTreeSelect
- [ ] **`api/system/role.ts`**：listRole / getRole / addRole / updateRole / dataScope / changeRoleStatus / delRole / allocatedUserList / unallocatedUserList / authUserCancel / authUserCancelAll / authUserSelectAll / deptTreeSelect
- [ ] **`api/system/menu.ts`**：listMenu / getMenu / treeselect / roleMenuTreeselect / addMenu / updateMenu / delMenu / updateMenuSort
- [ ] **`api/system/dept.ts`**：listDept / getDept / addDept / updateDept / delDept / listExclude / treeselect
- [ ] **`api/system/post.ts`**：listPost / getPost / addPost / updatePost / delPost

## 1.15 前端：通用组件

- [ ] **`components/Pagination/index.vue`**：基于 el-pagination，v-model:page、v-model:size
- [ ] **`components/SvgIcon/index.vue`**：基于 svg sprite（从 RuoYi 复制 icons 资源到 assets/icons/svg）

## 1.16 前端：登录页 + 个人中心

- [ ] **改造 `views/login.vue`**：
  - 登录成功 → 调用 user.login → 触发 permission.generateRoutes → 跳转首页
- [ ] **`views/system/user/profile/index.vue`**：个人信息展示 + 修改
- [ ] **`views/system/user/profile/resetPwd.vue`**：修改密码
- [ ] **`views/system/user/profile/avatar.vue`** 或单页面：上传头像

## 1.17 前端：用户/角色/菜单/部门/岗位管理页面

参考 `/home/leo/文档/zl/RuoYi-Vue3-ts/src/views/system/` 下对应模块的 Vue 文件，逐文件迁移/适配。

- [ ] **`views/system/user/index.vue`**：用户列表 + 搜索 + 新增/修改/删除/重置密码/分配角色/部门树
- [ ] **`views/system/user/authRole.vue`**：分配角色弹窗
- [ ] **`views/system/user/view.vue`** 或在 index 内嵌：查看用户详情
- [ ] **`views/system/role/index.vue`**：角色列表 + 搜索 + CRUD + 数据权限 + 分配用户
- [ ] **`views/system/role/authUser.vue`**：已授权用户列表 + 取消授权
- [ ] **`views/system/role/selectUser.vue`**：未授权用户列表 + 选择授权
- [ ] **`views/system/menu/index.vue`**：菜单树 CRUD（图标选择、菜单类型、权限字符串）
- [ ] **`views/system/dept/index.vue`**：部门树 CRUD
- [ ] **`views/system/post/index.vue`**：岗位列表 CRUD

## 1.18 验证 Agent 1

- [ ] **运行后端**：`cd backend && ./mvnw spring-boot:run -Dspring-boot.run.profiles=h2`
  - 验证：迁移脚本自动执行 / 默认 admin 用户创建 / 菜单树可见
- [ ] **测试登录**：`POST /api/login {username:"admin",password:"admin123"}` → 返回 token + roles + permissions
- [ ] **测试接口**：`GET /api/system/user/list` 带 token → 200 / 不带 → 401
- [ ] **测试权限**：`GET /api/system/role/list` → 200（admin 有权限）
- [ ] **运行前端**：`cd frontend && npm run dev`
  - 验证：登录页 → 登录 → 看到左侧菜单（系统管理 7 项）→ 跳转首页
  - 验证：用户管理 CRUD 流程
  - 验证：角色管理 + 分配用户
  - 验证：菜单管理 + 树形展示
  - 验证：修改密码、头像上传
- [ ] **代码质量**：`npm run build` 通过 + 后端 `./mvnw compile` 无错

## 1.19 提交

- [ ] **Commit**：`git add -A && git commit -m "feat: 实现 RuoYi RBAC 核心（用户/角色/菜单/部门/岗位）"`

---

# 任务 2：Agent 2 系统管理业务（与 Agent 3 并行）

**前置**：任务 1 完成且通过验证。

**目标**：实现参数设置、通知公告、操作日志、登录日志 4 个业务模块。

## 2.1 后端：参数设置

- [ ] **`entity/system/SysConfig.java`**：configId、configName、configKey、configValue、configType(Y/N 系统内置)、isBuiltin、remark
- [ ] **`SysConfigMapper / ISysConfigService / SysConfigServiceImpl / SysConfigController`**
  - 端点：`list / getInfo / add / update / remove / getByKey(configKey)`
  - 校验：configKey 唯一

## 2.2 后端：通知公告

- [ ] **`entity/system/SysNotice.java`**：noticeId、noticeTitle、noticeType(1通知/2公告)、noticeContent、status(0正常/1关闭)
- [ ] **`SysNoticeMapper / ISysNoticeService / SysNoticeServiceImpl / SysNoticeController`**
  - 端点：`list / getInfo / add / update / remove`
- [ ] **附加：通知已读**：`/system/notice/readUsers/{noticeId}` → 返回已读用户列表（简化：仅返回日志，本期不做实际已读状态）

## 2.3 后端：操作日志

- [ ] **`entity/monitor/SysOperLog.java`**：operId、title、businessType、method、requestMethod、operatorType、operName、deptName、operUrl、operIp、operParam、jsonResult、status(0/1)、errorMsg、operTime
- [ ] **`SysOperLogMapper / ISysOperLogService / SysOperLogServiceImpl / SysOperlogController`**
  - 端点：`list（带筛选：title/businessType/operName/status/createTime 区间） / remove(operIds) / clean / detail(operId)`
  - 通过 Agent 1 的 @Log 注解自动采集 + 异步线程池写入

## 2.4 后端：登录日志

- [ ] **`entity/monitor/SysLogininfor.java`**：infoId、userName、ipaddr、loginLocation、browser、os、status(0成功/1失败)、msg、loginTime
- [ ] **`SysLogininforMapper / ISysLogininforService / SysLogininforServiceImpl / SysLogininforController`**
  - 端点：`list（带筛选） / remove(infoIds) / clean / unlock(userName)`
- [ ] **`security/SysLogininforEventListener`**：监听 `AuthenticationSuccessEvent` / `AuthenticationFailureBadCredentialsEvent` / `AbstractAuthenticationFailureEvent` → 解析请求 → 异步写入登录日志

## 2.5 后端：接入 LogAspect 异步落库

- [ ] **完善 `aspect/LogAspect.java`**：环绕通知 + `@Async` 线程池（`TaskExecutionAutoConfiguration` 启用，`@EnableAsync`）
- [ ] **在 Agent 1 已有的 Controller 上加 `@Log` 注解**：所有 CRUD 方法都加，业务类型匹配

## 2.6 前端：API 模块

- [ ] **`api/system/config.ts`**：listConfig / getConfig / addConfig / updateConfig / delConfig
- [ ] **`api/system/notice.ts`**：listNotice / getNotice / addNotice / updateNotice / delNotice
- [ ] **`api/monitor/operlog.ts`**：list / delOperlog / cleanOperlog / detail(operId)
- [ ] **`api/monitor/logininfor.ts`**：list / delLogininfor / cleanLogininfor / unlockLogininfor(userName)

## 2.7 前端：通用组件完善

- [ ] **完善 `components/DictTag/index.vue`**：渲染字典色块（type: default/primary/success/warning/info/danger）
- [ ] **新建 `components/FileUpload/index.vue`**：基于 el-upload，支持头像/附件

## 2.8 前端：页面

- [ ] **`views/system/config/index.vue`**：参数列表 + 搜索 + CRUD + 刷新缓存按钮
- [ ] **`views/system/notice/index.vue`**：通知公告列表 + 搜索 + CRUD
- [ ] **`views/system/notice/ReadUsers.vue`**：已读用户列表（简化版）
- [ ] **`views/monitor/operlog/index.vue`**：操作日志列表 + 搜索 + 删除/清空 + 详情
- [ ] **`views/monitor/operlog/detail.vue`**：日志详情（请求参数、返回结果、错误信息）
- [ ] **`views/monitor/logininfor/index.vue`**：登录日志列表 + 搜索 + 删除/清空 + 解锁账号

## 2.9 验证 Agent 2

- [ ] **测试参数**：`POST /api/system/config` → 200 → `GET /api/system/config/key/sys.user.initPassword` → 200
- [ ] **测试通知**：`POST /api/system/notice` → 200 → 列表可见
- [ ] **测试操作日志**：执行任意 CRUD → `/monitor/operlog/list` 可见记录
- [ ] **测试登录日志**：登录成功 → 列表 1 条；登录失败 → 列表 +1 条 status=1
- [ ] **前端**：4 个页面 CRUD + 字典 tag 渲染 + 操作日志详情
- [ ] **构建**：`npm run build` + 后端 `./mvnw package`

## 2.10 提交

- [ ] **Commit**：`git add -A && git commit -m "feat: 实现系统管理业务（参数/通知/操作日志/登录日志）"`

---

# 任务 3：Agent 3 系统监控（与 Agent 2 并行）

**前置**：任务 1 完成且通过验证。Redis 服务可用。

**目标**：实现在线用户、服务监控、缓存监控、Druid 监控。

## 3.1 后端：依赖

- [ ] **`backend/pom.xml`** 增加：
  - `spring-boot-starter-data-redis`（已在 Agent 1 加入，确认）
  - `oshi-core: 6.4.4`
  - `hutool-system: 5.8.x`（可选，简化 IP 解析）

## 3.2 后端：在线用户

- [ ] **`dto/monitor/SysUserOnlineVO.java`**：tokenId、userName、ipaddr、loginLocation、browser、os、loginTime、roleKey
- [ ] **`service/monitor/ISysUserOnlineService`** + `SysUserOnlineServiceImpl`
  - `listOnlineUsers(ipaddr, userName)`：从 Redis SCAN `login_tokens:*` → 反序列化 → 转为 VO
  - `forceLogout(tokenId)`：Redis DELETE → SecurityContext 失效
- [ ] **`controller/monitor/SysUserOnlineController`**：
  - `GET /monitor/online/list`（带分页+筛选）
  - `DELETE /monitor/online/{tokenId}`

- [ ] **`security/JwtAuthenticationFilter`** 改造（Agent 1 已完成基础）：登录成功后写入 Redis `login_tokens:{token}` → JSON(LoginUser, expire=JWT expiration)

## 3.3 后端：服务监控

- [ ] **`dto/monitor/ServerInfoVO.java`**：嵌套对象：cpu (used/total/usage)、mem (used/total/usage)、jvm (used/total/usage/max)、sys (computerName/osName/osArch)、sysFiles (list: dirName/sysTypeName/total/used/free/usage)
- [ ] **`util/ServerInfoUtils.java`**：OSHI 采集 CPU/内存/JVM/磁盘
- [ ] **`controller/monitor/ServerController`**：
  - `GET /monitor/server` → 返回 ServerInfoVO

## 3.4 后端：缓存监控

- [ ] **`dto/monitor/CacheVO.java`**：info (RedisProperties)、dbSize、commandStats (list: name/value)
- [ ] **`controller/monitor/CacheController`**：
  - `GET /monitor/cache` → Redis info + dbsize + commandstats
  - `GET /monitor/cache/getNames` → Redis 所有 key 的前缀分类（如 login_tokens:、sys_config: 等）
  - `GET /monitor/cache/getKeys/{cacheName}` → 该前缀下所有 key
  - `GET /monitor/cache/getValue/{cacheName}/{cacheKey}` → 缓存内容
  - `DELETE /monitor/cache/clearCacheName/{cacheName}` → 按前缀清空
  - `DELETE /monitor/cache/clearCacheKey/{cacheKey}` → 清单个 key
  - `DELETE /monitor/cache/clearCacheAll` → flushdb

## 3.5 后端：Druid 监控

- [ ] **确认 `application.yml` 配置**：已在 Agent 1 保留，确认 admin/admin 账号
- [ ] **确认 SecurityConfig 白名单**：`/druid/**` 已加入

## 3.6 前端：API 模块

- [ ] **`api/monitor/online.ts`**：list / forceLogout
- [ ] **`api/monitor/server.ts`**：getServer
- [ ] **`api/monitor/cache.ts`**：getCache / listCacheName / listCacheKey / getCacheValue / clearCacheName / clearCacheKey / clearCacheAll

## 3.7 前端：页面

- [ ] **`views/monitor/online/index.vue`**：在线用户列表 + 搜索（IP/用户名）+ 强制下线
- [ ] **`views/monitor/server/index.vue`**：
  - 顶部：服务器信息卡片（计算机名、操作系统、IP）
  - CPU 使用率（ECharts 折线/仪表盘）
  - 内存使用（进度条）
  - JVM 内存（堆内存/非堆内存）
  - 磁盘使用（表格）
  - 每 5 秒自动刷新
- [ ] **`views/monitor/cache/index.vue`**：
  - Redis 信息卡片（Redis 版本、运行时间、连接数）
  - 命令统计（表格：命令名/调用次数/耗时）
  - 缓存名称列表 + 详情查看 + 清理按钮
- [ ] **`views/monitor/cache/list.vue`**：指定缓存名下的所有 key + 内容查看 + 删除
- [ ] **`views/monitor/druid/index.vue`**：iframe 嵌入 `/druid/index.html`

## 3.8 验证 Agent 3

- [ ] **测试在线用户**：开启 2 个浏览器登录 → `/monitor/online/list` 看到 2 条 → 强退其中一个 → 再次访问该 token 接口返回 401
- [ ] **测试服务监控**：`/monitor/server` → JSON 数据完整 → 前端 ECharts 渲染
- [ ] **测试缓存监控**：`/monitor/cache` → Redis info + dbSize + commandstats 可见；执行任意 CRUD 后 commandstats +1
- [ ] **测试 Druid**：访问 `/druid/index.html` → 登录成功 → SQL 执行监控可见
- [ ] **前端**：3 个监控页面 + Druid iframe
- [ ] **构建**：`npm run build` + 后端 `./mvnw package`

## 3.9 提交

- [ ] **Commit**：`git add -A && git commit -m "feat: 实现系统监控（在线用户/服务监控/缓存监控/Druid）"`

---

# 任务 4：最终集成验证（所有 Agent 完成后）

- [ ] **运行后端** + **运行前端** + **启动 Redis**
- [ ] **端到端流程**：
  1. 登录 admin → 看到所有菜单
  2. 创建新角色（只有部分菜单权限）→ 创建新用户绑定该角色 → 登录新用户 → 菜单按权限过滤
  3. 用户管理：CRUD + 重置密码 + 修改状态 + 分配角色
  4. 角色管理：CRUD + 数据权限 + 分配用户
  5. 菜单管理：CRUD + 图标选择 + 权限字符串
  6. 参数设置：CRUD + 按 key 查询
  7. 通知公告：CRUD
  8. 操作日志：执行各 CRUD → 查看日志详情
  9. 登录日志：登录成功/失败 → 查看
  10. 在线用户：强制下线
  11. 服务监控：ECharts 实时刷新
  12. 缓存监控：查看 + 清空
  13. Druid：访问监控页
- [ ] **构建验证**：`./mvnw clean package` + `npm run build` 都成功
- [ ] **代码质量**：`./mvnw checkstyle:check`（如有配置）
- [ ] **最终 Commit**：`git add -A && git commit -m "feat: 集成验证与文档更新"`

---

# 规格自检

✅ 规格覆盖度：spec 中每个模块/页面/接口都在任务中找到对应实现
✅ 占位符：所有步骤都有具体动作或代码片段
✅ 类型一致性：实体、字段、API 路径在三个 Agent 任务中保持一致
✅ 范围聚焦：3 个任务边界清晰，不重叠

---

# 执行交接

**计划已保存到** `docs/superpowers/plans/2026-07-11-zlinks-ruoyi-implementation.md`

**执行方式选择**：
- **子代理驱动（推荐）**：使用 subagent-driven-development，按 3 个 Agent 顺序执行（Agent 1 → Agent 2+3 并行）。每个 Agent 完成后进行规格合规性 + 代码质量两阶段审查。

选择执行方式：默认采用"子代理驱动"。