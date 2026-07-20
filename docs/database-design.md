# ZlinksPackageSystem 数据库设计

## 概述

系统支持三种数据库: **H2**(默认开发,内存模式)、**MySQL 8+**(生产)、**PostgreSQL 15+**(生产)。

三套初始化脚本:
- H2: `backend/src/main/resources/schema.sql`
- MySQL: `scripts/init-database.sql`
- PostgreSQL: `scripts/init-database-pgsql.sql`

ORM: MyBatis-Plus (`map-underscore-to-camel-case: true`), 逻辑删除字段 `is_deleted` (0=正常, 1=删除)。

---

## 约定字段 (BaseEntity)

所有业务实体继承 `BaseEntity`, 包含以下公共字段:

| 字段 | 列名 | Java 类型 | 自动填充 | 说明 |
|------|------|-----------|----------|------|
| createBy | create_by | String | INSERT | 创建人 |
| createTime | create_time | LocalDateTime | INSERT | 创建日期 |
| updateBy | update_by | String | INSERT_UPDATE | 更新人 |
| updateTime | update_time | LocalDateTime | INSERT_UPDATE | 更新时间 |
| remark | remark | String | -- | 备注信息 |
| isDeleted | is_deleted | Integer | `@TableLogic` | 逻辑删除 |

---

## 业务表 (17 张)

### 1. game — 游戏表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| game_name | VARCHAR(100) | NOT NULL | 游戏名 |
| game_direction | VARCHAR(20) | NOT NULL | 游戏方向 |
| source | VARCHAR(50) | | 来源 |
| git_url | VARCHAR(255) | | git 地址 |
| priority | INT | DEFAULT 0 | 优先级 |
| tags | VARCHAR(255) | | 标签 |
| project_type | VARCHAR(50) | | 项目工程类型 |
| manager | VARCHAR(50) | | 负责人 |
| white_branch | VARCHAR(100) | | 白包分支 |
| status | VARCHAR(20) | DEFAULT 'active' | 项目状态 |
| retention_record | TEXT | | 游戏留存记录 |
| android_folder_name | VARCHAR(100) | | 安卓文件夹名称 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Game.java` (`@TableName("game")`)

---

### 2. platform — 平台表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| platform_name | VARCHAR(100) | NOT NULL | 平台名称 |
| platform_code | VARCHAR(50) | | 平台编码 |
| sort_order | INT | DEFAULT 0 | 排序 |
| status | VARCHAR(20) | DEFAULT 'active' | 状态 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Platform.java` (`@TableName("platform")`)

---

### 3. copyright — 软著表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| copyright_name | VARCHAR(100) | NOT NULL | 软著名 |
| copyright_owner | VARCHAR(100) | | 著作权人 |
| copyright_number | VARCHAR(50) | | 软著号 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Copyright.java` (`@TableName("copyright")`)

---

### 4. company — 主体公司表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| company_name | VARCHAR(100) | NOT NULL | 公司名 |
| platform_id | BIGINT | FK -> platform(id) | 平台 ID |
| account | VARCHAR(100) | | 账号 |
| password | VARCHAR(100) | | 密码 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Company.java` (`@TableName("company")`), 虚拟字段: `platformName`

---

### 5. platform_match — 平台匹配表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| company_id | BIGINT | FK -> company(id) | 上架主体公司 ID |
| original_game | VARCHAR(100) | | 原游戏 |
| current_game_name | VARCHAR(100) | | 现游戏名 |
| batch | VARCHAR(50) | | 批次 |
| package_mode | VARCHAR(20) | | 打包模式 |
| sdk_version | VARCHAR(50) | | SDK 版本 |
| apk_version | VARCHAR(20) | | APK 版本 |
| platform_status | VARCHAR(20) | DEFAULT 'pending' | 平台状态 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `PlatformMatch.java` (`@TableName("platform_match")`)

---

