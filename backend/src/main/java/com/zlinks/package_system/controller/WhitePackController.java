package com.zlinks.package_system.controller;

import com.zlinks.package_system.service.WhitePackService;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Map;

@Tag(name = "白包处理")
@RestController
@RequestMapping("/api/white-pack")
@RequiredArgsConstructor
public class WhitePackController {

    private final WhitePackService whitePackService;

    @Operation(summary = "预览白包处理变更（dry-run）")
    @GetMapping("/preview")
    public Result<List<Map<String, String>>> preview(
            @Parameter(description = "游戏项目根路径", required = true)
            @RequestParam String projectPath) {
        List<Map<String, String>> changes = whitePackService.preview(projectPath);
        return Result.success(changes);
    }

    @Operation(summary = "执行白包处理")
    @PostMapping("/apply")
    public Result<List<Map<String, String>>> apply(
            @Parameter(description = "游戏项目根路径", required = true)
            @RequestParam String projectPath) {
        List<Map<String, String>> changes = whitePackService.apply(projectPath);
        return Result.success(changes);
    }
}
