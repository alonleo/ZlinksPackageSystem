package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class PermissionGroupRequest {

    @NotBlank(message = "权限组名称不能为空")
    private String groupName;

    private String remark;
}
