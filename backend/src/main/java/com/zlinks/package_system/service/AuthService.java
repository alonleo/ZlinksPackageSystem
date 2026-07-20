package com.zlinks.package_system.service;

import com.zlinks.package_system.dto.UserVO;
import com.zlinks.package_system.entity.User;

import java.util.List;

public interface AuthService {

    String login(String username, String password);

    User register(User user);

    UserVO getCurrentUser();

    List<String> mergeModulesByUser(Long userId, String scope);

    User changePassword(String oldPassword, String newPassword, String newUsername);
}