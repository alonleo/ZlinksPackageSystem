package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("game")
public class Game extends BaseEntity {

    private String gameName;
    private String gameDirection;
    private String source;
    private String gitUrl;
    private Integer priority;
    private String tags;
    private String projectType;
    private String manager;
    private String whiteBranch;
    private String status;
    private String retentionRecord;
    private String androidFolderName;
    private String remark;
}