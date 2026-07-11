package com.zlinks.package_system.aspect;

import cn.hutool.core.util.StrUtil;
import com.zlinks.package_system.annotation.Log;
import com.zlinks.package_system.entity.monitor.SysOperLog;
import com.zlinks.package_system.mapper.monitor.SysOperLogMapper;
import com.zlinks.package_system.util.SecurityUtils;
import com.zlinks.package_system.util.ServletUtils;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.aspectj.lang.JoinPoint;
import org.aspectj.lang.annotation.AfterReturning;
import org.aspectj.lang.annotation.AfterThrowing;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.annotation.Pointcut;
import org.aspectj.lang.reflect.MethodSignature;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.lang.reflect.Method;
import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

@Slf4j
@Aspect
@Component
@RequiredArgsConstructor
public class LogAspect {

    private final SysOperLogMapper operLogMapper;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Pointcut("@annotation(com.zlinks.package_system.annotation.Log)")
    public void logPointcut() {}

    @AfterReturning(pointcut = "logPointcut() && @annotation(logAnno)", returning = "result")
    public void doAfterReturning(JoinPoint jp, Log logAnno, Object result) {
        handleLog(jp, logAnno, result, null);
    }

    @AfterThrowing(pointcut = "logPointcut() && @annotation(logAnno)", throwing = "ex")
    public void doAfterThrowing(JoinPoint jp, Log logAnno, Throwable ex) {
        handleLog(jp, logAnno, null, ex);
    }

    protected void handleLog(JoinPoint jp, Log logAnno, Object result, Throwable ex) {
        try {
            SysOperLog operLog = new SysOperLog();
            operLog.setStatus(ex == null ? 0 : 1);
            operLog.setTitle(logAnno.title());
            operLog.setBusinessType(logAnno.businessType().getCode());
            operLog.setOperatorType(logAnno.operatorType().getCode());

            MethodSignature sig = (MethodSignature) jp.getSignature();
            Method method = sig.getMethod();
            operLog.setMethod(method.getDeclaringClass().getName() + "." + method.getName());

            HttpServletRequest req = ServletUtils.getRequest();
            if (req != null) {
                operLog.setRequestMethod(req.getMethod());
                operLog.setOperUrl(req.getRequestURI());
                operLog.setOperIp(ServletUtils.getClientIp());
            }

            operLog.setOperName(SecurityUtils.getUsername());

            if (logAnno.isSaveRequestData() && req != null) {
                Map<String, String[]> params = req.getParameterMap();
                operLog.setOperParam(StrUtil.sub(serialize(params), 0, 2000));
            }
            if (logAnno.isSaveResponseData() && result != null) {
                operLog.setJsonResult(StrUtil.sub(serialize(result), 0, 2000));
            }
            if (ex != null) {
                operLog.setErrorMsg(StrUtil.sub(ex.getMessage(), 0, 2000));
            }
            operLog.setOperTime(LocalDateTime.now());
            saveAsync(operLog);
        } catch (Exception e) {
            log.warn("LogAspect 记录日志失败: {}", e.getMessage());
        }
    }

    @Async
    public void saveAsync(SysOperLog logEntry) {
        try {
            operLogMapper.insert(logEntry);
        } catch (Exception e) {
            log.warn("异步写入操作日志失败: {}", e.getMessage());
        }
    }

    private String serialize(Object obj) {
        try {
            return objectMapper.writeValueAsString(obj);
        } catch (Exception e) {
            return String.valueOf(obj);
        }
    }
}
