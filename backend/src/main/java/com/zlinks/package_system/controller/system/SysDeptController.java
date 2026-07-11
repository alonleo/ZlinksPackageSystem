package com.zlinks.package_system.controller.system;

import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.entity.system.SysDept;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysDeptService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.List;

/**
 * 部门管理 Controller
 */
@RestController
@RequestMapping("/api/system/dept")
@RequiredArgsConstructor
public class SysDeptController extends BaseController {

    private final ISysDeptService deptService;

    @PreAuthorize("@ss.hasPermi('system:dept:list')")
    @GetMapping("/list")
    public Result<List<SysDept>> list(SysDept query) {
        List<SysDept> list = deptService.selectDeptList(query);
        return Result.success(list);
    }

    @PreAuthorize("@ss.hasPermi('system:dept:list')")
    @GetMapping("/list/exclude/{deptId}")
    public Result<List<SysDept>> listExclude(@PathVariable Long deptId) {
        return Result.success(deptService.selectDeptListExclude(deptId));
    }

    @PreAuthorize("@ss.hasPermi('system:dept:query')")
    @GetMapping(value = {"/", "/{deptId}"})
    public Result<SysDept> getInfo(@PathVariable(required = false) Long deptId) {
        return Result.success(deptService.getById(deptId));
    }

    @PreAuthorize("@ss.hasPermi('system:dept:add')")
    @Log(title = "部门管理", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysDept dept) {
        if (deptService.insertDept(dept)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:dept:edit')")
    @Log(title = "部门管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysDept dept) {
        if (deptService.updateDept(dept)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:dept:remove')")
    @Log(title = "部门管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{deptId}")
    public Result<Void> remove(@PathVariable Long deptId) {
        if (deptService.removeDept(deptId)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:dept:list')")
    @GetMapping("/treeselect")
    public Result<List<SysDept>> treeselect() {
        List<SysDept> list = deptService.selectDeptList(new SysDept());
        return Result.success(deptService.buildDeptTree(list));
    }
}