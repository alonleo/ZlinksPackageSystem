package com.zlinks.package_system.security;

import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.util.SecurityUtils;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.stereotype.Service;

import java.util.Collection;

/**
 * 自定义权限校验 Bean, 名称为 ss (RuoYi 习惯)
 * <p>
 * 用法: @PreAuthorize("@ss.hasPermi('system:user:list')")
 * <p>
 * 检查当前用户是否拥有指定权限 (或超级通配 *:*:*)
 */
@Service("ss")
public class PermissionService {

    /**
     * 校验当前登录用户是否拥有任一权限
     */
    public boolean hasPermi(String permission) {
        return hasAnyPermi(permission);
    }

    /**
     * 校验当前登录用户是否拥有全部指定权限
     */
    public boolean hasPermiAll(String... permissions) {
        if (permissions == null || permissions.length == 0) return false;
        Collection<? extends GrantedAuthority> auths = currentAuthorities();
        if (auths == null) return false;
        for (String p : permissions) {
            boolean ok = false;
            for (GrantedAuthority a : auths) {
                String s = a.getAuthority();
                if (UserConstants.ALL_PERMISSION.equals(s) || p.equals(s)) { ok = true; break; }
            }
            if (!ok) return false;
        }
        return true;
    }

    public boolean hasAnyPermi(String... permissions) {
        if (permissions == null || permissions.length == 0) return false;
        Collection<? extends GrantedAuthority> auths = currentAuthorities();
        if (auths == null) return false;
        for (String p : permissions) {
            for (GrantedAuthority a : auths) {
                String s = a.getAuthority();
                if (UserConstants.ALL_PERMISSION.equals(s) || p.equals(s)) {
                    return true;
                }
            }
        }
        return false;
    }

    public boolean hasRole(String role) {
        return SecurityUtils.getLoginUser() != null
                && SecurityUtils.getLoginUser().getAuthorities().stream()
                .anyMatch(a -> ("ROLE_" + role).equalsIgnoreCase(a.getAuthority())
                        || role.equalsIgnoreCase(a.getAuthority()));
    }

    public boolean isAdmin() {
        return SecurityUtils.isAdmin();
    }

    private Collection<? extends GrantedAuthority> currentAuthorities() {
        UserDetailsImpl u = SecurityUtils.getLoginUser();
        return u == null ? null : u.getAuthorities();
    }
}