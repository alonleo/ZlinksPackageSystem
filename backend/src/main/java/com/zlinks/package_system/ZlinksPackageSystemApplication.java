package com.zlinks.package_system;

import org.mybatis.spring.annotation.MapperScan;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@MapperScan("com.zlinks.package_system.mapper")
@EnableAsync
@EnableScheduling
public class ZlinksPackageSystemApplication {

    public static void main(String[] args) {
        SpringApplication.run(ZlinksPackageSystemApplication.class, args);
    }
}