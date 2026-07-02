package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("copyright")
public class Copyright extends BaseEntity {

    private String copyrightName;
    private String copyrightOwner;
    private String copyrightNumber;
    private String remark;
}