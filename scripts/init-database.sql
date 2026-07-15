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
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`company_id`) REFERENCES `company`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='平台匹配表';

-- 荣耀参数表
CREATE TABLE IF NOT EXISTS `honor_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `game_id` BIGINT COMMENT '游戏ID',
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
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`game_id`) REFERENCES `game`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='荣耀参数表';

-- VIVO参数表
CREATE TABLE IF NOT EXISTS `vivo_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `game_id` BIGINT COMMENT '游戏ID',
    `app_id` VARCHAR(100) COMMENT 'APPID',
    `contract_status` VARCHAR(20) COMMENT '签约状态',
    `media_id` VARCHAR(100) COMMENT 'MediaID',
    `td_app_id` VARCHAR(100) COMMENT 'TDAPPID',
    `ad_param_status` VARCHAR(20) COMMENT '广告位参数状态',
    `list_status` VARCHAR(20) COMMENT '黑白名单状态',
    `operator` VARCHAR(50) COMMENT '经办人',
    `remark` TEXT COMMENT '备注信息',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`game_id`) REFERENCES `game`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='VIVO参数表';

-- HUAWEI参数表
CREATE TABLE IF NOT EXISTS `huawei_param` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `game_id` BIGINT COMMENT '游戏ID',
    `package_name` VARCHAR(100) COMMENT '包名',
    `app_id` VARCHAR(100) COMMENT 'APPID',
    `agconnect_path` VARCHAR(255) COMMENT 'agconnect_services_json_path',
    `td_app_id` VARCHAR(100) COMMENT 'TDAPPID',
    `ad_param_status` VARCHAR(20) COMMENT '广告位参数状态',
    `list_status` VARCHAR(20) COMMENT '黑白名单状态',
    `operator` VARCHAR(50) COMMENT '经办人',
    `remark` TEXT COMMENT '备注信息',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`),
    FOREIGN KEY (`game_id`) REFERENCES `game`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='HUAWEI参数表';

-- 签名文件信息表
CREATE TABLE IF NOT EXISTS `sign_file` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `company_id` BIGINT COMMENT '上架主体ID',
    `store_file` VARCHAR(255) COMMENT 'store_file',
    `store_password` VARCHAR(100) COMMENT 'store_password',
    `key_alias` VARCHAR(100) COMMENT 'key_alias',
    `remark` TEXT COMMENT '备注信息',
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
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
    `update_time` DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `is_deleted` TINYINT DEFAULT 0 COMMENT '逻辑删除',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='权限组表';

-- 用户表
CREATE TABLE IF NOT EXISTS `user` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(50) NOT NULL COMMENT '账号',
    `password` VARCHAR(100) NOT NULL COMMENT '密码',
    `real_name` VARCHAR(50) COMMENT '姓名',
    `status` VARCHAR(20) DEFAULT 'active' COMMENT '在职状态',
    `remark` TEXT COMMENT '备注信息',
    `create_time` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建日期',
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
    FOREIGN KEY (`user_id`) REFERENCES `user`(`id`),
    FOREIGN KEY (`group_id`) REFERENCES `permission_group`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户权限组关联表';

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

-- 插入默认管理员用户 (密码: admin123)
INSERT INTO `user` (`username`, `password`, `real_name`, `status`) VALUES
('admin', '$2a$10$EqKcp1WFKVQIShMPC7B3kuznX9gAZMsVnSNjN0ABNuHVBCpzqABae', '系统管理员', 'active');

-- 将管理员用户关联到管理员组
INSERT INTO `user_group` (`user_id`, `group_id`) VALUES
((SELECT id FROM `user` WHERE username = 'admin'), 1);