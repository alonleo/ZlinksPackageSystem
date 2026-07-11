package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.system.SysConfig;
import com.zlinks.package_system.mapper.system.SysConfigMapper;
import com.zlinks.package_system.service.system.ISysConfigService;
import com.zlinks.package_system.util.BusinessException;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

@Service
public class SysConfigServiceImpl extends ServiceImpl<SysConfigMapper, SysConfig> implements ISysConfigService {

    @Override
    public IPage<SysConfig> selectPage(Page<SysConfig> page, SysConfig query) {
        LambdaQueryWrapper<SysConfig> w = new LambdaQueryWrapper<>();
        w.like(StringUtils.hasText(query.getConfigName()), SysConfig::getConfigName, query.getConfigName());
        w.like(StringUtils.hasText(query.getConfigKey()), SysConfig::getConfigKey, query.getConfigKey());
        w.eq(StringUtils.hasText(query.getConfigType()), SysConfig::getConfigType, query.getConfigType());
        w.orderByDesc(SysConfig::getConfigId);
        return page(page, w);
    }

    @Override
    public SysConfig getByKey(String configKey) {
        return getOne(new LambdaQueryWrapper<SysConfig>().eq(SysConfig::getConfigKey, configKey));
    }

    @Override
    public boolean checkConfigKeyUnique(SysConfig config) {
        Long id = config.getConfigId();
        SysConfig exist = getOne(new LambdaQueryWrapper<SysConfig>().eq(SysConfig::getConfigKey, config.getConfigKey()));
        return exist == null || (id != null && exist.getConfigId().equals(id));
    }

    public boolean saveConfig(SysConfig config) {
        if (!checkConfigKeyUnique(config)) throw new BusinessException("参数键名已存在");
        return saveOrUpdate(config);
    }
}