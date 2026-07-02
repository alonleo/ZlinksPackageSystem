package com.zlinks.package_system.service;

import com.zlinks.package_system.entity.User;

public interface AuthService {

    String login(String username, String password);

    User register(User user);

    User getCurrentUser();
}