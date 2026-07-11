package com.zlinks.package_system.util;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Component;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.TimeUnit;

/**
 * Redis 工具类
 */
@Component
public class RedisUtils {

    @Autowired
    private RedisTemplate<String, Object> redisTemplate;

    // ============================= Common ============================

    /**
     * 设置过期时间
     */
    public boolean expire(String key, long timeout, TimeUnit unit) {
        try {
            return Boolean.TRUE.equals(redisTemplate.expire(key, timeout, unit));
        } catch (Exception e) {
            return false;
        }
    }

    /**
     * 获取过期时间
     */
    public long getExpire(String key) {
        return redisTemplate.getExpire(key, TimeUnit.SECONDS);
    }

    /**
     * 是否有 key
     */
    public boolean hasKey(String key) {
        try {
            return Boolean.TRUE.equals(redisTemplate.hasKey(key));
        } catch (Exception e) {
            return false;
        }
    }

    /**
     * 删除单个 key
     */
    public boolean deleteObject(String key) {
        try {
            return Boolean.TRUE.equals(redisTemplate.delete(key));
        } catch (Exception e) {
            return false;
        }
    }

    /**
     * 批量删除 key
     */
    public long deleteObject(Collection<String> keys) {
        try {
            Long count = redisTemplate.delete(keys);
            return count == null ? 0L : count;
        } catch (Exception e) {
            return 0L;
        }
    }

    // ============================= String ============================

    public Object get(String key) {
        return key == null ? null : redisTemplate.opsForValue().get(key);
    }

    public <T> T get(String key, Class<T> clazz) {
        Object value = get(key);
        if (value == null) {
            return null;
        }
        if (clazz.isInstance(value)) {
            return clazz.cast(value);
        }
        // Fastjson/jackson 反序列化时多态会变成 LinkedHashMap, 此处需要由调用方自行处理
        return null;
    }

    public boolean set(String key, Object value) {
        try {
            redisTemplate.opsForValue().set(key, value);
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    public boolean set(String key, Object value, long timeout, TimeUnit unit) {
        try {
            redisTemplate.opsForValue().set(key, value, timeout, unit);
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    public long increment(String key, long delta) {
        try {
            Long value = redisTemplate.opsForValue().increment(key, delta);
            return value == null ? 0L : value;
        } catch (Exception e) {
            return 0L;
        }
    }

    // ============================= Hash ============================

    public Object hashGet(String key, String item) {
        return redisTemplate.opsForHash().get(key, item);
    }

    public Map<Object, Object> hashGetAll(String key) {
        return redisTemplate.opsForHash().entries(key);
    }

    public void hashPut(String key, String item, Object value) {
        redisTemplate.opsForHash().put(key, item, value);
    }

    public long hashDelete(String key, Object... items) {
        Long count = redisTemplate.opsForHash().delete(key, items);
        return count == null ? 0L : count;
    }

    // ============================= List ============================

    public List<Object> listRange(String key, long start, long end) {
        try {
            return redisTemplate.opsForList().range(key, start, end);
        } catch (Exception e) {
            return null;
        }
    }

    public long listSize(String key) {
        try {
            Long size = redisTemplate.opsForList().size(key);
            return size == null ? 0L : size;
        } catch (Exception e) {
            return 0L;
        }
    }

    // ============================= Set ============================

    public Set<Object> setMembers(String key) {
        try {
            return redisTemplate.opsForSet().members(key);
        } catch (Exception e) {
            return null;
        }
    }

    public long setAdd(String key, Object... values) {
        try {
            Long count = redisTemplate.opsForSet().add(key, values);
            return count == null ? 0L : count;
        } catch (Exception e) {
            return 0L;
        }
    }

    public long setRemove(String key, Object... values) {
        try {
            Long count = redisTemplate.opsForSet().remove(key, values);
            return count == null ? 0L : count;
        } catch (Exception e) {
            return 0L;
        }
    }
}