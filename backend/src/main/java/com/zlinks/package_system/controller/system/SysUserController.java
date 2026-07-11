package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysUser;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.util.SecurityUtils;
import com.zlinks.package_system.service.system.ISysUserService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

/**
 * 用户管理 Controller
 */
@RestController
@RequestMapping("/api/system/user")
@RequiredArgsConstructor
public class SysUserController extends BaseController {

    private final ISysUserService userService;

    @PreAuthorize("@ss.hasPermi('system:user:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysUser user) {
        Page<SysUser> page = startPage();
        return Result.success(getDataTable(userService.selectUserPage(page, user)));
    }

    @PreAuthorize("@ss.hasPermi('system:user:query')")
    @GetMapping(value = {"/", "/{userId}"})
    public Result<SysUser> getInfo(@PathVariable(required = false) Long userId) {
        return Result.success(userService.selectUserWithDetail(userId == null ? SecurityUtils.getUserId() : userId));
    }

    @PreAuthorize("@ss.hasPermi('system:user:add')")
    @Log(title = "用户管理", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysUser user) {
        if (!userService.checkUserNameUnique(user)) {
                    return Result.error("登录账号已存在");
                }
        userService.insertUser(user);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:user:edit')")
    @Log(title = "用户管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysUser user) {
        userService.updateUser(user);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:user:remove')")
    @Log(title = "用户管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{userIds}")
    public Result<Void> remove(@PathVariable Long[] userIds) {
        for (Long id : userIds) {
            if (UserConstants.USER_ADMIN.equals(userService.getById(id).getUserName())) {
                return Result.error("不允许删除超级管理员");
            }
        }
        for (Long id : userIds) userService.removeById(id);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:user:resetPwd')")
    @Log(title = "用户管理", businessType = BusinessType.EDIT)
    @PutMapping("/resetPwd")
    public Result<Void> resetPwd(@RequestBody SysUser user) {
        userService.resetPassword(user.getUserId(), user.getPassword());
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:user:edit')")
    @Log(title = "用户管理", businessType = BusinessType.EDIT)
    @PutMapping("/changeStatus")
    public Result<Void> changeStatus(@RequestBody SysUser user) {
        userService.changeStatus(user.getUserId(), user.getStatus());
        return Result.success();
    }

    @GetMapping("/profile")
    public Result<SysUser> profile() {
        return Result.success(userService.selectUserWithDetail(SecurityUtils.getUserId()));
    }

    @Log(title = "个人信息", businessType = BusinessType.EDIT)
    @PutMapping("/profile")
    public Result<Void> updateProfile(@RequestBody SysUser user) {
        userService.updateProfile(user);
        return Result.success();
    }

    @Log(title = "个人信息", businessType = BusinessType.EDIT)
    @PutMapping("/profile/updatePwd")
    public Result<Void> updatePwd(@RequestBody Map<String, String> body) {
        String oldPwd = body.get("oldPassword");
        String newPwd = body.get("newPassword");
        userService.updatePwd(SecurityUtils.getUserId(), oldPwd, newPwd);
        return Result.success();
    }

    @Log(title = "个人信息", businessType = BusinessType.EDIT)
    @PutMapping("/profile/avatar")
    public Result<Void> updateAvatar(@RequestBody Map<String, String> body) {
        userService.updateAvatar(SecurityUtils.getUserId(), body.get("avatar"));
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:user:query')")
    @GetMapping("/authRole/{userId}")
    public Result<java.util.List<com.zlinks.package_system.entity.system.SysRole>> authRole(@PathVariable Long userId) {
        SysUser u = userService.selectUserWithDetail(userId);
                java.util.List<com.zlinks.package_system.entity.system.SysRole> roleList = new java.util.ArrayList<>();
                if (u != null && u.getRoles() != null) {
                    for (Object o : u.getRoles()) {
                        if (o instanceof com.zlinks.package_system.entity.system.SysRole r) roleList.add(r);
                    }
                }
                return Result.success(roleList);
    }
}