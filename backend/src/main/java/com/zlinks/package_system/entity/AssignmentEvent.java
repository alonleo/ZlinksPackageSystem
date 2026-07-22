package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;

import java.time.LocalDateTime;

/**
 * 分配事件流(append-only)
 * <p>
 * 每次 task 状态变更时插入一条;记录事件类型、操作人、状态前后值、备注。
 * 与 assignment_history(管理员操作审计)的区别:event 涵盖所有人(包括受派人)的状态机动作,
 * history 仅记录管理员对任务元数据(角色/截止时间等)的修改。
 */
@Data
@TableName("assignment_event")
public class AssignmentEvent {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    private Long taskId;

    /** 事件类型:CREATED / ACCEPTED / STARTED / DONE / REJECTED / CANCELLED / COMMENTED */
    private String eventType;

    /** 操作人 user_id */
    private Long actorUserId;

    /** 状态变更前的 status */
    private String fromStatus;

    /** 状态变更后的 status */
    private String toStatus;

    /** 备注 */
    private String comment;

    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime createTime;
}
