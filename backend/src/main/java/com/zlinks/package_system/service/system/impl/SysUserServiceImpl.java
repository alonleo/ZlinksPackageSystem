package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysUser;
import com.zlinks.package_system.entity.system.SysUserRole;
import com.zlinks.package_system.exception.ServiceException;
import com.zlinks.package_system.mapper.system.SysUserMapper;
import com.zlinks.package_system.mapper.system.SysUserRoleMapper;
import com.zlinks.package_system.service.system.ISysUserService;
import com.zlinks.package_system.util.SecurityUtils;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class SysUserServiceImpl extends ServiceImpl<SysUserMapper, SysUser> implements ISysUserService {

    private final SysUserRoleMapper userRoleMapper;

    @Override
    public IPage<SysUser> selectUserPage(Page<SysUser> page, SysUser query) {
        LambdaQueryWrapper<SysUser> w = new LambdaQueryWrapper<>();
        if (StringUtils.hasText(query.getUserName())) w.like(SysUser::getUserName, query.getUserName());
        if (StringUtils.hasText(query.getNickName())) w.like(SysUser::getNickName, query.getNickName());
        if (StringUtils.hasText(query.getPhonenumber())) w.like(SysUser::getPhonenumber, query.getPhonenumber());
        if (StringUtils.hasText(query.getStatus())) w.eq(SysUser::getStatus, query.getStatus());
        if (query.getDeptId() != null) w.eq(SysUser::getDeptId, query.getDeptId());
                return baseMapper.selectUserPage(page, w);
            }

    @Override public SysUser selectUserWithDetail(Long userId) { return baseMapper.selectUserWithDetail(userId); }
    @Override public SysUser findByUsername(String userName) { return baseMapper.selectByUsername(userName); }

    @Override
    public boolean existsByUsername(String userName) {
        return count(new LambdaQueryWrapper<SysUser>()
                .eq(SysUser::getUserName, userName).eq(SysUser::getIsDeleted, 0)) > 0;
    }

    @Override
        @Transactional
        public boolean insertUser(SysUser user) {
            if (!checkUserNameUnique(user)) throw new ServiceException("登录账号已存在");
            if (StringUtils.hasText(user.getPhonenumber()) && !checkPhoneUnique(user))
                throw new ServiceException("手机号码已存在");
            if (StringUtils.hasText(user.getEmail()) && !checkEmailUnique(user))
                throw new ServiceException("邮箱账号已存在");
            if (!StringUtils.hasText(user.getPassword())) {
                user.setPassword(SecurityUtils.encryptPassword(UserConstants.DEFAULT_PASSWORD));
            } else if (!user.getPassword().startsWith("$2")) {
                // 明文 -> BCrypt
                user.setPassword(SecurityUtils.encryptPassword(user.getPassword()));
            }
        if (!StringUtils.hasText(user.getStatus())) user.setStatus(UserConstants.NORMAL);
        boolean ok = save(user);
        insertUserRoles(user.getUserId(), user.getRoleIds());
        return ok;
    }

    @Override
    @Transactional
    public boolean updateUser(SysUser user) {
        if (StringUtils.hasText(user.getPhonenumber()) && !checkPhoneUnique(user))
            throw new ServiceException("手机号码已存在");
        if (StringUtils.hasText(user.getEmail()) && !checkEmailUnique(user))
            throw new ServiceException("邮箱账号已存在");
        user.setPassword(null);
        boolean ok = updateById(user);
        userRoleMapper.deleteByUserId(user.getUserId());
        insertUserRoles(user.getUserId(), user.getRoleIds());
        return ok;
    }

    private void insertUserRoles(Long userId, Long[] roleIds) {
        if (roleIds == null || roleIds.length == 0) return;
        List<SysUserRole> list = new ArrayList<>();
        for (Long rid : roleIds) { SysUserRole ur = new SysUserRole(); ur.setUserId(userId); ur.setRoleId(rid); list.add(ur); }
        userRoleMapper.batchInsert(list);
    }

    @Override public boolean resetPassword(Long userId, String newPassword) {
        SysUser u = new SysUser(); u.setUserId(userId); u.setPassword(SecurityUtils.encryptPassword(newPassword));
        return updateById(u);
    }

    @Override public boolean changeStatus(Long userId, String status) {
        SysUser u = new SysUser(); u.setUserId(userId); u.setStatus(status); return updateById(u);
    }

    @Override public void updateLoginInfo(Long userId, String ip) {
        baseMapper.updateLoginInfo(userId, ip, java.time.LocalDateTime.now());
    }

    @Override
    public IPage<SysUser> allocatedUserPage(Page<SysUser> page, Long roleId, SysUser query) {
        LambdaQueryWrapper<SysUser> w = new LambdaQueryWrapper<>();
        if (StringUtils.hasText(query.getUserName())) w.like(SysUser::getUserName, query.getUserName());
        if (StringUtils.hasText(query.getPhonenumber())) w.like(SysUser::getPhonenumber, query.getPhonenumber());
        // 实际用 SQL where: ur.role_id = ?  -- 通过 selectAllocatedUserPage 自带 roleId 参数
        return baseMapper.selectAllocatedUserPage(page, roleId, w);
            }

            @Override
            public IPage<SysUser> unallocatedUserPage(Page<SysUser> page, Long roleId, SysUser query) {
                LambdaQueryWrapper<SysUser> w = new LambdaQueryWrapper<>();
                if (StringUtils.hasText(query.getUserName())) w.like(SysUser::getUserName, query.getUserName());
                if (StringUtils.hasText(query.getPhonenumber())) w.like(SysUser::getPhonenumber, query.getPhonenumber());
                return baseMapper.selectUnallocatedUserPage(page, roleId, w);
            }

    @Override public int cancelAuth(Long userId, Long roleId) { return userRoleMapper.deleteUserRole(userId, roleId); }

    @Override
    @Transactional
    public int cancelAuthAll(Long roleId, List<Long> userIds) {
        int n = 0; for (Long uid : userIds) n += userRoleMapper.deleteUserRole(uid, roleId); return n;
    }

    @Override
    @Transactional
    public int selectAuthAll(Long roleId, List<Long> userIds) {
        List<SysUserRole> list = new ArrayList<>();
        for (Long uid : userIds) { SysUserRole ur = new SysUserRole(); ur.setUserId(uid); ur.setRoleId(roleId); list.add(ur); }
        return userRoleMapper.batchInsert(list);
    }

    @Override
    public boolean checkUserNameUnique(SysUser user) {
        Long userId = user.getUserId();
        SysUser exist = getOne(new LambdaQueryWrapper<SysUser>().eq(SysUser::getUserName, user.getUserName()).eq(SysUser::getIsDeleted, 0));
        return exist == null || exist.getUserId().equals(userId);
    }

    @Override
    public boolean checkPhoneUnique(SysUser user) {
        Long userId = user.getUserId() == null ? -1L : user.getUserId();
        SysUser exist = getOne(new LambdaQueryWrapper<SysUser>().eq(SysUser::getPhonenumber, user.getPhonenumber()).eq(SysUser::getIsDeleted, 0));
        return exist == null || exist.getUserId().equals(userId);
    }

    @Override
    public boolean checkEmailUnique(SysUser user) {
        Long userId = user.getUserId() == null ? -1L : user.getUserId();
        SysUser exist = getOne(new LambdaQueryWrapper<SysUser>().eq(SysUser::getEmail, user.getEmail()).eq(SysUser::getIsDeleted, 0));
        return exist == null || exist.getUserId().equals(userId);
    }

    @Override public boolean updateProfile(SysUser user) { user.setPassword(null); return updateById(user); }

    @Override
    @Transactional
    public boolean updatePwd(Long userId, String oldPassword, String newPassword) {
        SysUser u = getById(userId);
        if (u == null) throw new ServiceException("用户不存在");
        if (!SecurityUtils.matchesPassword(oldPassword, u.getPassword()))
            throw new ServiceException("原密码错误");
        return resetPassword(userId, newPassword);
    }

    @Override public boolean updateAvatar(Long userId, String avatar) {
        SysUser u = new SysUser(); u.setUserId(userId); u.setAvatar(avatar); return updateById(u);
    }
}