package com.zlinks.package_system.controller.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.controller.system.BaseController;
import com.zlinks.package_system.entity.monitor.SysJob;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.monitor.ISysJobService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.quartz.SchedulerException;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/monitor/job")
@RequiredArgsConstructor
public class SysJobController extends BaseController {

    private final ISysJobService jobService;

    @PreAuthorize("@ss.hasPermi('monitor:job:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysJob query) {
        Page<SysJob> page = startPage();
        return Result.success(getDataTable(jobService.selectJobPage(page, query)));
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:query')")
    @GetMapping("/{jobId}")
    public Result<SysJob> getInfo(@PathVariable Long jobId) {
        return Result.success(jobService.getById(jobId));
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:add')")
    @Log(title = "定时任务", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysJob job) throws SchedulerException, ClassNotFoundException {
        job.setCreateBy("admin");
        jobService.insertJob(job);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:edit')")
    @Log(title = "定时任务", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysJob job) throws SchedulerException, ClassNotFoundException {
        job.setUpdateBy("admin");
        jobService.updateJob(job);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:remove')")
    @Log(title = "定时任务", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{jobIds}")
    public Result<Void> remove(@PathVariable Long[] jobIds) throws SchedulerException {
        jobService.deleteJobByIds(jobIds);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:changeStatus')")
    @Log(title = "定时任务", businessType = BusinessType.EDIT)
    @PutMapping("/changeStatus")
    public Result<Void> changeStatus(@RequestBody SysJob job) throws SchedulerException, ClassNotFoundException {
        jobService.changeStatus(job.getJobId(), job.getStatus());
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:job:changeStatus')")
    @Log(title = "定时任务", businessType = BusinessType.EDIT)
    @PutMapping("/run")
    public Result<Void> run(@RequestBody SysJob job) throws SchedulerException, ClassNotFoundException {
        jobService.runJobOnce(job.getJobId());
        return Result.success();
    }
}