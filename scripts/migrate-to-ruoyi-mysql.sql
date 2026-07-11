-- ============================================================
-- Zlinks Package System → RuoYi RBAC MySQL 迁移脚本
-- ============================================================
-- 用法: mysql -uroot -p zlinks_package_system < migrate-to-ruoyi-mysql.sql
-- 注意:
--   1. MySQL 默认自动提交, 此处用 START TRANSACTION + COMMIT/ROLLBACK
--   2. 旧表会重命名为 *_legacy 保留
--   3. 重复执行安全 (使用 NOT EXISTS 守护)
-- ============================================================

START TRANSACTION;

-- ==========================
-- 1. 新建 sys_* 表
-- ==========================

-- 部门表
CREATE TABLE IF NOT EXISTS `sys_dept` (
    `dept_id`     BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '部门id',
    `parent_id`   BIGINT(20) DEFAULT 0 COMMENT '父部门id',
    `ancestors`   VARCHAR(500) DEFAULT '' COMMENT '祖级列表',
    `dept_name`   VARCHAR(30) NOT NULL COMMENT '部门名称',
    `order_num`   INT(4) DEFAULT 0 COMMENT '显示顺序',
    `leader`      VARCHAR(20) DEFAULT NULL COMMENT '负责人',
    `phone`       VARCHAR(11) DEFAULT NULL COMMENT '联系电话',
    `email`       VARCHAR(50) DEFAULT NULL COMMENT '邮箱',
    `status`      CHAR(1) DEFAULT '0' COMMENT '部门状态（0正常 1停用）',
    `del_flag`    CHAR(1) DEFAULT '0' COMMENT '删除标志（0代表存在 2代表删除）',
    `create_by`   VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`   VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`      VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`dept_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='部门表';

-- 岗位表
CREATE TABLE IF NOT EXISTS `sys_post` (
    `post_id`     BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '岗位ID',
    `post_code`   VARCHAR(64) NOT NULL COMMENT '岗位编码',
    `post_name`   VARCHAR(50) NOT NULL COMMENT '岗位名称',
    `post_sort`   INT(4) DEFAULT 0 COMMENT '显示顺序',
    `status`      CHAR(1) DEFAULT '0' COMMENT '状态（0正常 1停用）',
    `create_by`   VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`   VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`      VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`post_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='岗位信息表';

-- 用户表
CREATE TABLE IF NOT EXISTS `sys_user` (
    `user_id`     BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '用户ID',
    `dept_id`     BIGINT(20) DEFAULT NULL COMMENT '部门ID',
    `user_name`   VARCHAR(30) NOT NULL COMMENT '登录账号',
    `nick_name`   VARCHAR(30) NOT NULL COMMENT '用户昵称',
    `email`       VARCHAR(50) DEFAULT '' COMMENT '用户邮箱',
    `phonenumber` VARCHAR(11) DEFAULT '' COMMENT '手机号码',
    `sex`         CHAR(1) DEFAULT '0' COMMENT '用户性别（0男 1女 2未知）',
    `avatar`      VARCHAR(255) DEFAULT '' COMMENT '头像地址',
    `password`    VARCHAR(100) DEFAULT '' COMMENT '密码',
    `status`      CHAR(1) DEFAULT '0' COMMENT '帐号状态（0正常 1停用）',
    `login_ip`    VARCHAR(128) DEFAULT '' COMMENT '最后登录IP',
    `login_date`  DATETIME DEFAULT NULL COMMENT '最后登录时间',
    `is_deleted`  TINYINT DEFAULT 0 COMMENT '逻辑删除',
    `create_by`   VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`   VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`      VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`user_id`),
    UNIQUE KEY `uk_sys_user_user_name` (`user_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户信息表';

-- 角色表
CREATE TABLE IF NOT EXISTS `sys_role` (
    `role_id`     BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '角色ID',
    `role_name`   VARCHAR(30) NOT NULL COMMENT '角色名称',
    `role_key`    VARCHAR(100) NOT NULL COMMENT '角色权限字符串',
    `role_sort`   INT(4) DEFAULT 0 COMMENT '显示顺序',
    `data_scope`  CHAR(1) DEFAULT '1' COMMENT '数据范围（1：全部 2：自定义 3：本部门 4：本部门及以下 5：仅本人）',
    `menu_check_strictly` TINYINT(1) DEFAULT 1 COMMENT '菜单树选择项是否关联显示',
    `dept_check_strictly` TINYINT(1) DEFAULT 1 COMMENT '部门树选择项是否关联显示',
    `status`      CHAR(1) DEFAULT '0' COMMENT '角色状态（0正常 1停用）',
    `del_flag`    CHAR(1) DEFAULT '0' COMMENT '删除标志（0代表存在 2代表删除）',
    `create_by`   VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`   VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`      VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`role_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='角色信息表';

-- 菜单/权限表
CREATE TABLE IF NOT EXISTS `sys_menu` (
    `menu_id`     BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '菜单ID',
    `menu_name`   VARCHAR(50) NOT NULL COMMENT '菜单名称',
    `parent_id`   BIGINT(20) DEFAULT 0 COMMENT '父菜单ID',
    `order_num`   INT(4) DEFAULT 0 COMMENT '显示顺序',
    `path`        VARCHAR(200) DEFAULT '' COMMENT '路由地址',
    `component`   VARCHAR(255) DEFAULT NULL COMMENT '组件路径',
    `query`       VARCHAR(255) DEFAULT NULL COMMENT '路由参数',
    `is_frame`    CHAR(1) DEFAULT '1' COMMENT '是否为外链（0是 1否）',
    `is_cache`    CHAR(1) DEFAULT '0' COMMENT '是否缓存（0缓存 1不缓存）',
    `menu_type`   CHAR(1) DEFAULT '' COMMENT '菜单类型（M目录 C菜单 F按钮）',
    `visible`     CHAR(1) DEFAULT '0' COMMENT '菜单状态（0显示 1隐藏）',
    `status`      CHAR(1) DEFAULT '0' COMMENT '菜单状态（0正常 1停用）',
    `perms`       VARCHAR(100) DEFAULT NULL COMMENT '权限标识',
    `icon`        VARCHAR(100) DEFAULT '#' COMMENT '菜单图标',
    `create_by`   VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`   VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`      VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`menu_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='菜单权限表';

-- 用户-角色关联表
CREATE TABLE IF NOT EXISTS `sys_user_role` (
    `user_id` BIGINT(20) NOT NULL COMMENT '用户ID',
    `role_id` BIGINT(20) NOT NULL COMMENT '角色ID',
    PRIMARY KEY (`user_id`, `role_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户和角色关联表';

-- 角色-菜单关联表
CREATE TABLE IF NOT EXISTS `sys_role_menu` (
    `role_id` BIGINT(20) NOT NULL COMMENT '角色ID',
    `menu_id` BIGINT(20) NOT NULL COMMENT '菜单ID',
    PRIMARY KEY (`role_id`, `menu_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='角色和菜单关联表';

-- 参数配置表
CREATE TABLE IF NOT EXISTS `sys_config` (
    `config_id`    BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '参数主键',
    `config_name`  VARCHAR(100) DEFAULT '' COMMENT '参数名称',
    `config_key`   VARCHAR(100) DEFAULT '' COMMENT '参数键名',
    `config_value` VARCHAR(500) DEFAULT '' COMMENT '参数键值',
    `config_type`  CHAR(1) DEFAULT 'N' COMMENT '系统内置（Y是 N否）',
    `is_builtin`   CHAR(1) DEFAULT 'N' COMMENT '是否内置',
    `create_by`    VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time`  DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`    VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time`  DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`       VARCHAR(500) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`config_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='参数配置表';

-- 通知公告表
CREATE TABLE IF NOT EXISTS `sys_notice` (
    `notice_id`      BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '公告ID',
    `notice_title`   VARCHAR(50) NOT NULL COMMENT '公告标题',
    `notice_type`    CHAR(1) NOT NULL COMMENT '公告类型（1通知 2公告）',
    `notice_content` TEXT COMMENT '公告内容',
    `status`         CHAR(1) DEFAULT '0' COMMENT '公告状态（0正常 1关闭）',
    `create_by`      VARCHAR(64) DEFAULT '' COMMENT '创建者',
    `create_time`    DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_by`      VARCHAR(64) DEFAULT '' COMMENT '更新者',
    `update_time`    DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `remark`         VARCHAR(255) DEFAULT NULL COMMENT '备注',
    PRIMARY KEY (`notice_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='通知公告表';

-- 操作日志表
CREATE TABLE IF NOT EXISTS `sys_oper_log` (
    `oper_id`        BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '日志主键',
    `title`          VARCHAR(50) DEFAULT '' COMMENT '模块标题',
    `business_type`  INT(1) DEFAULT 0 COMMENT '业务类型（0其它 1新增 2修改 3删除 ...）',
    `method`         VARCHAR(100) DEFAULT '' COMMENT '方法名称',
    `request_method` VARCHAR(10) DEFAULT '' COMMENT '请求方式',
    `operator_type`  INT(1) DEFAULT 0 COMMENT '操作类别（0其它 1后台用户 2手机端用户）',
    `oper_name`      VARCHAR(50) DEFAULT '' COMMENT '操作人员',
    `dept_name`      VARCHAR(50) DEFAULT '' COMMENT '部门名称',
    `oper_url`       VARCHAR(255) DEFAULT '' COMMENT '请求URL',
    `oper_ip`        VARCHAR(128) DEFAULT '' COMMENT '主机地址',
    `oper_param`     VARCHAR(2000) DEFAULT '' COMMENT '请求参数',
    `json_result`    VARCHAR(2000) DEFAULT '' COMMENT '返回参数',
    `status`         INT(1) DEFAULT 0 COMMENT '操作状态（0正常 1异常）',
    `error_msg`      VARCHAR(2000) DEFAULT '' COMMENT '错误消息',
    `oper_time`      DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '操作时间',
    PRIMARY KEY (`oper_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='操作日志记录';

-- 登录日志表
CREATE TABLE IF NOT EXISTS `sys_logininfor` (
    `info_id`        BIGINT(20) NOT NULL AUTO_INCREMENT COMMENT '访问ID',
    `user_name`      VARCHAR(50) DEFAULT '' COMMENT '用户账号',
    `ipaddr`         VARCHAR(128) DEFAULT '' COMMENT '登录IP地址',
    `login_location` VARCHAR(255) DEFAULT '' COMMENT '登录地点',
    `browser`        VARCHAR(50) DEFAULT '' COMMENT '浏览器类型',
    `os`             VARCHAR(50) DEFAULT '' COMMENT '操作系统',
    `status`         CHAR(1) DEFAULT '0' COMMENT '登录状态（0成功 1失败）',
    `msg`            VARCHAR(255) DEFAULT '' COMMENT '提示消息',
    `login_time`     DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '访问时间',
    PRIMARY KEY (`info_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='系统访问记录';

-- ==========================
-- 2. 数据迁移: user → sys_user
-- ==========================
INSERT INTO sys_user (user_name, nick_name, password, status, is_deleted, create_by, create_time, update_time, remark)
SELECT
    username,
    COALESCE(real_name, username),
    password,
    CASE WHEN status = 'active' THEN '0' ELSE '1' END,
    COALESCE(is_deleted, 0),
    'migration',
    COALESCE(create_time, CURRENT_TIMESTAMP),
    COALESCE(update_time, CURRENT_TIMESTAMP),
    COALESCE(remark, '')
FROM `user`
WHERE NOT EXISTS (SELECT 1 FROM sys_user WHERE sys_user.user_name = `user`.username);

-- ==========================
-- 3. 数据迁移: permission_group → sys_role
-- ==========================
INSERT INTO sys_role (role_name, role_key, role_sort, data_scope, status, del_flag, create_by, create_time, update_time, remark)
SELECT
    group_name,
    CONCAT('group_', id),
    id,
    '1',
    '0',
    '0',
    'migration',
    COALESCE(create_time, CURRENT_TIMESTAMP),
    COALESCE(update_time, CURRENT_TIMESTAMP),
    COALESCE(group_permission, '')
FROM permission_group
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE sys_role.role_key = CONCAT('group_', permission_group.id));

-- ==========================
-- 4. 数据迁移: user_group → sys_user_role
-- ==========================
INSERT INTO sys_user_role (user_id, role_id)
SELECT ug.user_id, ug.group_id
FROM user_group ug
WHERE EXISTS (SELECT 1 FROM sys_user WHERE sys_user.user_id = ug.user_id)
  AND EXISTS (SELECT 1 FROM sys_role WHERE sys_role.role_id = ug.group_id)
  AND NOT EXISTS (
    SELECT 1 FROM sys_user_role sur
    WHERE sur.user_id = ug.user_id AND sur.role_id = ug.group_id
  );

-- ==========================
-- 5. 旧表归档 (IF EXISTS + 检查 legacy 是否已存在)
-- ==========================
DROP PROCEDURE IF EXISTS rename_to_legacy;
DELIMITER //
CREATE PROCEDURE rename_to_legacy()
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_legacy' AND table_schema = DATABASE()) THEN
        RENAME TABLE `user` TO `user_legacy`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'permission_group' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'permission_group_legacy' AND table_schema = DATABASE()) THEN
        RENAME TABLE `permission_group` TO `permission_group_legacy`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_group' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_group_legacy' AND table_schema = DATABASE()) THEN
        RENAME TABLE `user_group` TO `user_group_legacy`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'notification' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'notification_legacy' AND table_schema = DATABASE()) THEN
        RENAME TABLE `notification` TO `notification_legacy`;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'operation_log' AND table_schema = DATABASE())
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'operation_log_legacy' AND table_schema = DATABASE()) THEN
        RENAME TABLE `operation_log` TO `operation_log_legacy`;
    END IF;
END //
DELIMITER ;

CALL rename_to_legacy();
DROP PROCEDURE rename_to_legacy;

COMMIT;

-- 默认菜单/角色/用户由后端 DataInitializer 自动插入