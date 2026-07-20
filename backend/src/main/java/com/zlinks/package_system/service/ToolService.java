package com.zlinks.package_system.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.Tool;

public interface ToolService extends IService<Tool> {
    CountResult getCounts();
}
