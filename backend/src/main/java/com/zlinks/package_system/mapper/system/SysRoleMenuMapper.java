package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.zlinks.package_system.entity.system.SysRoleMenu;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 角色-菜单关联 Mapper
 */
public interface SysRoleMenuMapper extends BaseMapper<SysRoleMenu> {

    int batchInsert(@Param("list") List<SysRoleMenu> list);

    int deleteByRoleId(@Param("roleId") Long roleId);
}