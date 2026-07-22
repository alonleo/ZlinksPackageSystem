package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

import java.util.List;

@Data
public class NotificationRequest {

    @NotBlank(message = "通知标题不能为空")
    private String title;

    private String content;

    private String module;

    private Long targetId;

    private String targetType;

    private List<Long> receiverIds;

    /** receiverType='group' 时填写:权限组 ID 列表 */
    private List<Long> groupIds;

    private String receiverType;

    private Integer isPinned;

    private String status;
}
