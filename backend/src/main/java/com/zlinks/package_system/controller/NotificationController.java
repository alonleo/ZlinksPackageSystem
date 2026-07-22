package com.zlinks.package_system.controller;

import com.alibaba.excel.EasyExcel;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.dto.NotificationExcelDTO;
import com.zlinks.package_system.dto.NotificationRequest;
import com.zlinks.package_system.entity.Notification;
import com.zlinks.package_system.entity.User;
import com.zlinks.package_system.security.UserDetailsImpl;
import com.zlinks.package_system.service.NotificationService;
import com.zlinks.package_system.service.UserService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.context.SecurityContextHolder;
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
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Tag(name = "通知管理")
@RestController
@RequestMapping("/api/notifications")
@RequiredArgsConstructor
public class NotificationController {

    private final NotificationService notificationService;
    private final UserService userService;
    private final ObjectMapper objectMapper;

    @Operation(summary = "获取通知列表(管理员视图)")
    @GetMapping
    public Result<PageResult<Notification>> list(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(required = false) String title,
            @RequestParam(required = false) String module,
            @RequestParam(required = false) String status) {

        LambdaQueryWrapper<Notification> wrapper = new LambdaQueryWrapper<>();
        wrapper.like(StringUtils.hasText(title), Notification::getTitle, title);
        wrapper.eq(StringUtils.hasText(module), Notification::getModule, module);
        wrapper.eq(StringUtils.hasText(status), Notification::getStatus, status);
        wrapper.orderByDesc(Notification::getIsPinned)
               .orderByDesc(Notification::getCreateTime);

        Page<Notification> page = notificationService.page(new Page<>(current, size), wrapper);
        fillNames(page.getRecords());

        PageResult<Notification> pageResult = new PageResult<>(page.getRecords(), page.getTotal(), page.getSize(), page.getCurrent());
        return Result.success(pageResult);
    }

    @Operation(summary = "获取置顶通知")
    @GetMapping("/pinned")
    public Result<List<Notification>> getPinned() {
        List<Notification> list = notificationService.list(
                new LambdaQueryWrapper<Notification>()
                        .eq(Notification::getIsPinned, 1)
                        .orderByDesc(Notification::getCreateTime));
        fillNames(list);
        return Result.success(list);
    }

    @Operation(summary = "获取最新公告")
    @GetMapping("/announcements")
    public Result<List<Notification>> getAnnouncements() {
        List<Notification> list = notificationService.list(
                new LambdaQueryWrapper<Notification>()
                        .orderByDesc(Notification::getCreateTime)
                        .last("LIMIT 5"));
        fillNames(list);
        return Result.success(list);
    }

    @Operation(summary = "获取通知详情")
    @GetMapping("/{id}")
    public Result<Notification> getById(@PathVariable Long id) {
        Notification notification = notificationService.getById(id);
        if (notification == null) {
            throw new BusinessException("通知不存在");
        }
        fillNames(List.of(notification));
        return Result.success(notification);
    }

    @Operation(summary = "当前用户的通知列表(按收件箱视角,含 isRead)")
    @GetMapping("/mine")
    public Result<PageResult<Map<String, Object>>> listMine(
            @RequestParam(defaultValue = "1") Integer current,
            @RequestParam(defaultValue = "10") Integer size,
            @RequestParam(defaultValue = "false") Boolean unreadOnly) {
        Long userId = getCurrentUserId();
        var records = notificationService.listMine(userId, Boolean.TRUE.equals(unreadOnly), current, size);
        List<Map<String, Object>> rows = records.stream().map(r -> {
            Map<String, Object> m = new HashMap<>();
            Notification n = r.notification();
            m.put("id", n.getId());
            m.put("title", n.getTitle());
            m.put("content", n.getContent());
            m.put("module", n.getModule());
            m.put("targetId", n.getTargetId());
            m.put("targetType", n.getTargetType());
            m.put("senderId", n.getSenderId());
            m.put("senderName", n.getSenderName());
            m.put("isPinned", n.getIsPinned());
            m.put("isRead", r.isRead());
            m.put("createTime", n.getCreateTime());
            return m;
        }).collect(Collectors.toList());
        // 简单包装为 PageResult(只填 records/total=records.size)
        PageResult<Map<String, Object>> pr = new PageResult<>(rows, (long) rows.size(), size, current);
        return Result.success(pr);
    }

