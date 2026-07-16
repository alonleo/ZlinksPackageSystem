package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class HonorParamExcelDTO {
    @ExcelProperty("产品ID") private Long productId;
    @ExcelProperty("包名") private String packageName;
    @ExcelProperty("AppId") private String appId;
    @ExcelProperty("AppSecret") private String appSecret;
    @ExcelProperty("MediaId") private String mediaId;
    @ExcelProperty("AGConnect路径") private String agconnectPath;
    @ExcelProperty("TDAppId") private String tdAppId;
    @ExcelProperty("广告参数状态") private String adParamStatus;
    @ExcelProperty("上架状态") private String listStatus;
    @ExcelProperty("操作人") private String operator;
    @ExcelProperty("备注") private String remark;
}