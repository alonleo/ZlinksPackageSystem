package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.system.SysNotice;
import com.zlinks.package_system.mapper.system.SysNoticeMapper;
import com.zlinks.package_system.service.system.ISysNoticeService;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

@Service
public class SysNoticeServiceImpl extends ServiceImpl<SysNoticeMapper, SysNotice> implements ISysNoticeService {

    @Override
    public IPage<SysNotice> selectPage(Page<SysNotice> page, SysNotice query) {
        LambdaQueryWrapper<SysNotice> w = new LambdaQueryWrapper<>();
        w.like(StringUtils.hasText(query.getNoticeTitle()), SysNotice::getNoticeTitle, query.getNoticeTitle());
        w.eq(StringUtils.hasText(query.getNoticeType()), SysNotice::getNoticeType, query.getNoticeType());
        w.eq(StringUtils.hasText(query.getStatus()), SysNotice::getStatus, query.getStatus());
        w.orderByDesc(SysNotice::getNoticeId);
        return page(page, w);
    }
}