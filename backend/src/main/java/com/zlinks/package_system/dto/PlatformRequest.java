package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class PlatformRequest {

    @NotBlank(message = "平台名称不能为空")
    private String platformName;

    private String platformCode;

    private Integer sortOrder;

    private String status;

    private String remark;
}