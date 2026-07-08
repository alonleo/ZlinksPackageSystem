package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.CopyrightExcelDTO;
import com.zlinks.package_system.dto.CopyrightRequest;
import com.zlinks.package_system.entity.Copyright;
import com.zlinks.package_system.service.CopyrightService;
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

@Tag(name = "软著管理")
@RestController
@RequestMapping("/api/copyrights")
@RequiredArgsConstructor
public class CopyrightController {

    private final CopyrightService copyrightService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取软著列表")
    @GetMapping
    public Result<PageResult<Copyright>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String copyrightName) {

        LambdaQueryWrapper<Copyright> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(copyrightName), Copyright::getCopyrightName, copyrightName);
        wrapper.orderByDesc(Copyright::getCreateTime);

        Page<Copyright> page = copyrightService.page(new Page<>(current, size), wrapper);
        PageResult<Copyright> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取软著详情")
    @GetMapping("/{id}")
    public Result<Copyright> getById(@PathVariable Long id) {
        Copyright copyright = copyrightService.getById(id);
        if (copyright == null) {
            throw new BusinessException("软著不存在");
        }
        return Result.success(copyright);
    }

    @Operation(summary = "创建软著")
    @PostMapping
    public Result<Copyright> create(@Valid @RequestBody CopyrightRequest request) {
        Copyright copyright = new Copyright();
        copyright.setCopyrightName(request.getCopyrightName());
        copyright.setCopyrightOwner(request.getCopyrightOwner());
        copyright.setCopyrightNumber(request.getCopyrightNumber());
        copyright.setRemark(request.getRemark());

        copyrightService.save(copyright);
        return Result.success(copyright);
    }

    @Operation(summary = "更新软著")
    @PutMapping("/{id}")
    public Result<Copyright> update(@PathVariable Long id, @Valid @RequestBody CopyrightRequest request) {
        Copyright copyright = copyrightService.getById(id);
        if (copyright == null) {
            throw new BusinessException("软著不存在");
        }

        copyright.setCopyrightName(request.getCopyrightName());
        copyright.setCopyrightOwner(request.getCopyrightOwner());
        copyright.setCopyrightNumber(request.getCopyrightNumber());
        copyright.setRemark(request.getRemark());

        copyrightService.updateById(copyright);
        return Result.success(copyright);
    }

    @Operation(summary = "导入软著数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<CopyrightExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(CopyrightExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<Copyright> list = new ArrayList<>();
        for (CopyrightExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getCopyrightName())) {
                continue;
            }
            Copyright entity = new Copyright();
            entity.setCopyrightName(dto.getCopyrightName());
            entity.setCopyrightOwner(dto.getCopyrightOwner());
            entity.setCopyrightNumber(dto.getCopyrightNumber());
            entity.setRemark(dto.getRemark());
            list.add(entity);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        copyrightService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出软著数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Copyright> list = copyrightService.list(new LambdaQueryWrapper<Copyright>()
                .orderByDesc(Copyright::getCreateTime));

        List<CopyrightExcelDTO> exportList = new ArrayList<>();
        for (Copyright item : list) {
            CopyrightExcelDTO dto = new CopyrightExcelDTO();
            dto.setCopyrightName(item.getCopyrightName());
            dto.setCopyrightOwner(item.getCopyrightOwner());
            dto.setCopyrightNumber(item.getCopyrightNumber());
            dto.setRemark(item.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("软著数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), CopyrightExcelDTO.class)
                    .sheet("软著数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("软著导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            CopyrightExcelDTO sample = new CopyrightExcelDTO();
            sample.setCopyrightName("");
            sample.setCopyrightOwner("");
            sample.setCopyrightNumber("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), CopyrightExcelDTO.class)
                    .sheet("软著数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除软著")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        Copyright entity = copyrightService.getById(id);
        if (entity == null) {
            throw new BusinessException("软著不存在");
        }
        copyrightService.removeById(id);
        return Result.success();
    }
}
