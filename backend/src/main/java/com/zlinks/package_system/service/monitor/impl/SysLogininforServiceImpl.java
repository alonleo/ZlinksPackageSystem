package com.zlinks.package_system.service.monitor.impl;

import cn.hutool.http.useragent.UserAgent;
import cn.hutool.http.useragent.UserAgentUtil;
import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.core.metadata.IPage;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.constant.CacheConstants;
import com.zlinks.package_system.entity.monitor.SysLogininfor;
import com.zlinks.package_system.mapper.monitor.SysLogininforMapper;
import com.zlinks.package_system.service.monitor.ISysLogininforService;
import com.zlinks.package_system.util.IpUtils;
import com.zlinks.package_system.util.RedisUtils;
import com.zlinks.package_system.util.ServletUtils;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.concurrent.TimeUnit;

@Slf4j
@Service
@RequiredArgsConstructor
public class SysLogininforServiceImpl extends ServiceImpl<SysLogininforMapper, SysLogininfor> implements ISysLogininforService {

    private final RedisUtils redisUtils;

    @Override
    public IPage<SysLogininfor> selectPage(Page<SysLogininfor> page, SysLogininfor query) {
        LambdaQueryWrapper<SysLogininfor> w = new LambdaQueryWrapper<>();
        w.like(StringUtils.hasText(query.getUserName()), SysLogininfor::getUserName, query.getUserName());
        w.like(StringUtils.hasText(query.getIpaddr()), SysLogininfor::getIpaddr, query.getIpaddr());
        w.eq(StringUtils.hasText(query.getStatus()), SysLogininfor::getStatus, query.getStatus());
        w.orderByDesc(SysLogininfor::getInfoId);
        return page(page, w);
    }

    @Override
    public boolean cleanAll() {
        return remove(new LambdaQueryWrapper<>());
    }

    @Override
    public boolean removeByIds(Long[] infoIds) {
        return removeBatchByIds(Arrays.asList(infoIds));
    }

    @Async
    @Override
    public void recordLogininfor(String userName, String status, String message) {
        try {
            HttpServletRequest req = ServletUtils.getRequest();
            String ip = req == null ? "unknown" : ServletUtils.getClientIp();
            UserAgent ua = req == null ? null : UserAgentUtil.parse(req.getHeader("User-Agent"));
            SysLogininfor info = new SysLogininfor();
            info.setUserName(userName);
            info.setIpaddr(ip);
            info.setLoginLocation(IpUtils.getCity(ip));
            info.setBrowser(ua == null ? "Unknown" : ua.getBrowser().getName());
            info.setOs(ua == null ? "Unknown" : ua.getOs().getName());
            info.setStatus(status);
            info.setMsg(message);
            info.setLoginTime(LocalDateTime.now());
            save(info);
        } catch (Exception e) {
            log.warn("记录登录日志失败: {}", e.getMessage());
        }
    }
}