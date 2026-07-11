package com.zlinks.package_system.controller.monitor;

import com.zlinks.package_system.util.Result;
import lombok.RequiredArgsConstructor;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import oshi.SystemInfo;
import oshi.hardware.CentralProcessor;
import oshi.hardware.GlobalMemory;
import oshi.hardware.HardwareAbstractionLayer;
import oshi.hardware.VirtualMemory;
import oshi.software.os.FileSystem;
import oshi.software.os.OSFileStore;
import oshi.software.os.OperatingSystem;

import java.lang.management.ManagementFactory;
import java.lang.management.MemoryMXBean;
import java.lang.management.MemoryUsage;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Properties;

/**
 * 服务监控 - OSHI 采集 CPU/内存/磁盘/JVM
 */
@RestController
@RequestMapping("/api/monitor/server")
@RequiredArgsConstructor
public class ServerController {

    @PreAuthorize("@ss.hasPermi('monitor:server:list')")
    @GetMapping
    public Result<Map<String, Object>> getInfo() {
        SystemInfo si = new SystemInfo();
        OperatingSystem os = si.getOperatingSystem();
        HardwareAbstractionLayer hal = si.getHardware();

        Map<String, Object> cpu = new LinkedHashMap<>();
        CentralProcessor processor = hal.getProcessor();
        long[] prevTicks = processor.getSystemCpuLoadTicks();
        try { Thread.sleep(1000); } catch (InterruptedException ignored) {}
        double cpuLoad = processor.getSystemCpuLoadBetweenTicks(prevTicks) * 100;
        cpu.put("cpuNum", processor.getLogicalProcessorCount());
        cpu.put("used", Math.round(cpuLoad * 100.0) / 100.0);
        cpu.put("sys", Math.round(cpuLoad * 100.0) / 100.0);
        cpu.put("free", Math.round((100 - cpuLoad) * 100.0) / 100.0);

        Map<String, Object> mem = new LinkedHashMap<>();
        GlobalMemory memory = hal.getMemory();
        long totalMem = memory.getTotal();
        long availMem = memory.getAvailable();
        long usedMem = totalMem - availMem;
        mem.put("total", toGb(totalMem));
        mem.put("used", toGb(usedMem));
        mem.put("free", toGb(availMem));
        mem.put("usage", Math.round(usedMem * 10000.0 / totalMem) / 100.0);

        Map<String, Object> jvm = new LinkedHashMap<>();
        MemoryMXBean memoryMBean = ManagementFactory.getMemoryMXBean();
        MemoryUsage heap = memoryMBean.getHeapMemoryUsage();
        MemoryUsage nonHeap = memoryMBean.getNonHeapMemoryUsage();
        long max = heap.getMax();
        long usedHeap = heap.getUsed();
        jvm.put("total", toMb(max));
        jvm.put("max", toMb(max));
        jvm.put("used", toMb(usedHeap));
        jvm.put("free", toMb(max - usedHeap));
        jvm.put("usage", Math.round(usedHeap * 10000.0 / max) / 100.0);
        jvm.put("name", ManagementFactory.getRuntimeMXBean().getVmName());
        jvm.put("version", System.getProperty("java.version"));
        jvm.put("startTime", ManagementFactory.getRuntimeMXBean().getStartTime());
        jvm.put("runTime", ManagementFactory.getRuntimeMXBean().getUptime());
        Properties props = System.getProperties();
        jvm.put("home", props.getProperty("java.home"));
        jvm.put("nonheapTotal", toMb(nonHeap.getInit()));
        jvm.put("nonheapUsed", toMb(nonHeap.getUsed()));

        Map<String, Object> sys = new LinkedHashMap<>();
        sys.put("computerName", os.getNetworkParams().getHostName());
                sys.put("computerIp", getLocalIp());
        sys.put("osName", props.getProperty("os.name"));
        sys.put("osArch", props.getProperty("os.arch"));
        sys.put("userDir", props.getProperty("user.dir"));
        sys.put("userHome", props.getProperty("user.home"));
        sys.put("osVersion", os.getVersionInfo().getVersion());

        List<Map<String, Object>> sysFiles = new ArrayList<>();
        FileSystem fs = os.getFileSystem();
        for (OSFileStore store : fs.getFileStores()) {
            long free = store.getUsableSpace();
            long total = store.getTotalSpace();
            long used = total - free;
            Map<String, Object> sf = new LinkedHashMap<>();
            sf.put("dirName", store.getMount());
            sf.put("sysTypeName", store.getType());
            sf.put("typeName", store.getName());
            sf.put("total", toGb(total));
            sf.put("free", toGb(free));
            sf.put("used", toGb(used));
            sf.put("usage", total == 0 ? 0 : Math.round(used * 10000.0 / total) / 100.0);
            sysFiles.add(sf);
        }

        Map<String, Object> rsp = new LinkedHashMap<>();
        rsp.put("cpu", cpu);
        rsp.put("mem", mem);
        rsp.put("jvm", jvm);
        rsp.put("sys", sys);
        rsp.put("sysFiles", sysFiles);
        return Result.success(rsp);
    }

    private static double toGb(long bytes) {
        return Math.round(bytes / 1024.0 / 1024.0 / 1024.0 * 100.0) / 100.0;
    }

    private static long toMb(long bytes) {
            return bytes / 1024 / 1024;
        }

        private static String getLocalIp() {
            try {
                return java.net.NetworkInterface.getNetworkInterfaces()
                        .nextElement().getInetAddresses().nextElement().getHostAddress();
            } catch (Exception e) {
                return "127.0.0.1";
            }
        }
}