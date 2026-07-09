package com.zlinks.package_system.controller;

import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.service.PlatformMatchService;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@Tag(name = "测试管理")
@RestController
@RequestMapping("/api/tests")
@RequiredArgsConstructor
public class PlatformMatchController {

    private final PlatformMatchService platformMatchService;

    @Operation(summary = "获取测试统计")
    @GetMapping("/counts")
    public Result<CountResult> getCounts() {
        return Result.success(platformMatchService.getCounts());
    }
}
