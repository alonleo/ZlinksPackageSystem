package com.zlinks.package_system.entity;

/**
 * 任务状态机(6 态)
 * <pre>
 *   Pending ──accept──► Accepted ──start──► InProgress ──done──► Done
 *      │                    │                    │
 *      └─reject──────────►  └─reject────────►  └─reject────────► Rejected
 *      └─cancel(admin)──►  └─cancel(admin)─►  └─cancel(admin)─► Cancelled
 * </pre>
 */
public enum TaskStatus {
    /** 已派发,等待受派人接受 */
    Pending,
    /** 受派人已接单 */
    Accepted,
    /** 受派人已开始处理 */
    InProgress,
    /** 完成 */
    Done,
    /** 受派人拒绝 */
    Rejected,
    /** 管理员撤销 */
    Cancelled
}
