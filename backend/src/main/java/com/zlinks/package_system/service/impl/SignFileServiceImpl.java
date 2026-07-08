package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.SignFile;
import com.zlinks.package_system.mapper.SignFileMapper;
import com.zlinks.package_system.service.SignFileService;
import org.springframework.stereotype.Service;

@Service
public class SignFileServiceImpl extends ServiceImpl<SignFileMapper, SignFile> implements SignFileService {
}
