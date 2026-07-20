package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.dto.PermissionScopeRequest;
import com.zlinks.package_system.entity.PermissionScope;
import com.zlinks.package_system.service.IPermissionScopeService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.Result;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.Collections;
import java.util.List;

@Tag(name = "权限组模块范围")
@RestController
@RequestMapping("/api/permission-groups/{groupId}/scopes")
@RequiredArgsConstructor
public class PermissionScopeController {
    private final IPermissionScopeService service;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Operation(summary = "列出某 group 全部 scope")
    @GetMapping
    public Result<List<PermissionScope>> list(@PathVariable Long groupId) {
        List<PermissionScope> scopes = service.list(new LambdaQueryWrapper<PermissionScope>().eq(PermissionScope::getGroupId, groupId));
        scopes.forEach(s -> s.setModules(parseModules(s.getModulesText())));
        return Result.success(scopes);
    }

    @Operation(summary = "取单个 scope 的 modules")
    @GetMapping("/{scope}")
    public Result<List<String>> getModules(@PathVariable Long groupId, @PathVariable String scope) {
        PermissionScope existing = service.getOne(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        if (existing == null) return Result.success(Collections.emptyList());
        return Result.success(parseModules(existing.getModulesText()));
    }

    @Operation(summary = "upsert scope")
    @PutMapping("/{scope}")
    public Result<PermissionScope> upsert(@PathVariable Long groupId,
                                          @PathVariable String scope,
                                          @Valid @RequestBody PermissionScopeRequest req) {
        if (!scope.equals(req.getScope())) throw new BusinessException("scope 路径与请求体不一致");
        if (!"backend".equals(scope) && !"desktop".equals(scope)) throw new BusinessException("scope 必须为 backend 或 desktop");
        List<String> modules = req.getModules() == null ? Collections.emptyList() : req.getModules();
        String text = serialize(modules);
        PermissionScope existing = service.getOne(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        if (existing == null) {
            PermissionScope entity = new PermissionScope();
            entity.setGroupId(groupId);
            entity.setScope(scope);
            entity.setModulesText(text);
            entity.setModules(modules);
            service.save(entity);
            return Result.success(entity);
        } else {
            existing.setModulesText(text);
            existing.setModules(modules);
            service.updateById(existing);
            return Result.success(existing);
        }
    }

    @Operation(summary = "删除单个 scope")
    @DeleteMapping("/{scope}")
    public Result<Void> delete(@PathVariable Long groupId, @PathVariable String scope) {
        service.remove(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        return Result.success();
    }

    private String serialize(List<String> modules) {
        if (modules == null) return "[]";
        try {
            return objectMapper.writeValueAsString(modules);
        } catch (Exception e) {
            throw new BusinessException("模块列表序列化失败");
        }
    }

    private List<String> parseModules(String modulesText) {
        if (modulesText == null || modulesText.isEmpty()) return Collections.emptyList();
        try {
            return objectMapper.readValue(modulesText, new TypeReference<List<String>>() {});
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }
}