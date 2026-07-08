package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class GameExcelDTO {

    @ExcelProperty("游戏名")
    private String gameName;

    @ExcelProperty("游戏方向")
    private String gameDirection;

    @ExcelProperty("来源")
    private String source;

    @ExcelProperty("Git地址")
    private String gitUrl;

    @ExcelProperty("优先级")
    private Integer priority;

    @ExcelProperty("标签")
    private String tags;

    @ExcelProperty("工程类型")
    private String projectType;

    @ExcelProperty("负责人")
    private String manager;

    @ExcelProperty("白包分支")
    private String whiteBranch;

    @ExcelProperty("状态")
    private String status;

    @ExcelProperty("安卓文件夹名")
    private String androidFolderName;

    @ExcelProperty("备注")
    private String remark;
}
