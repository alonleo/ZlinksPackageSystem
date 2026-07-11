package com.zlinks.package_system.security;

import com.fasterxml.jackson.annotation.JsonIgnore;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.io.Serializable;
import java.util.HashSet;
import java.util.Set;

/**
 * 登录用户信息 (用于 Redis 存储, 含 user + roles + permissions)
 */
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class LoginUser implements Serializable {

    private static final long serialVersionUID = 1L;

    /** 登录 token uuid */
    private String token;

    /** 用户ID */
    private Long userId;

    /** 用户名 */
    private String userName;

    /** 昵称 */
    private String nickName;

    /** 部门ID */
    private Long deptId;

    /** 部门名称 */
    private String deptName;

    /** 头像 */
    private String avatar;

    /** 密码 (BCrypt, 不返回给前端) */
    @JsonIgnore
    private String password;

    /** 状态 */
    private String status;

    /** 角色 key 集合 (用于 hasRole) */
    private Set<String> roles;

    /** 权限字符串集合 (用于 hasAuthority, 例如 system:user:list) */
    private Set<String> permissions;

    /** 登录时间 (毫秒) */
    private Long loginTime;

    /** 过期时间 (毫秒) */
    private Long expireTime;

    /** 登录 IP */
    private String ipaddr;

    public static LoginUser createEmpty() {
        return LoginUser.builder()
                .roles(new HashSet<>())
                .permissions(new HashSet<>())
                .build();
    }
}