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
    remark TEXT,
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP,
    is_deleted TINYINT DEFAULT 0
);

-- 用户权限组关联表
CREATE TABLE IF NOT EXISTS user_group (
    user_id BIGINT NOT NULL,
    group_id BIGINT NOT NULL,
    PRIMARY KEY (user_id, group_id)
);

-- 产品表
CREATE TABLE IF NOT EXISTS product (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    copyright_id BIGINT,
    game_id BIGINT,
    company_id BIGINT,
    platform VARCHAR(50),
    package_name VARCHAR(100),
    sdk_version VARCHAR(50),
    apk_version VARCHAR(50),
    batch VARCHAR(50),
    package_mode VARCHAR(20),
    status VARCHAR(20) DEFAULT 'pending',
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
    `is_deleted` TINYINT DEFAULT 0
);

-- 操作日志表
CREATE TABLE IF NOT EXISTS operation_log (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT,
    `username` VARCHAR(50),
    `module` VARCHAR(50),
    `action` VARCHAR(20),
    `target` VARCHAR(200),
    `ip_address` VARCHAR(50),
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);

-- ============================================================
-- RuoYi RBAC 表 (Agent 1)
-- ============================================================

CREATE TABLE IF NOT EXISTS sys_dept (
    dept_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    parent_id BIGINT DEFAULT 0,
    ancestors VARCHAR(500) DEFAULT '',
    dept_name VARCHAR(30) NOT NULL,
    order_num INT DEFAULT 0,
    leader VARCHAR(20),
    phone VARCHAR(11),
    email VARCHAR(50),
    status CHAR(1) DEFAULT '0',
    del_flag CHAR(1) DEFAULT '0',
    is_deleted SMALLINT DEFAULT 0,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_post (
    post_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    post_code VARCHAR(64) NOT NULL,
    post_name VARCHAR(50) NOT NULL,
    post_sort INT DEFAULT 0,
    status CHAR(1) DEFAULT '0',
    is_deleted SMALLINT DEFAULT 0,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_user (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    dept_id BIGINT,
    user_name VARCHAR(30) NOT NULL,
    nick_name VARCHAR(30) NOT NULL,
    email VARCHAR(50) DEFAULT '',
    phonenumber VARCHAR(11) DEFAULT '',
    sex CHAR(1) DEFAULT '0',
    avatar VARCHAR(255) DEFAULT '',
    password VARCHAR(100) DEFAULT '',
    status CHAR(1) DEFAULT '0',
    login_ip VARCHAR(128) DEFAULT '',
    login_date TIMESTAMP,
    is_deleted SMALLINT DEFAULT 0,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_role (
    role_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(30) NOT NULL,
    role_key VARCHAR(100) NOT NULL,
    role_sort INT DEFAULT 0,
    data_scope CHAR(1) DEFAULT '1',
    menu_check_strictly SMALLINT DEFAULT 1,
    dept_check_strictly SMALLINT DEFAULT 1,
    status CHAR(1) DEFAULT '0',
    del_flag CHAR(1) DEFAULT '0',
    is_deleted SMALLINT DEFAULT 0,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_menu (
    menu_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    menu_name VARCHAR(50) NOT NULL,
    parent_id BIGINT DEFAULT 0,
    order_num INT DEFAULT 0,
    path VARCHAR(200) DEFAULT '',
    component VARCHAR(255),
    query VARCHAR(255),
    is_frame CHAR(1) DEFAULT '1',
    is_cache CHAR(1) DEFAULT '0',
    menu_type CHAR(1) DEFAULT '',
    visible CHAR(1) DEFAULT '0',
    status CHAR(1) DEFAULT '0',
    perms VARCHAR(100),
    icon VARCHAR(100) DEFAULT '#',
    is_deleted SMALLINT DEFAULT 0,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_user_role (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS sys_role_menu (
    role_id BIGINT NOT NULL,
    menu_id BIGINT NOT NULL,
    PRIMARY KEY (role_id, menu_id)
);

CREATE TABLE IF NOT EXISTS sys_config (
    config_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    config_name VARCHAR(100) DEFAULT '',
    config_key VARCHAR(100) DEFAULT '',
    config_value VARCHAR(500) DEFAULT '',
    config_type CHAR(1) DEFAULT 'N',
    is_builtin CHAR(1) DEFAULT 'N',
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS sys_notice (
    notice_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    notice_title VARCHAR(50) NOT NULL,
    notice_type CHAR(1) NOT NULL,
    notice_content CLOB,
    status CHAR(1) DEFAULT '0',
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS sys_oper_log (
    oper_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(50) DEFAULT '',
    business_type INT DEFAULT 0,
    method VARCHAR(100) DEFAULT '',
    request_method VARCHAR(10) DEFAULT '',
    operator_type INT DEFAULT 0,
    oper_name VARCHAR(50) DEFAULT '',
    dept_name VARCHAR(50) DEFAULT '',
    oper_url VARCHAR(255) DEFAULT '',
    oper_ip VARCHAR(128) DEFAULT '',
    oper_param VARCHAR(2000) DEFAULT '',
    json_result VARCHAR(2000) DEFAULT '',
    status INT DEFAULT 0,
    error_msg VARCHAR(2000) DEFAULT '',
    oper_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sys_logininfor (
    info_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_name VARCHAR(50) DEFAULT '',
    ipaddr VARCHAR(128) DEFAULT '',
    login_location VARCHAR(255) DEFAULT '',
    browser VARCHAR(50) DEFAULT '',
    os VARCHAR(50) DEFAULT '',
    status CHAR(1) DEFAULT '0',
    msg VARCHAR(255) DEFAULT '',
    login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
