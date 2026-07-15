package com.zlinks.package_system.quartz;

import com.zlinks.package_system.util.SpringContextHolder;
import org.apache.commons.lang3.StringUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.Callable;

/**
 * 定时任务反射调用工具
 *
 * <p>解析 invokeTarget 字符串 (形如 "beanName.methodName('arg1','arg2')") 并通过反射执行.
 * 支持基础类型 / 字符串 / 布尔 / 长整型 / 浮点 / 整型 参数.</p>
 */
public class JobInvokeUtil {

    private static final Logger log = LoggerFactory.getLogger(JobInvokeUtil.class);

    /** Bean 名称和方法之间的分隔符 */
    private static final char SEPARATOR_BEAN = '.';
    /** 括号开始 */
    private static final char SEPARATOR_ARGS_START = '(';
    /** 括号结束 */
    private static final char SEPARATOR_ARGS_END = ')';
    /** 字符串引号 */
    private static final char SEPARATOR_QUOTE = '\'';
    /** 字符串引号 (双) */
    private static final char SEPARATOR_QUOTE_DOUBLE = '"';
    /** 参数分隔符 */
    private static final char SEPARATOR_ARGS = ',';

    /**
     * 执行方法
     *
     * @param invokeTarget 目标字符串
     * @return 执行结果 (含异常信息)
     */
    public static JobInvokeResult invokeMethod(String invokeTarget) {
        JobInvokeResult result = new JobInvokeResult();
        String beanName = getBeanName(invokeTarget);
        String methodName = getMethodName(invokeTarget);
        List<Object[]> methodParams = getMethodParams(invokeTarget);

        try {
            Object bean = SpringContextHolder.getBean(beanName);
            methodName = StringUtils.isEmpty(methodName) ? "run" : methodName;

            Class<?>[] paramTypes = null;
            Object[] args = null;
            if (methodParams != null && !methodParams.isEmpty()) {
                paramTypes = new Class<?>[methodParams.size()];
                args = new Object[methodParams.size()];
                for (int i = 0; i < methodParams.size(); i++) {
                    Object[] p = methodParams.get(i);
                    if (p.length == 1) {
                        paramTypes[i] = String.class;
                        args[i] = p[0];
                    } else if (p.length == 2 && p[0] instanceof String) {
                        String typeStr = (String) p[0];
                        String valStr = String.valueOf(p[1]);
                        switch (typeStr.toLowerCase()) {
                            case "boolean" -> { paramTypes[i] = Boolean.class; args[i] = Boolean.parseBoolean(valStr); }
                            case "long" -> { paramTypes[i] = Long.class; args[i] = Long.parseLong(valStr); }
                            case "double", "float" -> { paramTypes[i] = Double.class; args[i] = Double.parseDouble(valStr); }
                            case "int", "integer" -> { paramTypes[i] = Integer.class; args[i] = Integer.parseInt(valStr); }
                            default -> { paramTypes[i] = String.class; args[i] = valStr; }
                        }
                    } else {
                        paramTypes[i] = String.class;
                        args[i] = String.valueOf(p[0]);
                    }
                }
            } else {
                paramTypes = new Class<?>[0];
                args = new Object[0];
            }

            Method method = bean.getClass().getDeclaredMethod(methodName, paramTypes);
            method.setAccessible(true);
            Object invoke = method.invoke(bean, args);
            result.setSuccess(true);
            result.setResult(invoke);
            log.info("[JobInvoke] 执行 {}#{} 成功", beanName, methodName);
        } catch (InvocationTargetException ite) {
            Throwable cause = ite.getCause() != null ? ite.getCause() : ite;
            log.error("[JobInvoke] 执行 {}#{} 失败: {}", beanName, methodName, cause.getMessage(), cause);
            result.setSuccess(false);
            result.setError(cause);
        } catch (Exception e) {
            log.error("[JobInvoke] 执行 {}#{} 失败: {}", beanName, methodName, e.getMessage(), e);
            result.setSuccess(false);
            result.setError(e);
        }
        return result;
    }

