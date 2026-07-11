package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.entity.PermissionGroup;
import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.entity.UserGroup;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.AuthService;
import com.zlinks.package_system.service.PermissionGroupService;
import com.zlinks.package_system.service.UserGroupService;
import com.zlinks.package_system.service.UserService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.JwtUtil;
import lombok.RequiredArgsConstructor;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class AuthServiceImpl implements AuthService {

    private final AuthenticationManager authenticationManager;
    private final UserService userService;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;
    private final UserGroupService userGroupService;
    private final PermissionGroupService permissionGroupService;

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
    public User getCurrentUser() {
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

        return user;
    }
}