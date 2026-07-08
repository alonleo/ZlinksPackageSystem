package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.UserExcelDTO;
import com.zlinks.package_system.dto.UserRequest;
import com.zlinks.package_system.dto.UserVO;
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
import org.springframework.security.crypto.password.PasswordEncoder;
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
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Tag(name = "用户管理")
@RestController
@RequestMapping("/api/users")
@RequiredArgsConstructor
public class UserController {

    private final UserService userService;
    private final UserGroupService userGroupService;
    private final PermissionGroupService permissionGroupService;
    private final PasswordEncoder passwordEncoder;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取用户列表")
    @GetMapping
    public Result<PageResult<UserVO>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String username,
            @RequestParam(required = false) String realName,
            @RequestParam(required = false) String status,
            @RequestParam(required = false) Long groupId) {

        LambdaQueryWrapper<User> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(username), User::getUsername, username);
        wrapper.like(StringUtils.hasText(realName), User::getRealName, realName);
        wrapper.eq(StringUtils.hasText(status), User::getStatus, status);

        if (groupId != null) {
            List<Long> userIds = userGroupService.list(
                    new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getGroupId, groupId))
                    .stream().map(UserGroup::getUserId).collect(Collectors.toList());
            if (userIds.isEmpty()) {
                PageResult<UserVO> empty = new PageResult<>(List.of(), 0L, size.longValue(), current.longValue());
                return Result.success(empty);
            }
            wrapper.in(User::getId, userIds);
        }

        wrapper.orderByDesc(User::getCreateTime);

        Page<User> page = userService.page(new Page<>(current, size), wrapper);

        Map<Long, List<Long>> userGroupMap = buildUserGroupMap(page.getRecords());
        Map<Long, String> groupNameMap = buildGroupNameMap();

        List<UserVO> voList = page.getRecords().stream()
                .map(user -> convertToVO(user, userGroupMap, groupNameMap))
                .collect(Collectors.toList());

        PageResult<UserVO> pageResult = new PageResult<>(voList, page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取用户详情")
    @GetMapping("/{id}")
    public Result<UserVO> getById(@PathVariable Long id) {
        User user = userService.getById(id);
        if (user == null) {
            throw new BusinessException("用户不存在");
        }

        Map<Long, List<Long>> userGroupMap = buildUserGroupMap(List.of(user));
        Map<Long, String> groupNameMap = buildGroupNameMap();
        return Result.success(convertToVO(user, userGroupMap, groupNameMap));
    }

    @Operation(summary = "创建用户")
    @PostMapping
    public Result<UserVO> create(@Valid @RequestBody UserRequest request) {
        if (!StringUtils.hasText(request.getPassword())) {
            throw new BusinessException("密码不能为空");
        }
        if (userService.existsByUsername(request.getUsername())) {
            throw new BusinessException("用户名已存在");
        }

        User user = new User();
        user.setUsername(request.getUsername());
        user.setPassword(passwordEncoder.encode(request.getPassword()));
        user.setRealName(request.getRealName());
        user.setStatus(StringUtils.hasText(request.getStatus()) ? request.getStatus() : "active");
        user.setRemark(request.getRemark());

        userService.save(user);

        updateUserGroups(user.getId(), request.getGroupIds());

        Map<Long, List<Long>> userGroupMap = buildUserGroupMap(List.of(user));
        Map<Long, String> groupNameMap = buildGroupNameMap();
        return Result.success(convertToVO(user, userGroupMap, groupNameMap));
    }

    @Operation(summary = "更新用户")
    @PutMapping("/{id}")
    public Result<UserVO> update(@PathVariable Long id, @Valid @RequestBody UserRequest request) {
        User user = userService.getById(id);
        if (user == null) {
            throw new BusinessException("用户不存在");
        }

        if (!user.getUsername().equals(request.getUsername())
                && userService.existsByUsername(request.getUsername())) {
            throw new BusinessException("用户名已存在");
        }

        user.setUsername(request.getUsername());
        if (StringUtils.hasText(request.getPassword())) {
            user.setPassword(passwordEncoder.encode(request.getPassword()));
        }
        user.setRealName(request.getRealName());
        user.setStatus(request.getStatus());
        user.setRemark(request.getRemark());

        userService.updateById(user);

        updateUserGroups(user.getId(), request.getGroupIds());

        Map<Long, List<Long>> userGroupMap = buildUserGroupMap(List.of(user));
        Map<Long, String> groupNameMap = buildGroupNameMap();
        return Result.success(convertToVO(user, userGroupMap, groupNameMap));
    }

    @Operation(summary = "删除用户")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        User user = userService.getById(id);
        if (user == null) {
            throw new BusinessException("用户不存在");
        }
        userGroupService.remove(new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getUserId, id));
        userService.removeById(id);
        return Result.success();
    }

    @Operation(summary = "导入用户数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<UserExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(UserExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        Map<String, Long> groupNameMap = permissionGroupService.list().stream()
                .collect(Collectors.toMap(PermissionGroup::getGroupName, PermissionGroup::getId));

        List<User> users = new ArrayList<>();
        List<List<Long>> groupsForUsers = new ArrayList<>();

        for (UserExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getUsername())) {
                continue;
            }

            User user = new User();
            user.setUsername(dto.getUsername());
            user.setPassword(passwordEncoder.encode(dto.getPassword()));
            user.setRealName(dto.getRealName());
            user.setStatus(StringUtils.hasText(dto.getStatus()) ? dto.getStatus() : "active");
            user.setRemark(dto.getRemark());
            users.add(user);

            List<Long> gids = new ArrayList<>();
            if (StringUtils.hasText(dto.getGroupNames())) {
                for (String name : dto.getGroupNames().split(",")) {
                    Long gid = groupNameMap.get(name.trim());
                    if (gid != null) {
                        gids.add(gid);
                    }
                }
            }
            groupsForUsers.add(gids);
        }

        if (users.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        userService.saveBatch(users);

        for (int i = 0; i < users.size(); i++) {
            if (!groupsForUsers.get(i).isEmpty()) {
                updateUserGroups(users.get(i).getId(), groupsForUsers.get(i));
            }
        }

        return Result.success("成功导入 " + users.size() + " 条数据");
    }

    @Operation(summary = "导出用户数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<User> users = userService.list(new LambdaQueryWrapper<User>()
                .orderByDesc(User::getCreateTime));

        Map<Long, List<Long>> userGroupMap = buildUserGroupMap(users);
        Map<Long, String> groupNameMap = buildGroupNameMap();

        List<UserExcelDTO> exportList = new ArrayList<>();
        for (User user : users) {
            UserExcelDTO dto = new UserExcelDTO();
            dto.setUsername(user.getUsername());
            dto.setPassword(user.getPassword());
            dto.setRealName(user.getRealName());
            dto.setStatus(user.getStatus());

            List<Long> gids = userGroupMap.getOrDefault(user.getId(), List.of());
            String groupNames = gids.stream()
                    .map(groupNameMap::get)
                    .filter(name -> name != null)
                    .collect(Collectors.joining(","));
            dto.setGroupNames(groupNames);
            dto.setRemark(user.getRemark());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("用户数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), UserExcelDTO.class)
                    .sheet("用户数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("用户导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            UserExcelDTO sample = new UserExcelDTO();
            sample.setUsername("");
            sample.setPassword("");
            sample.setRealName("");
            sample.setStatus("");
            sample.setGroupNames("");
            sample.setRemark("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), UserExcelDTO.class)
                    .sheet("用户数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "获取权限组列表")
    @GetMapping("/groups")
    public Result<List<PermissionGroup>> getGroups() {
        List<PermissionGroup> groups = permissionGroupService.list();
        return Result.success(groups);
    }

    private void updateUserGroups(Long userId, List<Long> groupIds) {
        userGroupService.remove(new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getUserId, userId));
        if (groupIds != null && !groupIds.isEmpty()) {
            List<UserGroup> ugs = groupIds.stream().map(gid -> {
                UserGroup ug = new UserGroup();
                ug.setUserId(userId);
                ug.setGroupId(gid);
                return ug;
            }).collect(Collectors.toList());
            userGroupService.saveBatch(ugs);
        }
    }

    private Map<Long, List<Long>> buildUserGroupMap(List<User> users) {
        List<Long> userIds = users.stream().map(User::getId).collect(Collectors.toList());
        if (userIds.isEmpty()) {
            return Map.of();
        }
        return userGroupService.list(
                new LambdaQueryWrapper<UserGroup>().in(UserGroup::getUserId, userIds))
                .stream()
                .collect(Collectors.groupingBy(
                        UserGroup::getUserId,
                        Collectors.mapping(UserGroup::getGroupId, Collectors.toList())));
    }

    private Map<Long, String> buildGroupNameMap() {
        return permissionGroupService.list().stream()
                .collect(Collectors.toMap(PermissionGroup::getId, PermissionGroup::getGroupName));
    }

    private UserVO convertToVO(User user, Map<Long, List<Long>> userGroupMap, Map<Long, String> groupNameMap) {
        UserVO vo = new UserVO();
        vo.setId(user.getId());
        vo.setUsername(user.getUsername());
        vo.setRealName(user.getRealName());
        vo.setStatus(user.getStatus());

        List<Long> gids = userGroupMap.getOrDefault(user.getId(), List.of());
        vo.setGroupIds(gids);
        vo.setGroupNames(gids.stream()
                .map(groupNameMap::get)
                .filter(name -> name != null)
                .collect(Collectors.toList()));

        vo.setRemark(user.getRemark());
        vo.setCreateTime(user.getCreateTime());
        vo.setUpdateTime(user.getUpdateTime());
        return vo;
    }
}
