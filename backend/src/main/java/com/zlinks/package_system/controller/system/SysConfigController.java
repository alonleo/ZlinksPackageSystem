package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.constant.CacheConstants;
import com.zlinks.package_system.entity.system.SysConfig;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysConfigService;
import com.zlinks.package_system.util.RedisUtils;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/system/config")
@RequiredArgsConstructor
public class SysConfigController extends BaseController {

    private final ISysConfigService configService;
    private final RedisUtils redisUtils;

    @PreAuthorize("@ss.hasPermi('system:config:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysConfig config) {
        Page<SysConfig> page = startPage();
        return Result.success(getDataTable(configService.selectPage(page, config)));
    }

    @PreAuthorize("@ss.hasPermi('system:config:query')")
    @GetMapping(value = {"/", "/{configId}"})
    public Result<SysConfig> getInfo(@PathVariable(required = false) Long configId) {
        return Result.success(configId == null ? new SysConfig() : configService.getById(configId));
    }

    @GetMapping("/key/{configKey}")
    public Result<String> getByKey(@PathVariable String configKey) {
        SysConfig c = configService.getByKey(configKey);
        return Result.success(c == null ? null : c.getConfigValue());
    }

    @PreAuthorize("@ss.hasPermi('system:config:add')")
    @Log(title = "参数管理", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysConfig config) {
        configService.saveConfig(config);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:config:edit')")
    @Log(title = "参数管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysConfig config) {
        configService.saveConfig(config);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:config:remove')")
    @Log(title = "参数管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{configIds}")
    public Result<Void> remove(@PathVariable Long[] configIds) {
        configService.removeBatchByIds(java.util.Arrays.asList(configIds));
        redisUtils.deletePattern(CacheConstants.SYS_CONFIG_KEY + "*");
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('system:config:edit')")
    @Log(title = "参数管理", businessType = BusinessType.CLEAN)
    @DeleteMapping("/refreshCache")
    public Result<Void> refreshCache() {
        redisUtils.deletePattern(CacheConstants.SYS_CONFIG_KEY + "*");
        return Result.success();
    }
}