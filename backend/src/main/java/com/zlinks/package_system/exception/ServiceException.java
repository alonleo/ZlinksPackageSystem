package com.zlinks.package_system.exception;

import lombok.Getter;

/**
 * 业务异常 (替代 BusinessException, 与 Spring Security 兼容)
 */
@Getter
public class ServiceException extends RuntimeException {

    private static final long serialVersionUID = 1L;

    /** 错误码 */
    private final int code;

    /** 错误提示 */
    private String message;

    public ServiceException(String message) {
        this.code = 500;
        this.message = message;
    }

    public ServiceException(int code, String message) {
        this.code = code;
        this.message = message;
    }

    public ServiceException setMessage(String message) {
        this.message = message;
        return this;
    }

    @Override
    public String getMessage() {
        return message;
    }
}