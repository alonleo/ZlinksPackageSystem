package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysRole;
import com.zlinks.package_system.entity.system.SysUser;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysMenuService;
import com.zlinks.package_system.service.system.ISysRoleService;
import com.zlinks.package_system.service.system.ISysUserService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

/**
 * 角色管理 Controller
 */
@RestController
@RequestMapping("/api/system/role")
@RequiredArgsConstructor
public class SysRoleController extends BaseController {

    private final ISysRoleService roleService;
        private final ISysMenuService menuService;
        private final ISysUserService userService;

    @PreAuthorize("@ss.hasPermi('system:role:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysRole role) {
        Page<SysRole> page = startPage();
        return Result.success(getDataTable(roleService.selectRolePage(page, role)));
    }

    @PreAuthorize("@ss.hasPermi('system:role:query')")
    @GetMapping(value = {"/", "/{roleId}"})
    public Result<SysRole> getInfo(@PathVariable(required = false) Long roleId) {
        return Result.success(roleService.getById(roleId));
    }

    @PreAuthorize("@ss.hasPermi('system:role:add')")
        @Log(title = "角色管理", businessType = BusinessType.ADD)
        @PostMapping
        public Result<Void> add(@RequestBody SysRole role) {
            if (!checkRoleKeyUnique(role)) return Result.error("角色权限已存在");
            if (roleService.insertRole(role)) return Result.success();
            return Result.error("操作失败");
        }

    @PreAuthorize("@ss.hasPermi('system:role:edit')")
    @Log(title = "角色管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysRole role) {
        if (roleService.updateRole(role)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:role:remove')")
    @Log(title = "角色管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{roleIds}")
    public Result<Void> remove(@PathVariable Long[] roleIds) {
        for (Long id : roleIds) {
            if (UserConstants.ROLE_ADMIN.equals(roleService.getById(id).getRoleKey())) {
                return Result.error("不允许删除超级管理员角色");
            }
        }
        for (Long id : roleIds) roleService.removeById(id);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:role:edit')")
    @Log(title = "角色管理", businessType = BusinessType.EDIT)
    @PutMapping("/changeStatus")
    public Result<Void> changeStatus(@RequestBody SysRole role) {
        if (roleService.changeStatus(role.getRoleId(), role.getStatus())) return Result.success();
                return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:role:query')")
    @GetMapping("/menuIds/{roleId}")
    public Result<List<Long>> menuIds(@PathVariable Long roleId) {
        return Result.success(menuService.selectMenuIdsByRoleId(roleId));
    }

    @PreAuthorize("@ss.hasPermi('system:role:list')")
        @GetMapping("/optionselect")
        public Result<List<SysRole>> optionselect() {
            List<SysRole> roles = roleService.list();
            return Result.success(roles);
        }

    @PreAuthorize("@ss.hasPermi('system:role:query')")
    @GetMapping("/allocatedList")
    public Result<Map<String, Object>> allocatedList(@RequestParam Long roleId, SysUser query) {
        Page<SysUser> page = startPage();
        return Result.success(getDataTable(userService.allocatedUserPage(page, roleId, query)));
    }

    @PreAuthorize("@ss.hasPermi('system:role:query')")
    @GetMapping("/unallocatedList")
    public Result<Map<String, Object>> unallocatedList(@RequestParam Long roleId, SysUser query) {
        Page<SysUser> page = startPage();
        return Result.success(getDataTable(userService.unallocatedUserPage(page, roleId, query)));
    }

    private boolean checkRoleKeyUnique(SysRole role) {
        SysRole exist = roleService.getOne(new com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper<SysRole>()
                .eq(SysRole::getRoleKey, role.getRoleKey()).last("LIMIT 1"));
        return exist == null || exist.getRoleId().equals(role.getRoleId());
    }
}