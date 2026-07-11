package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.entity.system.SysPost;
import com.zlinks.package_system.enums.BusinessType;
import com.zlinks.package_system.service.system.ISysPostService;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

/**
 * 岗位管理 Controller
 */
@RestController
@RequestMapping("/api/system/post")
@RequiredArgsConstructor
public class SysPostController extends BaseController {

    private final ISysPostService postService;

    @PreAuthorize("@ss.hasPermi('system:post:list')")
    @GetMapping("/list")
    public Result<Map<String, Object>> list(SysPost query) {
        Page<SysPost> page = startPage();
        return Result.success(getDataTable(postService.selectPostPage(page, query)));
    }

    @PreAuthorize("@ss.hasPermi('system:post:query')")
    @GetMapping(value = {"/", "/{postId}"})
    public Result<SysPost> getInfo(@PathVariable(required = false) Long postId) {
        return Result.success(postService.getById(postId));
    }

    @PreAuthorize("@ss.hasPermi('system:post:add')")
    @Log(title = "岗位管理", businessType = BusinessType.ADD)
    @PostMapping
    public Result<Void> add(@RequestBody SysPost post) {
        if (postService.insertPost(post)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:post:edit')")
    @Log(title = "岗位管理", businessType = BusinessType.EDIT)
    @PutMapping
    public Result<Void> edit(@RequestBody SysPost post) {
        if (postService.updatePost(post)) return Result.success();
        return Result.error("操作失败");
    }

    @PreAuthorize("@ss.hasPermi('system:post:remove')")
    @Log(title = "岗位管理", businessType = BusinessType.REMOVE)
    @DeleteMapping("/{postIds}")
    public Result<Void> remove(@PathVariable Long[] postIds) {
        for (Long id : postIds) postService.removeById(id);
        return Result.success();
    }

    @GetMapping("/optionselect")
    public Result<List<SysPost>> optionselect() {
        return Result.success(postService.list());
    }
}