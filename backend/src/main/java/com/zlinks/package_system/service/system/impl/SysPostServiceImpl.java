package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.system.SysPost;
import com.zlinks.package_system.mapper.system.SysPostMapper;
import com.zlinks.package_system.service.system.ISysPostService;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

@Service
public class SysPostServiceImpl extends ServiceImpl<SysPostMapper, SysPost> implements ISysPostService {

    @Override
    public Page<SysPost> selectPostPage(Page<SysPost> page, SysPost query) {
        LambdaQueryWrapper<SysPost> w = new LambdaQueryWrapper<>();
        if (StringUtils.hasText(query.getPostCode())) w.like(SysPost::getPostCode, query.getPostCode());
        if (StringUtils.hasText(query.getPostName())) w.like(SysPost::getPostName, query.getPostName());
        if (StringUtils.hasText(query.getStatus())) w.eq(SysPost::getStatus, query.getStatus());
        w.orderByAsc(SysPost::getPostSort);
        return baseMapper.selectPostPage(page, w);
    }

    @Override
    public boolean insertPost(SysPost post) {
        return save(post);
    }

    @Override
    public boolean updatePost(SysPost post) {
        return updateById(post);
    }
}