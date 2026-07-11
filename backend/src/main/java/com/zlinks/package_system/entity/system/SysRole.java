package com.zlinks.package_system.entity.system;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.zlinks.package_system.entity.BaseEntity;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.util.List;

/**
 * 角色对象 sys_role
 */
@Data
@EqualsAndHashCode(callSuper = true)
@TableName("sys_role")
public class SysRole extends BaseEntity {

    /** 角色ID */
    @TableId(value = "role_id", type = IdType.AUTO)
    private Long roleId;

    /** 角色名称 */
    private String roleName;

    /** 角色权限字符串 */
    private String roleKey;

    /** 显示顺序 */
    private Integer roleSort;

    /** 数据范围 (1全部 2自定义 3本部门 4本部门及以下 5仅本人) */
    private String dataScope;

    /** 菜单树选择项是否关联显示 */
    private Integer menuCheckStrictly;

    /** 部门树选择项是否关联显示 */
    private Integer deptCheckStrictly;

    /** 角色状态 (0正常 1停用) */
    private String status;

    /** 删除标志 (0代表存在) */
    private String delFlag;

    /** 用户是否存在此角色标识 默认不存在 */
    @TableField(exist = false)
    private boolean flag = false;

    /** 菜单组 */
    @TableField(exist = false)
    private Long[] menuIds;

    /** 部门组 (数据权限) */
    @TableField(exist = false)
    private Long[] deptIds;
}