package com.zlinks.package_system.util;

import com.baomidou.mybatisplus.core.metadata.IPage;

import java.util.HashMap;
import java.util.Map;

/**
 * 分页工具类, 兼容 RuoYi 风格 TableDataInfo 响应
 *
 * <p>同时返回 rows / records / size / current / pages / total, 方便不同前端风格调用.</p>
 */
public class PageUtils {

    /**
     * 将 MyBatis-Plus IPage 转为分页响应 Map 结构
     */
    public static Map<String, Object> build(IPage<?> page) {
        Map<String, Object> rspData = new HashMap<>();
        rspData.put("rows", page.getRecords());
        rspData.put("records", page.getRecords());
        rspData.put("total", page.getTotal());
        rspData.put("size", page.getSize());
        rspData.put("current", page.getCurrent());
        rspData.put("pages", page.getPages());
        return rspData;
    }

    private PageUtils() {
    }
}