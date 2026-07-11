package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysRole;

import java.util.List;

public interface ISysRoleService extends IService<SysRole> {

    com.baomidou.mybatisplus.core.metadata.IPage<SysRole> selectRolePage(
                com.baomidou.mybatisplus.extension.plugins.pagination.Page<SysRole> page, SysRole query);

    List<SysRole> selectRolesByUserId(Long userId);

    boolean insertRole(SysRole role);

    boolean updateRole(SysRole role);

    boolean changeStatus(Long roleId, String status);

    boolean updateMenuPermissions(Long roleId, List<Long> menuIds);
}
