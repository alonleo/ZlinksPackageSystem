-- H2 Database Schema for Zlinks Package System

-- 权限组表
CREATE TABLE IF NOT EXISTS `permission_group` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `group_name` VARCHAR(50) NOT NULL,
    `group_permission` TEXT,
    `group_accounts` TEXT,
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 用户表
CREATE TABLE IF NOT EXISTS users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    password VARCHAR(100) NOT NULL,
    real_name VARCHAR(50),
    status VARCHAR(20) DEFAULT 'active',
    group_id BIGINT,
    remark TEXT,
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP,
    is_deleted TINYINT DEFAULT 0
);

-- 游戏表
CREATE TABLE IF NOT EXISTS `game` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `game_name` VARCHAR(100) NOT NULL,
    `game_direction` VARCHAR(20) NOT NULL,
    `source` VARCHAR(50),
    `git_url` VARCHAR(255),
    `priority` INT DEFAULT 0,
    `tags` VARCHAR(255),
    `project_type` VARCHAR(50),
    `manager` VARCHAR(50),
    `white_branch` VARCHAR(100),
    `status` VARCHAR(20) DEFAULT 'active',
    `retention_record` TEXT,
    `android_folder_name` VARCHAR(100),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 软著表
CREATE TABLE IF NOT EXISTS `copyright` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `copyright_name` VARCHAR(100) NOT NULL,
    `copyright_owner` VARCHAR(100),
    `copyright_number` VARCHAR(50),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 主体公司表
CREATE TABLE IF NOT EXISTS `company` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `company_name` VARCHAR(100) NOT NULL,
    `platform` VARCHAR(50),
    `account` VARCHAR(100),
    `password` VARCHAR(100),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 平台匹配表
CREATE TABLE IF NOT EXISTS `platform_match` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `company_id` BIGINT,
    `original_game` VARCHAR(100),
    `current_game_name` VARCHAR(100),
    `batch` VARCHAR(50),
    `package_mode` VARCHAR(20),
    `sdk_version` VARCHAR(50),
    `apk_version` VARCHAR(20),
    `platform_status` VARCHAR(20) DEFAULT 'pending',
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 荣耀参数表
CREATE TABLE IF NOT EXISTS `honor_param` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `game_id` BIGINT,
    `package_name` VARCHAR(100),
    `app_id` VARCHAR(100),
    `app_secret` VARCHAR(100),
    `media_id` VARCHAR(100),
    `agconnect_path` VARCHAR(255),
    `td_app_id` VARCHAR(100),
    `ad_param_status` VARCHAR(20),
    `list_status` VARCHAR(20),
    `operator` VARCHAR(50),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- VIVO参数表
CREATE TABLE IF NOT EXISTS `vivo_param` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `game_id` BIGINT,
    `app_id` VARCHAR(100),
    `contract_status` VARCHAR(20),
    `media_id` VARCHAR(100),
    `td_app_id` VARCHAR(100),
    `ad_param_status` VARCHAR(20),
    `list_status` VARCHAR(20),
    `operator` VARCHAR(50),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- HUAWEI参数表
CREATE TABLE IF NOT EXISTS `huawei_param` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `game_id` BIGINT,
    `package_name` VARCHAR(100),
    `app_id` VARCHAR(100),
    `agconnect_path` VARCHAR(255),
    `td_app_id` VARCHAR(100),
    `ad_param_status` VARCHAR(20),
    `list_status` VARCHAR(20),
    `operator` VARCHAR(50),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 签名文件信息表
CREATE TABLE IF NOT EXISTS `sign_file` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `company_id` BIGINT,
    `store_file` VARCHAR(255),
    `store_password` VARCHAR(100),
    `key_alias` VARCHAR(100),
    `remark` TEXT,
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- 通知表
CREATE TABLE IF NOT EXISTS `notification` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `title` VARCHAR(100) NOT NULL,
    `content` TEXT,
    `module` VARCHAR(50),
    `target_id` BIGINT,
    `target_type` VARCHAR(20),
    `sender_id` BIGINT,
    `receiver_ids` TEXT,
    `receiver_type` VARCHAR(20),
    `is_pinned` TINYINT DEFAULT 0,
    `status` VARCHAR(20) DEFAULT 'unread',
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);