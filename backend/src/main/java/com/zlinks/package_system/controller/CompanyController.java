package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.CompanyExcelDTO;
import com.zlinks.package_system.dto.CompanyRequest;
import com.zlinks.package_system.entity.Company;
import com.zlinks.package_system.service.CompanyService;
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

@Tag(name = "公司管理")
@RestController
@RequestMapping("/api/companies")
@RequiredArgsConstructor
public class CompanyController {

    private final CompanyService companyService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取公司列表")
    @GetMapping
    public Result<PageResult<Company>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String companyName) {

        LambdaQueryWrapper<Company> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(companyName), Company::getCompanyName, companyName);
        wrapper.orderByDesc(Company::getCreateTime);

        Page<Company> page = companyService.page(new Page<>(current, size), wrapper);
        PageResult<Company> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取公司详情")
    @GetMapping("/{id}")
    public Result<Company> getById(@PathVariable Long id) {
        Company company = companyService.getById(id);
        if (company == null) {
            throw new BusinessException("公司不存在");
        }
        return Result.success(company);
    }

    @Operation(summary = "创建公司")
    @PostMapping
    public Result<Company> create(@Valid @RequestBody CompanyRequest request) {
        Company company = new Company();
        company.setCompanyName(request.getCompanyName());
        company.setPlatform(request.getPlatform());
        company.setAccount(request.getAccount());
        company.setPassword(request.getPassword());
        company.setRemark(request.getRemark());

        companyService.save(company);
        return Result.success(company);
    }

    @Operation(summary = "更新公司")
    @PutMapping("/{id}")
    public Result<Company> update(@PathVariable Long id, @Valid @RequestBody CompanyRequest request) {
        Company company = companyService.getById(id);
        if (company == null) {
            throw new BusinessException("公司不存在");
        }

        company.setCompanyName(request.getCompanyName());
        company.setPlatform(request.getPlatform());
        company.setAccount(request.getAccount());
        company.setPassword(request.getPassword());
        company.setRemark(request.getRemark());

        companyService.updateById(company);
        return Result.success(company);
    }

    @Operation(summary = "导入公司数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<CompanyExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(CompanyExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<Company> companies = new ArrayList<>();
        for (CompanyExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getCompanyName())) {
                continue;
            }
            Company company = new Company();
            company.setCompanyName(dto.getCompanyName());
            company.setPlatform(dto.getPlatform());
            company.setAccount(dto.getAccount());
            company.setPassword(dto.getPassword());
            company.setRemark(dto.getRemark());
            companies.add(company);
        }

        if (companies.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        companyService.saveBatch(companies);
        return Result.success("成功导入 " + companies.size() + " 条数据");
    }

    @Operation(summary = "导出公司数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Company> companies = companyService.list(new LambdaQueryWrapper<Company>()
                .orderByDesc(Company::getCreateTime));

        List<CompanyExcelDTO> exportList = new ArrayList<>();
        for (Company company : companies) {
            CompanyExcelDTO dto = new CompanyExcelDTO();
            dto.setCompanyName(company.getCompanyName());
            dto.setPlatform(company.getPlatform());
            dto.setAccount(company.getAccount());
            dto.setPassword(company.getPassword());
            dto.setRemark(company.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("公司数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), CompanyExcelDTO.class)
                    .sheet("公司数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("公司导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            CompanyExcelDTO sample = new CompanyExcelDTO();
            sample.setCompanyName("");
            sample.setPlatform("");
            sample.setAccount("");
            sample.setPassword("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), CompanyExcelDTO.class)
                    .sheet("公司数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除公司")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        Company company = companyService.getById(id);
        if (company == null) {
            throw new BusinessException("公司不存在");
        }
        companyService.removeById(id);
        return Result.success();
    }
}
