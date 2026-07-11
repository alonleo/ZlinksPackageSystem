-- ============================================================
-- RuoYi 迁移回退脚本 (PostgreSQL & MySQL 通用)
-- ============================================================
-- 用法:
--   PostgreSQL: psql -d zlinks_package_system -f rollback-ruoyi.sql
--   MySQL:      mysql -uroot -p zlinks_package_system < rollback-ruoyi.sql
-- 作用: 删除 sys_* 表, 把 *_legacy 改回原名
-- 注意:
--   - 必须在迁移脚本已运行、已存在 *_legacy 表的前提执行
--   - 此操作会丢失 sys_* 表上的所有新数据 (用户/角色/菜单变更)
--   - 操作前请先备份数据库
-- ============================================================

-- ============================================================
-- PostgreSQL 版本 (无 DELIMITER 块)
-- ============================================================
-- 仅作说明, 实际 SQL 兼容 MySQL 8+ 与 PostgreSQL 13+
-- 注意: MySQL 需要在脚本中切换 DELIMITER, 这里用 IF EXISTS 兼容写法

-- 删除 sys_* 表 (PostgreSQL CASCADE)
DROP TABLE IF EXISTS sys_user_role CASCADE;
DROP TABLE IF EXISTS sys_role_menu CASCADE;
DROP TABLE IF EXISTS sys_user CASCADE;
DROP TABLE IF EXISTS sys_role CASCADE;
DROP TABLE IF EXISTS sys_menu CASCADE;
DROP TABLE IF EXISTS sys_dept CASCADE;
DROP TABLE IF EXISTS sys_post CASCADE;
DROP TABLE IF EXISTS sys_config CASCADE;
DROP TABLE IF EXISTS sys_notice CASCADE;
DROP TABLE IF EXISTS sys_oper_log CASCADE;
DROP TABLE IF EXISTS sys_logininfor CASCADE;

-- 把 legacy 改回原名 (PostgreSQL)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users_legacy' AND table_schema = current_schema())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users' AND table_schema = current_schema()) THEN
        EXECUTE 'ALTER TABLE users_legacy RENAME TO users';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users_legacy' IS NULL) THEN
        -- MySQL 用过 user 表的, 重命名回 user
        NULL;
    END IF;
EXCEPTION WHEN OTHERS THEN
    NULL;
END $$;

-- MySQL 兼容: 单独处理 user_legacy (因为 user 在 MySQL 是关键字, 旧表名是 user_legacy)
-- 注意: 在 MySQL 中, 旧 user 表可能被命名为 `user_legacy`
DROP PROCEDURE IF EXISTS rollback_rename;
DELIMITER //
CREATE PROCEDURE rollback_rename()
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user' AND table_schema = DATABASE()) THEN
        RENAME TABLE `user_legacy` TO `user`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users' AND table_schema = DATABASE()) THEN
        RENAME TABLE `users_legacy` TO `users`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'permission_group_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'permission_group' AND table_schema = DATABASE()) THEN
        RENAME TABLE `permission_group_legacy` TO `permission_group`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_group_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_group' AND table_schema = DATABASE()) THEN
        RENAME TABLE `user_group_legacy` TO `user_group`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'notification_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'notification' AND table_schema = DATABASE()) THEN
        RENAME TABLE `notification_legacy` TO `notification`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'operation_log_legacy' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'operation_log' AND table_schema = DATABASE()) THEN
        RENAME TABLE `operation_log_legacy` TO `operation_log`;
    END IF;
END //
DELIMITER ;

-- 在 MySQL 中执行 (PostgreSQL 会忽略)
CALL rollback_rename();
DROP PROCEDURE rollback_rename;

-- ============================================================
-- 回退完成. 业务表(game/product/company/copyright 等) 未被本脚本删除
-- ============================================================