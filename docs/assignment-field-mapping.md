# 任务分配模块字段对照矩阵（PR1）

> 本文档覆盖 PR1 引入的四张新表在三端的字段映射。
> `snake_case (DB) → camelCase (Java) → camelCase (TS) → PascalCase (C#)`
> 三套 DB 脚本：`backend/src/main/resources/schema.sql` (H2) · `scripts/init-database-pgsql.sql` (PG) · `scripts/init-database.sql` (MySQL)

---

## 1. `task` 任务主表

| DB 列 | DB 类型 (H2/PG/MySQL) | Java 字段 | Java 类型 | TS 字段 | TS 类型 | C# 字段 | C# 类型 | 备注 |
|---|---|---|---|---|---|---|---|---|
| `id` | BIGINT AUTO_INCREMENT/IDENTITY | `id` | `Long` | `id` | `number` | `Id` | `long` | 主键 |
| `target_type` | VARCHAR(16) | `targetType` | `String` | `targetType` | `'product' \| 'game' \| 'test'` | `TargetType` | `string` | 枚举:TaskTargetType |
| `target_id` | BIGINT | `targetId` | `Long` | `targetId` | `number` | `TargetId` | `long` | 目标主键 |
| `task_title` | VARCHAR(200) | `taskTitle` | `String` | `taskTitle` | `string` | `TaskTitle` | `string` | — |
| `task_desc` | VARCHAR(2000) | `taskDesc` | `String` | `taskDesc` | `string \| null` | `TaskDesc` | `string` | — |
| `assignee_user_id` | BIGINT | `assigneeUserId` | `Long` | `assigneeUserId` | `number` | `AssigneeUserId` | `long` | sys_user.user_id |
| `assigner_user_id` | BIGINT | `assignerUserId` | `Long` | `assignerUserId` | `number` | `AssignerUserId` | `long` | sys_user.user_id |
| `status` | VARCHAR(16) | `status` | `String` | `status` | `TaskStatus` 枚举 | `Status` | `string` | 6 态 |
| `role` | VARCHAR(16) | `role` | `String` | `role` | `TaskRole` 枚举 | `Role` | `string` | OWNER/WORKER |
| `deadline` | TIMESTAMP | `deadline` | `LocalDateTime` | `deadline` | `string \| null` | `Deadline` | `DateTime?` | 可空 |
| `accepted_at` | TIMESTAMP | `acceptedAt` | `LocalDateTime` | `acceptedAt` | `string \| null` | `AcceptedAt` | `DateTime?` | 可空 |
| `started_at` | TIMESTAMP | `startedAt` | `LocalDateTime` | `startedAt` | `string \| null` | `StartedAt` | `DateTime?` | 可空 |
| `finished_at` | TIMESTAMP | `finishedAt` | `LocalDateTime` | `finishedAt` | `string \| null` | `FinishedAt` | `DateTime?` | 可空 |
| `comment` | VARCHAR(2000) | `comment` | `String` | `comment` | `string \| null` | `Comment` | `string` | — |
| `test_payload` | CLOB/TEXT/TEXT | `testPayload` | `String` | `testPayload` | `string \| null` | `TestPayload` | `string` | target_type=test 时填写 |
| `create_by` | VARCHAR(64) | `createBy` | `String` | `createBy` | `string` | `CreateBy` | `string` | BaseEntity |
| `create_time` | TIMESTAMP | `createTime` | `LocalDateTime` | `createTime` | `string` | `CreateTime` | `DateTime` | BaseEntity |
| `update_by` | VARCHAR(64) | `updateBy` | `String` | `updateBy` | `string` | `UpdateBy` | `string` | BaseEntity |
| `update_time` | TIMESTAMP | `updateTime` | `LocalDateTime` | `updateTime` | `string` | `UpdateTime` | `DateTime` | BaseEntity |
| `is_deleted` | TINYINT/SMALLINT/TINYINT(1) | `isDeleted` | `Integer` | `isDeleted` | `number` | `IsDeleted` | `int` | @TableLogic |
| (虚拟) | — | `assigneeUserName` | `String` | `assigneeUserName` | `string \| null` | `AssigneeUserName` | `string` | 联表 sys_user |
| (虚拟) | — | `assignerUserName` | `String` | `assignerUserName` | `string \| null` | `AssignerUserName` | `string` | 联表 sys_user |
| (虚拟) | — | `targetName` | `String` | `targetName` | `string \| null` | `TargetName` | `string` | 联表 product/game |

> **索引**: `idx_task_assignee(assignee_user_id,status,is_deleted)` · `idx_task_assigner(assigner_user_id,status,is_deleted)` · `idx_task_target(target_type,target_id)`

---

## 2. `notification_recipient` 通知收件箱

| DB 列 | Java 字段 | TS 字段 | C# 字段 | 备注 |
|---|---|---|---|---|
| `id` | `id` | `id` | `Id` | 主键 |
| `notification_id` | `notificationId` | `notificationId` | `NotificationId` | FK notification.id |
| `user_id` | `userId` | `userId` | `UserId` | sys_user.user_id |
| `is_read` | `isRead` | `isRead` | `IsRead` | 0/1 |
| `read_at` | `readAt` | `readAt` | `ReadAt` | 可空 |
| `create_time` | `createTime` | `createTime` | `CreateTime` | — |

> **唯一索引**: `uq_notif_user(notification_id,user_id)`
> **索引**: `idx_recipient_user(user_id,is_read)`

---

## 3. `assignment_event` 分配事件流（append-only）

