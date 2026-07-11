package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysMenu;
import com.zlinks.package_system.exception.ServiceException;
import com.zlinks.package_system.mapper.system.SysMenuMapper;
import com.zlinks.package_system.service.system.ISysMenuService;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class SysMenuServiceImpl extends ServiceImpl<SysMenuMapper, SysMenu> implements ISysMenuService {

    @Override
        public List<SysMenu> selectMenuList(SysMenu query) {
            LambdaQueryWrapper<SysMenu> w = new LambdaQueryWrapper<>();
            if (StringUtils.hasText(query.getMenuName())) w.like(SysMenu::getMenuName, query.getMenuName());
            if (StringUtils.hasText(query.getStatus())) w.eq(SysMenu::getStatus, query.getStatus());
            return baseMapper.selectMenuList(w);
        }

    @Override
    public List<SysMenu> selectMenusByUser(Long userId) {
        // admin gets all menus
        return baseMapper.selectMenusByUser(userId);
    }

    @Override
    public List<SysMenu> buildMenuTree(List<SysMenu> menus) {
        List<SysMenu> result = new ArrayList<>();
        for (SysMenu m : menus) {
            if (m.getParentId() == null || m.getParentId() == 0L) {
                m.setChildren(getChildren(menus, m.getMenuId()));
                result.add(m);
            }
        }
        return result;
    }

    private List<SysMenu> getChildren(List<SysMenu> all, Long parentId) {
        List<SysMenu> children = new ArrayList<>();
        for (SysMenu m : all) {
            if (parentId.equals(m.getParentId())) {
                m.setChildren(getChildren(all, m.getMenuId()));
                children.add(m);
            }
        }
        return children;
    }

    @Override
    public List<Long> selectMenuIdsByRoleId(Long roleId) {
        return baseMapper.selectMenuIdsByRoleId(roleId);
    }

    @Override
    public boolean insertMenu(SysMenu menu) {
        if (UserConstants.YES.equals(menu.getIsFrame()) && !StringUtils.hasText(menu.getPath())) {
            throw new ServiceException("菜单路径不能为空");
        }
        return save(menu);
    }

    @Override
    public boolean updateMenu(SysMenu menu) {
        if (UserConstants.YES.equals(menu.getIsFrame()) && !StringUtils.hasText(menu.getPath())) {
            throw new ServiceException("菜单路径不能为空");
        }
        return updateById(menu);
    }

    @Override
    public boolean removeMenu(Long menuId) {
        int n = baseMapper.countChildren(menuId);
        if (n > 0) {
            throw new ServiceException("存在子菜单,不允许删除");
        }
        return removeById(menuId);
    }
}