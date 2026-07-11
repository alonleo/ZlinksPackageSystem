package com.zlinks.package_system.enums;

/**
 * 业务操作类型
 */
public enum BusinessType {
    /** 其它 */
    OTHER(0, "其它"),
    /** 新增 */
    ADD(1, "新增"),
    /** 修改 */
    EDIT(2, "修改"),
    /** 删除 */
    REMOVE(3, "删除"),
    /** 查询 */
    QUERY(4, "查询"),
    /** 导出 */
    EXPORT(5, "导出"),
    /** 导入 */
    IMPORT(6, "导入"),
    /** 强退 */
    FORCE(7, "强退"),
    /** 清空数据 */
    CLEAN(8, "清空数据"),
    /** 授权 */
    GRANT(9, "授权");

    private final int code;
    private final String desc;

    BusinessType(int code, String desc) {
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