    @Operation(summary = "当前用户未读通知数")
    @GetMapping("/unread-count")
    public Result<Long> unreadCount() {
        Long userId = getCurrentUserId();
        return Result.success(notificationService.countUnread(userId));
    }

    @Operation(summary = "单条标记已读")
    @PostMapping("/{id}/read")
    public Result<Void> markRead(@PathVariable Long id) {
        Long userId = getCurrentUserId();
        boolean ok = notificationService.markRead(userId, id);
        if (!ok) {
            // 没有收件箱记录 → 这条通知不属于此用户,不要静默成功
            throw new BusinessException("通知不存在或不属于当前用户");
        }
        return Result.success();
    }

    @Operation(summary = "全部标记已读")
    @PostMapping("/read-all")
    public Result<Integer> markAllRead() {
        Long userId = getCurrentUserId();
        return Result.success(notificationService.markAllRead(userId));
    }

    @Operation(summary = "创建通知(分发到 notification_recipient)")
    @PostMapping
    public Result<Map<String, Object>> create(@Valid @RequestBody NotificationRequest request) {
        Long userId = getCurrentUserId();

        Notification notification = new Notification();
        notification.setTitle(request.getTitle());
        notification.setContent(request.getContent());
        notification.setModule(request.getModule());
        notification.setTargetId(request.getTargetId());
        notification.setTargetType(request.getTargetType());
        notification.setSenderId(userId);
        notification.setIsPinned(request.getIsPinned() != null ? request.getIsPinned() : 0);
        notification.setStatus(request.getStatus() != null ? request.getStatus() : "unread");

        String rt = request.getReceiverType();
        // 'all' 时 receiverIds 为空;也允许 'user' 显式传 receiverIds;'group' 用 groupIds
        List<Long> receiverIds = request.getReceiverIds();
        List<Long> groupIds = request.getGroupIds();

        int distributed = notificationService.createAndDistribute(notification, rt, receiverIds, groupIds);
        // 写回主表冗余字段(便于兼容旧前端,receiverIds 写最简形式)
        List<Long> effectiveIds;
        if ("user".equalsIgnoreCase(rt)) {
            effectiveIds = receiverIds == null ? List.of() : receiverIds;
        } else if ("group".equalsIgnoreCase(rt)) {
            effectiveIds = List.of();
        } else {
            // 'all' 留空
            effectiveIds = List.of();
        }
        notification.setReceiverType(rt);
        notification.setReceiverIds(toJson(effectiveIds));
        notificationService.updateById(notification);

        fillNames(List.of(notification));
        Map<String, Object> data = new HashMap<>();
        data.put("notification", notification);
        data.put("distributed", distributed);
        return Result.success(data);
    }

    @Operation(summary = "更新通知")
    @PutMapping("/{id}")
    public Result<Notification> update(@PathVariable Long id, @Valid @RequestBody NotificationRequest request) {
        Notification notification = notificationService.getById(id);
        if (notification == null) {
            throw new BusinessException("通知不存在");
        }

        notification.setTitle(request.getTitle());
        notification.setContent(request.getContent());
        notification.setModule(request.getModule());
        notification.setTargetId(request.getTargetId());
        notification.setTargetType(request.getTargetType());
        notification.setReceiverType(request.getReceiverType());
        notification.setIsPinned(request.getIsPinned());
        notification.setStatus(request.getStatus());

        notificationService.updateById(notification);
        fillNames(List.of(notification));
        return Result.success(notification);
    }

    @Operation(summary = "导入通知数据")
    @PostMapping("/import")
    public Result<String> importData(@RequestParam("file") MultipartFile file) throws IOException {
        if (file.isEmpty()) {
            throw new BusinessException("请选择要导入的文件");
        }

        List<NotificationExcelDTO> excelList = EasyExcel.read(file.getInputStream())
                .head(NotificationExcelDTO.class)
                .sheet()
                .doReadSync();

        if (excelList.isEmpty()) {
            throw new BusinessException("导入数据为空");
        }

        List<Notification> list = new ArrayList<>();
        for (NotificationExcelDTO dto : excelList) {
            if (!StringUtils.hasText(dto.getTitle())) {
                continue;
            }
            Notification n = new Notification();
            n.setTitle(dto.getTitle());
            n.setContent(dto.getContent());
            n.setModule(dto.getModule());
            n.setTargetId(dto.getTargetId());
            n.setTargetType(dto.getTargetType());
            n.setSenderId(dto.getSenderId());
            n.setReceiverIds(dto.getReceiverIds());
            n.setReceiverType(dto.getReceiverType());
            n.setIsPinned(dto.getIsPinned() != null ? dto.getIsPinned() : 0);
            n.setStatus(StringUtils.hasText(dto.getStatus()) ? dto.getStatus() : "unread");
            list.add(n);
        }

        if (list.isEmpty()) {
            throw new BusinessException("没有有效数据可导入");
        }

        notificationService.saveBatch(list);
        return Result.success("成功导入 " + list.size() + " 条数据");
    }

