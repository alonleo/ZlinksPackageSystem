package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class CopyrightExcelDTO {

    @ExcelProperty("软著名称")
    private String copyrightName;

    @ExcelProperty("著作权人")
    private String copyrightOwner;

    @ExcelProperty("软著号")
    private String copyrightNumber;

    @ExcelProperty("备注")
    private String remark;
}
