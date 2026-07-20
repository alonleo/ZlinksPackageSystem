package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.Tool;
import com.zlinks.package_system.mapper.ToolMapper;
import com.zlinks.package_system.service.ToolService;
import org.springframework.stereotype.Service;

@Service
public class ToolServiceImpl extends ServiceImpl<ToolMapper, Tool> implements ToolService {

    @Override
    public CountResult getCounts() {
        long total = count();
        long running = count(new LambdaQueryWrapper<Tool>().eq(Tool::getStatus, "运行中"));
        return new CountResult(total, running);
    }
}
