package com.zlinks.package_system.constant;

/**
 * 任务调度通用常量
 *
 * <p>参考 RuoYi ScheduleConstants, 用于定义任务状态 / 策略等枚举字符串值.</p>
 */
public class ScheduleConstants {

    /** 任务正常状态 */
    public static final String NORMAL = "0";

    /** 任务暂停状态 */
    public static final String PAUSE = "1";

    /** 默认任务分组 */
    public static final String DEFAULT_JOB_GROUP = "DEFAULT";

    /** 立即执行一次 (并发) */
    public static final String MISFIRE_IGNORE_MISFIRES = "1";

    /** 立即执行一次 (单次) */
    public static final String MISFIRE_FIRE_AND_PROCEED = "2";

    /** 放弃执行 */
    public static final String MISFIRE_DO_NOTHING = "3";

    /** 允许并发 */
    public static final String CONCURRENT_ALLOW = "0";

    /** 禁止并发 */
    public static final String CONCURRENT_PROHIBIT = "1";

    /** Quartz Job 类名 */
    public static final String QUARTZ_JOB_CLASS = "com.zlinks.package_system.quartz.QuartzJobExecution";

    /** Quartz Trigger 持久化键前缀 */
    public static final String TRIGGER_KEY_PREFIX = "TASK_";

    /** Quartz Job 持久化键前缀 */
    public static final String JOB_KEY_PREFIX = "TASK_";

    /** JobDataMap 中任务参数 key */
    public static final String TASK_PROPERTIES = "TASK_PROPERTIES";

    /** JobDataMap 中并发标志 key */
    public static final String TASK_RUNNING = "TASK_RUNNING";
}