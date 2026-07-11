package com.zlinks.package_system.util;

import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.security.UserDetailsImpl;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;

/**
 * 安全工具类
 */
public class SecurityUtils {

    /**
     * 获取当前认证用户 (UserDetailsImpl)
     */
    public static UserDetailsImpl getLoginUser() {
        try {
            Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
            if (authentication == null || !authentication.isAuthenticated()) {
                return null;
            }
            Object principal = authentication.getPrincipal();
            if (principal instanceof UserDetailsImpl userDetails) {
                return userDetails;
            }
            return null;
        } catch (Exception e) {
            return null;
        }
    }

    /**
     * 获取当前用户ID
     */
    public static Long getUserId() {
        UserDetailsImpl user = getLoginUser();
        return user == null ? null : user.getUserId();
    }

    /**
     * 获取当前用户名
     */
    public static String getUsername() {
        UserDetailsImpl user = getLoginUser();
        return user == null ? "" : user.getUsername();
    }

    /**
     * 是否是超级管理员
     */
    public static boolean isAdmin() {
        UserDetailsImpl user = getLoginUser();
        return user != null && UserConstants.USER_ADMIN.equals(user.getUsername());
    }

    /**
     * 当前用户是否拥有某权限
     */
    public static boolean hasPermission(String permission) {
        if (isAdmin()) {
            return true;
        }
        UserDetailsImpl user = getLoginUser();
        if (user == null || user.getAuthorities() == null) {
            return false;
        }
        return user.getAuthorities().stream()
                .anyMatch(a -> UserConstants.ALL_PERMISSION.equals(a.getAuthority())
                        || permission.equals(a.getAuthority()));
    }

    /**
     * BCrypt 加密
     */
    public static String encryptPassword(String password) {
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        return encoder.encode(password);
    }

    /**
     * 校验密码
     */
    public static boolean matchesPassword(String rawPassword, String encodedPassword) {
        BCryptPasswordEncoder encoder = new BCryptPasswordEncoder();
        return encoder.matches(rawPassword, encodedPassword);
    }

    private SecurityUtils() {
    }
}