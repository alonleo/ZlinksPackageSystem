package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("platform_match")
public class PlatformMatch extends BaseEntity {

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