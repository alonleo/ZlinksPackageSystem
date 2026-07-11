package com.zlinks.package_system.security;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysMenu;
import com.zlinks.package_system.entity.system.SysRole;
import com.zlinks.package_system.entity.system.SysUser;
import com.zlinks.package_system.mapper.system.SysMenuMapper;
import com.zlinks.package_system.mapper.system.SysRoleMapper;
import com.zlinks.package_system.mapper.system.SysUserMapper;
import com.zlinks.package_system.mapper.system.SysUserRoleMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Service;

import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * 自定义 UserDetailsService
 * <p>
 * 加载 SysUser + 角色列表 + 权限字符串列表 (menu.perms where menu_type='F')
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class UserDetailsServiceImpl implements UserDetailsService {

    private final SysUserMapper sysUserMapper;
    private final SysUserRoleMapper sysUserRoleMapper;
    private final SysRoleMapper sysRoleMapper;
    private final SysMenuMapper sysMenuMapper;

    @Override
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {
        SysUser user = sysUserMapper.selectOne(
                new LambdaQueryWrapper<SysUser>()
                        .eq(SysUser::getUserName, username)
                        .eq(SysUser::getIsDeleted, 0)
        );
        if (user == null) {
            throw new UsernameNotFoundException("用户不存在: " + username);
        }
        if (UserConstants.DISABLE.equals(user.getStatus())) {
            throw new UsernameNotFoundException("账号已停用: " + username);
        }

        // 加载角色
        Set<String> roleKeys = new HashSet<>();
        List<Long> roleIds = sysUserRoleMapper.selectRoleIdsByUserId(user.getUserId());
        if (!roleIds.isEmpty()) {
            // 超级管理员
            if (roleIds.contains(1L)) {
                roleKeys.add(UserConstants.ROLE_ADMIN);
            }
            List<SysRole> roles = sysRoleMapper.selectBatchIds(roleIds);
            for (SysRole role : roles) {
                if (role != null && role.getRoleKey() != null) {
                    roleKeys.add(role.getRoleKey());
                }
            }
        }

        // 加载权限 (所有启用菜单的 perms)
        Set<String> permissions = new HashSet<>();
        if (roleKeys.contains(UserConstants.ROLE_ADMIN)) {
            permissions.add(UserConstants.ALL_PERMISSION);
        } else if (!roleIds.isEmpty()) {
            List<SysMenu> menus = sysMenuMapper.selectMenusByRoleIds(roleIds);
            permissions = menus.stream()
                    .map(SysMenu::getPerms)
                    .filter(p -> p != null && !p.isEmpty())
                    .collect(Collectors.toSet());
        }

        LoginUser loginUser = LoginUser.builder()
                .userId(user.getUserId())
                .userName(user.getUserName())
                .nickName(user.getNickName())
                .deptId(user.getDeptId())
                .avatar(user.getAvatar())
                .password(user.getPassword())
                .status(user.getStatus())
                .roles(roleKeys)
                .permissions(permissions)
                .build();

        return UserDetailsImpl.build(user, loginUser);
    }
}