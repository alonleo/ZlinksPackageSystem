package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.AssignmentHistory;
import com.zlinks.package_system.mapper.AssignmentHistoryMapper;
import com.zlinks.package_system.service.AssignmentHistoryService;
import org.springframework.stereotype.Service;

@Service
public class AssignmentHistoryServiceImpl extends ServiceImpl<AssignmentHistoryMapper, AssignmentHistory> implements AssignmentHistoryService {
}
