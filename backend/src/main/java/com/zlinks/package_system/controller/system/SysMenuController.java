package com.zlinks.package_system.controller.system;

import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.SysMenu;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysMenuService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.List;

/**
 * 菜单管理 Controller
 */
@RestController
@RequestMapping("/api/system/menu")
@RequiredArgsConstructor
public class SysMenuController extends BaseController {

    private final ISysMenuService menuService;

    @PreAuthorize("@ss.hasPermi('system:menu:list')")
    @GetMapping("/list")
    public Result<List<SysMenu>> list(SysMenu query) {
        List<SysMenu> list = menuService.selectMenuList(query);
        return Result.success(list);
    }

    @PreAuthorize("@ss.hasPermi('system:menu:query')")
    @GetMapping(value = {"/", "/{menuId}"})
    public Result<SysMenu> getInfo(@PathVariable(required = false) Long menuId) {
        return Result.success(menuService.getById(menuId));
    }

    @PreAuthorize("@ss.hasPermi('system:menu:add')")
    @Log(title = "菜单管理", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysMenu menu) {
        if (menuService.insertMenu(menu)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:menu:edit')")
    @Log(title = "菜单管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysMenu menu) {
        if (menuService.updateMenu(menu)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:menu:remove')")
    @Log(title = "菜单管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{menuId}")
    public Result<Void> remove(@PathVariable Long menuId) {
        if (menuService.removeMenu(menuId)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:menu:list')")
    @GetMapping("/treeselect")
    public Result<List<SysMenu>> treeselect(SysMenu query) {
        List<SysMenu> list = menuService.selectMenuList(query);
        // 构造树形
        return Result.success(menuService.buildMenuTree(list));
    }

    @PreAuthorize("@ss.hasPermi('system:menu:list')")
    @GetMapping("/roleMenuTreeselect/{roleId}")
    public Result<?> roleMenuTreeselect(@PathVariable Long roleId) {
        List<SysMenu> menus = menuService.selectMenuList(new SysMenu());
        java.util.Map<String, Object> rsp = new java.util.HashMap<>();
        rsp.put("checkedKeys", menuService.selectMenuIdsByRoleId(roleId));
        rsp.put("menus", menuService.buildMenuTree(menus));
        return Result.success(rsp);
    }
}