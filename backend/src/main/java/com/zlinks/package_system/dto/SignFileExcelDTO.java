package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class SignFileExcelDTO {

    @ExcelProperty("所属公司")
    private String companyName;

    @ExcelProperty("文件路径")
    private String storeFile;

    @ExcelProperty("密码")
    private String storePassword;

    @ExcelProperty("别名")
    private String keyAlias;

    @ExcelProperty("备注")
    private String remark;
}
