package com.zlinks.package_system.annotation;

import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.enums.OperatorType;

import java.lang.annotation.Documented;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 操作日志记录注解
 *
 * 用法: @Log(title = "用户管理", businessType = BusinessType.ADD)
 */
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
@Documented
public @interface Log {

    /** 模块标题 (必填) */
    String title() default "";

    /** 业务操作类型 */
    BusinessType businessType() default BusinessType.OTHER;

    /** 操作人类别 */
    OperatorType operatorType() default OperatorType.MANAGE;

    /** 是否保存请求参数 */
    boolean isSaveRequestData() default true;

    /** 是否保存响应参数 */
    boolean isSaveResponseData() default true;

    /** 排除指定的请求参数 (敏感字段) */
    String[] excludeParamNames() default {};
}