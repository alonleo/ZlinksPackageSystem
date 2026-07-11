package com.zlinks.package_system.util;

import com.baomidou.mybatisplus.core.metadata.IPage;

import java.util.HashMap;
import java.util.Map;

/**
 * 分页工具类, 兼容 RuoYi 风格 TableDataInfo 响应
 */
public class PageUtils {

    /**
     * 将 MyBatis-Plus IPage 转为 RuoYi TableDataInfo Map 结构
     */
    public static Map<String, Object> build(IPage<?> page) {
        Map<String, Object> rspData = new HashMap<>();
        rspData.put("rows", page.getRecords());
        rspData.put("total", page.getTotal());
        return rspData;
    }

    private PageUtils() {
    }
}