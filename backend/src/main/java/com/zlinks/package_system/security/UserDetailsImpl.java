package com.zlinks.package_system.security;

import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysUser;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.UserDetails;

import java.util.Collection;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * Spring Security UserDetails 实现
 * <p>
 * 包装 LoginUser, 将 permissions/roles 转为 GrantedAuthority 列表
 */
@Data
@Builder
@AllArgsConstructor
public class UserDetailsImpl implements UserDetails {

    private static final long serialVersionUID = 1L;

    private Long userId;
    private String username;
    private String password;
    private String status;
    private boolean enabled;
    private Collection<? extends GrantedAuthority> authorities;

    /**
     * 从 SysUser + LoginUser 构造 UserDetails
     */
    public static UserDetailsImpl build(SysUser user, LoginUser loginUser) {
        Set<String> permissions = loginUser.getPermissions() == null ? Set.of() : loginUser.getPermissions();
        List<GrantedAuthority> authList = permissions.stream()
                .map(SimpleGrantedAuthority::new)
                .collect(Collectors.toList());
        // 加一个 ROLE_USER 兜底, 避免 Spring Security Anonymous 报错
        boolean active = UserConstants.NORMAL.equals(user.getStatus());
        return UserDetailsImpl.builder()
                .userId(user.getUserId())
                .username(user.getUserName())
                .password(user.getPassword())
                .status(user.getStatus())
                .enabled(active)
                .authorities(authList)
                .build();
    }

    @Override
    public Collection<? extends GrantedAuthority> getAuthorities() {
        return authorities;
    }

    @Override
    public boolean isAccountNonExpired() {
        return true;
    }

    @Override
    public boolean isAccountNonLocked() {
        return true;
    }

    @Override
    public boolean isCredentialsNonExpired() {
        return true;
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }
}