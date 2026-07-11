package com.zlinks.package_system.controller.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.controller.system.BaseController;
import com.zlinks.package_system.entity.monitor.SysUserOnline;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.monitor.ISysUserOnlineService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/monitor/online")
@RequiredArgsConstructor
public class SysUserOnlineController extends BaseController {

    private final ISysUserOnlineService userOnlineService;

    @PreAuthorize("@ss.hasPermi('monitor:online:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(String ipaddr, String userName) {
        Page<SysUserOnline> page = startPage();
        return Result.success(getDataTable(userOnlineService.selectOnlinePage(page, ipaddr, userName)));
    }

    @PreAuthorize("@ss.hasPermi('monitor:online:forceLogout')")
    @Log(title = "在线用户", businessType = BusinessType.FORCE)
    @DeleteMapping("/{tokenId}")
    public Result<Void> forceLogout(@PathVariable String tokenId) {
        userOnlineService.forceLogout(tokenId);
        return Result.success();
    }
}