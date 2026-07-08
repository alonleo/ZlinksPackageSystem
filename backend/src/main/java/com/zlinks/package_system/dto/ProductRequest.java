package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class ProductRequest {

    @NotNull(message = "请选择软著")
    private Long copyrightId;

    private Long gameId;

    private Long companyId;

    private String platform;

    private String packageName;

    private String sdkVersion;

    private String apkVersion;

    private String batch;

    private String packageMode;

    private String status;

    private String remark;
}