### 6. product — 产品表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| copyright_id | BIGINT | FK -> copyright(id) | 软著 ID |
| game_id | BIGINT | FK -> game(id) | 游戏 ID |
| company_id | BIGINT | FK -> company(id) | 公司 ID |
| platform_id | BIGINT | | 平台 ID |
| package_name | VARCHAR(100) | | 包名 |
| sdk_version | VARCHAR(50) | | SDK 版本 |
| apk_version | VARCHAR(50) | | APK 版本 |
| batch | VARCHAR(50) | | 批次 |
| package_mode | VARCHAR(20) | | 打包模式 |
| status | VARCHAR(20) | DEFAULT 'pending' | 状态 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Product.java` (`@TableName("product")`), 虚拟字段: `copyrightName`, `gameName`, `companyName`, `platformName`

---

### 7. honor_param — 荣耀参数表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| product_id | BIGINT | NOT NULL, FK -> product(id) | 产品 ID |
| package_name | VARCHAR(100) | | 包名 |
| app_id | VARCHAR(100) | | APPID |
| app_secret | VARCHAR(100) | | APP_SECRET |
| media_id | VARCHAR(100) | | MediaID |
| agconnect_path | VARCHAR(255) | | agconnect_services_json_path |
| td_app_id | VARCHAR(100) | | TDAPPID |
| ad_param_status | VARCHAR(20) | | 广告位参数状态 |
| list_status | VARCHAR(20) | | 黑白名单状态 |
| operator | VARCHAR(50) | | 经办人 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `HonorParam.java` (`@TableName("honor_param")`)

---

### 8. vivo_param — VIVO 参数表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| product_id | BIGINT | NOT NULL, FK -> product(id) | 产品 ID |
| app_id | VARCHAR(100) | | APPID |
| contract_status | VARCHAR(20) | | 签约状态 |
| media_id | VARCHAR(100) | | MediaID |
| td_app_id | VARCHAR(100) | | TDAPPID |
| ad_param_status | VARCHAR(20) | | 广告位参数状态 |
| list_status | VARCHAR(20) | | 黑白名单状态 |
| operator | VARCHAR(50) | | 经办人 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `VivoParam.java` (`@TableName("vivo_param")`)

---

### 9. huawei_param — HUAWEI 参数表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| product_id | BIGINT | NOT NULL, FK -> product(id) | 产品 ID |
| package_name | VARCHAR(100) | | 包名 |
| app_id | VARCHAR(100) | | APPID |
| agconnect_path | VARCHAR(255) | | agconnect_services_json_path |
| td_app_id | VARCHAR(100) | | TDAPPID |
| ad_param_status | VARCHAR(20) | | 广告位参数状态 |
| list_status | VARCHAR(20) | | 黑白名单状态 |
| operator | VARCHAR(50) | | 经办人 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `HuaweiParam.java` (`@TableName("huawei_param")`)

---

### 10. sign_file — 签名文件信息表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| company_id | BIGINT | FK -> company(id) | 上架主体 ID |
| store_file | VARCHAR(255) | | store_file |
| store_password | VARCHAR(100) | | store_password |
| key_alias | VARCHAR(100) | | key_alias |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `SignFile.java` (`@TableName("sign_file")`), 虚拟字段: `companyName`

---

### 11. users — 业务用户表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| username | VARCHAR(50) | UNIQUE NOT NULL | 账号 |
| password | VARCHAR(100) | NOT NULL | 密码 |
| real_name | VARCHAR(50) | | 姓名 |
| status | VARCHAR(20) | DEFAULT 'active' | 在职状态 |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `User.java` (`@TableName("users")`), 虚拟字段: `groupIds`, `groupNames`

---

### 12. permission_group — 权限组表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| group_name | VARCHAR(50) | NOT NULL | 组名称 |
| group_permission | TEXT | | 组权限(JSON) |
| group_accounts | TEXT | | 组账号(JSON) |
| remark | TEXT | | 备注信息 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `PermissionGroup.java` (`@TableName("permission_group")`), 虚拟字段: `userCount`

---

### 13. permission_scope — 权限组模块范围表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| group_id | BIGINT | NOT NULL | 权限组 ID |
| scope | VARCHAR(16) | NOT NULL | 作用域: backend/desktop |
| modules_text | TEXT | | 模块列表(JSON 数组) |
| remark | TEXT | | 备注 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |
| **UNIQUE** | (group_id, scope) | | 唯一约束 |

**实体**: `PermissionScope.java` (`@TableName("permission_scope")`), 虚拟字段: `modules` (`List<String>`, 由 `modulesText` JSON 解析)

---

### 14. user_group — 用户权限组关联表

| 列名 | 类型 | 约束 |
|------|------|------|
| user_id | BIGINT | PK, FK -> users(id) |
| group_id | BIGINT | PK, FK -> permission_group(id) |

**实体**: `UserGroup.java` (`@TableName("user_group")`), 复合主键 (userId, groupId)

---

### 15. operation_log — 操作日志表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| user_id | BIGINT | | 操作用户 ID |
| username | VARCHAR(50) | | 操作用户名 |
| module | VARCHAR(50) | | 操作模块 |
| action | VARCHAR(20) | | 操作类型 |
| target | VARCHAR(200) | | 操作目标 |
| ip_address | VARCHAR(50) | | 操作 IP |
| remark | TEXT | | 备注 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `OperationLog.java` (`@TableName("operation_log")`)

---

### 16. notification — 通知表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| title | VARCHAR(100) | NOT NULL | 通知标题 |
| content | TEXT | | 通知内容 |
| module | VARCHAR(50) | | 所属模块 |
| target_id | BIGINT | | 目标 ID |
| target_type | VARCHAR(20) | | 目标类型 |
| sender_id | BIGINT | | 发送者 ID |
| receiver_ids | TEXT | | 接收者 ID 列表(JSON) |
| receiver_type | VARCHAR(20) | | 接收类型(user/group) |
| is_pinned | TINYINT | DEFAULT 0 | 是否置顶 |
| status | VARCHAR(20) | DEFAULT 'unread' | 状态 |
| remark | TEXT | | 备注 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建日期 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Notification.java` (`@TableName("notification")`), 虚拟字段: `senderName`, `receiverNames`

