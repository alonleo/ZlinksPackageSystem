package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("huawei_param")
public class HuaweiParam extends BaseEntity {

    private Long gameId;
    private String packageName;
    private String appId;
    private String agconnectPath;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}