package com.zlinks.package_system.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.Notification;
import com.zlinks.package_system.entity.NotificationRecipient;

import java.util.List;

public interface NotificationService extends IService<Notification> {

    /**
     * 创建通知并按 receiverType 真实下发到 notification_recipient。
     * <p>
     * - receiverType='all' : 全体 users 写入收件箱
     * - receiverType='user': 仅 receiverIds 列表中用户写入收件箱
     * - receiverType='group': 根据 groupIds 查询 user_group → 用户列表后写入
     * - 其他/空 : 回退到 'all' 行为
     *
     * @return 实际写入的 recipient 数量
     */
    int createAndDistribute(Notification notification, String receiverType, List<Long> userIds, List<Long> groupIds);

    /**
     * 单条标记已读(限定 currentUserId 的 recipient 行)。
     */
    boolean markRead(Long currentUserId, Long notificationId);

    /**
     * 当前用户全部已读。
     */
    int markAllRead(Long currentUserId);

    /**
     * 当前用户未读通知数。
     */
    long countUnread(Long currentUserId);

    /**
     * 当前用户的通知列表(按 recipient 视角,带 isRead 标记)。
     */
    List<NotificationWithReadFlag> listMine(Long currentUserId, boolean unreadOnly, int current, int size);

    /**
     * 当前用户某条通知的 recipient 行(用于权限校验)。
     */
    NotificationRecipient findRecipient(Long currentUserId, Long notificationId);

    /**
     * 简单 DTO:Notification + isRead 标记。
     */
    record NotificationWithReadFlag(Notification notification, boolean isRead) {}
}
