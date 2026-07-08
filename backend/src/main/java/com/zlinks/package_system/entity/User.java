package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonIgnore;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.util.List;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("users")
public class User extends BaseEntity {

    private String username;

    @JsonIgnore
    private String password;

    private String realName;
    private String status;
    private String remark;

    @TableField(exist = false)
    private List<Long> groupIds;

    @TableField(exist = false)
    private List<String> groupNames;
}
