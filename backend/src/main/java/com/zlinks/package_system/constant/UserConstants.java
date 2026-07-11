package com.zlinks.package_system.constant;

/**
 * 用户常量
 */
public class UserConstants {

    /** 默认用户密码 */
    public static final String DEFAULT_PASSWORD = "123456";

    /** 超级管理员用户名 */
    public static final String USER_ADMIN = "admin";

    /** 超级管理员角色 key */
    public static final String ROLE_ADMIN = "admin";

    /** 普通角色 key */
    public static final String ROLE_COMMON = "common";

    /** 超级管理员权限字符串 (通配) */
    public static final String ALL_PERMISSION = "*:*:*";

    /** 用户名长度限制 */
    public static final int USERNAME_MIN_LENGTH = 2;
    public static final int USERNAME_MAX_LENGTH = 30;

    /** 密码长度限制 */
    public static final int PASSWORD_MIN_LENGTH = 5;
    public static final int PASSWORD_MAX_LENGTH = 30;

    /** 菜单类型: 目录 */
    public static final String MENU_TYPE_DIR = "M";
    /** 菜单类型: 菜单 */
    public static final String MENU_TYPE_MENU = "C";
    /** 菜单类型: 按钮 */
    public static final String MENU_TYPE_BUTTON = "F";

    /** 正常状态 */
    public static final String NORMAL = "0";
    /** 停用状态 */
    public static final String DISABLE = "1";

    /** 是否: 是 */
    public static final String YES = "Y";
    /** 是否: 否 */
    public static final String NO = "N";

    /** 是否唯一标识 */
    public static final boolean UNIQUE = true;
    public static final boolean NOT_UNIQUE = false;

    private UserConstants() {
    }
}