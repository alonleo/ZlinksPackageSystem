package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.Game;
import com.zlinks.package_system.service.GameService;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
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

import jakarta.validation.Valid;

@Tag(name = "游戏管理")
@RestController
@RequestMapping("/api/games")
@RequiredArgsConstructor
public class GameController {

    private final GameService gameService;

    @Operation(summary = "获取游戏列表")
    @GetMapping
    public Result<PageResult<Game>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String gameName,
            @RequestParam(required = false) String status) {

        LambdaQueryWrapper<Game> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(gameName), Game::getGameName, gameName);
        wrapper.eq(StringUtils.hasText(status), Game::getStatus, status);
        wrapper.orderByDesc(Game::getCreateTime);

        Page<Game> page = gameService.page(new Page<>(current, size), wrapper);
        PageResult<Game> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());

        return Result.success(pageResult);
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

    @Operation(summary = "删除游戏")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        gameService.removeById(id);
        return Result.success();
    }
}