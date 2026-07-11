package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.conditions.Wrapper;
import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.core.toolkit.Constants;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.system.SysUser;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 用户 Mapper
 */
public interface SysUserMapper extends BaseMapper<SysUser> {

    /**
     * 分页查询用户列表 (含 dept / roles)
     */
    IPage<SysUser> selectUserPage(Page<SysUser> page, @Param(Constants.WRAPPER) Wrapper<SysUser> queryWrapper);

    /**
     * 完整用户 (含 dept / roles / post)
     */
    SysUser selectUserWithDetail(@Param("userId") Long userId);

    /**
         * 查询已分配某角色的用户 (分页)
         */
        IPage<SysUser> selectAllocatedUserPage(Page<SysUser> page,
                                               @Param("roleId") Long roleId,
                                               @Param(Constants.WRAPPER) Wrapper<SysUser> queryWrapper);

        /**
         * 查询未分配某角色的用户 (分页)
         */
        IPage<SysUser> selectUnallocatedUserPage(Page<SysUser> page,
                                                 @Param("roleId") Long roleId,
                                                 @Param(Constants.WRAPPER) Wrapper<SysUser> queryWrapper);

    /**
     * 按用户名查询 (含部门、角色)
     */
    SysUser selectByUsername(@Param("userName") String userName);

    /**
     * 更新最后登录信息
     */
    int updateLoginInfo(@Param("userId") Long userId,
                        @Param("ip") String ip,
                        @Param("loginDate") java.time.LocalDateTime loginDate);
}