    @Operation(summary = "导出通知数据")
    @GetMapping("/export")
    public void exportData(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        List<Notification> list = notificationService.list(new LambdaQueryWrapper<Notification>()
                .orderByDesc(Notification::getCreateTime));

        List<NotificationExcelDTO> exportList = new ArrayList<>();
        for (Notification n : list) {
            NotificationExcelDTO dto = new NotificationExcelDTO();
            dto.setTitle(n.getTitle());
            dto.setContent(n.getContent());
            dto.setModule(n.getModule());
            dto.setTargetId(n.getTargetId());
            dto.setTargetType(n.getTargetType());
            dto.setSenderId(n.getSenderId());
            dto.setReceiverIds(n.getReceiverIds());
            dto.setReceiverType(n.getReceiverType());
            dto.setIsPinned(n.getIsPinned());
            dto.setStatus(n.getStatus());
            exportList.add(dto);
        }

        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("通知数据", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), exportList);
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), NotificationExcelDTO.class)
                    .sheet("通知数据")
                    .doWrite(exportList);
        }
    }

    @Operation(summary = "下载导入模板")
    @GetMapping("/template")
    public void downloadTemplate(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse response) throws IOException {
        response.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("通知导入模板", StandardCharsets.UTF_8).replaceAll("\\+", "%20");

        if ("json".equals(format)) {
            NotificationExcelDTO sample = new NotificationExcelDTO();
            sample.setTitle("");
            sample.setContent("");
            sample.setModule("");
            sample.setTargetId(null);
            sample.setTargetType("");
            sample.setSenderId(null);
            sample.setReceiverIds("");
            sample.setReceiverType("");
            sample.setIsPinned(0);
            sample.setStatus("");
            response.setContentType("application/json");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".json");
            objectMapper.writeValue(response.getOutputStream(), List.of(sample));
        } else {
            response.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.setHeader("Content-Disposition", "attachment;filename*=UTF-8''" + fileName + ".xlsx");
            EasyExcel.write(response.getOutputStream(), NotificationExcelDTO.class)
                    .sheet("通知数据")
                    .doWrite(List.of());
        }
    }

    @Operation(summary = "删除通知")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        Notification notification = notificationService.getById(id);
        if (notification == null) {
            throw new BusinessException("通知不存在");
        }
        notificationService.removeById(id);
        return Result.success();
    }

    private Long getCurrentUserId() {
        var auth = SecurityContextHolder.getContext().getAuthentication();
        if (auth != null && auth.getPrincipal() instanceof UserDetailsImpl userDetails) {
            return userDetails.getUserId();
        }
        throw new BusinessException("未登录");
    }

    private String toJson(List<Long> ids) {
        if (ids == null || ids.isEmpty()) return "[]";
        try {
            return objectMapper.writeValueAsString(ids);
        } catch (Exception e) {
            return "[]";
        }
    }

    private List<Long> parseIds(String json) {
        if (!StringUtils.hasText(json)) return Collections.emptyList();
        try {
            return objectMapper.readValue(json, new TypeReference<List<Long>>() {});
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }

    private void fillNames(List<Notification> list) {
        if (list.isEmpty()) return;

        Map<Long, String> userMap = userService.list().stream()
                .collect(Collectors.toMap(User::getId, User::getUsername));

        list.forEach(n -> {
            if (n.getSenderId() != null) {
                n.setSenderName(userMap.getOrDefault(n.getSenderId(), String.valueOf(n.getSenderId())));
            }
            List<Long> receiverIds = parseIds(n.getReceiverIds());
            if (!receiverIds.isEmpty()) {
                n.setReceiverNames(receiverIds.stream()
                        .map(id -> userMap.getOrDefault(id, String.valueOf(id)))
                        .collect(Collectors.joining(", ")));
            }
        });
    }
}
