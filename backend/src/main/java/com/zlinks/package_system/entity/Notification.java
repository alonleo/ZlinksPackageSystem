package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("notification")
public class Notification extends BaseEntity {

    private String title;
    private String content;
    private String module;
    private Long targetId;
    private String targetType;
    private Long senderId;
    private String receiverIds;
    private String receiverType;
    private Integer isPinned;
    private String status;
}