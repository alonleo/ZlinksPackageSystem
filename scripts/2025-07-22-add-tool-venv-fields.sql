-- ============================================================
-- 为 tool 表追加 Python venv 相关字段
-- 触发场景: 首次新建 Python 脚本工具后,venv 配置因后端
--           DTO/DB 缺字段而丢失,运行脚本时退回到系统 Python
--           找不到装在 venv 里的依赖模块。
--
-- 适用数据库: MySQL / PostgreSQL(按文件末尾切换注释选用)
--
-- 执行前请先备份;两条语句幂等:
--   - 列已存在跳过(标准 SQL IF NOT EXISTS / MySQL 5.6+ 需手写条件)
-- ============================================================

-- ---------- MySQL ----------
ALTER TABLE `tool`
    ADD COLUMN `create_venv` TINYINT DEFAULT 0 AFTER `is_system_builtin`,
    ADD COLUMN `venv_directory` VARCHAR(500) AFTER `create_venv`,
    ADD COLUMN `requirements_path` VARCHAR(500) AFTER `venv_directory`,
    ADD COLUMN `pip_mirror_url` VARCHAR(500) AFTER `requirements_path`;

-- ---------- PostgreSQL ----------
-- 把上面 MySQL 段和本段分别独立运行;不要在同一个会话中混跑。
--
-- ALTER TABLE tool
--     ADD COLUMN IF NOT EXISTS create_venv SMALLINT DEFAULT 0,
--     ADD COLUMN IF NOT EXISTS venv_directory VARCHAR(500),
--     ADD COLUMN IF NOT EXISTS requirements_path VARCHAR(500),
--     ADD COLUMN IF NOT EXISTS pip_mirror_url VARCHAR(500);
