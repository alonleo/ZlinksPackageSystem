package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.entity.system.SysNotice;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysNoticeService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/system/notice")
@RequiredArgsConstructor
public class SysNoticeController extends BaseController {

    private final ISysNoticeService noticeService;

    @PreAuthorize("@ss.hasPermi('system:notice:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysNotice notice) {
        Page<SysNotice> page = startPage();
        return Result.success(getDataTable(noticeService.selectPage(page, notice)));
    }

    @PreAuthorize("@ss.hasPermi('system:notice:query')")
    @GetMapping("/{noticeId}")
    public Result<SysNotice> getInfo(@PathVariable Long noticeId) {
        return Result.success(noticeService.getById(noticeId));
    }

    @PreAuthorize("@ss.hasPermi('system:notice:add')")
    @Log(title = "通知公告", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysNotice notice) {
        noticeService.save(notice);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:notice:edit')")
    @Log(title = "通知公告", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysNotice notice) {
        noticeService.updateById(notice);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:notice:remove')")
    @Log(title = "通知公告", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{noticeIds}")
    public Result<Void> remove(@PathVariable Long[] noticeIds) {
        noticeService.removeBatchByIds(java.util.Arrays.asList(noticeIds));
        return Result.success();
    }
}