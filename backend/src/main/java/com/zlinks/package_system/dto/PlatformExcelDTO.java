package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class PlatformExcelDTO {

    @ExcelProperty("平台名称")
    private String platformName;

    @ExcelProperty("平台编码")
    private String platformCode;

    @ExcelProperty("排序")
    private Integer sortOrder;

    @ExcelProperty("状态")
    private String status;

    @ExcelProperty("备注")
    private String remark;
}