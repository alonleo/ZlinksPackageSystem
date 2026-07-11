package com.zlinks.package_system.service.system;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.system.SysDept;

import java.util.List;

public interface ISysDeptService extends IService<SysDept> {

    List<SysDept> selectDeptList(SysDept query);

    List<SysDept> selectDeptListExclude(Long excludeId);

    List<SysDept> buildDeptTree(List<SysDept> depts);

    boolean insertDept(SysDept dept);

    boolean updateDept(SysDept dept);

    boolean removeDept(Long deptId);
}
