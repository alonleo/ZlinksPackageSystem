package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.dto.GameExcelDTO;
import com.zlinks.package_system.entity.Game;
import com.zlinks.package_system.service.GameService;
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

@Tag(name = "游戏管理")
@RestController
@RequestMapping("/api/games")
@RequiredArgsConstructor
public class GameController {

    private final GameService gameService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取游戏统计")
    @GetMapping("/counts")
    public Result<CountResult> getCounts() {
        return Result.success(gameService.getCounts());
    }

    @Operation(summary = "获取游戏列表")
    @GetMapping
    public Result<PageResult<Game>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String gameName,
            @RequestParam(required = false) String gameDirection,
            @RequestParam(required = false) String source,
            @RequestParam(required = false) String manager,
            @RequestParam(required = false) String status,
            @RequestParam(required = false) Integer priority) {

        LambdaQueryWrapper<Game> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(gameName), Game::getGameName, gameName);
        wrapper.eq(StringUtils.hasText(gameDirection), Game::getGameDirection, gameDirection);
        wrapper.like(StringUtils.hasText(source), Game::getSource, source);
        wrapper.like(StringUtils.hasText(manager), Game::getManager, manager);
        wrapper.eq(StringUtils.hasText(status), Game::getStatus, status);
        wrapper.eq(priority != null, Game::getPriority, priority);
        wrapper.orderByDesc(Game::getCreateTime);

        Page<Game> page = gameService.page(new Page<>(current, size), wrapper);
        PageResult<Game> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());

        return Result.success(pageResult);
    }

    @Operation(summary = "获取筛选项")
    @GetMapping("/options")
    public Result<java.util.Map<String, java.util.List<String>>> getOptions() {
        java.util.List<Game> all = gameService.list();
        java.util.Map<String, java.util.List<String>> options = new java.util.LinkedHashMap<>();
        options.put("gameDirections", all.stream().map(Game::getGameDirection).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(java.util.stream.Collectors.toList()));
        options.put("sources", all.stream().map(Game::getSource).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(java.util.stream.Collectors.toList()));
        options.put("managers", all.stream().map(Game::getManager).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(java.util.stream.Collectors.toList()));
        options.put("statuses", all.stream().map(Game::getStatus).filter(s -> s != null && !s.isEmpty()).distinct().sorted().collect(java.util.stream.Collectors.toList()));
        return Result.success(options);
    }

    @Operation(summary = "获取游戏详情")
    @GetMapping("/{id}")
    public Result<Game> getById(@PathVariable Long id) {
        Game game = gameService.getById(id);
        return Result.success(game);
    }

    @Operation(summary = "创建游戏")
    @PostMapping
    public Result<Game> create(@Valid @RequestBody Game game) {
        gameService.save(game);
        return Result.success(game);
    }

    @Operation(summary = "更新游戏")
    @PutMapping("/{id}")
    public Result<Game> update(@PathVariable Long id, @Valid @RequestBody Game game) {
        game.setId(id);
        gameService.updateById(game);
        return Result.success(game);
    }

    @Operation(summary = "导入游戏数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<GameExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(GameExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<Game> list = new ArrayList<>();
        for (GameExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getGameName())) {
                continue;
            }
            Game game = new Game();
            game.setGameName(dto.getGameName());
            game.setGameDirection(dto.getGameDirection());
            game.setSource(dto.getSource());
            game.setGitUrl(dto.getGitUrl());
            game.setPriority(dto.getPriority());
            game.setTags(dto.getTags());
            game.setProjectType(dto.getProjectType());
            game.setManager(dto.getManager());
            game.setWhiteBranch(dto.getWhiteBranch());
            game.setStatus(StringUtils.hasText(dto.getStatus()) ? dto.getStatus() : "active");
            game.setAndroidFolderName(dto.getAndroidFolderName());
            game.setRemark(dto.getRemark());
            list.add(game);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        gameService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出游戏数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Game> list = gameService.list(new LambdaQueryWrapper<Game>()
                .orderByDesc(Game::getCreateTime));

        List<GameExcelDTO> exportList = new ArrayList<>();
        for (Game game : list) {
            GameExcelDTO dto = new GameExcelDTO();
            dto.setGameName(game.getGameName());
            dto.setGameDirection(game.getGameDirection());
            dto.setSource(game.getSource());
            dto.setGitUrl(game.getGitUrl());
            dto.setPriority(game.getPriority());
            dto.setTags(game.getTags());
            dto.setProjectType(game.getProjectType());
            dto.setManager(game.getManager());
            dto.setWhiteBranch(game.getWhiteBranch());
            dto.setStatus(game.getStatus());
            dto.setAndroidFolderName(game.getAndroidFolderName());
            dto.setRemark(game.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("游戏数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), GameExcelDTO.class)
                    .sheet("游戏数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("游戏导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            GameExcelDTO sample = new GameExcelDTO();
            sample.setGameName("");
            sample.setGameDirection("");
            sample.setSource("");
            sample.setGitUrl("");
            sample.setPriority(null);
            sample.setTags("");
            sample.setProjectType("");
            sample.setManager("");
            sample.setWhiteBranch("");
            sample.setStatus("");
            sample.setAndroidFolderName("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), GameExcelDTO.class)
                    .sheet("游戏数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除游戏")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        gameService.removeById(id);
        return Result.success();
    }
}