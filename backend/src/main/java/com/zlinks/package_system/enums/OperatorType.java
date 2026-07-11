package com.zlinks.package_system.enums;

/**
 * 操作人类别
 */
public enum OperatorType {
    /** 其它 */
    OTHER(0, "其它"),
    /** 后台用户 */
    MANAGE(1, "后台用户"),
    /** 手机端用户 */
    MOBILE(2, "手机端用户");

    private final int code;
    private final String desc;

    OperatorType(int code, String desc) {
        this.code = code;
        this.desc = desc;
    }

    public int getCode() {
        return code;
    }

    public String getDesc() {
        return desc;
    }
}