-- Zlinks Package System Database Initialization Script

CREATE DATABASE IF NOT EXISTS zlinks_package_system DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE zlinks_package_system;

-- 游戏表
CREATE TABLE IF NOT EXISTS `game` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `game_name` VARCHAR(100) NOT NULL COMMENT '游戏名',
    `game_direction` VARCHAR(20) NOT NULL COMMENT '游戏方向',
    `source` VARCHAR(50) COMMENT '来源',
    `git_url` VARCHAR(255) COMMENT 'git地址',
    `priority` INT DEFAULT 0 COMMENT '优先级',
    `tags` VARCHAR(255) COMMENT '标签',
    `project_type` VARCHAR(50) COMMENT '项目工程类型',
    `manager` VARCHAR(50) COMMENT '负责人',
    `white_branch` VARCHAR(100) COMMENT '白包分支',
    `status` VARCHAR(20) DEFAULT 'active' COMMENT '项目状态',
    `retention_record` TEXT COMMENT '游戏留存记录',
    `android_folder_name` VARCHAR(100) COMMENT '安卓文件夹名称',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='游戏表';

-- 平台表
CREATE TABLE IF NOT EXISTS `platform` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `platform_name` VARCHAR(100) NOT NULL COMMENT '平台名称',
    `platform_code` VARCHAR(50) COMMENT '平台编码',
    `sort_order` INT DEFAULT 0 COMMENT '排序',
    `status` VARCHAR(20) DEFAULT 'active' COMMENT '状态',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='平台表';

-- 软著表
CREATE TABLE IF NOT EXISTS `copyright` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `copyright_name` VARCHAR(100) NOT NULL COMMENT '软著名',
    `copyright_owner` VARCHAR(100) COMMENT '著作权人',
    `copyright_number` VARCHAR(50) COMMENT '软著号',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='软著表';

-- 主体公司表
CREATE TABLE IF NOT EXISTS `company` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `company_name` VARCHAR(100) NOT NULL COMMENT '公司名',
    `platform_id` BIGINT COMMENT '平台ID',
    `account` VARCHAR(100) COMMENT '账号',
    `password` VARCHAR(100) COMMENT '密码',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='主体公司表';

-- 平台匹配表
CREATE TABLE IF NOT EXISTS `platform_match` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `company_id` BIGINT COMMENT '上架主体公司ID',
    `original_game` VARCHAR(100) COMMENT '原游戏',
    `current_game_name` VARCHAR(100) COMMENT '现游戏名',
    `batch` VARCHAR(50) COMMENT '批次',
    `package_mode` VARCHAR(20) COMMENT '打包模式',
    `sdk_version` VARCHAR(50) COMMENT 'SDK版本',
    `apk_version` VARCHAR(20) COMMENT 'APK版本',
    `platform_status` VARCHAR(20) DEFAULT 'pending' COMMENT '平台状态',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`company_id`) REFERENCES `company`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='平台匹配表';

-- 荣耀参数表
CREATE TABLE IF NOT EXISTS `honor_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `product_id` BIGINT NOT NULL COMMENT '产品ID',
    `package_name` VARCHAR(100) COMMENT '包名',
    `app_id` VARCHAR(100) COMMENT 'APPID',
    `app_secret` VARCHAR(100) COMMENT 'APP_SECRET',
    `media_id` VARCHAR(100) COMMENT 'MediaID',
    `agconnect_path` VARCHAR(255) COMMENT 'agconnect_services_json_path',
    `td_app_id` VARCHAR(100) COMMENT 'TDAPPID',
    `ad_param_status` VARCHAR(20) COMMENT '广告位参数状态',
    `list_status` VARCHAR(20) COMMENT '黑白名单状态',
    `operator` VARCHAR(50) COMMENT '经办人',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '',
    `update_by` VARCHAR(64) DEFAULT '',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`product_id`) REFERENCES `product`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='荣耀参数表';

-- VIVO参数表
CREATE TABLE IF NOT EXISTS `vivo_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `product_id` BIGINT NOT NULL COMMENT '产品ID',
    `app_id` VARCHAR(100) COMMENT 'APPID',
    `contract_status` VARCHAR(20) COMMENT '签约状态',
    `media_id` VARCHAR(100) COMMENT 'MediaID',
    `td_app_id` VARCHAR(100) COMMENT 'TDAPPID',
    `ad_param_status` VARCHAR(20) COMMENT '广告位参数状态',
    `list_status` VARCHAR(20) COMMENT '黑白名单状态',
    `operator` VARCHAR(50) COMMENT '经办人',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '',
    `update_by` VARCHAR(64) DEFAULT '',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`product_id`) REFERENCES `product`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='VIVO参数表';