---

### 17. tool — 工具库表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK AUTO_INCREMENT | 主键 |
| name | VARCHAR(100) | NOT NULL | 工具名称 |
| description | VARCHAR(500) | | 描述 |
| category | VARCHAR(50) | | 分类 |
| version | VARCHAR(50) | | 版本 |
| status | VARCHAR(20) | DEFAULT '未运行' | 状态 |
| manager | VARCHAR(50) | | 负责人 |
| run_mode | VARCHAR(20) | DEFAULT 'Script' | 运行模式 |
| language | VARCHAR(20) | | 语言 |
| interpreter_path | VARCHAR(500) | | 解释器路径 |
| script_path | VARCHAR(500) | | 脚本路径 |
| executable_path | VARCHAR(500) | | 可执行文件路径 |
| working_directory | VARCHAR(500) | | 工作目录 |
| environment_variables | TEXT | | 环境变量(JSON) |
| default_argument_prefix | VARCHAR(10) | DEFAULT '--' | 默认参数前缀 |
| git_url | VARCHAR(500) | | git 地址 |
| clone_directory | VARCHAR(500) | | 克隆目录 |
| arguments_json | TEXT | | 参数配置(JSON) |
| notification_json | TEXT | | 通知配置(JSON) |
| is_system_builtin | TINYINT | DEFAULT 0 | 是否系统内置 |
| remark | TEXT | | 备注 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建人 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新人 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_time | TIMESTAMP | | 更新时间 |
| is_deleted | TINYINT | DEFAULT 0 | 逻辑删除 |

**实体**: `Tool.java` (`@TableName("tool")`)

---

## RuoYi RBAC 系统表 (13 张)

### sys_dept — 部门表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| dept_id | BIGINT | PK AUTO_INCREMENT | 部门 id |
| parent_id | BIGINT | DEFAULT 0 | 父部门 id |
| ancestors | VARCHAR(500) | DEFAULT '' | 祖级列表 |
| dept_name | VARCHAR(30) | NOT NULL | 部门名称 |
| order_num | INT | DEFAULT 0 | 显示顺序 |
| leader | VARCHAR(20) | | 负责人 |
| phone | VARCHAR(11) | | 联系电话 |
| email | VARCHAR(50) | | 邮箱 |
| status | CHAR(1) | DEFAULT '0' | 部门状态(0正常 1停用) |
| del_flag | CHAR(1) | DEFAULT '0' | 删除标志(0存在 2删除) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysDept.java` (`@TableName("sys_dept")`)

---

### sys_post — 岗位信息表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| post_id | BIGINT | PK AUTO_INCREMENT | 岗位 ID |
| post_code | VARCHAR(64) | NOT NULL | 岗位编码 |
| post_name | VARCHAR(50) | NOT NULL | 岗位名称 |
| post_sort | INT | DEFAULT 0 | 显示顺序 |
| status | CHAR(1) | DEFAULT '0' | 状态(0正常 1停用) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysPost.java` (`@TableName("sys_post")`)

