package com.zlinks.package_system.quartz;

import com.zlinks.package_system.constant.ScheduleConstants;
import com.zlinks.package_system.entity.monitor.SysJob;
import org.quartz.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Quartz 调度操作工具
 *
 * <p>封装创建 / 修改 / 暂停 / 立即执行 / 删除 Job 与 Trigger.</p>
 */
public class ScheduleUtils {

    private static final Logger log = LoggerFactory.getLogger(ScheduleUtils.class);

    /**
     * 创建 Cron 调度任务
     */
    public static void createScheduleJob(Scheduler scheduler, SysJob job) throws SchedulerException, ClassNotFoundException {
        Class<? extends Job> jobClass = getJobClass();
        Long jobId = job.getJobId();
        String jobKey = ScheduleConstants.JOB_KEY_PREFIX + jobId;

        JobDetail jobDetail = JobBuilder.newJob(jobClass)
                .withIdentity(jobKey)
                .build();
        jobDetail.getJobDataMap().put(ScheduleConstants.TASK_PROPERTIES, job);

        CronTrigger trigger = buildCronTrigger(job, jobKey);
        // 错误策略
        switch (job.getMisfirePolicy() == null ? ScheduleConstants.MISFIRE_DO_NOTHING : job.getMisfirePolicy()) {
            case ScheduleConstants.MISFIRE_IGNORE_MISFIRES -> trigger = trigger.getTriggerBuilder()
                    .withIdentity(trigger.getKey())
                    .withSchedule(CronScheduleBuilder.cronSchedule(job.getCronExpression())
                            .withMisfireHandlingInstructionIgnoreMisfires())
                    .build();
            case ScheduleConstants.MISFIRE_FIRE_AND_PROCEED -> trigger = trigger.getTriggerBuilder()
                    .withIdentity(trigger.getKey())
                    .withSchedule(CronScheduleBuilder.cronSchedule(job.getCronExpression())
                            .withMisfireHandlingInstructionFireAndProceed())
                    .build();
            default -> trigger = trigger.getTriggerBuilder()
                    .withIdentity(trigger.getKey())
                    .withSchedule(CronScheduleBuilder.cronSchedule(job.getCronExpression())
                            .withMisfireHandlingInstructionDoNothing())
                    .build();
        }
        // 是否禁止并发
        if (ScheduleConstants.CONCURRENT_PROHIBIT.equals(job.getConcurrent())) {
            jobDetail = jobDetail.getJobBuilder().storeDurably(true).build();
        }

        scheduler.scheduleJob(jobDetail, trigger);
        if (ScheduleConstants.PAUSE.equals(job.getStatus())) {
            scheduler.pauseJob(JobKey.jobKey(jobKey));
        }
        log.info("[ScheduleUtils] 注册 Quartz 任务 [jobId={}, cron={}]", jobId, job.getCronExpression());
    }

    private static CronTrigger buildCronTrigger(SysJob job, String jobKey) {
        CronScheduleBuilder cron = CronScheduleBuilder.cronSchedule(job.getCronExpression());
        return TriggerBuilder.newTrigger()
                .withIdentity(jobKey)
                .withSchedule(cron)
                .build();
    }

    /**
     * 立即执行一次
     */
    public static void runOnce(Scheduler scheduler, SysJob job) throws SchedulerException, ClassNotFoundException {
        Long jobId = job.getJobId();
        String jobKey = ScheduleConstants.JOB_KEY_PREFIX + jobId;

        JobDataMap dataMap = new JobDataMap();
        dataMap.put(ScheduleConstants.TASK_PROPERTIES, job);

        JobKey key = JobKey.jobKey(jobKey);
        if (scheduler.checkExists(key)) {
            // 已存在, 用临时 trigger 触发一次
            JobDetail jd = scheduler.getJobDetail(key);
            jd.getJobDataMap().putAll(dataMap);
            Trigger trigger = TriggerBuilder.newTrigger()
                    .withIdentity(jobKey + "_once_" + System.currentTimeMillis())
                    .forJob(jd)
                    .startNow()
                    .build();
            scheduler.scheduleJob(trigger);
        } else {
            // 不存在, 临时注册一个 detail + trigger 跑一次
            Class<? extends Job> jobClass = getJobClass();
            JobDetail jd = JobBuilder.newJob(jobClass)
                    .withIdentity(jobKey)
                    .storeDurably(false)
                    .build();
            jd.getJobDataMap().putAll(dataMap);
            Trigger trigger = TriggerBuilder.newTrigger()
                    .withIdentity(jobKey + "_once_" + System.currentTimeMillis())
                    .startNow()
                    .build();
            scheduler.scheduleJob(jd, trigger);
        }
        log.info("[ScheduleUtils] 立即执行一次 [jobId={}, jobName={}]", jobId, job.getJobName());
    }

    @SuppressWarnings("unchecked")
    private static Class<? extends Job> getJobClass() throws ClassNotFoundException {
        return (Class<? extends Job>) Class.forName(ScheduleConstants.QUARTZ_JOB_CLASS);
    }
}