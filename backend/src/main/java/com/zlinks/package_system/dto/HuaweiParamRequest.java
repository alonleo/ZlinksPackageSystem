package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class HuaweiParamRequest {
    @NotNull(message = "请选择产品")
    private Long productId;
    private String packageName;
    private String appId;
    private String agconnectPath;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}