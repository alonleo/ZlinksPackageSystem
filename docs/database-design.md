# ZlinksPackageSystem 数据库设计

## 数据库表结构

### 1. 游戏表 (game)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| game_name | VARCHAR(100) | 游戏名 | NOT NULL |
| game_direction | VARCHAR(20) | 游戏方向 | NOT NULL |
| source | VARCHAR(50) | 来源 | |
| git_url | VARCHAR(255) | git地址 | |
| priority | INT | 优先级 | DEFAULT 0 |
| tags | VARCHAR(255) | 标签 | |
| project_type | VARCHAR(50) | 项目工程类型 | |
| manager | VARCHAR(50) | 负责人 | |
| white_branch | VARCHAR(100) | 白包分支 | |
| status | VARCHAR(20) | 项目状态 | DEFAULT 'active' |
| retention_record | TEXT | 游戏留存记录 | |
| android_folder_name | VARCHAR(100) | 安卓文件夹名称 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 2. 软著表 (copyright)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| copyright_name | VARCHAR(100) | 软著名 | NOT NULL |
| copyright_owner | VARCHAR(100) | 著作权人 | |
| copyright_number | VARCHAR(50) | 软著号 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 3. 平台匹配表 (platform_match)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| company_id | BIGINT | 上架主体公司ID | FOREIGN KEY |
| original_game | VARCHAR(100) | 原游戏 | |
| current_game_name | VARCHAR(100) | 现游戏名 | |
| batch | VARCHAR(50) | 批次 | |
| package_mode | VARCHAR(20) | 打包模式 | |
| sdk_version | VARCHAR(50) | SDK版本 | |
| apk_version | VARCHAR(20) | APK版本 | |
| platform_status | VARCHAR(20) | 平台状态 | DEFAULT 'pending' |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 4. 平台参数表

#### 4.1 荣耀参数表 (honor_param)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| game_id | BIGINT | 游戏ID | FOREIGN KEY |
| package_name | VARCHAR(100) | 包名 | |
| app_id | VARCHAR(100) | APPID | |
| app_secret | VARCHAR(100) | APP_SECRET | |
| media_id | VARCHAR(100) | MediaID | |
| agconnect_path | VARCHAR(255) | agconnect_services_json_path | |
| td_app_id | VARCHAR(100) | TDAPPID | |
| ad_param_status | VARCHAR(20) | 广告位参数状态 | |
| list_status | VARCHAR(20) | 黑白名单状态 | |
| operator | VARCHAR(50) | 经办人 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

#### 4.2 VIVO参数表 (vivo_param)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| game_id | BIGINT | 游戏ID | FOREIGN KEY |
| app_id | VARCHAR(100) | APPID | |
| contract_status | VARCHAR(20) | 签约状态 | |
| media_id | VARCHAR(100) | MediaID | |
| td_app_id | VARCHAR(100) | TDAPPID | |
| ad_param_status | VARCHAR(20) | 广告位参数状态 | |
| list_status | VARCHAR(20) | 黑白名单状态 | |
| operator | VARCHAR(50) | 经办人 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

#### 4.3 HUAWEI参数表 (huawei_param)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| game_id | BIGINT | 游戏ID | FOREIGN KEY |
| package_name | VARCHAR(100) | 包名 | |
| app_id | VARCHAR(100) | APPID | |
| agconnect_path | VARCHAR(255) | agconnect_services_json_path | |
| td_app_id | VARCHAR(100) | TDAPPID | |
| ad_param_status | VARCHAR(20) | 广告位参数状态 | |
| list_status | VARCHAR(20) | 黑白名单状态 | |
| operator | VARCHAR(50) | 经办人 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 5. 主体公司表 (company)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| company_name | VARCHAR(100) | 公司名 | NOT NULL |
| platform | VARCHAR(50) | 平台 | |
| account | VARCHAR(100) | 账号 | |
| password | VARCHAR(100) | 密码 | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 6. 签名文件信息表 (sign_file)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| company_id | BIGINT | 上架主体ID | FOREIGN KEY |
| store_file | VARCHAR(255) | store_file | |
| store_password | VARCHAR(100) | store_password | |
| key_alias | VARCHAR(100) | key_alias | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 7. 用户表 (user)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| username | VARCHAR(50) | 账号 | UNIQUE, NOT NULL |
| password | VARCHAR(100) | 密码 | NOT NULL |
| real_name | VARCHAR(50) | 姓名 | |
| status | VARCHAR(20) | 在职状态 | DEFAULT 'active' |
| group_id | BIGINT | 所属组ID | FOREIGN KEY |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 8. 权限组表 (permission_group)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| group_name | VARCHAR(50) | 组名称 | NOT NULL |
| group_permission | TEXT | 组权限(JSON) | |
| group_accounts | TEXT | 组账号(JSON) | |
| remark | TEXT | 备注信息 | |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

