package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class OperationLogExcelDTO {

    @ExcelProperty("用户名")
    private String username;

    @ExcelProperty("模块")
    private String module;

    @ExcelProperty("操作")
    private String action;

    @ExcelProperty("目标")
    private String target;

    @ExcelProperty("IP地址")
    private String ipAddress;

    @ExcelProperty("操作时间")
    private String createTime;
}
