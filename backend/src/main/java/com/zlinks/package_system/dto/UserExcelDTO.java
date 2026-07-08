package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class UserExcelDTO {

    @ExcelProperty("用户名")
    private String username;

    @ExcelProperty("密码")
    private String password;

    @ExcelProperty("姓名")
    private String realName;

    @ExcelProperty("状态")
    private String status;

    @ExcelProperty("权限组")
    private String groupNames;

    @ExcelProperty("备注")
    private String remark;
}