| DB 列 | Java 字段 | TS 字段 | C# 字段 | 备注 |
|---|---|---|---|---|
| `id` | `id` | `id` | `Id` | 主键 |
| `task_id` | `taskId` | `taskId` | `TaskId` | FK task.id |
| `event_type` | `eventType` | `eventType` | `EventType` | CREATED/ACCEPTED/STARTED/DONE/REJECTED/CANCELLED/COMMENTED |
| `actor_user_id` | `actorUserId` | `actorUserId` | `ActorUserId` | 操作人 |
| `from_status` | `fromStatus` | `fromStatus` | `FromStatus` | 可空 |
| `to_status` | `toStatus` | `toStatus` | `ToStatus` | 可空 |
| `comment` | `comment` | `comment` | `Comment` | 可空 |
| `create_time` | `createTime` | `createTime` | `CreateTime` | — |

> **索引**: `idx_event_task(task_id,create_time)`

---

## 4. `assignment_history` 管理员审计快照

| DB 列 | Java 字段 | TS 字段 | C# 字段 | 备注 |
|---|---|---|---|---|
| `id` | `id` | `id` | `Id` | 主键 |
| `task_id` | `taskId` | `taskId` | `TaskId` | FK task.id |
| `action_type` | `actionType` | `actionType` | `ActionType` | CREATE/UPDATE/REASSIGN/DEADLINE_CHANGE/ROLE_CHANGE/CANCEL |
| `operator_user_id` | `operatorUserId` | `operatorUserId` | `OperatorUserId` | 操作人(管理员) |
| `before_snapshot` | `beforeSnapshot` | `beforeSnapshot` | `BeforeSnapshot` | JSON |
| `after_snapshot` | `afterSnapshot` | `afterSnapshot` | `AfterSnapshot` | JSON |
| `comment` | `comment` | `comment` | `Comment` | — |
| `create_time` | `createTime` | `createTime` | `CreateTime` | — |

> **索引**: `idx_history_task(task_id,create_time)` · `idx_history_operator(operator_user_id,create_time)`

---

## 5. 6 态状态机常量

```
Pending → Accepted → InProgress → Done
   │         │           │
   ├─→ Rejected ←────────┤
   └─→ Cancelled (admin) ┘
```

| 状态 | Java 枚举 | TS 字面量 | C# 字面量 |
|---|---|---|---|
| 已派发 | `TaskStatus.Pending` | `'Pending'` | `"Pending"` |
| 已接受 | `TaskStatus.Accepted` | `'Accepted'` | `"Accepted"` |
| 进行中 | `TaskStatus.InProgress` | `'InProgress'` | `"InProgress"` |
| 已完成 | `TaskStatus.Done` | `'Done'` | `"Done"` |
| 已拒绝 | `TaskStatus.Rejected` | `'Rejected'` | `"Rejected"` |
| 已取消 | `TaskStatus.Cancelled` | `'Cancelled'` | `"Cancelled"` |

---

## 6. 历史问题修复（PR1 同步完成）

| ID | 修复点 | 文件 |
|---|---|---|
| F1 | PG/MySQL 补全 `sys_job` / `sys_job_log`（H2 已有，PG/MySQL 缺） | `scripts/init-database-pgsql.sql:619-660` · `scripts/init-database.sql:585-619` |
| F2 | 校验：现有 17 张表在 PG/MySQL 脚本中均已含 `create_by`/`update_by` 列（**已 OK，无需 ALTER**） | — |
| F5 | 本次新增 `notification_recipient` 表 + Service 骨架，为 PR2 修复 receiverType='user'/'group' 分发提供基础设施 | `entity/NotificationRecipient.java` · `mapper/NotificationRecipientMapper.java` |
| F7 | 桌面端 `User.Id int→long` 修复在 PR6 进行 | — |

---

## 7. 待 PR2 完成的接口契约占位

PR1 仅落表 + 实体骨架 + Mapper + Service 接口。以下 API 将在 PR2 完整实现：

| 路径 | 方法 | 入参 | 返回 |
|---|---|---|---|
| `POST /api/tasks` | POST | `{ targetType, targetId, assigneeUserIds[], role, deadline?, taskTitle, taskDesc? }` | `Result<List<TaskDTO>>` |
| `GET /api/tasks` | GET | `?targetType&targetId&assigneeUserId&status&current&size` | `Result<PageResult<TaskDTO>>` |
| `GET /api/tasks/{id}` | GET | — | `Result<TaskDTO>` |
| `PUT /api/tasks/{id}` | PUT | `{ role?, deadline?, taskTitle?, taskDesc? }` | `Result<TaskDTO>` |
| `DELETE /api/tasks/{id}` | DELETE | `{ comment? }` | `Result<Void>` |
| `GET /api/me/tasks` | GET | `?status&targetType&current&size` | `Result<PageResult<TaskDTO>>` |
| `POST /api/me/tasks/{id}/accept` | POST | `{ comment? }` | `Result<Void>` |
| `POST /api/me/tasks/{id}/start` | POST | — | `Result<Void>` |
| `POST /api/me/tasks/{id}/done` | POST | `{ comment?, actualResult? }` | `Result<Void>` |
| `POST /api/me/tasks/{id}/reject` | POST | `{ comment? }` | `Result<Void>` |
| `GET /api/notifications/mine` | GET | `?unreadOnly&current&size` | `Result<PageResult<NotificationWithReadFlag>>` |
| `GET /api/notifications/unread-count` | GET | — | `Result<Long>` |
| `POST /api/notifications/{id}/read` | POST | — | `Result<Void>` |
| `POST /api/notifications/read-all` | POST | — | `Result<Void>` |