---

### sys_user — 用户信息表 (RuoYi)

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| user_id | BIGINT | PK AUTO_INCREMENT | 用户 ID |
| dept_id | BIGINT | FK -> sys_dept(dept_id) | 部门 ID |
| user_name | VARCHAR(30) | UNIQUE NOT NULL | 登录账号 |
| nick_name | VARCHAR(30) | NOT NULL | 用户昵称 |
| email | VARCHAR(50) | DEFAULT '' | 用户邮箱 |
| phonenumber | VARCHAR(11) | DEFAULT '' | 手机号码 |
| sex | CHAR(1) | DEFAULT '0' | 用户性别(0男 1女 2未知) |
| avatar | VARCHAR(255) | DEFAULT '' | 头像地址 |
| password | VARCHAR(100) | DEFAULT '' | 密码 |
| status | CHAR(1) | DEFAULT '0' | 帐号状态(0正常 1停用) |
| login_ip | VARCHAR(128) | DEFAULT '' | 最后登录 IP |
| login_date | TIMESTAMP | | 最后登录时间 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysUser.java` (`@TableName("sys_user")`), 虚拟字段: `dept`, `roles`, `roleIds`, `postIds`

---

### sys_role — 角色信息表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| role_id | BIGINT | PK AUTO_INCREMENT | 角色 ID |
| role_name | VARCHAR(30) | NOT NULL | 角色名称 |
| role_key | VARCHAR(100) | NOT NULL | 角色权限字符串 |
| role_sort | INT | DEFAULT 0 | 显示顺序 |
| data_scope | CHAR(1) | DEFAULT '1' | 数据范围 |
| menu_check_strictly | TINYINT(1) | DEFAULT 1 | 菜单树选择项是否关联显示 |
| dept_check_strictly | TINYINT(1) | DEFAULT 1 | 部门树选择项是否关联显示 |
| status | CHAR(1) | DEFAULT '0' | 角色状态(0正常 1停用) |
| del_flag | CHAR(1) | DEFAULT '0' | 删除标志(0存在 2删除) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysRole.java` (`@TableName("sys_role")`)

---

