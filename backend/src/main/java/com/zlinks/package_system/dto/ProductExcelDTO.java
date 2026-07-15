package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class ProductExcelDTO {

    @ExcelProperty("软著名称")
    private String copyrightName;

    @ExcelProperty("游戏名称")
    private String gameName;

    @ExcelProperty("公司名称")
    private String companyName;

    @ExcelProperty("平台")
    private String platformName;

    @ExcelProperty("包名")
    private String packageName;

    @ExcelProperty("SDK版本")
    private String sdkVersion;

    @ExcelProperty("APK版本")
    private String apkVersion;

    @ExcelProperty("批次")
    private String batch;

    @ExcelProperty("打包模式")
    private String packageMode;

    @ExcelProperty("状态")
    private String status;

    @ExcelProperty("备注")
    private String remark;
}
