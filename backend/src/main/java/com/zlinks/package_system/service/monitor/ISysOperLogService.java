package com.zlinks.package_system.service.monitor;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.monitor.SysOperLog;

public interface ISysOperLogService extends IService<SysOperLog> {

    IPage<SysOperLog> selectOperLogPage(Page<SysOperLog> page, SysOperLog query);

    boolean cleanAll();

    boolean removeByIds(Long[] operIds);
}