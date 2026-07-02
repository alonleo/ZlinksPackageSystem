package com.zlinks.package_system.controller;

import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.service.AuthService;
import com.zlinks.package_system.util.Result;
import io.swagger.annotations.Api;
import io.swagger.annotations.ApiOperation;
import lombok.Data;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import javax.validation.Valid;

@Api(tags = "认证管理")
@RestController
@RequestMapping("/api/auth")
@RequiredArgsConstructor
public class AuthController {

    private final AuthService authService;

    @ApiOperation("用户登录")
    @PostMapping("/login")
    public Result<String> login(@Valid @RequestBody LoginRequest request) {
        String token = authService.login(request.getUsername(), request.getPassword());
        return Result.success(token);
    }

    @ApiOperation("用户注册")
    @PostMapping("/register")
    public Result<User> register(@Valid @RequestBody User user) {
        User registeredUser = authService.register(user);
        return Result.success(registeredUser);
    }

    @ApiOperation("获取当前用户信息")
    @GetMapping("/info")
    public Result<User> getCurrentUser() {
        User user = authService.getCurrentUser();
        return Result.success(user);
    }

    @Data
    public static class LoginRequest {
        private String username;
        private String password;
    }
}