package com.zlinks.package_system.controller.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.controller.system.BaseController;
import com.zlinks.package_system.entity.monitor.SysOperLog;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.monitor.ISysOperLogService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/monitor/operlog")
@RequiredArgsConstructor
public class SysOperlogController extends BaseController {

    private final ISysOperLogService operLogService;

    @PreAuthorize("@ss.hasPermi('monitor:operlog:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysOperLog operLog) {
        Page<SysOperLog> page = startPage();
        return Result.success(getDataTable(operLogService.selectOperLogPage(page, operLog)));
    }

    @PreAuthorize("@ss.hasPermi('monitor:operlog:query')")
    @GetMapping("/{operId}")
    public Result<SysOperLog> getInfo(@PathVariable Long operId) {
        return Result.success(operLogService.getById(operId));
    }

    @PreAuthorize("@ss.hasPermi('monitor:operlog:remove')")
        @Log(title = "操作日志", businessType = BusinessType.REMOVE)
        @DeleteMapping("/{operIds}")
    public Result<Void> remove(@PathVariable Long[] operIds) {
        operLogService.removeByIds(operIds);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:operlog:remove')")
    @Log(title = "操作日志", businessType = BusinessType.CLEAN)
    @DeleteMapping("/clean")
    public Result<Void> clean() {
        operLogService.cleanAll();
        return Result.success();
    }
}