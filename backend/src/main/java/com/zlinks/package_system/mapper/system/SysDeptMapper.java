package com.zlinks.package_system.mapper.system;

import com.baomidou.mybatisplus.core.conditions.Wrapper;
import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.baomidou.mybatisplus.core.toolkit.Constants;
import com.zlinks.package_system.entity.system.SysDept;
import org.apache.ibatis.annotations.Param;

import java.util.List;

/**
 * 部门 Mapper
 */
public interface SysDeptMapper extends BaseMapper<SysDept> {

    /**
     * 查询子部门数
     */
    int countChildren(@Param("deptId") Long deptId);

    /**
     * 查询部门列表 (支持 Wrapper)
     */
    List<SysDept> selectDeptList(@Param(Constants.WRAPPER) Wrapper<SysDept> queryWrapper);

    /**
     * 排除某部门的列表 (用于选择父部门时)
     */
    List<SysDept> selectDeptListExclude(@Param("excludeId") Long excludeId);
}