package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import com.fasterxml.jackson.annotation.JsonFormat;
import lombok.Data;
import lombok.EqualsAndHashCode;

import java.time.LocalDateTime;

/**
 * 任务分配主表(target_type=product/game/test)
 * <p>
 * - 一个 task 记录对应一对 (assignee_user_id, target_id, target_type),即"某个受派人对某个目标的某个任务"。
 * - 批量分配 N 个用户时,N 条 task 记录,每条独立。
 * - target_type='test' 时,test_payload 携带测试业务字段 (expected_result / test_environment / test_steps) 的 JSON。
 */
@Data
@EqualsAndHashCode(callSuper = true)
@TableName("task")
public class Task extends BaseEntity {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    /** 目标类型枚举:product / game / test */
    private String targetType;

    /** 目标主键 ID */
    private Long targetId;

    /** 任务标题(管理员填写) */
    private String taskTitle;

    /** 任务描述 */
    private String taskDesc;

    /** 受派人用户 ID(sys_user.user_id) */
    private Long assigneeUserId;

    /** 分配人用户 ID */
    private Long assignerUserId;

    /** 状态:TaskStatus 枚举 (Pending/Accepted/InProgress/Done/Rejected/Cancelled) */
    private String status;

    /** 角色:OWNER / WORKER */
    private String role;

    /** 截止时间(可空) */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime deadline;

    /** 接受时间(Pending → Accepted) */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime acceptedAt;

    /** 开始时间(Accepted → InProgress) */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime startedAt;

    /** 完成时间(InProgress → Done/Rejected/Cancelled) */
    @JsonFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private LocalDateTime finishedAt;

    /** 受派人 / 管理员填写的备注 */
    private String comment;

    /** target_type='test' 时填写的测试业务 JSON */
    private String testPayload;

    // ========== 虚拟字段(非持久化,联表查询返回) ==========

    @TableField(exist = false)
    private String assigneeUserName;

    @TableField(exist = false)
    private String assignerUserName;

    @TableField(exist = false)
    private String targetName;
}
