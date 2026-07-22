package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.time.LocalDateTime;

/**
 * 通知收件箱(一对多:一条 notification 可对应多 user)
 * <p>
 * 解决现有 Notification.receiverType='all'/'user'/'group' 实际不分发的 F5 问题。
 * 发送方写入一条 notification;每位受派人各自一行 notification_recipient,isRead 独立维护。
 */
@Data
@TableName("notification_recipient")
public class NotificationRecipient {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    private Long notificationId;

    private Long userId;

    /** 0 未读 / 1 已读 */
    private Integer isRead;

    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime readAt;

    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime createTime;
}
