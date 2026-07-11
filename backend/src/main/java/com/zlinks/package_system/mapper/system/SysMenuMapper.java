package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.conditions.Wrapper;
import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.baomidou.mybatisplus.core.toolkit.Constants;
import com.zlinks.package_system.entity.system.SysMenu;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 菜单 Mapper
 */
public interface SysMenuMapper extends BaseMapper<SysMenu> {

    /**
     * 查询菜单列表 (包含 admin 通配)
     */
    List<SysMenu> selectMenusByUser(@Param("userId") Long userId);

    /**
     * 查询角色对应的菜单 ID 列表
     */
    List<Long> selectMenuIdsByRoleId(@Param("roleId") Long roleId);

    /**
     * 查询角色对应的菜单列表
     */
    List<SysMenu> selectMenusByRoleId(@Param("roleId") Long roleId);

    /**
     * 根据角色 ID 集合查询菜单 (取并集, 用于 permissions)
     */
    List<SysMenu> selectMenusByRoleIds(@Param("roleIds") List<Long> roleIds);

    /**
     * 查询子菜单数量
     */
    int countChildren(@Param("menuId") Long menuId);

    /**
     * 查询菜单列表 (支持 Wrapper 条件)
     */
    List<SysMenu> selectMenuList(@Param(Constants.WRAPPER) Wrapper<SysMenu> queryWrapper);
}