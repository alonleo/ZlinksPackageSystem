package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("product")
public class Product extends BaseEntity {

    private Long copyrightId;
    private Long gameId;
    private Long companyId;
    private String platform;
    private String packageName;
    private String sdkVersion;
    private String apkVersion;
    private String batch;
    private String packageMode;
    private String status;
    private String remark;

    @TableField(exist = false)
    private String copyrightName;

    @TableField(exist = false)
    private String gameName;

    @TableField(exist = false)
    private String companyName;
}
