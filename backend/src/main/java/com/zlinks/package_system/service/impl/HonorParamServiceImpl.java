package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.HonorParam;
import com.zlinks.package_system.mapper.HonorParamMapper;
import com.zlinks.package_system.service.IHonorParamService;
import org.springframework.stereotype.Service;

@Service
public class HonorParamServiceImpl extends ServiceImpl<HonorParamMapper, HonorParam> implements IHonorParamService {
}