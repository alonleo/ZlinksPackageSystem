package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class CompanyRequest {

    @NotBlank(message = "公司名称不能为空")
    private String companyName;

    private String platform;

    private String account;

    private String password;

    private String remark;
}
