package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class PermissionGroupExcelDTO {

    @ExcelProperty("权限组名称")
    private String groupName;

    @ExcelProperty("组账号")
    private String groupAccounts;

    @ExcelProperty("备注")
    private String remark;
}
