package com.zlinks.package_system.service.monitor;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.monitor.SysUserOnline;

import java.util.List;

public interface ISysUserOnlineService {
    IPage<SysUserOnline> selectOnlinePage(Page<SysUserOnline> page, String ipaddr, String userName);
    boolean forceLogout(String tokenId);
    List<SysUserOnline> listAll();
}