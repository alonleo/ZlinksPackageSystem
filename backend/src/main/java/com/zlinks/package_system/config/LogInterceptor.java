package com.zlinks.package_system.config;

import com.zlinks.package_system.entity.OperationLog;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.OperationLogService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;

import java.util.Map;

@Component
@RequiredArgsConstructor
public class LogInterceptor implements HandlerInterceptor {

    private final OperationLogService operationLogService;

    private static final Map<String, String> MODULE_LABELS = Map.ofEntries(
            Map.entry("auth", "认证"),
            Map.entry("users", "用户管理"),
            Map.entry("games", "游戏管理"),
            Map.entry("products", "产品管理"),
            Map.entry("companies", "公司管理"),
            Map.entry("sign-files", "签名管理"),
            Map.entry("copyrights", "软著管理"),
            Map.entry("platforms", "平台管理"),
            Map.entry("notifications", "通知管理"),
            Map.entry("permission-groups", "权限管理"),
            Map.entry("operation-logs", "日志管理")
    );

    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, Object handler, Exception ex) {
        try {
            String path = request.getRequestURI();
            if (!path.startsWith("/api/")) return;

            String[] parts = path.split("/");
            if (parts.length < 3) return;

            String moduleKey = parts[2];
            String module = MODULE_LABELS.getOrDefault(moduleKey, moduleKey);
            String method = request.getMethod();
            String action = resolveAction(method, path);

            if (action == null) return;

            OperationLog log = new OperationLog();
            log.setModule(module);
            log.setAction(action);
            log.setTarget(path.substring(path.indexOf("/api/")));
            log.setIpAddress(getClientIp(request));

            var auth = SecurityContextHolder.getContext().getAuthentication();
            if (auth != null && auth.getPrincipal() instanceof UserDetailsImpl userDetails) {
                log.setUserId(userDetails.getUserId());
                log.setUsername(userDetails.getUsername());
            }

            operationLogService.save(log);
        } catch (Exception ignored) {
            // 日志记录失败不影响业务
        }
    }

    private String resolveAction(String method, String path) {
        if ("POST".equals(method)) {
            if (path.contains("/auth/login")) return "LOGIN";
            if (path.contains("/import")) return "IMPORT";
            return "CREATE";
        }
        if ("PUT".equals(method)) return "UPDATE";
        if ("DELETE".equals(method)) return "DELETE";
        if ("GET".equals(method)) {
            if (path.endsWith("/export") || path.endsWith("/template")) return null;
            return "QUERY";
        }
        return null;
    }

    private String getClientIp(HttpServletRequest request) {
        String ip = request.getHeader("X-Forwarded-For");
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getHeader("X-Real-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getRemoteAddr();
        }
        if (ip == null) return "unknown";
        return ip.contains(",") ? ip.split(",")[0].trim() : ip;
    }
}
