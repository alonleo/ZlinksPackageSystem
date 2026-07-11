package com.zlinks.package_system.util;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.web.context.request.RequestAttributes;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/**
 * 客户端工具类
 */
public class ServletUtils {

    /**
     * 获取当前请求对象
     */
    public static HttpServletRequest getRequest() {
        try {
            RequestAttributes attributes = RequestContextHolder.getRequestAttributes();
            return attributes == null ? null : ((ServletRequestAttributes) attributes).getRequest();
        } catch (Exception e) {
            return null;
        }
    }

    /**
     * 获取客户端 IP
     */
    public static String getClientIp() {
        HttpServletRequest request = getRequest();
        if (request == null) {
            return "unknown";
        }
        String ip = request.getHeader("X-Forwarded-For");
        if (isInvalidIp(ip)) {
            ip = request.getHeader("X-Real-IP");
        }
        if (isInvalidIp(ip)) {
            ip = request.getHeader("Proxy-Client-IP");
        }
        if (isInvalidIp(ip)) {
            ip = request.getHeader("WL-Proxy-Client-IP");
        }
        if (isInvalidIp(ip)) {
            ip = request.getRemoteAddr();
        }
        // 多级代理取第一个
        if (ip != null && ip.contains(",")) {
            ip = ip.split(",")[0].trim();
        }
        return "0:0:0:0:0:0:0:1".equals(ip) ? "127.0.0.1" : ip;
    }

    private static boolean isInvalidIp(String ip) {
        return ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip);
    }

    /**
     * 获取 User-Agent
     */
    public static String getUserAgent() {
        HttpServletRequest request = getRequest();
        return request == null ? "" : request.getHeader("User-Agent");
    }

    private ServletUtils() {
    }
}