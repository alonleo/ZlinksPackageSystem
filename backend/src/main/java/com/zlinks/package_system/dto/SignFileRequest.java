package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class SignFileRequest {

    @NotNull(message = "请选择所属公司")
    private Long companyId;

    private String storeFile;

    private String storePassword;

    private String keyAlias;

    private String remark;
}
