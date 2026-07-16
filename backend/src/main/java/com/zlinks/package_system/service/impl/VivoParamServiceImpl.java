package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.VivoParam;
import com.zlinks.package_system.mapper.VivoParamMapper;
import com.zlinks.package_system.service.IVivoParamService;
import org.springframework.stereotype.Service;

@Service
public class VivoParamServiceImpl extends ServiceImpl<VivoParamMapper, VivoParam> implements IVivoParamService {
}