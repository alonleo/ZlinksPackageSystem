package com.zlinks.package_system.controller.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.controller.system.BaseController;
import com.zlinks.package_system.entity.monitor.SysJobLog;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.monitor.ISysJobLogService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/monitor/jobLog")
@RequiredArgsConstructor
public class SysJobLogController extends BaseController {

    private final ISysJobLogService jobLogService;

    @PreAuthorize("@ss.hasPermi('monitor:job:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysJobLog query) {
        Page<SysJobLog> page = startPage();
        return Result.success(getDataTable(jobLogService.selectJobLogPage(page, query)));
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:query')")
    @GetMapping("/{jobLogId}")
    public Result<SysJobLog> getInfo(@PathVariable Long jobLogId) {
        return Result.success(jobLogService.getById(jobLogId));
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:remove')")
    @Log(title = "调度日志", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{jobLogIds}")
    public Result<Void> remove(@PathVariable Long[] jobLogIds) {
        jobLogService.deleteByIds(jobLogIds);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:remove')")
    @Log(title = "调度日志", businessType = BusinessType.CLEAN)
    @DeleteMapping("/clean")
    public Result<Void> clean() {
        jobLogService.cleanAll();
        return Result.success();
    }
}