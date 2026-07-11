package com.zlinks.package_system.entity.system;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableLogic;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.zlinks.package_system.entity.BaseEntity;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.time.LocalDateTime;
import java.util.List;

/**
 * 用户对象 sys_user
 */
@Data
@EqualsAndHashCode(callSuper = true)
@TableName("sys_user")
public class SysUser extends BaseEntity {

    /** 用户ID */
    @TableId(value = "user_id", type = IdType.AUTO)
    private Long userId;

    /** 部门ID */
    private Long deptId;

    /** 登录账号 */
    private String userName;

    /** 用户昵称 */
    private String nickName;

    /** 用户邮箱 */
    private String email;

    /** 手机号码 */
    private String phonenumber;

    /** 用户性别 (0男 1女 2未知) */
    private String sex;

    /** 头像地址 */
    private String avatar;

    /** 密码 (不返回) */
    @JsonIgnore
    private String password;

    /** 帐号状态 (0正常 1停用) */
    private String status;

    /** 最后登录IP */
    private String loginIp;

    /** 最后登录时间 */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime loginDate;

    /** 部门对象 */
    @TableField(exist = false)
    private Object dept;

    /** 角色对象列表 */
    @TableField(exist = false)
    private List<Object> roles;

    /** 角色 ID 组 */
    @TableField(exist = false)
    private Long[] roleIds;

    /** 岗位 ID 组 */
    @TableField(exist = false)
    private Long[] postIds;
}