package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.util.ArrayList;
import java.util.List;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("permission_scope")
public class PermissionScope extends BaseEntity {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    @TableField("group_id")
    private Long groupId;

    private String scope;

    @TableField("modules_text")
    private String modulesText;

    /**
     * 派生字段:从 modulesText JSON 字符串解析出的模块列表
     * 不持久化,仅用于 API 响应输出
     */
    @TableField(exist = false)
    private List<String> modules = new ArrayList<>();
}
