package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("permission_group")
public class PermissionGroup extends BaseEntity {

    private String groupName;
    private String groupPermission;
    private String groupAccounts;
    private String remark;
}