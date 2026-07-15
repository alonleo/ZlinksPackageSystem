package com.zlinks.package_system.service.monitor.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.constant.ScheduleConstants;
import com.zlinks.package_system.entity.monitor.SysJob;
import com.zlinks.package_system.mapper.monitor.SysJobMapper;
import com.zlinks.package_system.quartz.ScheduleUtils;
import com.zlinks.package_system.service.monitor.ISysJobService;
import lombok.RequiredArgsConstructor;
import org.apache.commons.lang3.StringUtils;
import org.quartz.JobKey;
import org.quartz.Scheduler;
import org.quartz.SchedulerException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
public class SysJobServiceImpl implements ISysJobService {

    private final SysJobMapper jobMapper;
    private final Scheduler scheduler;

    @Override
    public Page<SysJob> selectJobPage(Page<SysJob> page, SysJob query) {
        LambdaQueryWrapper<SysJob> wrapper = new LambdaQueryWrapper<>();
        if (StringUtils.isNotBlank(query.getJobName())) {
            wrapper.like(SysJob::getJobName, query.getJobName());
        }
        if (StringUtils.isNotBlank(query.getJobGroup())) {
            wrapper.eq(SysJob::getJobGroup, query.getJobGroup());
        }
        if (StringUtils.isNotBlank(query.getStatus())) {
            wrapper.eq(SysJob::getStatus, query.getStatus());
        }
        if (StringUtils.isNotBlank(query.getInvokeTarget())) {
            wrapper.like(SysJob::getInvokeTarget, query.getInvokeTarget());
        }
        wrapper.orderByDesc(SysJob::getJobId);
        return jobMapper.selectPage(page, wrapper);
    }

    @Override
    public SysJob getById(Long jobId) {
        return jobMapper.selectById(jobId);
    }

    @Override
    public List<SysJob> listAll() {
        return jobMapper.selectList(new LambdaQueryWrapper<>());
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int insertJob(SysJob job) throws SchedulerException, ClassNotFoundException {
        int rows = jobMapper.insert(job);
        if (rows > 0 && ScheduleConstants.NORMAL.equals(job.getStatus())) {
            ScheduleUtils.createScheduleJob(scheduler, job);
        }
        return rows;
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int updateJob(SysJob job) throws SchedulerException, ClassNotFoundException {
        SysJob old = jobMapper.selectById(job.getJobId());
        if (old == null) return 0;
        int rows = jobMapper.updateById(job);
        if (rows > 0) {
            // 存在则先删除再创建
            try {
                boolean existing = scheduler.checkExists(JobKey.jobKey(
                        ScheduleConstants.JOB_KEY_PREFIX + job.getJobId()));
                if (existing) {
                    scheduler.deleteJob(JobKey.jobKey(
                            ScheduleConstants.JOB_KEY_PREFIX + job.getJobId()));
                }
            } catch (SchedulerException ignore) {}
            if (ScheduleConstants.NORMAL.equals(job.getStatus())) {
                ScheduleUtils.createScheduleJob(scheduler, job);
            }
        }
        return rows;
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int deleteJobByIds(Long[] ids) throws SchedulerException {
        int rows = 0;
        for (Long id : ids) {
            rows += deleteJobById(id);
        }
        return rows;
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int deleteJobById(Long id) throws SchedulerException {
        SysJob job = jobMapper.selectById(id);
        int rows = jobMapper.deleteById(id);
        if (rows > 0 && job != null) {
            try {
                scheduler.deleteJob(JobKey.jobKey(
                        ScheduleConstants.JOB_KEY_PREFIX + id));
            } catch (SchedulerException ignore) {}
        }
        return rows;
    }

    @Override
    @Transactional(rollbackFor = Exception.class)
    public int changeStatus(Long jobId, String status) throws SchedulerException, ClassNotFoundException {
        SysJob job = jobMapper.selectById(jobId);
        if (job == null) return 0;
        job.setStatus(status);
        int rows = jobMapper.updateById(job);
        if (rows > 0) {
            JobKey key = JobKey.jobKey(ScheduleConstants.JOB_KEY_PREFIX + jobId);
            if (ScheduleConstants.NORMAL.equals(status)) {
                // 恢复: 重新加入调度
                if (!scheduler.checkExists(key)) {
                    ScheduleUtils.createScheduleJob(scheduler, job);
                }
            } else {
                // 暂停: 移除触发器 (保留 JobDetail 以便恢复)
                if (scheduler.checkExists(key)) {
                    scheduler.pauseJob(key);
                }
            }
        }
        return rows;
    }

    @Override
    public int runJobOnce(Long jobId) throws SchedulerException, ClassNotFoundException {
        SysJob job = jobMapper.selectById(jobId);
        if (job == null) return 0;
        ScheduleUtils.runOnce(scheduler, job);
        return 1;
    }

    @Override
    public int refreshAllJobs() throws SchedulerException, ClassNotFoundException {
        // 启动时调用: 重新注册所有 status=0 的任务
        List<SysJob> jobs = jobMapper.selectList(
                new LambdaQueryWrapper<SysJob>().eq(SysJob::getStatus, ScheduleConstants.NORMAL));
        for (SysJob job : jobs) {
            JobKey key = JobKey.jobKey(ScheduleConstants.JOB_KEY_PREFIX + job.getJobId());
            if (!scheduler.checkExists(key)) {
                ScheduleUtils.createScheduleJob(scheduler, job);
            }
        }
        return jobs.size();
    }
}