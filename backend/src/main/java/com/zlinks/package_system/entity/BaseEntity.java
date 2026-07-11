package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.FieldFill;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableLogic;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.io.Serializable;
import java.time.LocalDateTime;

/**
 * 实体基类
 * <p>
 * 提供审计字段 (createBy / createTime / updateBy / updateTime / remark / isDeleted).
 * <p>
 * 注意: 不再在基类上声明 @TableId("id") 以避免与子类的 @TableId(userId/roleId/...) 冲突.
 * 旧业务实体 (User/Game/...) 使用字段名 `id` 时, MyBatis-Plus 会按默认约定 (字段名 id) 识别为主键.
 * 新 RuoYi 实体使用各自专属 ID 字段 (userId/roleId/menuId/deptId/postId).
 */
@Data
public abstract class BaseEntity implements Serializable {

    private static final long serialVersionUID = 1L;

    /** 通用主键 - 旧业务实体使用. 默认为 exist=false 以避免与 Sys* 实体表冲突. 旧实体需要时, 须在本类中重新声明该字段并标注 @TableId. */
        @com.baomidou.mybatisplus.annotation.TableField(exist = false)
        private Long id;

    /** 创建者 */
    @TableField(value = "create_by", fill = FieldFill.INSERT)
    private String createBy;

    /** 创建时间 */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    @TableField(value = "create_time", fill = FieldFill.INSERT)
    private LocalDateTime createTime;

    /** 更新者 */
    @TableField(value = "update_by", fill = FieldFill.INSERT_UPDATE)
    private String updateBy;

    /** 更新时间 */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    @TableField(value = "update_time", fill = FieldFill.INSERT_UPDATE)
    private LocalDateTime updateTime;

    /** 备注 */
    @TableField(value = "remark")
    private String remark;

    /** 逻辑删除 (1删除 0未删除) */
    @TableLogic
    @TableField(value = "is_deleted")
    private Integer isDeleted;
}