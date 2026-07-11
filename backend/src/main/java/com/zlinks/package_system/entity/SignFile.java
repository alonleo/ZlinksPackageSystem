package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("sign_file")
public class SignFile extends BaseEntity {


    /** 兼容字段 - 重新声明 id 为 PK */
    @TableId(value = "id", type = IdType.AUTO)
    private Long id;
    private Long companyId;
    private String storeFile;
    private String storePassword;
    private String keyAlias;
    private String remark;

    @TableField(exist = false)
    private String companyName;
}