package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.PlatformExcelDTO;
import com.zlinks.package_system.dto.PlatformRequest;
import com.zlinks.package_system.entity.Platform;
import com.zlinks.package_system.service.PlatformService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
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
import org.springframework.web.multipart.MultipartFile;

import jakarta.validation.Valid;
import java.io.IOException;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

@Tag(name = "平台管理")
@RestController
@RequestMapping("/api/platforms")
@RequiredArgsConstructor
public class PlatformController {

    private final PlatformService platformService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取平台列表")
    @GetMapping
    public Result<PageResult<Platform>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String platformName,
            @RequestParam(required = false) String status) {

        LambdaQueryWrapper<Platform> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(platformName), Platform::getPlatformName, platformName);
        wrapper.eq(StringUtils.hasText(status), Platform::getStatus, status);
        wrapper.orderByAsc(Platform::getSortOrder);
        wrapper.orderByDesc(Platform::getCreateTime);

        Page<Platform> page = platformService.page(new Page<>(current, size), wrapper);
        PageResult<Platform> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取全部平台（用于下拉）")
    @GetMapping("/all")
    public Result<List<Platform>> listAll() {
        LambdaQueryWrapper<Platform> wrapper = new LambdaQueryWrapper<>();
        wrapper.orderByAsc(Platform::getSortOrder);
        wrapper.orderByDesc(Platform::getCreateTime);
        return Result.success(platformService.list(wrapper));
    }

    @Operation(summary = "获取平台详情")
    @GetMapping("/{id}")
    public Result<Platform> getById(@PathVariable Long id) {
        Platform platform = platformService.getById(id);
        if (platform == null) {
            throw new BusinessException("平台不存在");
        }
        return Result.success(platform);
    }

    @Operation(summary = "创建平台")
    @PostMapping
    public Result<Platform> create(@Valid @RequestBody PlatformRequest request) {
        Platform platform = new Platform();
        platform.setPlatformName(request.getPlatformName());
        platform.setPlatformCode(request.getPlatformCode());
        platform.setSortOrder(request.getSortOrder() != null ? request.getSortOrder() : 0);
        platform.setStatus(request.getStatus() != null ? request.getStatus() : "active");
        platform.setRemark(request.getRemark());

        platformService.save(platform);
        return Result.success(platform);
    }

    @Operation(summary = "更新平台")
    @PutMapping("/{id}")
    public Result<Platform> update(@PathVariable Long id, @Valid @RequestBody PlatformRequest request) {
        Platform platform = platformService.getById(id);
        if (platform == null) {
            throw new BusinessException("平台不存在");
        }

        platform.setPlatformName(request.getPlatformName());
        platform.setPlatformCode(request.getPlatformCode());
        if (request.getSortOrder() != null) platform.setSortOrder(request.getSortOrder());
        platform.setStatus(request.getStatus());
        platform.setRemark(request.getRemark());

        platformService.updateById(platform);
        return Result.success(platform);
    }

    @Operation(summary = "导入平台数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<PlatformExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(PlatformExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<Platform> list = new ArrayList<>();
        for (PlatformExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getPlatformName())) {
                continue;
            }
            Platform entity = new Platform();
            entity.setPlatformName(dto.getPlatformName());
            entity.setPlatformCode(dto.getPlatformCode());
            entity.setSortOrder(dto.getSortOrder());
            entity.setStatus(StringUtils.hasText(dto.getStatus()) ? dto.getStatus() : "active");
            entity.setRemark(dto.getRemark());
            list.add(entity);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        platformService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出平台数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Platform> list = platformService.list(new LambdaQueryWrapper<Platform>()
                .orderByAsc(Platform::getSortOrder)
                .orderByDesc(Platform::getCreateTime));

        List<PlatformExcelDTO> exportList = new ArrayList<>();
        for (Platform item : list) {
            PlatformExcelDTO dto = new PlatformExcelDTO();
            dto.setPlatformName(item.getPlatformName());
            dto.setPlatformCode(item.getPlatformCode());
            dto.setSortOrder(item.getSortOrder());
            dto.setStatus(item.getStatus());
            dto.setRemark(item.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("平台数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), PlatformExcelDTO.class)
                    .sheet("平台数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("平台导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            PlatformExcelDTO sample = new PlatformExcelDTO();
            sample.setPlatformName("");
            sample.setPlatformCode("");
            sample.setSortOrder(0);
            sample.setStatus("active");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), PlatformExcelDTO.class)
                    .sheet("平台数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除平台")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        Platform entity = platformService.getById(id);
        if (entity == null) {
            throw new BusinessException("平台不存在");
        }
        platformService.removeById(id);
        return Result.success();
    }
}