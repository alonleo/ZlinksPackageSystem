package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.PermissionGroupExcelDTO;
import com.zlinks.package_system.dto.PermissionGroupRequest;
import com.zlinks.package_system.entity.PermissionGroup;
import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.entity.UserGroup;
import com.zlinks.package_system.service.PermissionGroupService;
import com.zlinks.package_system.service.UserGroupService;
import com.zlinks.package_system.service.UserService;
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

@Tag(name = "权限组管理")
@RestController
@RequestMapping("/api/permission-groups")
@RequiredArgsConstructor
public class PermissionGroupController {

    private final PermissionGroupService permissionGroupService;
    private final UserService userService;
    private final UserGroupService userGroupService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取权限组列表")
    @GetMapping
    public Result<PageResult<PermissionGroup>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String groupName) {

        LambdaQueryWrapper<PermissionGroup> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(groupName), PermissionGroup::getGroupName, groupName);
        wrapper.orderByAsc(PermissionGroup::getId);

        Page<PermissionGroup> page = permissionGroupService.page(new Page<>(current, size), wrapper);

        List<Long> groupIds = page.getRecords().stream()
                .map(PermissionGroup::getId)
                .collect(Collectors.toList());
        if (!groupIds.isEmpty()) {
            Map<Long, Long> countMap = userGroupService.list(
                    new LambdaQueryWrapper<UserGroup>().in(UserGroup::getGroupId, groupIds))
                    .stream()
                    .collect(Collectors.groupingBy(UserGroup::getGroupId, Collectors.counting()));
            page.getRecords().forEach(g -> g.setUserCount(countMap.getOrDefault(g.getId(), 0L)));
        }

        PageResult<PermissionGroup> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取权限组详情")
    @GetMapping("/{id}")
    public Result<PermissionGroup> getById(@PathVariable Long id) {
        PermissionGroup group = permissionGroupService.getById(id);
        if (group == null) {
            throw new BusinessException("权限组不存在");
        }
        long userCount = userGroupService.count(
                new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getGroupId, id));
        group.setUserCount(userCount);
        return Result.success(group);
    }

    @Operation(summary = "获取权限组关联用户")
    @GetMapping("/{id}/users")
    public Result<List<User>> getUsers(@PathVariable Long id) {
        List<Long> userIds = userGroupService.list(
                new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getGroupId, id))
                .stream().map(UserGroup::getUserId).collect(Collectors.toList());
        if (userIds.isEmpty()) {
            return Result.success(List.of());
        }
        List<User> users = userService.listByIds(userIds);
        return Result.success(users);
    }

    @Operation(summary = "获取可分配用户")
    @GetMapping("/{id}/available-users")
    public Result<List<User>> getAvailableUsers(@PathVariable Long id) {
        List<Long> assignedUserIds = userGroupService.list(
                new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getGroupId, id))
                .stream().map(UserGroup::getUserId).collect(Collectors.toList());

        List<User> allUsers = userService.list();
        List<User> available = allUsers.stream()
                .filter(u -> !assignedUserIds.contains(u.getId()))
                .collect(Collectors.toList());
        return Result.success(available);
    }

    @Operation(summary = "添加用户到权限组")
    @PostMapping("/{id}/users/{userId}")
    public Result<Void> addUser(@PathVariable Long id, @PathVariable Long userId) {
        User user = userService.getById(userId);
        if (user == null) {
            throw new BusinessException("用户不存在");
        }
        long exists = userGroupService.count(
                new LambdaQueryWrapper<UserGroup>()
                        .eq(UserGroup::getUserId, userId)
                        .eq(UserGroup::getGroupId, id));
        if (exists == 0) {
            UserGroup ug = new UserGroup();
            ug.setUserId(userId);
            ug.setGroupId(id);
            userGroupService.save(ug);
        }
        return Result.success();
    }

    @Operation(summary = "从权限组移除用户")
    @DeleteMapping("/{id}/users/{userId}")
    public Result<Void> removeUser(@PathVariable Long id, @PathVariable Long userId) {
        userGroupService.remove(
                new LambdaQueryWrapper<UserGroup>()
                        .eq(UserGroup::getUserId, userId)
                        .eq(UserGroup::getGroupId, id));
        return Result.success();
    }

    @Operation(summary = "创建权限组")
    @PostMapping
    public Result<PermissionGroup> create(@Valid @RequestBody PermissionGroupRequest request) {
        PermissionGroup group = new PermissionGroup();
        group.setGroupName(request.getGroupName());
        group.setRemark(request.getRemark());

        permissionGroupService.save(group);
        return Result.success(group);
    }

    @Operation(summary = "更新权限组")
    @PutMapping("/{id}")
    public Result<PermissionGroup> update(@PathVariable Long id, @Valid @RequestBody PermissionGroupRequest request) {
        PermissionGroup group = permissionGroupService.getById(id);
        if (group == null) {
            throw new BusinessException("权限组不存在");
        }

        group.setGroupName(request.getGroupName());
        group.setRemark(request.getRemark());

        permissionGroupService.updateById(group);
        return Result.success(group);
    }

    @Operation(summary = "导入权限组数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<PermissionGroupExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(PermissionGroupExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<PermissionGroup> list = new ArrayList<>();
        for (PermissionGroupExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getGroupName())) {
                continue;
            }
            PermissionGroup group = new PermissionGroup();
            group.setGroupName(dto.getGroupName());
            group.setGroupAccounts(dto.getGroupAccounts());
            group.setRemark(dto.getRemark());
            list.add(group);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        permissionGroupService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出权限组数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<PermissionGroup> list = permissionGroupService.list(new LambdaQueryWrapper<PermissionGroup>()
                .orderByAsc(PermissionGroup::getId));

        List<PermissionGroupExcelDTO> exportList = new ArrayList<>();
        for (PermissionGroup group : list) {
            PermissionGroupExcelDTO dto = new PermissionGroupExcelDTO();
            dto.setGroupName(group.getGroupName());
            dto.setGroupAccounts(group.getGroupAccounts());
            dto.setRemark(group.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("权限组数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), PermissionGroupExcelDTO.class)
                    .sheet("权限组数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("权限组导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            PermissionGroupExcelDTO sample = new PermissionGroupExcelDTO();
            sample.setGroupName("");
            sample.setGroupAccounts("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), PermissionGroupExcelDTO.class)
                    .sheet("权限组数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除权限组")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        PermissionGroup group = permissionGroupService.getById(id);
        if (group == null) {
            throw new BusinessException("权限组不存在");
        }
        userGroupService.remove(new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getGroupId, id));
        permissionGroupService.removeById(id);
        return Result.success();
    }
}
