package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("vivo_param")
public class VivoParam extends BaseEntity {


    /** 兼容字段 - 重新声明 id 为 PK */
    @TableId(value = "id", type = IdType.AUTO)
    private Long id;
    @TableField("product_id")
    private Long productId;
    private String appId;
    private String contractStatus;
    private String mediaId;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}