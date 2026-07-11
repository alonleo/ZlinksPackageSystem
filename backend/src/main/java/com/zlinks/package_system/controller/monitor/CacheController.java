package com.zlinks.package_system.controller.monitor;

import com.zlinks.package_system.util.RedisUtils;
import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.data.redis.connection.RedisConnection;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.core.Cursor;
import org.springframework.data.redis.core.RedisConnectionUtils;
import org.springframework.data.redis.core.ScanOptions;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.*;

/**
 * 缓存监控 - Redis
 */
@RestController
@RequestMapping("/api/monitor/cache")
@RequiredArgsConstructor
public class CacheController {

    private final RedisUtils redisUtils;

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @GetMapping
    public Result<Map<String, Object>> getInfo() {
        Map<String, Object> rsp = new LinkedHashMap<>();
        try {
            rsp.put("info", redisUtils.getRedisInfo());
            rsp.put("dbSize", redisUtils.getDbSize());
        } catch (Exception e) {
            rsp.put("info", Collections.emptyMap());
            rsp.put("dbSize", 0);
        }
        rsp.put("commandStats", getCommandStats());
        return Result.success(rsp);
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @GetMapping("/getNames")
    public Result<Collection<String>> getNames() {
        Set<String> keys = redisUtils.scan(CacheKeys.PREFIX + "*");
        Set<String> result = new LinkedHashSet<>();
        if (keys != null) {
            for (String k : keys) {
                int idx = k.indexOf(":", CacheKeys.PREFIX.length() + 1);
                if (idx > 0) result.add(k.substring(0, idx + 1));
                else result.add(k);
            }
        }
        return Result.success(result);
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @GetMapping("/getKeys/{cacheName}")
    public Result<Collection<String>> getKeys(@PathVariable String cacheName) {
        return Result.success(redisUtils.scan(cacheName + "*"));
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @GetMapping("/getValue/{cacheName}/{cacheKey}")
    public Result<Map<String, Object>> getValue(@PathVariable String cacheName, @PathVariable String cacheKey) {
        Object v = redisUtils.get(cacheKey);
        Map<String, Object> rsp = new LinkedHashMap<>();
        rsp.put("cacheName", cacheName);
        rsp.put("cacheKey", cacheKey);
        rsp.put("cacheValue", v);
        rsp.put("remark", "");
        return Result.success(rsp);
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @DeleteMapping("/clearCacheName/{cacheName}")
    public Result<Void> clearCacheName(@PathVariable String cacheName) {
        Set<String> keys = redisUtils.scan(cacheName + "*");
        if (keys != null) redisUtils.deleteObject(keys);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @DeleteMapping("/clearCacheKey/{cacheKey}")
    public Result<Void> clearCacheKey(@PathVariable String cacheKey) {
        redisUtils.deleteObject(cacheKey);
        return Result.success();
    }

    @PreAuthorize("@ss.hasPermi('monitor:cache:list')")
    @DeleteMapping("/clearCacheAll")
    public Result<Void> clearCacheAll() {
        redisUtils.flushDb();
        return Result.success();
    }

    private List<Map<String, String>> getCommandStats() {
        // 简化：返回空，避免在没集成 StringRedisTemplate 的情况下报错
        return new ArrayList<>();
    }

    static class CacheKeys {
        static final String PREFIX = "login_tokens:";
    }
}