package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;

import java.io.Serializable;

@Data
@TableName("user_group")
public class UserGroup implements Serializable {

    private Long userId;
    private Long groupId;
}
