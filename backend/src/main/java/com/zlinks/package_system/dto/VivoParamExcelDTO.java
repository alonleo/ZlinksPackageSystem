package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class VivoParamExcelDTO {
    @ExcelProperty("产品ID") private Long productId;
    @ExcelProperty("AppId") private String appId;
    @ExcelProperty("合同状态") private String contractStatus;
    @ExcelProperty("MediaId") private String mediaId;
    @ExcelProperty("TDAppId") private String tdAppId;
    @ExcelProperty("广告参数状态") private String adParamStatus;
    @ExcelProperty("上架状态") private String listStatus;
    @ExcelProperty("操作人") private String operator;
    @ExcelProperty("备注") private String remark;
}