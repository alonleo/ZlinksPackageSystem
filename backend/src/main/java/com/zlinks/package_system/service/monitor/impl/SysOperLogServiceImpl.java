package com.zlinks.package_system.service.monitor.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.monitor.SysOperLog;
import com.zlinks.package_system.mapper.monitor.SysOperLogMapper;
import com.zlinks.package_system.service.monitor.ISysOperLogService;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.util.Arrays;

@Service
public class SysOperLogServiceImpl extends ServiceImpl<SysOperLogMapper, SysOperLog> implements ISysOperLogService {

    @Override
    public IPage<SysOperLog> selectOperLogPage(Page<SysOperLog> page, SysOperLog query) {
        LambdaQueryWrapper<SysOperLog> w = new LambdaQueryWrapper<>();
        w.like(StringUtils.hasText(query.getTitle()), SysOperLog::getTitle, query.getTitle());
        w.eq(query.getBusinessType() != null, SysOperLog::getBusinessType, query.getBusinessType());
        w.like(StringUtils.hasText(query.getOperName()), SysOperLog::getOperName, query.getOperName());
        w.eq(query.getStatus() != null, SysOperLog::getStatus, query.getStatus());
        if (query.getParams() != null) {
            Object begin = query.getParams().get("beginTime");
            Object end = query.getParams().get("endTime");
            if (begin instanceof String b && !b.isEmpty()) w.ge(SysOperLog::getOperTime, b);
            if (end instanceof String e && !e.isEmpty()) w.le(SysOperLog::getOperTime, e);
        }
        w.orderByDesc(SysOperLog::getOperId);
        return page(page, w);
    }

    @Override
    public boolean cleanAll() {
        return remove(new LambdaQueryWrapper<>());
    }

    @Override
    public boolean removeByIds(Long[] operIds) {
        return removeBatchByIds(Arrays.asList(operIds));
    }
}