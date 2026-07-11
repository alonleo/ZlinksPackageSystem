package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysRole;
import com.zlinks.package_system.entity.system.SysRoleMenu;
import com.zlinks.package_system.exception.ServiceException;
import com.zlinks.package_system.mapper.system.SysRoleMapper;
import com.zlinks.package_system.mapper.system.SysRoleMenuMapper;
import com.zlinks.package_system.service.system.ISysRoleService;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class SysRoleServiceImpl extends ServiceImpl<SysRoleMapper, SysRole> implements ISysRoleService {

    private final SysRoleMenuMapper roleMenuMapper;

    @Override
            public com.baomidou.mybatisplus.core.metadata.IPage<SysRole> selectRolePage(Page<SysRole> page, SysRole query) {
            LambdaQueryWrapper<SysRole> w = new LambdaQueryWrapper<>();
            w.eq(SysRole::getDelFlag, "0");
            if (StringUtils.hasText(query.getRoleName())) w.like(SysRole::getRoleName, query.getRoleName());
            if (StringUtils.hasText(query.getRoleKey())) w.like(SysRole::getRoleKey, query.getRoleKey());
            if (StringUtils.hasText(query.getStatus())) w.eq(SysRole::getStatus, query.getStatus());
            w.orderByAsc(SysRole::getRoleSort);
            return page(page, w);
        }

    @Override
    public List<SysRole> selectRolesByUserId(Long userId) {
        return baseMapper.selectRolesByUserId(userId);
    }

    @Override
        @Transactional
        public boolean insertRole(SysRole role) {
            if (UserConstants.ROLE_ADMIN.equals(role.getRoleKey())) {
                throw new ServiceException("不允许创建超级管理员角色");
            }
            boolean ok = save(role);
            if (role.getMenuIds() != null && role.getMenuIds().length > 0) {
                updateMenuPermissions(role.getRoleId(), java.util.Arrays.asList(role.getMenuIds()));
            }
            return ok;
        }

        @Override
        @Transactional
        public boolean updateRole(SysRole role) {
            if (UserConstants.ROLE_ADMIN.equals(role.getRoleKey())) {
                throw new ServiceException("不允许修改超级管理员角色");
            }
            boolean ok = updateById(role);
            if (role.getMenuIds() != null) {
                updateMenuPermissions(role.getRoleId(), java.util.Arrays.asList(role.getMenuIds()));
            }
            return ok;
        }

    @Override
    public boolean changeStatus(Long roleId, String status) {
        SysRole r = new SysRole();
        r.setRoleId(roleId);
        r.setStatus(status);
        return updateById(r);
    }

    @Override
    @Transactional
    public boolean updateMenuPermissions(Long roleId, List<Long> menuIds) {
        roleMenuMapper.deleteByRoleId(roleId);
        if (menuIds == null || menuIds.isEmpty()) return true;
        List<SysRoleMenu> list = new ArrayList<>();
        for (Long menuId : menuIds) {
            SysRoleMenu rm = new SysRoleMenu();
            rm.setRoleId(roleId);
            rm.setMenuId(menuId);
            list.add(rm);
        }
        return roleMenuMapper.batchInsert(list) > 0;
    }
}