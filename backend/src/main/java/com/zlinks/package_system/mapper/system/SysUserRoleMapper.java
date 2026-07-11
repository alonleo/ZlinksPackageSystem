package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.zlinks.package_system.entity.system.SysUserRole;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 用户-角色关联 Mapper
 */
public interface SysUserRoleMapper extends BaseMapper<SysUserRole> {

    /**
     * 查询某用户的角色 ID 集合
     */
    List<Long> selectRoleIdsByUserId(@Param("userId") Long userId);

    /**
     * 查询某角色的用户 ID 集合
     */
    List<Long> selectUserIdsByRoleId(@Param("roleId") Long roleId);

    /**
     * 批量插入 (如果不存在)
     */
    int batchInsert(@Param("list") List<SysUserRole> list);

    /**
     * 取消授权单个
     */
    int deleteUserRole(@Param("userId") Long userId, @Param("roleId") Long roleId);

    /**
     * 取消授权 (用户所有)
     */
    int deleteByUserId(@Param("userId") Long userId);

    /**
     * 取消授权 (角色所有)
     */
    int deleteByRoleId(@Param("roleId") Long roleId);
}