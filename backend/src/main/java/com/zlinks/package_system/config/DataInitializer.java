package com.zlinks.package_system.config;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.entity.PermissionGroup;
import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.service.PermissionGroupService;
import com.zlinks.package_system.service.UserService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class DataInitializer implements CommandLineRunner {

    private final UserService userService;
    private final PermissionGroupService permissionGroupService;
    private final PasswordEncoder passwordEncoder;

    @Override
    public void run(String... args) {
        initPermissionGroups();
        initAdminUser();
    }

    private void initPermissionGroups() {
        long count = permissionGroupService.count();
        if (count == 0) {
            log.info("Initializing default permission groups...");

            PermissionGroup adminGroup = new PermissionGroup();
            adminGroup.setGroupName("管理员组");
            adminGroup.setGroupPermission("{\"modules\":[\"all\"]}");
            adminGroup.setRemark("系统管理员，拥有所有权限");
            permissionGroupService.save(adminGroup);

            PermissionGroup devGroup = new PermissionGroup();
            devGroup.setGroupName("开发组");
            devGroup.setGroupPermission("{\"modules\":[\"games\",\"products\"]}");
            devGroup.setRemark("开发人员，负责游戏和产品管理");
            permissionGroupService.save(devGroup);

            PermissionGroup testGroup = new PermissionGroup();
            testGroup.setGroupName("测试组");
            testGroup.setGroupPermission("{\"modules\":[\"tests\"]}");
            testGroup.setRemark("测试人员，负责测试管理");
            permissionGroupService.save(testGroup);

            PermissionGroup opsGroup = new PermissionGroup();
            opsGroup.setGroupName("运营组");
            opsGroup.setGroupPermission("{\"modules\":[\"products\",\"companies\"]}");
            opsGroup.setRemark("运营人员，负责产品和公司管理");
            permissionGroupService.save(opsGroup);

            log.info("Default permission groups initialized.");
        }
    }

    private void initAdminUser() {
        boolean exists = userService.existsByUsername("admin");
        if (!exists) {
            log.info("Initializing default admin user...");

            User admin = new User();
            admin.setUsername("admin");
            admin.setPassword(passwordEncoder.encode("admin123"));
            admin.setRealName("系统管理员");
            admin.setStatus("active");
            admin.setGroupId(1L);
            userService.save(admin);

            log.info("Default admin user initialized. Username: admin, Password: admin123");
        }
    }
}