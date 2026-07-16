package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Data;

import java.util.List;

@Data
public class PermissionScopeRequest {
    @NotNull(message = "请选择权限组")
    private Long groupId;

    @NotBlank(message = "scope 不能为空")
    private String scope;

    private List<String> modules;
}
