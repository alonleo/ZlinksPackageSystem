package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.time.LocalDateTime;

/**
 * 管理员操作审计快照
 * <p>
 * 管理员对 task 进行的非状态机类修改(角色变更 / 截止时间变更 / 重派 / 取消分配 / 创建等)留痕,
 * 包含 before/after 完整 JSON 快照便于审计追溯。
 */
@Data
@TableName("assignment_history")
public class AssignmentHistory {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    private Long taskId;

    /** CREATE / UPDATE / REASSIGN / DEADLINE_CHANGE / ROLE_CHANGE / CANCEL */
    private String actionType;

    /** 操作人 user_id(应为管理员组) */
    private Long operatorUserId;

    /** 操作前快照 JSON */
    private String beforeSnapshot;

    /** 操作后快照 JSON */
    private String afterSnapshot;

    private String comment;

    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime createTime;
}