### sys_menu — 菜单权限表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| menu_id | BIGINT | PK AUTO_INCREMENT | 菜单 ID |
| menu_name | VARCHAR(50) | NOT NULL | 菜单名称 |
| parent_id | BIGINT | DEFAULT 0 | 父菜单 ID |
| order_num | INT | DEFAULT 0 | 显示顺序 |
| path | VARCHAR(200) | DEFAULT '' | 路由地址 |
| component | VARCHAR(255) | | 组件路径 |
| query | VARCHAR(255) | | 路由参数 |
| is_frame | CHAR(1) | DEFAULT '1' | 是否为外链(0是 1否) |
| is_cache | CHAR(1) | DEFAULT '0' | 是否缓存(0缓存 1不缓存) |
| menu_type | CHAR(1) | DEFAULT '' | 菜单类型(M目录 C菜单 F按钮) |
| visible | CHAR(1) | DEFAULT '0' | 菜单状态(0显示 1隐藏) |
| status | CHAR(1) | DEFAULT '0' | 菜单状态(0正常 1停用) |
| perms | VARCHAR(100) | | 权限标识 |
| icon | VARCHAR(100) | DEFAULT '#' | 菜单图标 |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysMenu.java` (`@TableName("sys_menu")`)

---

### sys_user_role — 用户和角色关联表

| 列名 | 类型 | 约束 |
|------|------|------|
| user_id | BIGINT | PK, FK -> sys_user(user_id) |
| role_id | BIGINT | PK, FK -> sys_role(role_id) |

**实体**: `SysUserRole.java` (`@TableName("sys_user_role")`), 复合主键

---

### sys_role_menu — 角色和菜单关联表

| 列名 | 类型 | 约束 |
|------|------|------|
| role_id | BIGINT | PK, FK -> sys_role(role_id) |
| menu_id | BIGINT | PK, FK -> sys_menu(menu_id) |

**实体**: `SysRoleMenu.java` (`@TableName("sys_role_menu")`), 复合主键

---

### sys_config — 参数配置表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| config_id | BIGINT | PK AUTO_INCREMENT | 参数主键 |
| config_name | VARCHAR(100) | DEFAULT '' | 参数名称 |
| config_key | VARCHAR(100) | DEFAULT '' | 参数键名 |
| config_value | VARCHAR(500) | DEFAULT '' | 参数键值 |
| config_type | CHAR(1) | DEFAULT 'N' | 系统内置(Y是 N否) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | | 备注 |

**实体**: `SysConfig.java` (`@TableName("sys_config")`)

---

### sys_notice — 通知公告表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| notice_id | BIGINT | PK AUTO_INCREMENT | 公告 ID |
| notice_title | VARCHAR(50) | NOT NULL | 公告标题 |
| notice_type | CHAR(1) | NOT NULL | 公告类型(1通知 2公告) |
| notice_content | TEXT | | 公告内容 |
| status | CHAR(1) | DEFAULT '0' | 公告状态(0正常 1关闭) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(255) | | 备注 |

**实体**: `SysNotice.java` (`@TableName("sys_notice")`)

---

### sys_oper_log — 操作日志记录 (RuoYi)

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| oper_id | BIGINT | PK AUTO_INCREMENT | 日志主键 |
| title | VARCHAR(50) | DEFAULT '' | 模块标题 |
| business_type | INT | DEFAULT 0 | 业务类型(0其它 1新增 2修改 3删除) |
| method | VARCHAR(100) | DEFAULT '' | 方法名称 |
| request_method | VARCHAR(10) | DEFAULT '' | 请求方式 |
| operator_type | INT | DEFAULT 0 | 操作类别(0其它 1后台用户 2手机端用户) |
| oper_name | VARCHAR(50) | DEFAULT '' | 操作人员 |
| dept_name | VARCHAR(50) | DEFAULT '' | 部门名称 |
| oper_url | VARCHAR(255) | DEFAULT '' | 请求 URL |
| oper_ip | VARCHAR(128) | DEFAULT '' | 主机地址 |
| oper_param | VARCHAR(2000) | DEFAULT '' | 请求参数 |
| json_result | VARCHAR(2000) | DEFAULT '' | 返回参数 |
| status | INT | DEFAULT 0 | 操作状态(0正常 1异常) |
| error_msg | VARCHAR(2000) | DEFAULT '' | 错误消息 |
| oper_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 操作时间 |

**实体**: `SysOperLog.java` (`@TableName("sys_oper_log")`)

---

### sys_logininfor — 系统访问记录

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| info_id | BIGINT | PK AUTO_INCREMENT | 访问 ID |
| user_name | VARCHAR(50) | DEFAULT '' | 用户账号 |
| ipaddr | VARCHAR(128) | DEFAULT '' | 登录 IP 地址 |
| login_location | VARCHAR(255) | DEFAULT '' | 登录地点 |
| browser | VARCHAR(50) | DEFAULT '' | 浏览器类型 |
| os | VARCHAR(50) | DEFAULT '' | 操作系统 |
| status | CHAR(1) | DEFAULT '0' | 登录状态(0成功 1失败) |
| msg | VARCHAR(255) | DEFAULT '' | 提示消息 |
| login_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 访问时间 |

**实体**: `SysLogininfor.java` (`@TableName("sys_logininfor")`)

---

### sys_job — 定时任务调度表 (H2/PG, 不含 MySQL)

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| job_id | BIGINT | PK AUTO_INCREMENT | 任务 ID |
| job_name | VARCHAR(64) | NOT NULL DEFAULT '' | 任务名称 |
| job_group | VARCHAR(64) | NOT NULL DEFAULT 'DEFAULT' | 任务组名 |
| invoke_target | VARCHAR(500) | NOT NULL | 调用目标字符串 |
| cron_expression | VARCHAR(255) | DEFAULT '' | cron 执行表达式 |
| misfire_policy | VARCHAR(20) | DEFAULT '3' | 计划执行错误策略 |
| concurrent | CHAR(1) | DEFAULT '1' | 是否并发执行(0允许 1禁止) |
| status | CHAR(1) | DEFAULT '0' | 状态(0正常 1暂停) |
| create_by | VARCHAR(64) | DEFAULT '' | 创建者 |
| create_time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| update_by | VARCHAR(64) | DEFAULT '' | 更新者 |
| update_time | TIMESTAMP | | 更新时间 |
| remark | VARCHAR(500) | DEFAULT '' | 备注信息 |

**实体**: `SysJob.java` (`@TableName("sys_job")`)

---

### sys_job_log — 定时任务调度日志表 (H2/PG, 不含 MySQL)

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| job_log_id | BIGINT | PK AUTO_INCREMENT | 任务日志 ID |
| job_name | VARCHAR(64) | NOT NULL | 任务名称 |
| job_group | VARCHAR(64) | NOT NULL | 任务组名 |
| invoke_target | VARCHAR(500) | NOT NULL | 调用目标字符串 |
| cron_expression | VARCHAR(255) | DEFAULT '' | cron 执行表达式 |
| start_time | TIMESTAMP | | 开始时间 |
| end_time | TIMESTAMP | | 结束时间 |
| status | CHAR(1) | DEFAULT '0' | 状态(0正常 1异常) |
| job_message | VARCHAR(1000) | DEFAULT '' | 日志信息 |
| exception_info | TEXT | | 异常信息 |

**实体**: `SysJobLog.java` (`@TableName("sys_job_log")`)

---

## 实体关系图 (ER)

```
game ──< product >── copyright
                  │
