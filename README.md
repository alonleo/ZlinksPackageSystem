# Zlinks Package System

游戏包管理系统 — 集成游戏管理、产品上架、多渠道参数配置、测试管理的一体化解决方案。

## 项目结构

```
ZlinksPackageSystem/
├── backend/                        # 后端服务 (Spring Boot 3.3.7)
├── frontend/                       # Web 前端 (Vue 3 + Element Plus)
├── desktop/                        # 桌面工具端 (Avalonia 11.2)
│   └── ZlinksPackageSystem.Desktop/
├── docs/                           # 项目文档
│   ├── database-design.md          # 数据库设计
│   └── superpowers/                # 功能规格与实施计划
├── scripts/                        # 脚本
│   ├── white-pack.py               # Unity+FakerAndroid 白包自动化处理
│   ├── init-database.sql           # MySQL 数据库初始化
│   ├── init-database-pgsql.sql     # PostgreSQL 数据库初始化
│   ├── migrate-to-ruoyi-mysql.sql  # MySQL RuoYi 迁移脚本
│   ├── migrate-to-ruoyi-pgsql.sql  # PostgreSQL RuoYi 迁移脚本
│   ├── rollback-ruoyi.sql          # RuoYi 迁移回滚
│   ├── start-backend.bat           # 后端启动
│   ├── start-frontend.bat          # 前端启动
│   └── start-desktop.bat           # 桌面端启动
├── 启动指南.bat
└── deploy.md                       # 部署文档
```

## 技术栈

### 后端
| 类别 | 技术 | 版本 |
|------|------|------|
| 框架 | Spring Boot | 3.3.7 |
| 语言 | Java / Jakarta EE | 21 / 9 |
| 安全 | Spring Security + JWT | jjwt 0.12.5 |
| ORM | MyBatis-Plus | 3.5.7 |
| 数据库 | PostgreSQL 15+ / MySQL 8.0 / H2 | — |
| 连接池 | Druid | 1.2.20 |
| 缓存 | Redis (Lettuce) | — |
| 定时任务 | Quartz | — |
| API 文档 | Knife4j (OpenAPI 3) | 4.5.0 |
| 工具 | Hutool / EasyExcel / OSHI | — |

### Web 前端
| 类别 | 技术 | 版本 |
|------|------|------|
| 框架 | Vue 3 (Composition API) | 3.4 |
| 语言 | TypeScript | 5.3 |
| 构建 | Vite | 5 |
| UI 库 | Element Plus | 2.6 |
| 状态管理 | Pinia | 2.1 |
| 路由 | Vue Router | 4.3 |
| 图表 | ECharts | 5.4 |

### 桌面端
| 类别 | 技术 | 版本 |
|------|------|------|
| 框架 | .NET / Avalonia UI | 10 / 11.2 |
| 模式 | MVVM | — |
| 工具包 | CommunityToolkit.Mvvm | 8.2.2 |
| DI | Microsoft.Extensions.DependencyInjection | 8.0 |
| 跨平台 | Windows / Linux / macOS | — |

## 快速开始

### 环境要求

- JDK 21+
- Node.js 18+ / Bun
- .NET 10 SDK
- Redis
- 数据库 (三选一)：PostgreSQL 15+ / MySQL 8.0 / H2 (开发免安装)

> **开发模式**: 默认使用 H2 内存数据库，无需安装任何数据库即可启动。

### 数据库初始化 (生产环境)

**PostgreSQL**
```bash
createdb zlinks_package_system
psql -d zlinks_package_system -f scripts/init-database-pgsql.sql
```

**MySQL**
```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS zlinks_package_system DEFAULT CHARACTER SET utf8mb4;"
mysql -u root -p zlinks_package_system < scripts/init-database.sql
```

### 启动后端

```bash
cd backend

# H2 内存数据库 (开发模式，默认)
./mvnw spring-boot:run

# PostgreSQL
./mvnw spring-boot:run -Dspring-boot.run.profiles=pgsql

# MySQL
./mvnw spring-boot:run -Dspring-boot.run.profiles=mysql
```

### 启动 Web 前端

```bash
cd frontend
npm install      # 或 bun install
npm run dev      # 或 bun run dev
```

### 启动桌面端

```bash
cd desktop/ZlinksPackageSystem.Desktop
dotnet run
```

## 访问地址

| 服务 | 地址 |
|------|------|
| Web 前端 | http://localhost:3000 |
| 后端 API | http://localhost:8080 |
| Swagger 文档 | http://localhost:8080/doc.html |
| Druid 监控 | http://localhost:8080/druid (admin / admin) |

## 默认账号

| 用户名 | 密码 | 说明 |
|--------|------|------|
| admin | admin123 | 系统启动时自动创建 |

## 功能模块

### 打包管理
- **游戏管理**: 游戏信息 CRUD、Git 仓库配置、工程类型/白包分支管理
- **产品管理**: 上架产品管理、批量打包触发
- **测试管理**: 打包状态跟踪与测试执行
- **广告参数**: 荣耀 / VIVO / 华为三平台参数配置，支持 Excel 导入导出
- **平台管理**: 平台匹配信息维护
- **签名管理**: 签名证书信息管理
- **软著管理**: 软著信息维护
- **公司管理**: 上架主体公司信息管理

### 系统管理 (RuoYi RBAC)
- **用户管理**: 系统用户 CRUD、状态管理
- **角色管理**: 角色配置、菜单权限分配
- **菜单管理**: 侧边栏菜单树配置
- **部门管理**: 组织架构管理
- **岗位管理**: 岗位信息维护
- **参数设置**: 系统参数配置
- **通知公告**: 系统通知管理

### 系统监控
- **服务监控**: CPU / 内存 / JVM / 磁盘实时监控仪表盘
- **缓存监控**: Redis 缓存详情
- **在线用户**: 当前在线用户列表与强制下线
- **定时任务**: Quartz 任务调度管理
- **操作日志**: 系统操作审计
- **登录日志**: 登录成功/失败记录

### 桌面端
- 游戏/产品/参数查看与编辑
- 工具库 (自定义脚本/可执行程序管理、Git 克隆、进程管理)
- 飞书通知集成
- 离线 admin 本地登录

## Profile 切换

| Profile | 数据库 | 适用场景 |
|---------|--------|----------|
| (默认/h2) | H2 内存库 | 本地开发，免安装 |
| pgsql | PostgreSQL | 生产环境 (推荐) |
| mysql | MySQL | 生产环境 (可选) |

## 故障排除

### 登录失败

1. **确认数据库已启动并初始化**
2. **确认 Redis 已启动** (`redis-cli ping`)
3. **检查后端服务是否正常启动** (查看日志)
4. **H2 开发模式下重启后端会重置数据**，系统会自动重建 admin 账号

### 端口冲突

| 服务 | 默认端口 | 配置位置 |
|------|----------|----------|
| 后端 | 8080 | `application.yml` → `server.port` |
| 前端 | 3000 | `vite.config.ts` |
| PostgreSQL | 5432 | `application.yml` |
| MySQL | 3306 | `application-mysql.yml` |
| Redis | 6379 | `application.yml` |

### 前端无法访问后端

确保后端已启动，检查 `vite.config.ts` 中代理配置 (默认代理 `/api` → `http://localhost:8080`)。

## 开发规范

- 后端遵循 RESTful API 设计规范
- 前端使用 Composition API + TypeScript
- 桌面端使用 MVVM 模式
- 数据库使用逻辑删除 (`isDeleted` 字段)
- 统一 `Result` / `PageResult` 响应格式
- 全局异常处理 + 操作日志 AOP
