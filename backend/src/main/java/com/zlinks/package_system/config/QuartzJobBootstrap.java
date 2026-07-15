package com.zlinks.package_system.config;

import com.zlinks.package_system.service.monitor.ISysJobService;
import com.zlinks.package_system.util.SpringContextHolder;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;

/**
 * 启动时加载所有 status=0 的定时任务到 Quartz 调度器
 */
@Slf4j
@Component
@Order(2) // DataInitializer @Order(1) 先初始化 RBAC 数据
public class QuartzJobBootstrap implements CommandLineRunner {

    @Override
    public void run(String... args) {
        try {
            ISysJobService jobService = SpringContextHolder.getBean(ISysJobService.class);
            int n = jobService.refreshAllJobs();
            log.info("[QuartzJobBootstrap] 已加载 {} 个定时任务", n);
        } catch (Exception e) {
            log.error("[QuartzJobBootstrap] 加载定时任务失败", e);
        }
    }
}