    private static String getBeanName(String invokeTarget) {
        if (invokeTarget == null) return "";
        int dot = invokeTarget.indexOf(SEPARATOR_BEAN);
        return dot > 0 ? invokeTarget.substring(0, dot) : "";
    }

    private static String getMethodName(String invokeTarget) {
        if (invokeTarget == null) return "";
        int dot = invokeTarget.indexOf(SEPARATOR_BEAN);
        int paren = invokeTarget.indexOf(SEPARATOR_ARGS_START);
        if (dot < 0) return "";
        if (paren < 0) return invokeTarget.substring(dot + 1);
        return invokeTarget.substring(dot + 1, paren);
    }

    /**
     * 解析方法参数, 支持字符串 / boolean / long / int / double / float.
     * 单参数 (无类型前缀) 视为字符串.
     */
    private static List<Object[]> getMethodParams(String invokeTarget) {
        if (invokeTarget == null) return java.util.Collections.emptyList();
        int start = invokeTarget.indexOf(SEPARATOR_ARGS_START);
        int end = invokeTarget.lastIndexOf(SEPARATOR_ARGS_END);
        if (start < 0 || end < 0 || end <= start + 1) return java.util.Collections.emptyList();
        String paramStr = invokeTarget.substring(start + 1, end);
        List<Object[]> params = new ArrayList<>();
        int i = 0;
        int len = paramStr.length();
        StringBuilder current = new StringBuilder();
        boolean inQuote = false;
        char quoteChar = 0;
        while (i < len) {
            char c = paramStr.charAt(i);
            if (inQuote) {
                if (c == quoteChar && (i + 1 >= len || paramStr.charAt(i + 1) != quoteChar)) {
                    inQuote = false;
                } else if (c == quoteChar && paramStr.charAt(i + 1) == quoteChar) {
                    current.append(c);
                    i++;
                } else {
                    current.append(c);
                }
            } else {
                if (c == SEPARATOR_QUOTE || c == SEPARATOR_QUOTE_DOUBLE) {
                    inQuote = true;
                    quoteChar = c;
                } else if (c == SEPARATOR_ARGS) {
                    params.add(parseSingleParam(current.toString()));
                    current.setLength(0);
                } else {
                    current.append(c);
                }
            }
            i++;
        }
        if (current.length() > 0 || !params.isEmpty()) {
            params.add(parseSingleParam(current.toString()));
        }
        return params;
    }

    /**
     * 解析单个参数: 若为 type(value) 形式则返回 [type, value], 否则返回 [value].
     */
    private static Object[] parseSingleParam(String raw) {
        if (raw == null) return new Object[]{""};
        String s = raw.trim();
        int paren = s.indexOf('(');
        int parenEnd = s.lastIndexOf(')');
        if (paren > 0 && parenEnd > paren) {
            String type = s.substring(0, paren).trim();
            String val = s.substring(paren + 1, parenEnd).trim();
            // 去掉引号
            if (val.length() >= 2 && (val.charAt(0) == '\'' || val.charAt(0) == '"')
                    && val.charAt(0) == val.charAt(val.length() - 1)) {
                val = val.substring(1, val.length() - 1);
            }
            return new Object[]{type, val};
        }
        if (s.length() >= 2 && (s.charAt(0) == '\'' || s.charAt(0) == '"')
                && s.charAt(0) == s.charAt(s.length() - 1)) {
            s = s.substring(1, s.length() - 1);
        }
        return new Object[]{s};
    }

    /**
     * 调用结果封装
     */
    public static class JobInvokeResult implements Callable<JobInvokeResult> {
        private boolean success;
        private Object result;
        private Throwable error;

        public boolean isSuccess() { return success; }
        public void setSuccess(boolean success) { this.success = success; }
        public Object getResult() { return result; }
        public void setResult(Object result) { this.result = result; }
        public Throwable getError() { return error; }
        public void setError(Throwable error) { this.error = error; }

        @Override
        public JobInvokeResult call() {
            return this;
        }
    }
}