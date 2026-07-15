package com.zlinks.package_system.service.system.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.system.SysDept;
import com.zlinks.package_system.exception.ServiceException;
import com.zlinks.package_system.mapper.system.SysDeptMapper;
import com.zlinks.package_system.service.system.ISysDeptService;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.apache.commons.lang3.StringUtils;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class SysDeptServiceImpl extends ServiceImpl<SysDeptMapper, SysDept> implements ISysDeptService {

    @Override
    public List<SysDept> selectDeptList(SysDept query) {
LambdaQueryWrapper<SysDept> w = new LambdaQueryWrapper<>();
        if (StringUtils.isNotBlank(query.getDeptName())) w.like(SysDept::getDeptName, query.getDeptName());
        if (StringUtils.isNotBlank(query.getStatus())) w.eq(SysDept::getStatus, query.getStatus());
        return baseMapper.selectDeptList(w);
    }

    @Override
    public List<SysDept> selectDeptListExclude(Long excludeId) {
        return baseMapper.selectDeptListExclude(excludeId);
    }

    @Override
    public List<SysDept> buildDeptTree(List<SysDept> depts) {
        List<SysDept> result = new ArrayList<>();
        for (SysDept d : depts) {
            if (d.getParentId() == null || d.getParentId() == 0L) {
                d.setChildren(getChildren(depts, d.getDeptId()));
                result.add(d);
            }
        }
        return result;
    }

    private List<SysDept> getChildren(List<SysDept> all, Long parentId) {
        List<SysDept> children = new ArrayList<>();
        for (SysDept d : all) {
            if (parentId.equals(d.getParentId())) {
                d.setChildren(getChildren(all, d.getDeptId()));
                children.add(d);
            }
        }
        return children;
    }

    @Override
    public boolean insertDept(SysDept dept) {
        SysDept parent = getById(dept.getParentId());
        if (parent != null) {
            dept.setAncestors(parent.getAncestors() + "," + parent.getDeptId());
        } else {
            dept.setAncestors("0");
        }
        return save(dept);
    }

    @Override
    public boolean updateDept(SysDept dept) {
        return updateById(dept);
    }

    @Override
    public boolean removeDept(Long deptId) {
        int n = baseMapper.countChildren(deptId);
        if (n > 0) {
            throw new ServiceException("存在下级部门,不允许删除");
        }
        SysDept d = getById(deptId);
        if (d != null) {
            d.setDelFlag("2");
            return updateById(d);
        }
        return false;
    }
}