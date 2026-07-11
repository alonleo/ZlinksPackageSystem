package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysNotice;

public interface ISysNoticeService extends IService<SysNotice> {
    IPage<SysNotice> selectPage(Page<SysNotice> page, SysNotice query);
}