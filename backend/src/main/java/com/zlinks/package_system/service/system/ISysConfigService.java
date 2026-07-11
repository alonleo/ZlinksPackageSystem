package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysConfig;

public interface ISysConfigService extends IService<SysConfig> {
    IPage<SysConfig> selectPage(Page<SysConfig> page, SysConfig query);
    SysConfig getByKey(String configKey);
    boolean checkConfigKeyUnique(SysConfig config);
    boolean saveConfig(SysConfig config);
}