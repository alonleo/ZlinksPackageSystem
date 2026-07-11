package com.zlinks.package_system.controller;

import com.zlinks.package_system.constant.CacheConstants;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysMenu;
import com.zlinks.package_system.entity.system.SysRole;
import com.zlinks.package_system.entity.system.SysUser;
import com.zlinks.package_system.security.LoginUser;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.system.ISysMenuService;
import com.zlinks.package_system.service.system.ISysRoleService;
import com.zlinks.package_system.service.system.ISysUserService;
import com.zlinks.package_system.util.JwtUtil;
import com.zlinks.package_system.util.RedisUtils;
import com.zlinks.package_system.util.Result;
import com.zlinks.package_system.util.SecurityUtils;
import com.zlinks.package_system.util.ServletUtils;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletRequest;
import lombok.Data;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.*;

import java.util.*;
import java.util.stream.Collectors;

/**
 * RuoYi 风格认证 / 信息 / 路由 接口
 * <p>
 * 兼容旧 AuthController (/api/auth/login) 的同时, 提供 RuoYi 标准路径:
 * - POST /api/login  返回 token
 * - GET  /api/getInfo   返回 { user, roles, permissions }
 * - GET  /api/getRouters 返回路由树 (前端动态路由)
 */
@Slf4j
@Tag(name = "认证(RuoYi)")
@RestController
@RequiredArgsConstructor
public class RuoYiAuthController {

    private final AuthenticationManager authenticationManager;
    private final ISysUserService userService;
    private final ISysRoleService roleService;
    private final ISysMenuService menuService;
    private final JwtUtil jwtUtil;
    private final RedisUtils redisUtils;

    @Operation(summary = "RuoYi 风格登录")
    @PostMapping("/api/login")
    public Result<Map<String, Object>> login(@RequestBody LoginBody body) {
        // 1. 调 Spring Security 认证 (会自动调用 UserDetailsServiceImpl 加载 SysUser + permissions)
        Authentication authentication = authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(body.getUsername(), body.getPassword()));

        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        String token = jwtUtil.generateToken(userDetails.getUserId(), userDetails.getUsername());

        // 2. 把 LoginUser 缓存到 Redis
        SysUser user = userService.findByUsername(userDetails.getUsername());
        Set<String> permissions = new HashSet<>();
        for (org.springframework.security.core.GrantedAuthority a : userDetails.getAuthorities()) {
            permissions.add(a.getAuthority());
        }
        Set<String> roles = new HashSet<>();
        if (user != null && user.getRoles() != null) {
            for (Object o : user.getRoles()) {
                if (o instanceof SysRole sr && sr.getRoleKey() != null) roles.add(sr.getRoleKey());
            }
        }
        if (permissions.contains(UserConstants.ALL_PERMISSION)) roles.add(UserConstants.ROLE_ADMIN);

        LoginUser loginUser = LoginUser.builder()
                .token(token)
                .userId(userDetails.getUserId())
                .userName(userDetails.getUsername())
                .nickName(user == null ? null : user.getNickName())
                .deptId(user == null ? null : user.getDeptId())
                .avatar(user == null ? null : user.getAvatar())
                .password(user == null ? null : user.getPassword())
                .status(userDetails.getStatus())
                .roles(roles)
                .permissions(permissions)
                .loginTime(System.currentTimeMillis())
                .expireTime(System.currentTimeMillis() + 24L * 3600 * 1000)
                .ipaddr(ServletUtils.getClientIp())
                .build();

        String key = CacheConstants.LOGIN_TOKEN_KEY + token;
        redisUtils.set(key, loginUser, 24L, java.util.concurrent.TimeUnit.HOURS);

        // 3. 更新最后登录信息
        userService.updateLoginInfo(userDetails.getUserId(), ServletUtils.getClientIp());

