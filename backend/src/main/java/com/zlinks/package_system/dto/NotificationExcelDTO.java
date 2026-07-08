package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class NotificationExcelDTO {

    @ExcelProperty("标题")
    private String title;

    @ExcelProperty("内容")
    private String content;

    @ExcelProperty("模块")
    private String module;

    @ExcelProperty("目标ID")
    private Long targetId;

    @ExcelProperty("目标类型")
    private String targetType;

    @ExcelProperty("发送者ID")
    private Long senderId;

    @ExcelProperty("接收者ID")
    private String receiverIds;

    @ExcelProperty("接收类型")
    private String receiverType;

    @ExcelProperty("置顶")
    private Integer isPinned;

    @ExcelProperty("状态")
    private String status;
}
