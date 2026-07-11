package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysPost;

import java.util.List;

public interface ISysPostService extends IService<SysPost> {

    com.baomidou.mybatisplus.core.metadata.IPage<SysPost> selectPostPage(
            com.baomidou.mybatisplus.extension.plugins.pagination.Page<SysPost> page, SysPost query);

    boolean insertPost(SysPost post);

    boolean updatePost(SysPost post);
}
