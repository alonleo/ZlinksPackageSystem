package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.UserVO;
import com.zlinks.package_system.entity.PermissionGroup;
import com.zlinks.package_system.entity.PermissionScope;
import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.entity.UserGroup;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.AuthService;
import com.zlinks.package_system.service.IPermissionScopeService;
import com.zlinks.package_system.service.PermissionGroupService;
import com.zlinks.package_system.service.UserGroupService;
import com.zlinks.package_system.service.UserService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.JwtUtil;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.BeanUtils;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class AuthServiceImpl implements AuthService {

    private static final ObjectMapper OBJECT_MAPPER = new ObjectMapper();

    private final AuthenticationManager authenticationManager;
    private final UserService userService;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;
    private final UserGroupService userGroupService;
    private final PermissionGroupService permissionGroupService;
    private final IPermissionScopeService permissionScopeService;

    @Override
    public String login(String username, String password) {
        Authentication authentication = authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(username, password));

        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();

        return jwtUtil.generateToken(userDetails.getUserId(), userDetails.getUsername());
    }

    @Override
    public User register(User user) {
        if (userService.existsByUsername(user.getUsername())) {
            throw new BusinessException("Username already exists");
        }

        user.setPassword(passwordEncoder.encode(user.getPassword()));
        user.setStatus("active");
        userService.save(user);

        return user;
    }

    @Override
    public UserVO getCurrentUser() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication == null || !authentication.isAuthenticated()) {
            throw new BusinessException("User not authenticated");
        }

        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        User user = userService.getById(userDetails.getUserId());

        List<UserGroup> ugs = userGroupService.list(
                new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getUserId, user.getId()));
        List<Long> groupIds = ugs.stream().map(UserGroup::getGroupId).collect(Collectors.toList());
        user.setGroupIds(groupIds);

        if (!groupIds.isEmpty()) {
            Map<Long, String> nameMap = permissionGroupService.listByIds(groupIds).stream()
                    .collect(Collectors.toMap(PermissionGroup::getId, PermissionGroup::getGroupName));
            List<String> groupNames = groupIds.stream()
                    .map(nameMap::get)
                    .filter(name -> name != null)
                    .collect(Collectors.toList());
            user.setGroupNames(groupNames);
        }

        UserVO vo = new UserVO();
        BeanUtils.copyProperties(user, vo);
        vo.setGroupIds(user.getGroupIds());
        vo.setGroupNames(user.getGroupNames());
        vo.setDesktopModules(mergeModulesByUser(user.getId(), "desktop"));
        return vo;
    }

    @Override
    public List<String> mergeModulesByUser(Long userId, String scope) {
        if (userId == null) {
            return Collections.emptyList();
        }
        List<UserGroup> ugs = userGroupService.list(
                new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getUserId, userId));
        if (ugs.isEmpty()) {
            return Collections.emptyList();
        }
        List<Long> groupIds = ugs.stream().map(UserGroup::getGroupId).collect(Collectors.toList());
        List<PermissionScope> scopes = permissionScopeService.list(
                new LambdaQueryWrapper<PermissionScope>()
                        .in(PermissionScope::getGroupId, groupIds)
                        .eq(PermissionScope::getScope, scope));
        Set<String> merged = new LinkedHashSet<>();
        for (PermissionScope s : scopes) {
            if (s.getModulesText() == null) {
                continue;
            }
            try {
                List<String> mods = OBJECT_MAPPER.readValue(s.getModulesText(), new TypeReference<List<String>>() {});
                if (mods.contains("all")) {
                    return List.of("all");
                }
                merged.addAll(mods);
            } catch (Exception ignored) {
            }
        }
        return new ArrayList<>(merged);
    }

    @Override
    public User changePassword(String oldPassword, String newPassword, String newUsername) {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication == null || !authentication.isAuthenticated()) {
            throw new BusinessException("用户未登录");
        }

        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        String currentUsername = userDetails.getUsername();

        authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(currentUsername, oldPassword));

        User user = userService.getById(userDetails.getUserId());
        if (user == null) {
            throw new BusinessException("用户不存在");
        }

        if (newUsername != null && !newUsername.isBlank() && !newUsername.equals(user.getUsername())) {
            if (userService.existsByUsername(newUsername)) {
                throw new BusinessException("用户名已被占用");
            }
            user.setUsername(newUsername);
        }

        if (newPassword != null && !newPassword.isBlank()) {
            user.setPassword(passwordEncoder.encode(newPassword));
        }

        userService.updateById(user);
        return user;
    }
}