package com.zlinks.package_system.controller.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.controller.system.BaseController;
import com.zlinks.package_system.entity.monitor.SysLogininfor;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.monitor.ISysLogininforService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/monitor/logininfor")
@RequiredArgsConstructor
public class SysLogininforController extends BaseController {

    private final ISysLogininforService logininforService;

    @PreAuthorize("@ss.hasPermi('monitor:logininfor:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysLogininfor info) {
        Page<SysLogininfor> page = startPage();
        return Result.success(getDataTable(logininforService.selectPage(page, info)));
    }

    @PreAuthorize("@ss.hasPermi('monitor:logininfor:remove')")
    @Log(title = "登录日志", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{infoIds}")
    public Result<Void> remove(@PathVariable Long[] infoIds) {
        logininforService.removeByIds(infoIds);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:logininfor:remove')")
    @Log(title = "登录日志", businessType = BusinessType.CLEAN)
    @DeleteMapping("/clean")
    public Result<Void> clean() {
        logininforService.cleanAll();
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:logininfor:unlock')")
    @Log(title = "账户解锁", businessType = BusinessType.EDIT)
    @PutMapping("/unlock/{userName}")
    public Result<Void> unlock(@PathVariable String userName) {
        // 占位实现 - 实际可通过 Redis 维护登录失败次数
        return Result.success();
    }
}