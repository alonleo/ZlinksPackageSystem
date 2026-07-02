package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.PermissionGroup;
import com.zlinks.package_system.mapper.PermissionGroupMapper;
import com.zlinks.package_system.service.PermissionGroupService;
import org.springframework.stereotype.Service;

@Service
public class PermissionGroupServiceImpl extends ServiceImpl<PermissionGroupMapper, PermissionGroup> implements PermissionGroupService {
}