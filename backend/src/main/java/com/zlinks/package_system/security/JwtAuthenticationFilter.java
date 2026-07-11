package com.zlinks.package_system.security;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.zlinks.package_system.constant.CacheConstants;
import com.zlinks.package_system.util.JwtUtil;
import com.zlinks.package_system.util.RedisUtils;
import com.zlinks.package_system.util.Result;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.MediaType;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

/**
 * JWT 过滤器: 解析 token, 从 Redis 加载 LoginUser, 设置到 SecurityContext
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class JwtAuthenticationFilter extends OncePerRequestFilter {

    private final JwtUtil jwtUtil;
    private final UserDetailsService userDetailsService;
    private final RedisUtils redisUtils;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {
        String token = getTokenFromRequest(request);
        if (!StringUtils.hasText(token) || !jwtUtil.validateToken(token)) {
            filterChain.doFilter(request, response);
            return;
        }

        // 先尝试从 Redis 读取 LoginUser, 命中则直接恢复 SecurityContext
        String redisKey = CacheConstants.LOGIN_TOKEN_KEY + token;
        Object cached;
        try {
            cached = redisUtils.get(redisKey);
        } catch (Exception e) {
            log.warn("Redis 读取失败, 退化为 UserDetailsService 加载: {}", e.getMessage());
            cached = null;
        }

        if (cached instanceof LoginUser loginUser) {
            try {
                UserDetails userDetails = userDetailsService.loadUserByUsername(loginUser.getUserName());
                UsernamePasswordAuthenticationToken auth = new UsernamePasswordAuthenticationToken(
                        userDetails, null, userDetails.getAuthorities());
                auth.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));
                SecurityContextHolder.getContext().setAuthentication(auth);
            } catch (Exception e) {
                log.warn("重新加载 UserDetails 失败: {}", e.getMessage());
                // 写入 401
                writeUnauthorized(response, "认证信息已失效");
                return;
            }
        } else {
            // Redis 中没有, 重新加载一次 (登录后第一次访问)
            try {
                String username = jwtUtil.getUsernameFromToken(token);
                UserDetails userDetails = userDetailsService.loadUserByUsername(username);
                UsernamePasswordAuthenticationToken auth = new UsernamePasswordAuthenticationToken(
                        userDetails, null, userDetails.getAuthorities());
                auth.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));
                SecurityContextHolder.getContext().setAuthentication(auth);
            } catch (Exception e) {
                log.warn("JWT 解析失败或用户不存在: {}", e.getMessage());
                writeUnauthorized(response, "登录已过期");
                return;
            }
        }

        filterChain.doFilter(request, response);
    }

    private String getTokenFromRequest(HttpServletRequest request) {
        String bearerToken = request.getHeader("Authorization");
        if (StringUtils.hasText(bearerToken) && bearerToken.startsWith("Bearer ")) {
            return bearerToken.substring(7);
        }
        return null;
    }

    private void writeUnauthorized(HttpServletResponse response, String message) throws IOException {
        response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        response.setCharacterEncoding("UTF-8");
        Result<Void> result = Result.error(401, message);
        response.getWriter().write(objectMapper.writeValueAsString(result));
    }
}