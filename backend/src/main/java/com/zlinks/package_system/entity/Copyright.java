package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("copyright")
public class Copyright extends BaseEntity {


    /** 兼容字段 - 重新声明 id 为 PK */
    @TableId(value = "id", type = IdType.AUTO)
    private Long id;
    private String copyrightName;
    private String copyrightOwner;
    private String copyrightNumber;
    private String remark;
}