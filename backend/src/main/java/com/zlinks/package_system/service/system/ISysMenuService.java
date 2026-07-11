package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysMenu;

import java.util.List;

public interface ISysMenuService extends IService<SysMenu> {

    List<SysMenu> selectMenuList(SysMenu query);

    List<SysMenu> selectMenusByUser(Long userId);

    List<SysMenu> buildMenuTree(List<SysMenu> menus);

    List<Long> selectMenuIdsByRoleId(Long roleId);

    boolean insertMenu(SysMenu menu);

    boolean updateMenu(SysMenu menu);

    boolean removeMenu(Long menuId);
}
