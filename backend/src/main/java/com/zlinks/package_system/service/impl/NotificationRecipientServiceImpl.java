package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.NotificationRecipient;
import com.zlinks.package_system.mapper.NotificationRecipientMapper;
import com.zlinks.package_system.service.NotificationRecipientService;
import org.springframework.stereotype.Service;

@Service
public class NotificationRecipientServiceImpl extends ServiceImpl<NotificationRecipientMapper, NotificationRecipient> implements NotificationRecipientService {
}
