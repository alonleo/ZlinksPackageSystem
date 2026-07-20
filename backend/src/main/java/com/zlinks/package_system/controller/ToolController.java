package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.Tool;
import com.zlinks.package_system.service.ToolService;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.stream.Collectors;

@Tag(name = "工具库管理")
@RestController
@RequestMapping("/api/tools")
@RequiredArgsConstructor
public class ToolController {

    private final ToolService toolService;

    @Operation(summary = "获取工具统计")
    @GetMapping("/counts")
    public Result<CountResult> getCounts() {
        return Result.success(toolService.getCounts());
    }

    @Operation(summary = "获取工具列表")
    @GetMapping
    public Result<PageResult<Tool>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "20") Integer size,
            @RequestParam(required = false) String name,
            @RequestParam(required = false) String category,
            @RequestParam(required = false) String manager,
            @RequestParam(required = false) String status,
            @RequestParam(required = false) Integer isSystemBuiltin) {

        LambdaQueryWrapper<Tool> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(name), Tool::getName, name);
        wrapper.eq(StringUtils.hasText(category), Tool::getCategory, category);
        wrapper.like(StringUtils.hasText(manager), Tool::getManager, manager);
        wrapper.eq(StringUtils.hasText(status), Tool::getStatus, status);
        wrapper.eq(isSystemBuiltin != null, Tool::getIsSystemBuiltin, isSystemBuiltin);
        wrapper.orderByDesc(Tool::getCreateTime);

        Page<Tool> page = toolService.page(new Page<>(current, size), wrapper);
        PageResult<Tool> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取工具筛选选项")
    @GetMapping("/options")
    public Result<java.util.Map<String, java.util.List<String>>> getOptions() {
        List<Tool> all = toolService.list();
        java.util.Map<String, java.util.List<String>> options = new java.util.LinkedHashMap<>();
        options.put("categories", all.stream().map(Tool::getCategory).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(Collectors.toList()));
        options.put("managers", all.stream().map(Tool::getManager).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(Collectors.toList()));
        options.put("statuses", all.stream().map(Tool::getStatus).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(Collectors.toList()));
        return Result.success(options);
    }

    @Operation(summary = "获取工具详情")
    @GetMapping("/{id}")
    public Result<Tool> getById(@PathVariable Long id) {
        return Result.success(toolService.getById(id));
    }

    @Operation(summary = "创建工具")
    @PostMapping
    public Result<Tool> create(@Valid @RequestBody Tool tool) {
        toolService.save(tool);
        return Result.success(tool);
    }

    @Operation(summary = "更新工具")
    @PutMapping("/{id}")
    public Result<Tool> update(@PathVariable Long id, @Valid @RequestBody Tool tool) {
        tool.setId(id);
        toolService.updateById(tool);
        return Result.success(tool);
    }

    @Operation(summary = "删除工具")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        toolService.removeById(id);
        return Result.success();
    }
}
