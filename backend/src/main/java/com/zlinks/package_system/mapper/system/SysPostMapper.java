package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.conditions.Wrapper;
import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.baomidou.mybatisplus.core.toolkit.Constants;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.system.SysPost;
import org.apache.ibatis.annotations.Param;

/**
 * 岗位 Mapper
 */
public interface SysPostMapper extends BaseMapper<SysPost> {

    /**
     * 分页查询岗位
     */
    Page<SysPost> selectPostPage(Page<SysPost> page, @Param(Constants.WRAPPER) Wrapper<SysPost> queryWrapper);
}