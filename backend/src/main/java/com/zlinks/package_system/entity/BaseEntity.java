package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.FieldFill;
import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableLogic;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.io.Serializable;
import java.time.LocalDateTime;

/**
 * 实体基类
 * <p>
 * 兼容旧业务实体 (User/Game/Product/...): 通用 id + is_deleted
 * 兼容新 RuoYi 实体 (SysUser/SysRole/...): 业务 id 由各实体自身管理 (userId/roleId/...)
 */
@Data
public abstract class BaseEntity implements Serializable {

    private static final long serialVersionUID = 1L;

    /** 通用主键 - 旧业务实体使用 (新 RuoYi 实体不使用此字段, 由自身 userId/roleId 等替代) */
    @TableId(value = "id", type = IdType.AUTO)
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