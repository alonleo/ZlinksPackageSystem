package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.conditions.Wrapper;
import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.core.toolkit.Constants;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.system.SysRole;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 角色 Mapper
 */
public interface SysRoleMapper extends BaseMapper<SysRole> {

    /**
     * 分页查询角色
     */
    IPage<SysRole> selectRolePage(Page<SysRole> page, @Param(Constants.WRAPPER) Wrapper<SysRole> queryWrapper);

    /**
     * 按用户 ID 查询角色列表
     */
    List<SysRole> selectRolesByUserId(@Param("userId") Long userId);
}