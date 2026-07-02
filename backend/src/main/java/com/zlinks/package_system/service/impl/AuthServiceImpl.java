package com.zlinks.package_system.service.impl;

import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.AuthService;
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

@Service
@RequiredArgsConstructor
public class AuthServiceImpl implements AuthService {

    private final AuthenticationManager authenticationManager;
    private final UserService userService;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;

    @Override
    public String login(String username, String password) {
        Authentication authentication = authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(username, password));

        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();

        return jwtUtil.generateToken(userDetails.getId(), userDetails.getUsername());
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
        return userService.getById(userDetails.getId());
    }
}