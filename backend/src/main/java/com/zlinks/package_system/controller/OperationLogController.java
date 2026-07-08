package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.OperationLogExcelDTO;
import com.zlinks.package_system.entity.OperationLog;
import com.zlinks.package_system.service.OperationLogService;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.io.IOException;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;

@Tag(name = "操作日志")
@RestController
@RequestMapping("/api/operation-logs")
@RequiredArgsConstructor
public class OperationLogController {

    private final OperationLogService operationLogService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取操作日志列表")
    @GetMapping
    public Result<PageResult<OperationLog>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String username,
            @RequestParam(required = false) String module,
            @RequestParam(required = false) String action) {

        LambdaQueryWrapper<OperationLog> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(username), OperationLog::getUsername, username);
        wrapper.eq(StringUtils.hasText(module), OperationLog::getModule, module);
        wrapper.eq(StringUtils.hasText(action), OperationLog::getAction, action);
        wrapper.orderByDesc(OperationLog::getCreateTime);

        Page<OperationLog> page = operationLogService.page(new Page<>(current, size), wrapper);
        PageResult<OperationLog> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "导出操作日志")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format,
                           @RequestParam(required = false) String username,
                           @RequestParam(required = false) String module,
                           @RequestParam(required = false) String action,
                           HttpServletResponse response) throws IOException {
        LambdaQueryWrapper<OperationLog> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(username), OperationLog::getUsername, username);
        wrapper.eq(StringUtils.hasText(module), OperationLog::getModule, module);
        wrapper.eq(StringUtils.hasText(action), OperationLog::getAction, action);
        wrapper.orderByDesc(OperationLog::getCreateTime);
        wrapper.last("LIMIT 10000");

        List<OperationLog> list = operationLogService.list(wrapper);

        DateTimeFormatter fmt = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");
        List<OperationLogExcelDTO> exportList = new ArrayList<>();
        for (OperationLog log : list) {
            OperationLogExcelDTO dto = new OperationLogExcelDTO();
            dto.setUsername(log.getUsername());
            dto.setModule(log.getModule());
            dto.setAction(log.getAction());
            dto.setTarget(log.getTarget());
            dto.setIpAddress(log.getIpAddress());
            dto.setCreateTime(log.getCreateTime() != null ? log.getCreateTime().format(fmt) : "");
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("操作日志", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), OperationLogExcelDTO.class)
                    .sheet("操作日志")
                    .doWrite(exportList);
        }
    }
}
