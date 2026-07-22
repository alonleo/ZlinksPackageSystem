package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.Notification;
import com.zlinks.package_system.entity.NotificationRecipient;
import com.zlinks.package_system.entity.UserGroup;
import com.zlinks.package_system.mapper.NotificationMapper;
import com.zlinks.package_system.mapper.NotificationRecipientMapper;
import com.zlinks.package_system.mapper.UserGroupMapper;
import com.zlinks.package_system.mapper.UserMapper;
import com.zlinks.package_system.service.NotificationService;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.CollectionUtils;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class NotificationServiceImpl extends ServiceImpl<NotificationMapper, Notification> implements NotificationService {

    private final NotificationRecipientMapper recipientMapper;
    private final UserGroupMapper userGroupMapper;
    private final UserMapper userMapper;

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int createAndDistribute(Notification notification, String receiverType, List<Long> userIds, List<Long> groupIds) {
        // 1) 落主表(继承 BaseEntity,create_time/update_time 由 MyBatis-Plus 自动填充)
        this.save(notification);

        // 2) 解析实际收件人集合
        Set<Long> resolvedUserIds = resolveReceiverUserIds(receiverType, userIds, groupIds);
        if (resolvedUserIds.isEmpty()) {
            return 0;
        }

        // 3) 批量写收件箱(unique 索引保证幂等)
        LocalDateTime now = LocalDateTime.now();
        List<NotificationRecipient> rows = new ArrayList<>(resolvedUserIds.size());
        for (Long uid : resolvedUserIds) {
            NotificationRecipient r = new NotificationRecipient();
            r.setNotificationId(notification.getId());
            r.setUserId(uid);
            r.setIsRead(0);
            r.setCreateTime(now);
            rows.add(r);
        }
        // ignore: 已存在的(notification_id, user_id) 由 DB unique 索引兜底
        for (NotificationRecipient r : rows) {
            try {
                recipientMapper.insert(r);
            } catch (org.springframework.dao.DuplicateKeyException ignored) {
                // 已存在
            }
        }
        return rows.size();
    }

    private Set<Long> resolveReceiverUserIds(String receiverType, List<Long> userIds, List<Long> groupIds) {
        if ("user".equalsIgnoreCase(receiverType)) {
            return userIds == null ? Set.of() : new HashSet<>(userIds);
        }
        if ("group".equalsIgnoreCase(receiverType)) {
            if (CollectionUtils.isEmpty(groupIds)) return Set.of();
            List<UserGroup> ugList = userGroupMapper.selectList(
                    new LambdaQueryWrapper<UserGroup>().in(UserGroup::getGroupId, groupIds));
            return ugList.stream().map(UserGroup::getUserId).collect(Collectors.toSet());
        }
        // 'all' 或 null/空 → 全体用户
        return userMapper.selectList(new LambdaQueryWrapper<>())
                .stream()
                .map(u -> u.getId())
                .collect(Collectors.toSet());
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public boolean markRead(Long currentUserId, Long notificationId) {
        if (currentUserId == null || notificationId == null) return false;
        NotificationRecipient r = recipientMapper.selectOne(new LambdaQueryWrapper<NotificationRecipient>()
                .eq(NotificationRecipient::getNotificationId, notificationId)
                .eq(NotificationRecipient::getUserId, currentUserId));
        if (r == null) return false;
        if (Integer.valueOf(1).equals(r.getIsRead())) return true;
        r.setIsRead(1);
        r.setReadAt(LocalDateTime.now());
        return recipientMapper.updateById(r) > 0;
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int markAllRead(Long currentUserId) {
        if (currentUserId == null) return 0;
        List<NotificationRecipient> unread = recipientMapper.selectList(new LambdaQueryWrapper<NotificationRecipient>()
                .eq(NotificationRecipient::getUserId, currentUserId)
                .eq(NotificationRecipient::getIsRead, 0));
        if (unread.isEmpty()) return 0;
        LocalDateTime now = LocalDateTime.now();
        int n = 0;
        for (NotificationRecipient r : unread) {
            r.setIsRead(1);
            r.setReadAt(now);
            n += recipientMapper.updateById(r);
        }
        return n;
    }

    @Override
    public long countUnread(Long currentUserId) {
        if (currentUserId == null) return 0;
        return recipientMapper.selectCount(new LambdaQueryWrapper<NotificationRecipient>()
                .eq(NotificationRecipient::getUserId, currentUserId)
                .eq(NotificationRecipient::getIsRead, 0));
    }

    @Override
    public List<NotificationWithReadFlag> listMine(Long currentUserId, boolean unreadOnly, int current, int size) {
        if (currentUserId == null) return List.of();
        LambdaQueryWrapper<NotificationRecipient> w = new LambdaQueryWrapper<NotificationRecipient>()
                .eq(NotificationRecipient::getUserId, currentUserId)
                .orderByDesc(NotificationRecipient::getCreateTime);
        if (unreadOnly) {
            w.eq(NotificationRecipient::getIsRead, 0);
        }
        Page<NotificationRecipient> page = recipientMapper.selectPage(new Page<>(current, size), w);
        List<NotificationRecipient> records = page.getRecords();
        if (records.isEmpty()) return List.of();

        List<Long> notifIds = records.stream().map(NotificationRecipient::getNotificationId).collect(Collectors.toList());
        List<Notification> notifs = this.listByIds(notifIds);
        // 保持 recipient 顺序
        return records.stream().map(r -> {
            Notification n = notifs.stream()
                    .filter(x -> x.getId().equals(r.getNotificationId()))
                    .findFirst()
                    .orElse(null);
            boolean isRead = Integer.valueOf(1).equals(r.getIsRead());
            return new NotificationWithReadFlag(n, isRead);
        }).filter(x -> x.notification() != null).collect(Collectors.toList());
    }

    @Override
    public NotificationRecipient findRecipient(Long currentUserId, Long notificationId) {
        if (currentUserId == null || notificationId == null) return null;
        return recipientMapper.selectOne(new LambdaQueryWrapper<NotificationRecipient>()
                .eq(NotificationRecipient::getNotificationId, notificationId)
                .eq(NotificationRecipient::getUserId, currentUserId));
    }
}
