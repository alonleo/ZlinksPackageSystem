package com.zlinks.package_system.service.monitor.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.monitor.SysJobLog;
import com.zlinks.package_system.mapper.monitor.SysJobLogMapper;
import com.zlinks.package_system.service.monitor.ISysJobLogService;
import lombok.RequiredArgsConstructor;
import org.apache.commons.lang3.StringUtils;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class SysJobLogServiceImpl implements ISysJobLogService {

    private final SysJobLogMapper jobLogMapper;

    @Override
    public Page<SysJobLog> selectJobLogPage(Page<SysJobLog> page, SysJobLog query) {
        LambdaQueryWrapper<SysJobLog> wrapper = new LambdaQueryWrapper<>();
        if (StringUtils.isNotBlank(query.getJobName())) {
            wrapper.like(SysJobLog::getJobName, query.getJobName());
        }
        if (StringUtils.isNotBlank(query.getJobGroup())) {
            wrapper.eq(SysJobLog::getJobGroup, query.getJobGroup());
        }
        if (StringUtils.isNotBlank(query.getStatus())) {
            wrapper.eq(SysJobLog::getStatus, query.getStatus());
        }
        if (StringUtils.isNotBlank(query.getInvokeTarget())) {
            wrapper.like(SysJobLog::getInvokeTarget, query.getInvokeTarget());
        }
        wrapper.orderByDesc(SysJobLog::getStartTime);
        return jobLogMapper.selectPage(page, wrapper);
    }

    @Override
    public SysJobLog getById(Long jobLogId) {
        return jobLogMapper.selectById(jobLogId);
    }

    @Override
    public int addJobLog(SysJobLog log) {
        return jobLogMapper.insert(log);
    }

    @Override
    public int deleteByIds(Long[] ids) {
        return jobLogMapper.deleteBatchIds(java.util.Arrays.asList(ids));
    }

    @Override
    public int cleanAll() {
        return jobLogMapper.delete(new LambdaQueryWrapper<>());
    }
}