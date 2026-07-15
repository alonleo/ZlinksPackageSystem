package com.zlinks.package_system.service.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.monitor.SysJobLog;

public interface ISysJobLogService {

    Page<SysJobLog> selectJobLogPage(Page<SysJobLog> page, SysJobLog query);

    SysJobLog getById(Long jobLogId);

    int addJobLog(SysJobLog log);

    int deleteByIds(Long[] ids);

    int cleanAll();
}