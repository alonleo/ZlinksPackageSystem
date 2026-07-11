package com.zlinks.package_system.service.monitor;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.monitor.SysLogininfor;

public interface ISysLogininforService extends IService<SysLogininfor> {

    IPage<SysLogininfor> selectPage(Page<SysLogininfor> page, SysLogininfor query);

    boolean cleanAll();

    boolean removeByIds(Long[] infoIds);

    /** 异步记录登录日志 */
    void recordLogininfor(String userName, String status, String message);
}