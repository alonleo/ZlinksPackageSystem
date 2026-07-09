package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.PlatformMatch;
import com.zlinks.package_system.mapper.PlatformMatchMapper;
import com.zlinks.package_system.service.PlatformMatchService;
import org.springframework.stereotype.Service;

@Service
public class PlatformMatchServiceImpl extends ServiceImpl<PlatformMatchMapper, PlatformMatch> implements PlatformMatchService {

    @Override
    public CountResult getCounts() {
        long total = count();
        long pending = count(new LambdaQueryWrapper<PlatformMatch>().eq(PlatformMatch::getPlatformStatus, "pending"));
        return new CountResult(total, pending);
    }
}
