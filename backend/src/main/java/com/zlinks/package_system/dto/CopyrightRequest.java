package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class CopyrightRequest {

    @NotBlank(message = "软著名称不能为空")
    private String copyrightName;

    private String copyrightOwner;

    private String copyrightNumber;

    private String remark;
}
