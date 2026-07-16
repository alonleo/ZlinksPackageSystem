package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.dto.HonorParamExcelDTO;
import com.zlinks.package_system.dto.HonorParamRequest;
import com.zlinks.package_system.entity.HonorParam;
import com.zlinks.package_system.service.IHonorParamService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.BeanUtils;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@Tag(name = "荣耀参数")
@RestController
@RequestMapping("/api/honor-params")
@RequiredArgsConstructor
public class HonorParamController {
    private final IHonorParamService service;

    @Operation(summary = "统计") @GetMapping("/counts")
    public Result<Map<String, Long>> counts() {
        long total = service.count();
        long active = service.count(new LambdaQueryWrapper<HonorParam>().eq(HonorParam::getAdParamStatus, "active"));
        Map<String, Long> map = new LinkedHashMap<>(); map.put("total", total); map.put("active", active);
        return Result.success(map);
    }

    @Operation(summary = "分页") @GetMapping
    public Result<PageResult<HonorParam>> page(@RequestParam(defaultValue = "1") long current, @RequestParam(defaultValue = "10") long size,
            @RequestParam(required = false) Long productId, @RequestParam(required = false) String adParamStatus, @RequestParam(required = false) String listStatus) {
        LambdaQueryWrapper<HonorParam> w = new LambdaQueryWrapper<>();
        if (productId != null) w.eq(HonorParam::getProductId, productId);
        if (adParamStatus != null && !adParamStatus.isEmpty()) w.eq(HonorParam::getAdParamStatus, adParamStatus);
        if (listStatus != null && !listStatus.isEmpty()) w.eq(HonorParam::getListStatus, listStatus);
        w.orderByDesc(HonorParam::getUpdateTime);
        Page<HonorParam> page = service.page(new Page<>(current, size), w);
        return Result.success(new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent()));
    }

    @Operation(summary = "筛选项") @GetMapping("/options")
    public Result<Map<String,Object>> options() {
        Map<String,Object> m = new LinkedHashMap<>();
        m.put("adParamStatuses", List.of("pending","active","inactive"));
        m.put("listStatuses", List.of("listed","unlisted","paused"));
        return Result.success(m);
    }

    @Operation(summary = "详情") @GetMapping("/{id}")
    public Result<HonorParam> getById(@PathVariable Long id) {
        HonorParam e = service.getById(id);
        if (e == null) throw new BusinessException("荣耀参数不存在");
        return Result.success(e);
    }

    @Operation(summary = "新增") @PostMapping
    public Result<HonorParam> create(@Valid @RequestBody HonorParamRequest req) {
        HonorParam e = new HonorParam(); BeanUtils.copyProperties(req, e); service.save(e);
        return Result.success(e);
    }

    @Operation(summary = "更新") @PutMapping("/{id}")
    public Result<HonorParam> update(@PathVariable Long id, @Valid @RequestBody HonorParamRequest req) {
        HonorParam e = service.getById(id);
        if (e == null) throw new BusinessException("荣耀参数不存在");
        BeanUtils.copyProperties(req, e); e.setId(id); service.updateById(e);
        return Result.success(e);
    }

    @Operation(summary = "删除") @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        if (service.getById(id) == null) throw new BusinessException("荣耀参数不存在");
        service.removeById(id); return Result.success();
    }

    @Operation(summary = "导入") @PostMapping("/import")
    public Result<String> importFile(MultipartFile file) {
        if (file == null || file.isEmpty()) throw new BusinessException("文件不能为空");
        try {
            List<HonorParamExcelDTO> list = com.alibaba.excel.EasyExcel.read(file.getInputStream()).head(HonorParamExcelDTO.class).sheet().doReadSync();
            if (list.isEmpty()) throw new BusinessException("导入数据为空");
            int c = 0;
            for (var d : list) { HonorParam e = new HonorParam(); BeanUtils.copyProperties(d, e); service.save(e); c++; }
            return Result.success("导入成功 " + c + " 条");
        } catch (IOException ex) { throw new BusinessException("导入失败: " + ex.getMessage()); }
    }

    @Operation(summary = "导出") @GetMapping("/export")
    public void export(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse resp) throws IOException {
        var all = service.list(); var rows = new ArrayList<HonorParamExcelDTO>();
        for (var e : all) { var d = new HonorParamExcelDTO(); BeanUtils.copyProperties(e, d); rows.add(d); }
        resp.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        resp.setCharacterEncoding("utf-8");
        resp.setHeader("Content-disposition", "attachment;filename*=utf-8''" + URLEncoder.encode("honor-params.xlsx", StandardCharsets.UTF_8).replace("+", "%20"));
        com.alibaba.excel.EasyExcel.write(resp.getOutputStream(), HonorParamExcelDTO.class).sheet("Honor").doWrite(rows);
    }

    @Operation(summary = "模板") @GetMapping("/template")
    public void template(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse resp) throws IOException {
        resp.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        resp.setCharacterEncoding("utf-8");
        resp.setHeader("Content-disposition", "attachment;filename*=utf-8''" + URLEncoder.encode("honor-params-template.xlsx", StandardCharsets.UTF_8).replace("+", "%20"));
        com.alibaba.excel.EasyExcel.write(resp.getOutputStream(), HonorParamExcelDTO.class).sheet("Honor").doWrite(new ArrayList<>());
    }
}
