package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.SignFileExcelDTO;
import com.zlinks.package_system.dto.SignFileRequest;
import com.zlinks.package_system.entity.Company;
import com.zlinks.package_system.entity.SignFile;
import com.zlinks.package_system.service.CompanyService;
import com.zlinks.package_system.service.SignFileService;
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
import java.util.Map;
import java.util.stream.Collectors;

@Tag(name = "签名文件管理")
@RestController
@RequestMapping("/api/sign-files")
@RequiredArgsConstructor
public class SignFileController {

    private final SignFileService signFileService;
    private final CompanyService companyService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取签名文件列表")
    @GetMapping
    public Result<PageResult<SignFile>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) Long companyId) {

        LambdaQueryWrapper<SignFile> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(companyId != null, SignFile::getCompanyId, companyId);
        wrapper.orderByDesc(SignFile::getCreateTime);

        Page<SignFile> page = signFileService.page(new Page<>(current, size), wrapper);

        Map<Long, String> companyNameMap = buildCompanyNameMap(page.getRecords());
        page.getRecords().forEach(sf -> sf.setCompanyName(companyNameMap.get(sf.getCompanyId())));

        PageResult<SignFile> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取签名文件详情")
    @GetMapping("/{id}")
    public Result<SignFile> getById(@PathVariable Long id) {
        SignFile sf = signFileService.getById(id);
        if (sf == null) {
            throw new BusinessException("签名文件不存在");
        }

        Map<Long, String> companyNameMap = buildCompanyNameMap(List.of(sf));
        sf.setCompanyName(companyNameMap.get(sf.getCompanyId()));
        return Result.success(sf);
    }

    @Operation(summary = "创建签名文件")
    @PostMapping
    public Result<SignFile> create(@Valid @RequestBody SignFileRequest request) {
        SignFile sf = new SignFile();
        sf.setCompanyId(request.getCompanyId());
        sf.setStoreFile(request.getStoreFile());
        sf.setStorePassword(request.getStorePassword());
        sf.setKeyAlias(request.getKeyAlias());
        sf.setRemark(request.getRemark());

        signFileService.save(sf);
        return Result.success(sf);
    }

    @Operation(summary = "更新签名文件")
    @PutMapping("/{id}")
    public Result<SignFile> update(@PathVariable Long id, @Valid @RequestBody SignFileRequest request) {
        SignFile sf = signFileService.getById(id);
        if (sf == null) {
            throw new BusinessException("签名文件不存在");
        }

        sf.setCompanyId(request.getCompanyId());
        sf.setStoreFile(request.getStoreFile());
        sf.setStorePassword(request.getStorePassword());
        sf.setKeyAlias(request.getKeyAlias());
        sf.setRemark(request.getRemark());

        signFileService.updateById(sf);
        return Result.success(sf);
    }

    @Operation(summary = "导入签名文件数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<SignFileExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(SignFileExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        Map<String, Long> companyNameMap = companyService.list().stream()
                .collect(Collectors.toMap(Company::getCompanyName, Company::getId));

        List<SignFile> list = new ArrayList<>();
        for (SignFileExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getCompanyName())) {
                continue;
            }
            Long companyId = companyNameMap.get(dto.getCompanyName());
            if (companyId == null) {
                continue;
            }
            SignFile sf = new SignFile();
            sf.setCompanyId(companyId);
            sf.setStoreFile(dto.getStoreFile());
            sf.setStorePassword(dto.getStorePassword());
            sf.setKeyAlias(dto.getKeyAlias());
            sf.setRemark(dto.getRemark());
            list.add(sf);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        signFileService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出签名文件数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<SignFile> list = signFileService.list(new LambdaQueryWrapper<SignFile>()
                .orderByDesc(SignFile::getCreateTime));

        Map<Long, String> companyNameMap = buildCompanyNameMap(list);

        List<SignFileExcelDTO> exportList = new ArrayList<>();
        for (SignFile sf : list) {
            SignFileExcelDTO dto = new SignFileExcelDTO();
            dto.setCompanyName(companyNameMap.get(sf.getCompanyId()));
            dto.setStoreFile(sf.getStoreFile());
            dto.setStorePassword(sf.getStorePassword());
            dto.setKeyAlias(sf.getKeyAlias());
            dto.setRemark(sf.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("签名文件数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), SignFileExcelDTO.class)
                    .sheet("签名文件数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("签名文件导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            SignFileExcelDTO sample = new SignFileExcelDTO();
            sample.setCompanyName("");
            sample.setStoreFile("");
            sample.setStorePassword("");
            sample.setKeyAlias("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), SignFileExcelDTO.class)
                    .sheet("签名文件数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除签名文件")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        SignFile sf = signFileService.getById(id);
        if (sf == null) {
            throw new BusinessException("签名文件不存在");
        }
        signFileService.removeById(id);
        return Result.success();
    }

    @Operation(summary = "获取公司列表")
    @GetMapping("/companies")
    public Result<List<Company>> getCompanies() {
        List<Company> companies = companyService.list();
        return Result.success(companies);
    }

    private Map<Long, String> buildCompanyNameMap(List<SignFile> signFiles) {
        List<Long> companyIds = signFiles.stream()
                .map(SignFile::getCompanyId)
                .filter(id -> id != null)
                .distinct()
                .collect(Collectors.toList());

        if (companyIds.isEmpty()) {
            return Map.of();
        }

        return companyService.listByIds(companyIds).stream()
                .collect(Collectors.toMap(Company::getId, Company::getCompanyName));
    }
}
