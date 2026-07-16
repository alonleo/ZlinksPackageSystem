package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.PermissionScope;
import com.zlinks.package_system.mapper.PermissionScopeMapper;
import com.zlinks.package_system.service.IPermissionScopeService;
import org.springframework.stereotype.Service;

@Service
public class PermissionScopeServiceImpl extends ServiceImpl<PermissionScopeMapper, PermissionScope> implements IPermissionScopeService {
}
