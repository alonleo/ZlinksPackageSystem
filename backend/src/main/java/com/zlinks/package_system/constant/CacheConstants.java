package com.zlinks.package_system.constant;

/**
 * 缓存 key 常量
 */
public class CacheConstants {

    /** 登录 token 前缀 */
    public static final String LOGIN_TOKEN_KEY = "login_tokens:";

    /** 验证码前缀 */
    public static final String CAPTCHA_CODE_KEY = "captcha_codes:";

    /** 密码错误次数前缀 */
    public static final String PWD_RETRY_CNT_KEY = "pwd_err_cnt:";

    /** 参数管理 cache key */
    public static final String SYS_CONFIG_KEY = "sys_config:";

    /** 字典管理 cache key */
    public static final String SYS_DICT_KEY = "sys_dict:";

    /** 防重提交 cache key */
    public static final String REPEAT_SUBMIT_KEY = "repeat_submit:";

    /** 限流 cache key */
    public static final String RATE_LIMIT_KEY = "rate_limit:";

    private CacheConstants() {
    }
}