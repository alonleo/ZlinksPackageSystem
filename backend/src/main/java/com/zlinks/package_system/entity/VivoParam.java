package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("vivo_param")
public class VivoParam extends BaseEntity {

    private Long gameId;
    private String appId;
    private String contractStatus;
    private String mediaId;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}