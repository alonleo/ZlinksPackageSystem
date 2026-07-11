package com.zlinks.package_system.service.monitor.impl;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.constant.CacheConstants;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.monitor.SysUserOnline;
import com.zlinks.package_system.security.LoginUser;
import com.zlinks.package_system.service.monitor.ISysUserOnlineService;
import com.zlinks.package_system.util.RedisUtils;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class SysUserOnlineServiceImpl implements ISysUserOnlineService {

    private final RedisUtils redisUtils;

    @Override
    public List<SysUserOnline> listAll() {
        Set<String> keys = redisUtils.keys(CacheConstants.LOGIN_TOKEN_KEY + "*");
        if (keys == null || keys.isEmpty()) return Collections.emptyList();
        Collection<Object> values = redisUtils.multiGet(keys);
        List<SysUserOnline> list = new ArrayList<>();
        for (int i = 0; i < keys.size() && i < values.size(); i++) {
            Object v = values instanceof List ? ((List<?>) values).get(i) : null;
            if (v instanceof LoginUser lu) list.add(toVO(lu, keys.iterator().next()));
        }
        return list;
    }

    @Override
    public Page<SysUserOnline> selectOnlinePage(Page<SysUserOnline> page, String ipaddr, String userName) {
        List<SysUserOnline> all = listAll();
        List<SysUserOnline> filtered = all.stream()
                .filter(o -> ipaddr == null || ipaddr.isEmpty() || (o.getIpaddr() != null && o.getIpaddr().contains(ipaddr)))
                .filter(o -> userName == null || userName.isEmpty() || (o.getUserName() != null && o.getUserName().contains(userName)))
                .collect(Collectors.toList());
        page.setTotal(filtered.size());
        int from = (int) Math.min((page.getCurrent() - 1) * page.getSize(), filtered.size());
        int to = (int) Math.min(from + page.getSize(), filtered.size());
        page.setRecords(filtered.subList(from, to));
        return page;
    }

    @Override
    public boolean forceLogout(String tokenId) {
        return redisUtils.deleteObject(CacheConstants.LOGIN_TOKEN_KEY + tokenId);
    }

    private SysUserOnline toVO(LoginUser lu, String tokenKey) {
        SysUserOnline o = new SysUserOnline();
        o.setTokenId(tokenKey.replace(CacheConstants.LOGIN_TOKEN_KEY, ""));
        o.setUserName(lu.getUserName());
        o.setIpaddr(lu.getIpaddr());
        o.setLoginLocation(lu.getLoginLocation());
        o.setBrowser(lu.getBrowser());
        o.setOs(lu.getOs());
        String rk = lu.getRoles() == null ? "" : lu.getRoles().stream().findFirst().orElse("");
        o.setRoleKey(rk);
        o.setLoginTime(lu.getLoginTime() == null ? LocalDateTime.now()
                : LocalDateTime.ofInstant(java.time.Instant.ofEpochMilli(lu.getLoginTime()), java.time.ZoneId.systemDefault()));
        return o;
    }
}