-- HUAWEI参数表
CREATE TABLE IF NOT EXISTS `huawei_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `product_id` BIGINT NOT NULL COMMENT '产品ID',
    `package_name` VARCHAR(100) COMMENT '包名',
    `app_id` VARCHAR(100) COMMENT 'APPID',
    `agconnect_path` VARCHAR(255) COMMENT 'agconnect_services_json_path',
    `td_app_id` VARCHAR(100) COMMENT 'TDAPPID',
    `ad_param_status` VARCHAR(20) COMMENT '广告位参数状态',
    `list_status` VARCHAR(20) COMMENT '黑白名单状态',
    `operator` VARCHAR(50) COMMENT '经办人',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '',
    `update_by` VARCHAR(64) DEFAULT '',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`product_id`) REFERENCES `product`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='HUAWEI参数表';

-- 签名文件信息表
CREATE TABLE IF NOT EXISTS `sign_file` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `company_id` BIGINT COMMENT '上架主体ID',
    `store_file` VARCHAR(255) COMMENT 'store_file',
    `store_password` VARCHAR(100) COMMENT 'store_password',
    `key_alias` VARCHAR(100) COMMENT 'key_alias',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`company_id`) REFERENCES `company`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='签名文件信息表';

-- 产品表
CREATE TABLE IF NOT EXISTS `product` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `copyright_id` BIGINT COMMENT '软著ID',
    `game_id` BIGINT COMMENT '游戏ID',
    `company_id` BIGINT COMMENT '公司ID',
    `platform_id` BIGINT COMMENT '平台ID',
    `package_name` VARCHAR(100) COMMENT '包名',
    `sdk_version` VARCHAR(50) COMMENT 'SDK版本',
    `apk_version` VARCHAR(50) COMMENT 'APK版本',
    `batch` VARCHAR(50) COMMENT '批次',
    `package_mode` VARCHAR(20) COMMENT '打包模式',
    `status` VARCHAR(20) DEFAULT 'pending' COMMENT '状态',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`copyright_id`) REFERENCES `copyright`(`id`),
    FOREIGN KEY (`game_id`) REFERENCES `game`(`id`),
    FOREIGN KEY (`company_id`) REFERENCES `company`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='产品表';

-- 权限组表
CREATE TABLE IF NOT EXISTS `permission_group` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `group_name` VARCHAR(50) NOT NULL COMMENT '组名称',
    `group_permission` TEXT COMMENT '组权限(JSON)',
    `group_accounts` TEXT COMMENT '组账号(JSON)',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='权限组表';

-- 权限组模块范围表
CREATE TABLE IF NOT EXISTS `permission_scope` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `group_id` BIGINT NOT NULL COMMENT '权限组ID',
    `scope` VARCHAR(16) NOT NULL COMMENT '作用域: backend/desktop',
    `modules_text` TEXT COMMENT '模块列表(JSON数组)',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_group_scope` (`group_id`, `scope`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='权限组模块范围表';

-- 用户表 (注意: 表名用 users, 避免 user 是 MySQL 保留字/与 RuoYi sys_user 迁移冲突)
CREATE TABLE IF NOT EXISTS `users` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(50) NOT NULL COMMENT '账号',
    `password` VARCHAR(100) NOT NULL COMMENT '密码',
    `real_name` VARCHAR(50) COMMENT '姓名',
    `status` VARCHAR(20) DEFAULT 'active' COMMENT '在职状态',
    `remark` TEXT COMMENT '备注信息',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户表';

-- 用户权限组关联表
CREATE TABLE IF NOT EXISTS `user_group` (
    `user_id` BIGINT NOT NULL,
    `group_id` BIGINT NOT NULL,
    PRIMARY KEY (`user_id`, `group_id`),
    FOREIGN KEY (`user_id`) REFERENCES `users`(`id`),
    FOREIGN KEY (`group_id`) REFERENCES `permission_group`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户权限组关联表';

-- 操作日志表
CREATE TABLE IF NOT EXISTS `operation_log` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `user_id` BIGINT COMMENT '操作用户ID',
    `username` VARCHAR(50) COMMENT '操作用户名',
    `module` VARCHAR(50) COMMENT '操作模块',
    `action` VARCHAR(20) COMMENT '操作类型',
    `target` VARCHAR(200) COMMENT '操作目标',
    `ip_address` VARCHAR(50) COMMENT '操作IP',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    KEY `idx_oplog_user_id` (`user_id`),
    KEY `idx_oplog_module` (`module`),
    KEY `idx_oplog_create_time` (`create_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='操作日志表';

-- 通知表
CREATE TABLE IF NOT EXISTS `notification` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `title` VARCHAR(100) NOT NULL COMMENT '通知标题',
    `content` TEXT COMMENT '通知内容',
    `module` VARCHAR(50) COMMENT '所属模块',
    `target_id` BIGINT COMMENT '目标ID',
    `target_type` VARCHAR(20) COMMENT '目标类型',
    `sender_id` BIGINT COMMENT '发送者ID',
    `receiver_ids` TEXT COMMENT '接收者ID列表(JSON)',
    `receiver_type` VARCHAR(20) COMMENT '接收类型(user/group)',
    `is_pinned` TINYINT DEFAULT 0 COMMENT '是否置顶',
    `status` VARCHAR(20) DEFAULT 'unread' COMMENT '状态',
    `create_by` VARCHAR(64) DEFAULT '' COMMENT '创建人',
    `update_by` VARCHAR(64) DEFAULT '' COMMENT '更新人',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='通知表';

-- 插入默认权限组
INSERT INTO `permission_group` (`group_name`, `group_permission`, `remark`) VALUES
('管理员组', '{"modules":["all"]}', '系统管理员，拥有所有权限'),
('开发组', '{"modules":["games","products"]}', '开发人员，负责游戏和产品管理'),
('测试组', '{"modules":["tests"]}', '测试人员，负责测试管理'),
('运营组', '{"modules":["products","companies"]}', '运营人员，负责产品和公司管理');

-- 插入默认权限组的模块范围（按 scope 拆分：backend/desktop）
-- 顺序与上面 permission_group INSERT 一致：1=管理员组, 2=开发组, 3=测试组, 4=运营组
INSERT INTO `permission_scope` (`group_id`, `scope`, `modules_text`, `create_by`) VALUES
(1, 'backend', '["all"]', 'system'),
(1, 'desktop',  '["all"]', 'system'),
(2, 'backend', '["home","package"]', 'system'),
(2, 'desktop',  '["home","products"]', 'system'),
(3, 'backend', '["home","package"]', 'system'),
(3, 'desktop',  '["home","tests"]', 'system'),
(4, 'backend', '["home","package"]', 'system'),
(4, 'desktop',  '["home","products"]', 'system');

-- 插入默认管理员用户 (密码: admin123)
INSERT INTO `users` (`username`, `password`, `real_name`, `status`) VALUES
('admin', '$2a$10$EqKcp1WFKVQIShMPC7B3kuznX9gAZMsVnSNjN0ABNuHVBCpzqABae', '系统管理员', 'active');

-- 将管理员用户关联到管理员组
INSERT INTO `user_group` (`user_id`, `group_id`) VALUES
((SELECT id FROM `users` WHERE username = 'admin'), 1);
-- 工具库表
CREATE TABLE IF NOT EXISTS `tool` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500),
    `category` VARCHAR(50),
    `version` VARCHAR(50),
    `status` VARCHAR(20) DEFAULT '未运行',
    `manager` VARCHAR(50),
    `run_mode` VARCHAR(20) DEFAULT 'Script',
    `language` VARCHAR(20),
    `interpreter_path` VARCHAR(500),
    `script_path` VARCHAR(500),
    `executable_path` VARCHAR(500),
    `working_directory` VARCHAR(500),
    `environment_variables` TEXT,
    `default_argument_prefix` VARCHAR(10) DEFAULT '--',
    `git_url` VARCHAR(500),
    `clone_directory` VARCHAR(500),
    `arguments_json` TEXT,
    `notification_json` TEXT,
    `is_system_builtin` TINYINT DEFAULT 0,
    `create_by` VARCHAR(64) DEFAULT '',
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_by` VARCHAR(64) DEFAULT '',
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- ==========================
-- RuoYi RBAC 系统表(由 F1 合并)
-- ==========================
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
