package com.zlinks.package_system.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.User;

public interface UserService extends IService<User> {

    User findByUsername(String username);

    boolean existsByUsername(String username);
}