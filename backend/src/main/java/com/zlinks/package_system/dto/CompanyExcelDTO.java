package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class CompanyExcelDTO {

    @ExcelProperty("公司名称")
    private String companyName;

    @ExcelProperty("平台")
    private String platformName;

    @ExcelProperty("账号")
    private String account;

    @ExcelProperty("密码")
    private String password;

    @ExcelProperty("备注")
    private String remark;
}
