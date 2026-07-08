# Zlinks Package System 部署文档

## 环境要求

| 组件 | 最低版本 | 说明 |
|------|---------|------|
| JDK | 21+ | 后端运行环境 |
| PostgreSQL | 15+ | 默认数据库 |
| MySQL | 8.0 | 可选，通过 profile 切换 |
| Node.js | 18+ | 前端构建 |
| Nginx | 1.20+ | 反向代理（推荐） |

---

## 数据库准备

### PostgreSQL（默认）

```bash
# 安装 PostgreSQL 15+
sudo apt install postgresql-15

# 创建数据库
sudo -u postgres createdb zlinks_package_system

# 创建用户并授权
sudo -u postgres psql -c "CREATE USER root WITH PASSWORD 'root';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE zlinks_package_system TO root;"
sudo -u postgres psql -c "GRANT ALL ON SCHEMA public TO root;"
sudo -u postgres psql -d zlinks_package_system -c "GRANT ALL ON ALL TABLES IN SCHEMA public TO root;"

# 初始化表结构
psql -U root -d zlinks_package_system -f scripts/init-database-pgsql.sql
```

### MySQL（可选）

```bash
# 安装 MySQL 8.0
sudo apt install mysql-server-8.0

# 创建数据库
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS zlinks_package_system DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 初始化表结构
mysql -u root -p zlinks_package_system < scripts/init-database.sql
```

---

## 后端部署

### 1. 构建

```bash
cd backend

# 打包（跳过测试）
./mvnw clean package -DskipTests

# 产物位置: backend/target/package-system-1.0.0.jar
```

### 2. 运行

```bash
# PostgreSQL（默认）
java -jar target/package-system-1.0.0.jar

# MySQL
java -jar target/package-system-1.0.0.jar --spring.profiles.active=mysql

# 自定义端口和数据库地址
java -jar target/package-system-1.0.0.jar \
  --server.port=8080 \
  --spring.datasource.url=jdbc:postgresql://192.168.1.100:5432/zlinks_package_system
```

### 3. Systemd 服务

创建服务文件 `/etc/systemd/system/zlinks-backend.service`:

```ini
[Unit]
Description=Zlinks Package System Backend
After=network.target postgresql.service

[Service]
Type=simple
User=zlinks
WorkingDirectory=/opt/zlinks/backend
ExecStart=/usr/bin/java -jar /opt/zlinks/backend/package-system-1.0.0.jar
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

启动服务:

```bash
sudo systemctl daemon-reload
sudo systemctl enable zlinks-backend
sudo systemctl start zlinks-backend
```

---

## 前端部署

### 1. 构建

```bash
cd frontend

npm install
npm run build

# 产物位置: frontend/dist/
```

### 2. Nginx 配置

创建 `/etc/nginx/conf.d/zlinks.conf`:

```nginx
server {
    listen       80;
    server_name  your-domain.com;

    # 前端静态资源
    root   /opt/zlinks/frontend/dist;
    index  index.html;

    # SPA 路由回退
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API 反向代理
    location /api/ {
        proxy_pass http://127.0.0.1:8080/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 上传文件大小限制
    client_max_body_size 10M;
}
```

重新加载 Nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

---

## 桌面端部署

### 构建

```bash
cd desktop/ZlinksPackageSystem.Desktop

# 发布为独立应用
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

产物 `publish/` 目录可直接分发到目标 Windows 机器运行。

---

## 完整部署流程

```bash
# 1. 准备目录
sudo mkdir -p /opt/zlinks/{backend,frontend}
sudo useradd -r -s /bin/false zlinks

# 2. 初始化数据库（PostgreSQL）
sudo -u postgres createdb zlinks_package_system
psql -U root -d zlinks_package_system -f scripts/init-database-pgsql.sql

# 3. 部署后端
cp backend/target/package-system-1.0.0.jar /opt/zlinks/backend/
sudo cp zlinks-backend.service /etc/systemd/system/
sudo systemctl enable --now zlinks-backend

# 4. 部署前端
cp -r frontend/dist/* /opt/zlinks/frontend/
sudo cp zlinks.conf /etc/nginx/conf.d/
sudo nginx -t && sudo systemctl reload nginx

# 5. 设置权限
sudo chown -R zlinks:zlinks /opt/zlinks
```

---

## Profile 对照

| Profile | 数据库 | 启动参数 |
|---------|--------|---------|
| 默认（无） | PostgreSQL | `java -jar app.jar` |
| `mysql` | MySQL | `java -jar app.jar --spring.profiles.active=mysql` |
| `h2` | H2 内存库 | `java -jar app.jar --spring.profiles.active=h2` |

---

## 默认账号

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin | admin123 | 系统管理员 |

---

## 访问地址

| 服务 | 地址 |
|------|------|
| 前端页面 | http://your-domain.com |
| API 接口 | http://your-domain.com/api/ |
| Swagger 文档 | http://your-domain.com/api/doc.html |
| Druid 监控 | http://your-domain.com/api/druid |

---

## 故障排除

### 后端无法连接数据库

```bash
# 检查 PostgreSQL 运行状态
sudo systemctl status postgresql

# 测试连接
psql -U root -d zlinks_package_system -c "SELECT 1"

# 查看后端日志
sudo journalctl -u zlinks-backend -f
```

### Nginx 502 Bad Gateway

```bash
# 确认后端正在运行
curl http://127.0.0.1:8080/

# 检查 Nginx 错误日志
sudo tail -f /var/log/nginx/error.log
```

### 页面刷新 404

检查 Nginx 配置中 `try_files` 指令是否正确指向 `/index.html`。SPA 应用需要将未知路由回退到入口文件。