### 9. 通知表 (notification)
| 字段名 | 类型 | 说明 | 约束 |
|--------|------|------|------|
| id | BIGINT | 主键 | PRIMARY KEY, AUTO_INCREMENT |
| title | VARCHAR(100) | 通知标题 | NOT NULL |
| content | TEXT | 通知内容 | |
| module | VARCHAR(50) | 所属模块 | |
| target_id | BIGINT | 目标ID | |
| target_type | VARCHAR(20) | 目标类型 | |
| sender_id | BIGINT | 发送者ID | |
| receiver_ids | TEXT | 接收者ID列表(JSON) | |
| receiver_type | VARCHAR(20) | 接收类型(user/group) | |
| is_pinned | TINYINT | 是否置顶 | DEFAULT 0 |
| status | VARCHAR(20) | 状态 | DEFAULT 'unread' |
| create_time | DATETIME | 创建日期 | DEFAULT CURRENT_TIMESTAMP |
| update_time | DATETIME | 更新时间 | |
| is_deleted | TINYINT | 逻辑删除 | DEFAULT 0 |

## API接口设计

### 1. 认证接口
- `POST /api/auth/login` - 用户登录
- `POST /api/auth/logout` - 用户登出
- `GET /api/auth/info` - 获取用户信息

### 2. 用户管理接口
- `GET /api/users` - 获取用户列表
- `POST /api/users` - 创建用户
- `PUT /api/users/{id}` - 更新用户
- `DELETE /api/users/{id}` - 删除用户
- `GET /api/users/{id}` - 获取用户详情

### 3. 权限组接口
- `GET /api/permission-groups` - 获取权限组列表
- `POST /api/permission-groups` - 创建权限组
- `PUT /api/permission-groups/{id}` - 更新权限组
- `DELETE /api/permission-groups/{id}` - 删除权限组

### 4. 游戏管理接口
- `GET /api/games` - 获取游戏列表
- `POST /api/games` - 创建游戏
- `PUT /api/games/{id}` - 更新游戏
- `DELETE /api/games/{id}` - 删除游戏
- `GET /api/games/{id}` - 获取游戏详情

### 5. 软著管理接口
- `GET /api/copyrights` - 获取软著列表
- `POST /api/copyrights` - 创建软著
- `PUT /api/copyrights/{id}` - 更新软著
- `DELETE /api/copyrights/{id}` - 删除软著

### 6. 平台匹配接口
- `GET /api/platform-matches` - 获取平台匹配列表
- `POST /api/platform-matches` - 创建平台匹配
- `PUT /api/platform-matches/{id}` - 更新平台匹配
- `DELETE /api/platform-matches/{id}` - 删除平台匹配

### 7. 平台参数接口
- `GET /api/params/honor` - 获取荣耀参数列表
- `POST /api/params/honor` - 创建荣耀参数
- `PUT /api/params/honor/{id}` - 更新荣耀参数
- `DELETE /api/params/honor/{id}` - 删除荣耀参数

- `GET /api/params/vivo` - 获取VIVO参数列表
- `POST /api/params/vivo` - 创建VIVO参数
- `PUT /api/params/vivo/{id}` - 更新VIVO参数
- `DELETE /api/params/vivo/{id}` - 删除VIVO参数

- `GET /api/params/huawei` - 获取HUAWEI参数列表
- `POST /api/params/huawei` - 创建HUAWEI参数
- `PUT /api/params/huawei/{id}` - 更新HUAWEI参数
- `DELETE /api/params/huawei/{id}` - 删除HUAWEI参数

### 8. 主体公司接口
- `GET /api/companies` - 获取公司列表
- `POST /api/companies` - 创建公司
- `PUT /api/companies/{id}` - 更新公司
- `DELETE /api/companies/{id}` - 删除公司

### 9. 签名文件接口
- `GET /api/sign-files` - 获取签名文件列表
- `POST /api/sign-files` - 创建签名文件
- `PUT /api/sign-files/{id}` - 更新签名文件
- `DELETE /api/sign-files/{id}` - 删除签名文件

### 10. 通知接口
- `GET /api/notifications` - 获取通知列表
- `POST /api/notifications` - 创建通知
- `PUT /api/notifications/{id}/read` - 标记已读
- `GET /api/notifications/unread` - 获取未读通知

### 11. 首页接口
- `GET /api/home/logs` - 获取最近日志
- `GET /api/home/announcements` - 获取公告
- `GET /api/home/quick-links` - 获取快捷访问

### 12. 产品管理接口
- `GET /api/products` - 获取产品列表
- `POST /api/products` - 创建产品
- `PUT /api/products/{id}` - 更新产品
- `DELETE /api/products/{id}` - 删除产品
- `POST /api/products/{id}/package` - 主动打包

### 13. 测试管理接口
- `GET /api/tests` - 获取测试列表
- `GET /api/tests/{id}/status` - 获取打包状态
- `POST /api/tests/{id}/test` - 主动测试

## 统一响应格式
```json
{
  "code": 200,
  "message": "success",
  "data": {}
}
```

## 分页响应格式
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