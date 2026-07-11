package com.zlinks.package_system.entity.system;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.zlinks.package_system.entity.BaseEntity;
import lombok.Data;
import lombok.EqualsAndHashCode;

/**
 * 岗位对象 sys_post
 */
@Data
@EqualsAndHashCode(callSuper = true)
@TableName("sys_post")
public class SysPost extends BaseEntity {

    /** 岗位ID */
    @TableId(value = "post_id", type = IdType.AUTO)
    private Long postId;

    /** 岗位编码 */
    private String postCode;

    /** 岗位名称 */
    private String postName;

    /** 岗位排序 */
    private Integer postSort;

    /** 状态 (0正常 1停用) */
    private String status;
}