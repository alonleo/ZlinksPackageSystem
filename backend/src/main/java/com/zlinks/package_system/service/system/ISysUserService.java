package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysUser;

import java.util.List;
import java.util.Map;

public interface ISysUserService extends IService<SysUser> {

    /** 分页查询用户 */
    com.baomidou.mybatisplus.core.metadata.IPage<SysUser> selectUserPage(
            com.baomidou.mybatisplus.extension.plugins.pagination.Page<SysUser> page, SysUser query);

    /** 按 userId 查询 (含 dept + roles) */
    SysUser selectUserWithDetail(Long userId);

    /** 按用户名查询 */
    SysUser findByUsername(String userName);

    /** 是否存在用户名 */
    boolean existsByUsername(String userName);

    /** 新增用户 (含 roleIds) */
    boolean insertUser(SysUser user);

    /** 修改用户 (含 roleIds) */
    boolean updateUser(SysUser user);

    /** 修改密码 */
    boolean resetPassword(Long userId, String newPassword);

    /** 修改状态 */
    boolean changeStatus(Long userId, String status);

    /** 更新最后登录信息 */
    void updateLoginInfo(Long userId, String ip);

    /** 已分配某角色的用户分页 */
    com.baomidou.mybatisplus.core.metadata.IPage<SysUser> allocatedUserPage(
            com.baomidou.mybatisplus.extension.plugins.pagination.Page<SysUser> page, Long roleId, SysUser query);

    /** 未分配某角色的用户分页 */
    com.baomidou.mybatisplus.core.metadata.IPage<SysUser> unallocatedUserPage(
            com.baomidou.mybatisplus.extension.plugins.pagination.Page<SysUser> page, Long roleId, SysUser query);

    /** 取消授权 */
    int cancelAuth(Long userId, Long roleId);

    /** 批量取消授权 */
    int cancelAuthAll(Long roleId, List<Long> userIds);

    /** 批量选择授权 (新分配) */
    int selectAuthAll(Long roleId, List<Long> userIds);

    /** 校验用户名唯一 */
    boolean checkUserNameUnique(SysUser user);

    /** 校验手机号唯一 */
    boolean checkPhoneUnique(SysUser user);

    /** 校验邮箱唯一 */
    boolean checkEmailUnique(SysUser user);

    /** 更新个人信息 (current user) */
    boolean updateProfile(SysUser user);

    /** 当前用户修改密码 */
    boolean updatePwd(Long userId, String oldPassword, String newPassword);

    /** 当前用户头像 */
    boolean updateAvatar(Long userId, String avatar);
}