platform ──< company ──< sign_file
         │        │
         │        └──< platform_match
         │
         └──< product

        product
          │
          ├──< honor_param (product_id)
          ├──< vivo_param  (product_id)
          └──< huawei_param(product_id)

users >──< user_group >──< permission_group >──< permission_scope

RuoYi RBAC:
sys_user >──< sys_user_role >──< sys_role >──< sys_role_menu >──< sys_menu
sys_dept >──< sys_user (dept_id)
```

---

## API 接口设计

### 认证
| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/login | 用户登录 |
| POST | /api/auth/logout | 用户登出 |
| GET | /api/auth/info | 获取当前用户信息 |

### 用户管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/users | 获取用户列表 |
| POST | /api/users | 创建用户 |
| PUT | /api/users/{id} | 更新用户 |
| DELETE | /api/users/{id} | 删除用户 |
| GET | /api/users/{id} | 获取用户详情 |

### 权限组
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/permission-groups | 获取权限组列表 |
| POST | /api/permission-groups | 创建权限组 |
| PUT | /api/permission-groups/{id} | 更新权限组 |
| DELETE | /api/permission-groups/{id} | 删除权限组 |
| GET | /api/permission-groups/{id}/users | 获取权限组下用户列表 |

### 权限范围
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/permission-scopes?groupId={id} | 获取权限范围列表 |
| POST | /api/permission-scopes | 创建/更新权限范围 |
| DELETE | /api/permission-scopes/{id} | 删除权限范围 |
| GET | /api/permission-scopes/modules | 获取可用模块列表 |

### 游戏管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/games | 获取游戏列表 |
| POST | /api/games | 创建游戏 |
| PUT | /api/games/{id} | 更新游戏 |
| DELETE | /api/games/{id} | 删除游戏 |
| GET | /api/games/{id} | 获取游戏详情 |
| GET | /api/games/counts | 获取游戏统计(首页) |

### 平台管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/platforms | 获取平台列表 |
| POST | /api/platforms | 创建平台 |
| PUT | /api/platforms/{id} | 更新平台 |
| DELETE | /api/platforms/{id} | 删除平台 |
| GET | /api/platforms/all | 获取所有平台(下拉选择) |

### 软著管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/copyrights | 获取软著列表 |
| POST | /api/copyrights | 创建软著 |
| PUT | /api/copyrights/{id} | 更新软著 |
| DELETE | /api/copyrights/{id} | 删除软著 |

### 平台匹配
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/platform-matches | 获取平台匹配列表 |
| POST | /api/platform-matches | 创建平台匹配 |
| PUT | /api/platform-matches/{id} | 更新平台匹配 |
| DELETE | /api/platform-matches/{id} | 删除平台匹配 |

### 平台参数
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/params/honor | 获取荣耀参数列表 |
| POST | /api/params/honor | 创建荣耀参数 |
| PUT | /api/params/honor/{id} | 更新荣耀参数 |
| DELETE | /api/params/honor/{id} | 删除荣耀参数 |
| GET | /api/params/vivo | 获取 VIVO 参数列表 |
| POST | /api/params/vivo | 创建 VIVO 参数 |
| PUT | /api/params/vivo/{id} | 更新 VIVO 参数 |
| DELETE | /api/params/vivo/{id} | 删除 VIVO 参数 |
| GET | /api/params/huawei | 获取 HUAWEI 参数列表 |
| POST | /api/params/huawei | 创建 HUAWEI 参数 |
| PUT | /api/params/huawei/{id} | 更新 HUAWEI 参数 |
| DELETE | /api/params/huawei/{id} | 删除 HUAWEI 参数 |

### 主体公司
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/companies | 获取公司列表 |
| POST | /api/companies | 创建公司 |
| PUT | /api/companies/{id} | 更新公司 |
| DELETE | /api/companies/{id} | 删除公司 |

### 签名文件
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/sign-files | 获取签名文件列表 |
| POST | /api/sign-files | 创建签名文件 |
| PUT | /api/sign-files/{id} | 更新签名文件 |
| DELETE | /api/sign-files/{id} | 删除签名文件 |

### 产品管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/products | 获取产品列表 |
| POST | /api/products | 创建产品 |
| PUT | /api/products/{id} | 更新产品 |
| DELETE | /api/products/{id} | 删除产品 |
| GET | /api/products/all | 获取所有产品(下拉选择) |
| POST | /api/products/{id}/package | 主动打包 |
| GET | /api/products/counts | 获取产品统计(首页) |

### 通知管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/notifications | 获取通知列表(分页) |
| POST | /api/notifications | 创建通知 |
| PUT | /api/notifications/{id} | 更新通知 |
| DELETE | /api/notifications/{id} | 删除通知 |
| GET | /api/notifications/announcements | 获取公告列表 |
| GET | /api/notifications/pinned | 获取置顶通知 |
| GET | /api/notifications/unread | 获取未读通知 |
| PUT | /api/notifications/{id}/read | 标记已读 |

### 工具库
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/tools | 获取工具列表(分页) |
| POST | /api/tools | 创建工具 |
| PUT | /api/tools/{id} | 更新工具 |
| DELETE | /api/tools/{id} | 删除工具 |

### 操作日志
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/operation-logs | 获取操作日志列表(分页) |

### 首页
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/home/logs | 获取最近日志 |
| GET | /api/home/announcements | 获取公告 |
| GET | /api/home/quick-links | 获取快捷访问 |

### RuoYi 系统管理
| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/system/user/list | 系统用户列表 |
| GET | /api/system/role/list | 角色列表 |
| GET | /api/system/menu/list | 菜单列表 |
| GET | /api/system/dept/list | 部门列表 |
| GET | /api/system/post/list | 岗位列表 |
| GET | /api/system/config/list | 参数配置列表 |
| GET | /api/system/notice/list | 通知公告列表 |

---

## 统一响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {}
}
```

分页响应:
```json
{
  "code": 200,
  "message": "success",
  "data": {
    "records": [],
    "total": 100,
    "size": 10,
    "current": 1,
    "pages": 10
  }
}
```

错误响应:
```json
{
  "code": 500,
  "message": "Internal Server Error",
  "data": null
}
```

---

## 数据库配置

| Profile | 数据库 | 驱动 | URL | 启动方式 |
|---------|--------|------|-----|---------|
| h2 (默认) | H2 | org.h2.Driver | jdbc:h2:mem:zlinks_package_system;DB_CLOSE_DELAY=-1;MODE=MySQL | 默认启动 |
| mysql | MySQL 8+ | com.mysql.cj.jdbc.Driver | jdbc:mysql://localhost:3306/zlinks_package_system | --spring.profiles.active=mysql |
| (默认) | PostgreSQL 15+ | org.postgresql.Driver | jdbc:postgresql://localhost:5432/zlinks_package_system | --spring.profiles.active=pgsql |

连接池: Alibaba Druid。H2 使用 `schema.sql` 初始化; PG/MySQL 使用 `scripts/` 目录下的独立脚本。
