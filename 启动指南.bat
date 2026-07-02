@echo off
echo ========================================
echo Zlinks Package System - 启动指南
echo ========================================
echo.

echo 1. 请确保MySQL数据库已启动
echo 2. 创建数据库: zlinks_package_system
echo 3. 运行初始化脚本: scripts\init-database.sql
echo.

echo 步骤1: 初始化数据库
echo 请手动执行以下命令:
echo mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS zlinks_package_system DEFAULT CHARACTER SET utf8mb4;"
echo mysql -u root -p zlinks_package_system ^< scripts\init-database.sql
echo.

echo 步骤2: 启动后端服务
echo cd backend
echo mvnw.cmd spring-boot:run
echo 或者使用IDE运行 ZlinksPackageSystemApplication.java
echo.

echo 步骤3: 启动前端服务
echo cd frontend
echo npm install
echo npm run dev
echo.

echo 步骤4: 访问系统
echo 前端地址: http://localhost:3000
echo 后端API: http://localhost:8080
echo Swagger文档: http://localhost:8080/doc.html
echo.

echo 默认账号: admin
echo 默认密码: admin123
echo.

pause