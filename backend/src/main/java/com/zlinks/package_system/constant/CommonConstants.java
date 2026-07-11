package com.zlinks.package_system.constant;

/**
 * 通用常量
 */
public class CommonConstants {

    public static final String UTF_8 = "UTF-8";

    public static final String GBK = "GBK";

    /** http 请求 */
    public static final String HTTP = "http://";

    /** https 请求 */
    public static final String HTTPS = "https://";

    /** 通用成功标识 */
    public static final String SUCCESS = "0";

    /** 通用失败标识 */
    public static final String FAIL = "1";

    /** 登录成功 */
    public static final String LOGIN_SUCCESS = "Success";

    /** 注销 */
    public static final String LOGOUT = "Logout";

    /** 登录失败 */
    public static final String LOGIN_FAIL = "Error";

    /** 自动去除表前缀 */
    public static final String AUTO_REMOVED_TABLE_PREFIX = "sys_";

    /** 当前记录起始索引 */
    public static final String PAGE_NUM = "pageNum";

    /** 每页显示记录数 */
    public static final String PAGE_SIZE = "pageSize";

    /** 排序列 */
    public static final String ORDER_BY_COLUMN = "orderByColumn";

    /** 排序的方向 "desc" 或者 "asc" */
    public static final String IS_ASC = "isAsc";

    private CommonConstants() {
    }
}