package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.dto.ProductExcelDTO;
import com.zlinks.package_system.dto.ProductRequest;
import com.zlinks.package_system.entity.Company;
import com.zlinks.package_system.entity.Copyright;
import com.zlinks.package_system.entity.Game;
import com.zlinks.package_system.entity.Platform;
import com.zlinks.package_system.entity.Product;
import com.zlinks.package_system.service.CompanyService;
import com.zlinks.package_system.service.CopyrightService;
import com.zlinks.package_system.service.GameService;
import com.zlinks.package_system.service.PlatformService;
import com.zlinks.package_system.service.ProductService;
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
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Tag(name = "产品管理")
@RestController
@RequestMapping("/api/products")
@RequiredArgsConstructor
public class ProductController {

    private final ProductService productService;
    private final CopyrightService copyrightService;
    private final GameService gameService;
    private final CompanyService companyService;
    private final PlatformService platformService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取产品统计")
    @GetMapping("/counts")
    public Result<CountResult> getCounts() {
        return Result.success(productService.getCounts());
    }

    @Operation(summary = "获取产品列表")
    @GetMapping
    public Result<PageResult<Product>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) Long copyrightId,
            @RequestParam(required = false) Long gameId,
            @RequestParam(required = false) Long companyId,
            @RequestParam(required = false) Long platformId,
            @RequestParam(required = false) String batch,
            @RequestParam(required = false) String status) {

        LambdaQueryWrapper<Product> wrapper = new LambdaQueryWrapper<>();
        wrapper.eq(copyrightId != null, Product::getCopyrightId, copyrightId);
        wrapper.eq(gameId != null, Product::getGameId, gameId);
        wrapper.eq(companyId != null, Product::getCompanyId, companyId);
        wrapper.eq(platformId != null, Product::getPlatformId, platformId);
        wrapper.eq(StringUtils.hasText(batch), Product::getBatch, batch);
        wrapper.eq(StringUtils.hasText(status), Product::getStatus, status);
        wrapper.orderByDesc(Product::getCreateTime);

        Page<Product> page = productService.page(new Page<>(current, size), wrapper);

        Map<Long, String> copyrightNameMap = buildCopyrightNameMap(page.getRecords());
        Map<Long, String> gameNameMap = buildGameNameMap(page.getRecords());
        Map<Long, String> companyNameMap = buildCompanyNameMap(page.getRecords());
        Map<Long, String> platformNameMap = buildPlatformNameMap(page.getRecords());

        page.getRecords().forEach(p -> {
            p.setCopyrightName(copyrightNameMap.get(p.getCopyrightId()));
            p.setGameName(gameNameMap.get(p.getGameId()));
            p.setCompanyName(companyNameMap.get(p.getCompanyId()));
            p.setPlatformName(platformNameMap.get(p.getPlatformId()));
        });

        PageResult<Product> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "全部产品（用于下拉）")
    @GetMapping("/all")
    public Result<List<Product>> all() {
        return Result.success(productService.list(new LambdaQueryWrapper<Product>().orderByAsc(Product::getId)));
    }

    @Operation(summary = "获取筛选项")
    @GetMapping("/options")
    public Result<Map<String, List<?>>> getOptions() {
        List<Product> all = productService.list();
        Map<String, List<?>> options = new LinkedHashMap<>();
        List<Map<String, Object>> platformList = platformService.list().stream()
                .map(p -> {
                    Map<String, Object> m = new LinkedHashMap<>();
                    m.put("id", p.getId());
                    m.put("name", p.getPlatformName());
                    return m;
                }).collect(Collectors.toList());
        options.put("platforms", platformList);
        options.put("batches", all.stream().map(Product::getBatch).filter(s -> StringUtils.hasText(s)).distinct().sorted().collect(Collectors.toList()));
        options.put("statuses", all.stream().map(Product::getStatus).filter(s -> StringUtils.hasText(s)).distinct().sorted().collect(Collectors.toList()));
        return Result.success(options);
    }

    @Operation(summary = "获取产品详情")
    @GetMapping("/{id}")
    public Result<Product> getById(@PathVariable Long id) {
        Product product = productService.getById(id);
        if (product == null) {
            throw new BusinessException("产品不存在");
        }
        fillNames(List.of(product));
        return Result.success(product);
    }

    @Operation(summary = "创建产品")
    @PostMapping
    public Result<Product> create(@Valid @RequestBody ProductRequest request) {
        Product product = new Product();
        product.setCopyrightId(request.getCopyrightId());
        product.setGameId(request.getGameId());
        product.setCompanyId(request.getCompanyId());
        product.setPlatformId(request.getPlatformId());
        product.setPackageName(request.getPackageName());
        product.setSdkVersion(request.getSdkVersion());
        product.setApkVersion(request.getApkVersion());
        product.setBatch(request.getBatch());
        product.setPackageMode(request.getPackageMode());
        product.setStatus(request.getStatus() != null ? request.getStatus() : "pending");
        product.setRemark(request.getRemark());

        productService.save(product);
        return Result.success(product);
    }

    @Operation(summary = "更新产品")
    @PutMapping("/{id}")
    public Result<Product> update(@PathVariable Long id, @Valid @RequestBody ProductRequest request) {
        Product product = productService.getById(id);
        if (product == null) {
            throw new BusinessException("产品不存在");
        }

        product.setCopyrightId(request.getCopyrightId());
        product.setGameId(request.getGameId());
        product.setCompanyId(request.getCompanyId());
        product.setPlatformId(request.getPlatformId());
        product.setPackageName(request.getPackageName());
        product.setSdkVersion(request.getSdkVersion());
        product.setApkVersion(request.getApkVersion());
        product.setBatch(request.getBatch());
        product.setPackageMode(request.getPackageMode());
        product.setStatus(request.getStatus());
        product.setRemark(request.getRemark());

        productService.updateById(product);
        return Result.success(product);
    }

    @Operation(summary = "导入产品数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<ProductExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(ProductExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        Map<String, Long> copyrightNameMap = copyrightService.list().stream()
                .collect(Collectors.toMap(Copyright::getCopyrightName, Copyright::getId));
        Map<String, Long> gameNameMap = gameService.list().stream()
                .collect(Collectors.toMap(Game::getGameName, Game::getId));
        Map<String, Long> companyNameMap = companyService.list().stream()
                .collect(Collectors.toMap(Company::getCompanyName, Company::getId));
        Map<String, Long> platformNameMap = platformService.list().stream()
                .collect(Collectors.toMap(Platform::getPlatformName, Platform::getId));

        List<Product> list = new ArrayList<>();
        for (ProductExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getCopyrightName())) {
                continue;
            }
            Product p = new Product();
            p.setCopyrightId(copyrightNameMap.get(dto.getCopyrightName()));
            p.setGameId(gameNameMap.get(dto.getGameName()));
            p.setCompanyId(companyNameMap.get(dto.getCompanyName()));
            p.setPlatformId(platformNameMap.get(dto.getPlatformName()));
            p.setPackageName(dto.getPackageName());
            p.setSdkVersion(dto.getSdkVersion());
            p.setApkVersion(dto.getApkVersion());
            p.setBatch(dto.getBatch());
            p.setPackageMode(dto.getPackageMode());
            p.setStatus(StringUtils.hasText(dto.getStatus()) ? dto.getStatus() : "pending");
            p.setRemark(dto.getRemark());
            list.add(p);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        productService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出产品数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Product> list = productService.list(new LambdaQueryWrapper<Product>()
                .orderByDesc(Product::getCreateTime));

        Map<Long, String> copyrightNameMap = buildCopyrightNameMap(list);
        Map<Long, String> gameNameMap = buildGameNameMap(list);
        Map<Long, String> companyNameMap = buildCompanyNameMap(list);
        Map<Long, String> platformNameMap = buildPlatformNameMap(list);

        List<ProductExcelDTO> exportList = new ArrayList<>();
        for (Product p : list) {
            ProductExcelDTO dto = new ProductExcelDTO();
            dto.setCopyrightName(copyrightNameMap.get(p.getCopyrightId()));
            dto.setGameName(gameNameMap.get(p.getGameId()));
            dto.setCompanyName(companyNameMap.get(p.getCompanyId()));
            dto.setPlatformName(platformNameMap.get(p.getPlatformId()));
            dto.setPackageName(p.getPackageName());
            dto.setSdkVersion(p.getSdkVersion());
            dto.setApkVersion(p.getApkVersion());
            dto.setBatch(p.getBatch());
            dto.setPackageMode(p.getPackageMode());
            dto.setStatus(p.getStatus());
            dto.setRemark(p.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("产品数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), ProductExcelDTO.class)
                    .sheet("产品数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("产品导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            ProductExcelDTO sample = new ProductExcelDTO();
            sample.setCopyrightName("");
            sample.setGameName("");
            sample.setCompanyName("");
            sample.setPlatformName("");
            sample.setPackageName("");
            sample.setSdkVersion("");
            sample.setApkVersion("");
            sample.setBatch("");
            sample.setPackageMode("");
            sample.setStatus("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), ProductExcelDTO.class)
                    .sheet("产品数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除产品")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        Product product = productService.getById(id);
        if (product == null) {
            throw new BusinessException("产品不存在");
        }
        productService.removeById(id);
        return Result.success();
    }

    @Operation(summary = "主动打包")
    @PostMapping("/{id}/package")
    public Result<String> triggerPackage(@PathVariable Long id) {
        Product product = productService.getById(id);
        if (product == null) {
            throw new BusinessException("产品不存在");
        }
        return Result.success("打包任务已提交");
    }

    @Operation(summary = "获取软著列表")
    @GetMapping("/copyrights")
    public Result<List<Copyright>> getCopyrights() {
        return Result.success(copyrightService.list());
    }

    @Operation(summary = "获取游戏列表")
    @GetMapping("/games")
    public Result<List<Game>> getGames() {
        return Result.success(gameService.list());
    }

    @Operation(summary = "获取公司列表")
    @GetMapping("/companies")
    public Result<List<Company>> getCompanies() {
        return Result.success(companyService.list());
    }

    @Operation(summary = "获取平台列表")
    @GetMapping("/platforms")
    public Result<List<Platform>> getPlatforms() {
        return Result.success(platformService.list());
    }

    private void fillNames(List<Product> products) {
        Map<Long, String> copyrightNameMap = buildCopyrightNameMap(products);
        Map<Long, String> gameNameMap = buildGameNameMap(products);
        Map<Long, String> companyNameMap = buildCompanyNameMap(products);
        Map<Long, String> platformNameMap = buildPlatformNameMap(products);
        products.forEach(p -> {
            p.setCopyrightName(copyrightNameMap.get(p.getCopyrightId()));
            p.setGameName(gameNameMap.get(p.getGameId()));
            p.setCompanyName(companyNameMap.get(p.getCompanyId()));
            p.setPlatformName(platformNameMap.get(p.getPlatformId()));
        });
    }

    private Map<Long, String> buildCopyrightNameMap(List<Product> products) {
        List<Long> ids = products.stream().map(Product::getCopyrightId).filter(id -> id != null).distinct().collect(Collectors.toList());
        if (ids.isEmpty()) return Map.of();
        return copyrightService.listByIds(ids).stream().collect(Collectors.toMap(Copyright::getId, Copyright::getCopyrightName));
    }

    private Map<Long, String> buildGameNameMap(List<Product> products) {
        List<Long> ids = products.stream().map(Product::getGameId).filter(id -> id != null).distinct().collect(Collectors.toList());
        if (ids.isEmpty()) return Map.of();
        return gameService.listByIds(ids).stream().collect(Collectors.toMap(Game::getId, Game::getGameName));
    }

    private Map<Long, String> buildCompanyNameMap(List<Product> products) {
        List<Long> ids = products.stream().map(Product::getCompanyId).filter(id -> id != null).distinct().collect(Collectors.toList());
        if (ids.isEmpty()) return Map.of();
        return companyService.listByIds(ids).stream().collect(Collectors.toMap(Company::getId, Company::getCompanyName));
    }

    private Map<Long, String> buildPlatformNameMap(List<Product> products) {
        List<Long> ids = products.stream().map(Product::getPlatformId).filter(id -> id != null).distinct().collect(Collectors.toList());
        if (ids.isEmpty()) return Map.of();
        return platformService.listByIds(ids).stream().collect(Collectors.toMap(Platform::getId, Platform::getPlatformName));
    }
}