        Map<String, Object> rsp = new HashMap<>();
        rsp.put("token", token);
        return Result.success(rsp);
    }

    @Operation(summary = "获取当前用户信息 (含角色 / 权限)")
    @GetMapping("/api/getInfo")
    public Result<Map<String, Object>> getInfo() {
        UserDetailsImpl ud = SecurityUtils.getLoginUser();
        if (ud == null) return Result.unauthorized();
        SysUser user = userService.selectUserWithDetail(ud.getUserId());

        Set<String> roles = new HashSet<>();
        if (user != null && user.getRoles() != null) {
            for (Object o : user.getRoles()) {
                if (o instanceof SysRole sr && sr.getRoleKey() != null) roles.add(sr.getRoleKey());
            }
        }
        // 从 authorities 中提取 permissions (UserDetailsImpl 包装)
        Set<String> permissions = new HashSet<>();
        if (ud.getAuthorities() != null) {
            for (org.springframework.security.core.GrantedAuthority a : ud.getAuthorities()) {
                permissions.add(a.getAuthority());
            }
        }

        Map<String, Object> rsp = new HashMap<>();
        rsp.put("user", user);
        rsp.put("roles", roles);
        rsp.put("permissions", permissions);
        return Result.success(rsp);
    }

    @Operation(summary = "获取前端动态路由")
    @GetMapping("/api/getRouters")
    public Result<List<Map<String, Object>>> getRouters() {
        UserDetailsImpl ud = SecurityUtils.getLoginUser();
        if (ud == null) return Result.success(java.util.Collections.emptyList());

        List<SysMenu> menus;
        if (UserConstants.ROLE_ADMIN.equals(ud.getUsername()) ||
                ud.getAuthorities().stream().anyMatch(a -> UserConstants.ALL_PERMISSION.equals(a.getAuthority()))) {
            // 超级管理员: 返回所有菜单 (菜单类型 = M / C)
            SysMenu q = new SysMenu();
            q.setStatus(UserConstants.NORMAL);
            menus = menuService.selectMenuList(q);
        } else {
            menus = menuService.selectMenusByUser(ud.getUserId());
        }
        // 仅保留目录/菜单
        menus = menus.stream()
                .filter(m -> UserConstants.MENU_TYPE_DIR.equals(m.getMenuType()) ||
                        UserConstants.MENU_TYPE_MENU.equals(m.getMenuType()))
                .filter(m -> UserConstants.NORMAL.equals(m.getStatus()))
                .filter(m -> UserConstants.NORMAL.equals(m.getVisible()))
                .collect(Collectors.toList());

        List<Map<String, Object>> tree = buildRouterTree(menus, 0L);
        return Result.success(tree);
    }

    private List<Map<String, Object>> buildRouterTree(List<SysMenu> menus, Long parentId) {
        List<Map<String, Object>> result = new ArrayList<>();
        for (SysMenu m : menus) {
            if (parentId.equals(m.getParentId() == null ? 0L : m.getParentId())) {
                Map<String, Object> r = new LinkedHashMap<>();
                r.put("name", m.getPath() == null ? m.getMenuName() : cap(m.getPath()));
                r.put("path", m.getPath() == null ? "" : m.getPath());
                if (m.getParentId() == null || m.getParentId() == 0L) {
                    r.put("redirect", "noRedirect");
                    r.put("alwaysShow", true);
                }
                r.put("hidden", "1".equals(m.getVisible()));
                Meta meta = new Meta();
                meta.setTitle(m.getMenuName());
                meta.setIcon(m.getIcon());
                meta.setNoCache("1".equals(m.getIsCache()));
                r.put("meta", meta);

                String comp = m.getComponent();
                if (UserConstants.MENU_TYPE_DIR.equals(m.getMenuType())) {
                    r.put("component", comp == null || comp.isEmpty() ? "Layout" : comp);
                } else {
                    r.put("component", comp == null || comp.isEmpty() ? "ParentView" : comp);
                }
                List<Map<String, Object>> children = buildRouterTree(menus, m.getMenuId());
                if (!children.isEmpty()) {
                    r.put("children", children);
                }
                result.add(r);
            }
        }
        return result;
    }

    private static String cap(String s) {
        if (s == null || s.isEmpty()) return s;
        return Character.toUpperCase(s.charAt(0)) + s.substring(1);
    }

    @Data
    public static class LoginBody {
        private String username;
        private String password;
        private String code;
        private String uuid;
    }

    @Data
    public static class Meta {
        private String title;
        private String icon;
        private boolean noCache;
    }
}