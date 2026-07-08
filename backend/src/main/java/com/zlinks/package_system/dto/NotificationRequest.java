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

    private String receiverType;

    private Integer isPinned;

    private String status;
}
