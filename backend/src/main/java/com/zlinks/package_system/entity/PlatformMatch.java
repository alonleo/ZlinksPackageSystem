package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("platform_match")
public class PlatformMatch extends BaseEntity {


    /** 兼容字段 - 重新声明 id 为 PK */
    @TableId(value = "id", type = IdType.AUTO)
    private Long id;
    private Long companyId;
    private String originalGame;
    private String currentGameName;
    private String batch;
    private String packageMode;
    private String sdkVersion;
    private String apkVersion;
    private String platformStatus;
    private String remark;
}