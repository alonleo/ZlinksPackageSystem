# Zlinks Package System

游戏包管理系统 - 集成游戏管理、产品上架、测试管理的一体化解决方案

## 项目结构

```
ZlinksPackageSystem/
├── backend/                    # 后端服务 (Spring Boot)
│   ├── src/
│   │   ├── main/
│   │   │   ├── java/
│   │   │   │   └── com/zlinks/package_system/
│   │   │   │       ├── controller/    # REST控制器
│   │   │   │       ├── service/       # 业务逻辑
│   │   │   │       ├── mapper/        # MyBatis映射器
│   │   │   │       ├── entity/        # 实体类
│   │   │   │       ├── config/        # 配置类
│   │   │   │       ├── security/      # 安全认证
│   │   │   │       └── util/          # 工具类
│   │   │   └── resources/
│   │   │       ├── application.yml    # 应用配置
│   │   │       └── mapper/           # MyBatis XML
│   │   └── test/                     # 单元测试
│   └── pom.xml                       # Maven配置
│
├── frontend/                   # 前端应用 (Vue 3)
│   ├── src/
│   │   ├── api/                # API接口
│   │   ├── assets/             # 静态资源
│   │   ├── components/         # 公共组件
│   │   ├── layouts/            # 布局组件
│   │   ├── router/             # 路由配置
│   │   ├── stores/             # 状态管理
│   │   ├── types/              # TypeScript类型
│   │   └── views/              # 页面视图
│   ├── package.json            # 依赖配置
│   └── vite.config.ts          # Vite配置
│
├── desktop/                    # 桌面应用 (WPF)
│   └── ZlinksPackageSystem.Desktop/
│       ├── ViewModels/         # 视图模型
│       ├── Views/              # 视图
│       ├── Models/             # 数据模型
│       ├── Services/           # 服务
│       └── *.csproj            # 项目文件
│
├── docs/                       # 项目文档
│   └── database-design.md      # 数据库设计
│
├── scripts/                    # 脚本文件
│   ├── start-backend.bat       # 后端启动脚本
│   ├── start-frontend.bat      # 前端启动脚本
│   ├── start-desktop.bat       # 桌面端启动脚本
│   └── init-database.sql       # 数据库初始化
│
└── 需求表结构.md                # 需求文档
```

## 技术栈

### 后端
- **框架**: Spring Boot 2.7.18
- **ORM**: MyBatis-Plus 3.5.5
- **数据库**: MySQL 8.0
- **连接池**: Druid
- **安全**: Spring Security + JWT
- **文档**: Knife4j (Swagger)

### 前端
- **框架**: Vue 3.4
- **构建**: Vite 5
- **语言**: TypeScript 5.3
- **UI**: Element Plus 2.6
- **状态**: Pinia 2.1
- **路由**: Vue Router 4.3
- **HTTP**: Axios

### 桌面端
- **框架**: .NET 6 WPF
- **模式**: MVVM
- **工具包**: CommunityToolkit.Mvvm
- **UI**: MaterialDesign
- **DI**: Microsoft.Extensions.DependencyInjection

## 快速开始

### 环境要求
- JDK 1.8+
- Node.js 18+ / Bun
- .NET 6 SDK
- MySQL 8.0

### 数据库初始化
```bash
mysql -u root -p < scripts/init-database.sql
```

### 启动后端
```bash
cd backend
mvnw.cmd spring-boot:run
```

### 启动前端
```bash
cd frontend
npm install
npm run dev
```

### 启动桌面端
```bash
cd desktop/ZlinksPackageSystem.Desktop
dotnet run
```

## 访问地址

- 前端: http://localhost:3000
- 后端API: http://localhost:8080
- Swagger文档: http://localhost:8080/doc.html
- Druid监控: http://localhost:8080/druid

## 默认账号

- 用户名: admin
- 密码: admin123

## 功能模块

1. **首页**: 最近日志、公告展示、快捷访问
2. **游戏管理**: 游戏信息CRUD、Git配置
3. **产品管理**: 上架产品管理、批量打包
4. **测试管理**: 打包状态、测试执行
5. **用户管理**: 用户CRUD、状态管理
6. **权限管理**: 权限组配置、功能权限
7. **公司管理**: 主体公司信息
8. **软著管理**: 软著信息维护
9. **通知管理**: 系统通知、模块通知

## 故障排除

### 登录失败

如果使用 admin/admin123 登录失败，请检查：

1. **MySQL服务是否启动**
   ```bash
   # 检查MySQL服务状态
   mysql -u root -p -e "SELECT 1"
   ```

2. **数据库是否创建**
   ```bash
   mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS zlinks_package_system DEFAULT CHARACTER SET utf8mb4;"
   ```

3. **初始化脚本是否执行**
   ```bash
   mysql -u root -p zlinks_package_system < scripts/init-database.sql
   ```

4. **后端服务是否启动**
   - 检查8080端口是否被占用
   - 查看后端日志是否有错误信息

5. **数据库连接配置**
   - 检查 `backend/src/main/resources/application.yml` 中的数据库连接信息
   - 默认用户名: root, 密码: root

### 自动初始化

系统启动时会自动检查并创建默认管理员账号：
- 如果数据库中没有admin用户，会自动创建
- 默认密码: admin123
- 密码使用BCrypt加密存储

### 常见问题

1. **端口冲突**
   - 后端默认端口: 8080
   - 前端默认端口: 3000
   - 可在配置文件中修改

2. **数据库连接失败**
   - 检查MySQL服务是否启动
   - 检查用户名密码是否正确
   - 检查数据库名称是否正确

3. **前端无法访问后端**
   - 确保后端服务已启动
   - 检查vite.config.ts中的代理配置

## 开发规范

- 后端遵循RESTful API设计规范
- 前端使用Composition API + TypeScript
- 桌面端使用MVVM模式
- 数据库使用逻辑删除
- 统一响应格式和异常处理