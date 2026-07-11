package com.zlinks.package_system.util;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;

import java.net.InetAddress;
import java.net.UnknownHostException;

/**
 * IP 工具 - 简化版（离线解析，返回 "内网IP" 或前两段）
 */
@Slf4j
public class IpUtils {

    private static final String UNKNOWN = "unknown";

    public static String getClientIp(HttpServletRequest request) {
        if (request == null) return UNKNOWN;
        String ip = request.getHeader("X-Forwarded-For");
        if (isUnknown(ip)) ip = request.getHeader("Proxy-Client-IP");
        if (isUnknown(ip)) ip = request.getHeader("WL-Proxy-Client-IP");
        if (isUnknown(ip)) ip = request.getHeader("HTTP_CLIENT_IP");
        if (isUnknown(ip)) ip = request.getHeader("HTTP_X_FORWARDED_FOR");
        if (isUnknown(ip)) ip = request.getRemoteAddr();
        if (StringUtils.isNotBlank(ip) && ip.contains(",")) ip = ip.split(",")[0].trim();
        return "0:0:0:0:0:0:0:1".equals(ip) ? "127.0.0.1" : ip;
    }

    private static boolean isUnknown(String ip) {
        return ip == null || ip.isEmpty() || UNKNOWN.equalsIgnoreCase(ip);
    }

    /**
     * 简易 IP 归属地（生产环境接入离线/在线 IP 库）
     */
    public static String getCity(String ip) {
        if (ip == null || ip.isEmpty() || "127.0.0.1".equals(ip) || "localhost".equals(ip)) {
            return "内网IP";
        }
        try {
            InetAddress addr = InetAddress.getByName(ip);
            if (addr.isLoopbackAddress()) return "内网IP";
            String host = addr.getHostAddress();
            String[] parts = host.split("\\.");
            if (parts.length >= 2) return parts[0] + "." + parts[1] + ".0.0";
        } catch (UnknownHostException e) {
            log.debug("IP 解析失败: {}", ip);
        }
        return "未知";
    }
}