package com.zlinks.package_system.controller.system;

import com.baomidou.mybatisplus.core.metadata.IPage;
import com.zlinks.package_system.util.PageUtils;
import com.zlinks.package_system.util.ServletUtils;

import java.util.Map;

/**
 * Controller 基础类, 提供 startPage + getDataTable (RuoYi 风格)
 */
public class BaseController {

    /**
     * 读取当前请求的 pageNum / pageSize, 构造 MyBatis-Plus 分页对象
     */
    protected <T> com.baomidou.mybatisplus.extension.plugins.pagination.Page<T> startPage() {
        int pageNum = 1;
        int pageSize = 10;
        if (ServletUtils.getRequest() != null) {
            String pn = ServletUtils.getRequest().getParameter("pageNum");
            String ps = ServletUtils.getRequest().getParameter("pageSize");
            if (pn != null && !pn.isEmpty()) pageNum = Integer.parseInt(pn);
            if (ps != null && !ps.isEmpty()) pageSize = Integer.parseInt(ps);
        }
        return com.baomidou.mybatisplus.extension.plugins.pagination.Page.of(pageNum, pageSize);
    }

    /**
     * 响应分页数据 (RuoYi 兼容: { rows, total })
     */
    protected Map<String, Object> getDataTable(IPage<?> page) {
        return PageUtils.build(page);
    }
}