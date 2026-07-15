package com.zlinks.package_system.service.monitor;

import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.entity.monitor.SysJob;
import org.quartz.SchedulerException;

import java.util.List;

public interface ISysJobService {

    Page<SysJob> selectJobPage(Page<SysJob> page, SysJob query);

    SysJob getById(Long jobId);

    List<SysJob> listAll();

    int insertJob(SysJob job) throws SchedulerException, ClassNotFoundException;

    int updateJob(SysJob job) throws SchedulerException, ClassNotFoundException;

    int deleteJobByIds(Long[] ids) throws SchedulerException;

    int deleteJobById(Long id) throws SchedulerException;

    int changeStatus(Long jobId, String status) throws SchedulerException, ClassNotFoundException;

    int runJobOnce(Long jobId) throws SchedulerException, ClassNotFoundException;

    int refreshAllJobs() throws SchedulerException, ClassNotFoundException;
}