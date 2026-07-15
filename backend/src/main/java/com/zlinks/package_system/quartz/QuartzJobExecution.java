package com.zlinks.package_system.quartz;

import com.zlinks.package_system.constant.ScheduleConstants;
import com.zlinks.package_system.entity.monitor.SysJob;
import com.zlinks.package_system.entity.monitor.SysJobLog;
import com.zlinks.package_system.service.monitor.ISysJobLogService;
import com.zlinks.package_system.util.SpringContextHolder;
import org.quartz.JobExecutionContext;
import org.quartz.JobExecutionException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.LocalDateTime;

/**
 * Quartz 实际执行 Job
 *
 * <p>通过 JobDataMap 接收 SysJob 调用信息, 调用 JobInvokeUtil.invokeMethod 反射执行业务方法,
 * 写入 sys_job_log 执行记录.</p>
 */
public class QuartzJobExecution implements org.quartz.Job {

    private static final Logger log = LoggerFactory.getLogger(QuartzJobExecution.class);

    @Override
    public void execute(JobExecutionContext context) throws JobExecutionException {
        SysJob sysJob = (SysJob) context.getMergedJobDataMap().get(ScheduleConstants.TASK_PROPERTIES);
        if (sysJob == null) {
            log.warn("[QuartzJob] 未找到任务参数, jobKey={}", context.getJobDetail().getKey());
            return;
        }

        // 是否并发: 1=禁止, 已存在的运行实例直接跳过
        if (ScheduleConstants.CONCURRENT_PROHIBIT.equals(sysJob.getConcurrent())
                && context.getJobDetail().getJobDataMap().getBoolean(ScheduleConstants.TASK_RUNNING)) {
            log.info("[QuartzJob] 任务 [{}] 禁止并发, 跳过本次执行", sysJob.getJobName());
            return;
        }
        context.getJobDetail().getJobDataMap().put(ScheduleConstants.TASK_RUNNING, Boolean.TRUE);

        ISysJobLogService jobLogService = SpringContextHolder.getBean(ISysJobLogService.class);
        SysJobLog jobLog = new SysJobLog();
        jobLog.setJobName(sysJob.getJobName());
        jobLog.setJobGroup(sysJob.getJobGroup());
        jobLog.setInvokeTarget(sysJob.getInvokeTarget());
        jobLog.setCronExpression(sysJob.getCronExpression());
        jobLog.setStartTime(LocalDateTime.now());
        try {
            JobInvokeUtil.JobInvokeResult result = JobInvokeUtil.invokeMethod(sysJob.getInvokeTarget());
            if (result.isSuccess()) {
                jobLog.setStatus(ScheduleConstants.NORMAL);
                jobLog.setJobMessage("任务执行成功");
            } else {
                jobLog.setStatus(ScheduleConstants.PAUSE);
                String msg = result.getError() != null ? result.getError().getMessage() : "未知异常";
                jobLog.setJobMessage("任务执行失败: " + msg);
                jobLog.setExceptionInfo(stackToString(result.getError()));
            }
        } catch (Exception e) {
            jobLog.setStatus(ScheduleConstants.PAUSE);
            jobLog.setJobMessage("任务执行异常: " + e.getMessage());
            jobLog.setExceptionInfo(stackToString(e));
            log.error("[QuartzJob] 任务 [{}] 执行异常", sysJob.getJobName(), e);
        } finally {
            jobLog.setEndTime(LocalDateTime.now());
            try {
                if (jobLogService != null) jobLogService.addJobLog(jobLog);
            } catch (Exception ignore) {
            }
            context.getJobDetail().getJobDataMap().put(ScheduleConstants.TASK_RUNNING, Boolean.FALSE);
        }
    }

    private static String stackToString(Throwable t) {
        if (t == null) return "";
        java.io.StringWriter sw = new java.io.StringWriter();
        try (java.io.PrintWriter pw = new java.io.PrintWriter(sw)) {
            t.printStackTrace(pw);
        }
        return sw.toString().length() > 2000 ? sw.toString().substring(0, 2000) : sw.toString();
    }
}