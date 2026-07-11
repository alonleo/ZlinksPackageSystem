package com.zlinks.package_system.entity.system;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.zlinks.package_system.entity.BaseEntity;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.util.ArrayList;
import java.util.List;

/**
 * 菜单权限对象 sys_menu
 */
@Data
@EqualsAndHashCode(callSuper = true)
@TableName("sys_menu")
public class SysMenu extends BaseEntity {

    /** 菜单ID */
    @TableId(value = "menu_id", type = IdType.AUTO)
    private Long menuId;

    /** 菜单名称 */
    private String menuName;

    /** 父菜单ID */
    private Long parentId;

    /** 显示顺序 */
    private Integer orderNum;

    /** 路由地址 */
    private String path;

    /** 组件路径 */
    private String component;

    /** 路由参数 */
    private String query;

    /** 是否为外链 (0是 1否) */
    private String isFrame;

    /** 是否缓存 (0缓存 1不缓存) */
    private String isCache;

    /** 菜单类型 (M目录 C菜单 F按钮) */
    private String menuType;

    /** 显示状态 (0显示 1隐藏) */
    private String visible;

    /** 菜单状态 (0正常 1停用) */
    private String status;

    /** 权限标识 */
    private String perms;

    /** 菜单图标 */
    private String icon;

    /** 子菜单 */
    @TableField(exist = false)
    private List<SysMenu> children = new ArrayList<>();
}