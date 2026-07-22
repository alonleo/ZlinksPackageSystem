package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.AssignmentEvent;
import com.zlinks.package_system.mapper.AssignmentEventMapper;
import com.zlinks.package_system.service.AssignmentEventService;
import org.springframework.stereotype.Service;

@Service
public class AssignmentEventServiceImpl extends ServiceImpl<AssignmentEventMapper, AssignmentEvent> implements AssignmentEventService {
}
