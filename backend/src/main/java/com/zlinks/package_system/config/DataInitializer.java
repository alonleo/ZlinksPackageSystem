package com.zlinks.package_system.config;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.constant.UserConstants;
import com.zlinks.package_system.entity.system.*;
import com.zlinks.package_system.service.system.*;
import com.zlinks.package_system.util.SecurityUtils;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * 系统初始化 - RuoYi RBAC
 * <p>
 * 启动时若无任何 sys_user, 创建默认:
 * - 部门: 集团总公司 / 研发部门 / 测试部门 / 运维部门
 * - 岗位: 董事长 / 研发工程师 / 测试工程师 / 运维工程师
 * - 菜单: 系统管理 (用户/角色/菜单/部门/岗位) + 监控目录占位 (Agent 3 添加)
 * - 角色: admin (全部权限 *:*:*) / common
 * - 用户: admin/admin123 (BCrypt)
 */
@Slf4j
@Component
@Order(1)
@RequiredArgsConstructor
public class DataInitializer implements CommandLineRunner {

    private final ISysUserService userService;
    private final ISysRoleService roleService;
    private final ISysMenuService menuService;
    private final ISysDeptService deptService;
    private final ISysPostService postService;

    @Override
    @Transactional(rollbackFor = Exception.class)
    public void run(String... args) {
        // 仅当 sys_user 为空时执行
        if (userService.count() > 0) {
            log.info("sys_user 已有数据, 跳过 RBAC 默认初始化");
            return;
        }
        log.info("首次启动, 初始化 RuoYi RBAC 默认数据...");
        try {
            initDepts();
            initPosts();
            initMenus();
            initRoles();
            initAdminUser();
            log.info("RuoYi RBAC 默认数据初始化完成");
        } catch (Exception e) {
            log.error("初始化失败", e);
            throw e;
        }
    }

    private void initDepts() {
        SysDept root = newDept(0L, "0", "集团总公司", 0, "集团领导", "15888888888", "admin@zlinks.com");
        deptService.save(root);

        SysDept dev = newDept(root.getDeptId(), root.getAncestors() + "," + root.getDeptId(), "研发部门", 1, "研发总监", "15888888881", "rd@zlinks.com");
        deptService.save(dev);

        SysDept test = newDept(root.getDeptId(), root.getAncestors() + "," + root.getDeptId(), "测试部门", 2, "测试主管", "15888888882", "qa@zlinks.com");
        deptService.save(test);

        SysDept ops = newDept(root.getDeptId(), root.getAncestors() + "," + root.getDeptId(), "运维部门", 3, "运维主管", "15888888883", "ops@zlinks.com");
        deptService.save(ops);

        log.info("初始化默认部门完成");
    }

    private SysDept newDept(Long parentId, String ancestors, String name, int orderNum, String leader, String phone, String email) {
        SysDept d = new SysDept();
        d.setParentId(parentId);
        d.setAncestors(ancestors);
        d.setDeptName(name);
        d.setOrderNum(orderNum);
        d.setLeader(leader);
        d.setPhone(phone);
        d.setEmail(email);
        d.setStatus(UserConstants.NORMAL);
        d.setDelFlag("0");
        return d;
    }

    private void initPosts() {
        savePost("ceo", "董事长", 1);
        savePost("se", "研发工程师", 2);
        savePost("qa", "测试工程师", 3);
        savePost("ops", "运维工程师", 4);
        log.info("初始化默认岗位完成");
    }

    private void savePost(String code, String name, int sort) {
        SysPost p = new SysPost();
        p.setPostCode(code);
        p.setPostName(name);
        p.setPostSort(sort);
        p.setStatus(UserConstants.NORMAL);
        postService.save(p);
    }

    private final List<SysMenu> savedMenus = new ArrayList<>();

    private void initMenus() {
        // M=目录 C=菜单 F=按钮
        // 顶级目录: 系统管理 (menu_id=1) — 作为按钮权限的 parent 保留
        SysMenu sysDir = newMenu("系统管理", 0L, 1, "system", null, "M", "0", "0", "system", null);
        menuService.save(sysDir);

        // 用户/角色/菜单/参数/通知 5 项提升为顶级菜单 (parentId=0)
        SysMenu userMenu = newMenu("用户管理", 0L, 2, "user", "system/user/index", "C", "0", "0", "user", "system:user:list");
        SysMenu roleMenu = newMenu("角色管理", 0L, 3, "role", "system/role/index", "C", "0", "0", "peoples", "system:role:list");
        SysMenu menuMenu = newMenu("菜单管理", 0L, 4, "menu", "system/menu/index", "C", "0", "0", "tree-table", "system:menu:list");
        SysMenu configMenu = newMenu("参数设置", 0L, 5, "config", "system/config/index", "C", "0", "0", "list", "system:config:list");
        SysMenu noticeMenu = newMenu("通知管理", 0L, 6, "notice", "system/notice/index", "C", "0", "0", "message", "system:notice:list");

        // 部门/岗位按钮保留挂在 sysDir 上 (后端完整保留，前端无入口)
        SysMenu deptMenu = newMenu("部门管理", sysDir.getMenuId(), 1, "dept", "system/dept/index", "C", "0", "0", "tree", "system:dept:list");
        SysMenu postMenu = newMenu("岗位管理", sysDir.getMenuId(), 2, "post", "system/post/index", "C", "0", "0", "post", "system:post:list");

        for (SysMenu m : Arrays.asList(userMenu, roleMenu, menuMenu, configMenu, noticeMenu, deptMenu, postMenu)) {
            menuService.save(m);
            savedMenus.add(m);
        }

        // 用户管理按钮
        addButton(userMenu, "用户查询", "system:user:query", 1);
        addButton(userMenu, "用户新增", "system:user:add", 2);
        addButton(userMenu, "用户修改", "system:user:edit", 3);
        addButton(userMenu, "用户删除", "system:user:remove", 4);
        addButton(userMenu, "重置密码", "system:user:resetPwd", 5);
        addButton(userMenu, "分配角色", "system:user:edit", 6);
        addButton(userMenu, "状态修改", "system:user:edit", 7);
        addButton(userMenu, "个人信息", "system:user:query", 8);

        // 角色管理按钮
        addButton(roleMenu, "角色查询", "system:role:query", 1);
        addButton(roleMenu, "角色新增", "system:role:add", 2);
        addButton(roleMenu, "角色修改", "system:role:edit", 3);
        addButton(roleMenu, "角色删除", "system:role:remove", 4);
        addButton(roleMenu, "分配权限", "system:role:edit", 5);
        addButton(roleMenu, "分配用户", "system:role:edit", 6);
        addButton(roleMenu, "状态修改", "system:role:edit", 7);

        // 菜单管理按钮
        addButton(menuMenu, "菜单查询", "system:menu:query", 1);
        addButton(menuMenu, "菜单新增", "system:menu:add", 2);
        addButton(menuMenu, "菜单修改", "system:menu:edit", 3);
        addButton(menuMenu, "菜单删除", "system:menu:remove", 4);

        // 参数设置按钮
        addButton(configMenu, "参数查询", "system:config:query", 1);
        addButton(configMenu, "参数新增", "system:config:add", 2);
        addButton(configMenu, "参数修改", "system:config:edit", 3);
        addButton(configMenu, "参数删除", "system:config:remove", 4);

        // 通知管理按钮
        addButton(noticeMenu, "公告查询", "system:notice:query", 1);
        addButton(noticeMenu, "公告新增", "system:notice:add", 2);
        addButton(noticeMenu, "公告修改", "system:notice:edit", 3);
        addButton(noticeMenu, "公告删除", "system:notice:remove", 4);

        // 部门管理按钮（后端保留，前端入口已删除）
        addButton(deptMenu, "部门查询", "system:dept:query", 1);
        addButton(deptMenu, "部门新增", "system:dept:add", 2);
        addButton(deptMenu, "部门修改", "system:dept:edit", 3);
        addButton(deptMenu, "部门删除", "system:dept:remove", 4);

        // 岗位管理按钮（后端保留，前端入口已删除）
        addButton(postMenu, "岗位查询", "system:post:query", 1);
        addButton(postMenu, "岗位新增", "system:post:add", 2);
        addButton(postMenu, "岗位修改", "system:post:edit", 3);
        addButton(postMenu, "岗位删除", "system:post:remove", 4);

        // 监控目录 (顶级目录)
        SysMenu monitorDir = newMenu("系统监控", 0L, 2, "monitor", null, "M", "0", "0", "monitor", null);
        menuService.save(monitorDir);
        savedMenus.add(monitorDir);

        // 监控子菜单: 服务监控 / 在线用户 / 定时任务 / 数据监控(Druid) / 缓存监控 / 登录日志 / 操作日志
        SysMenu serverMenu = newMenu("服务监控", monitorDir.getMenuId(), 1, "server", "system/monitor/server/index", "C", "0", "0", "cpu", "monitor:server:list");
        SysMenu onlineMenu = newMenu("在线用户", monitorDir.getMenuId(), 2, "online", "system/monitor/online/index", "C", "0", "0", "online", "monitor:online:list");
        SysMenu jobMenu = newMenu("定时任务", monitorDir.getMenuId(), 3, "job", "system/monitor/job/index", "C", "0", "0", "timer", "monitor:job:list");
        SysMenu druidMenu = newMenu("数据监控", monitorDir.getMenuId(), 4, "druid", "system/monitor/druid/index", "C", "0", "0", "data-analysis", "monitor:druid:list");
        SysMenu cacheMenu = newMenu("缓存监控", monitorDir.getMenuId(), 5, "cache", "system/monitor/cache/index", "C", "0", "0", "histogram", "monitor:cache:list");
        SysMenu logininforMenu = newMenu("登录日志", monitorDir.getMenuId(), 6, "logininfor", "system/monitor/logininfor/index", "C", "0", "0", "document", "monitor:logininfor:list");
        SysMenu operlogMenu = newMenu("操作日志", monitorDir.getMenuId(), 7, "operlog", "system/monitor/operlog/index", "C", "0", "0", "form", "monitor:operlog:list");
        for (SysMenu m : Arrays.asList(serverMenu, onlineMenu, jobMenu, druidMenu, cacheMenu, logininforMenu, operlogMenu)) {
            menuService.save(m);
            savedMenus.add(m);
        }

        // 各监控页按钮权限 (按 RuoYi 习惯)
        addButton(serverMenu, "服务查询", "monitor:server:query", 1);

        addButton(onlineMenu, "在线查询", "monitor:online:query", 1);
        addButton(onlineMenu, "强退用户", "monitor:online:forceLogout", 2);

        addButton(jobMenu, "任务查询", "monitor:job:query", 1);
        addButton(jobMenu, "任务新增", "monitor:job:add", 2);
        addButton(jobMenu, "任务修改", "monitor:job:edit", 3);
        addButton(jobMenu, "任务删除", "monitor:job:remove", 4);
        addButton(jobMenu, "状态修改", "monitor:job:changeStatus", 5);
        addButton(jobMenu, "任务导出", "monitor:job:export", 6);

        addButton(druidMenu, "数据查询", "monitor:druid:query", 1);

        addButton(cacheMenu, "缓存查询", "monitor:cache:list", 1);
        addButton(cacheMenu, "缓存删除", "monitor:cache:remove", 2);

        addButton(logininforMenu, "登录查询", "monitor:logininfor:query", 1);
        addButton(logininforMenu, "登录删除", "monitor:logininfor:remove", 2);
        addButton(logininforMenu, "账户解锁", "monitor:logininfor:unlock", 3);
        addButton(logininforMenu, "日志导出", "monitor:logininfor:export", 4);

        addButton(operlogMenu, "操作查询", "monitor:operlog:query", 1);
        addButton(operlogMenu, "操作删除", "monitor:operlog:remove", 2);
        addButton(operlogMenu, "日志导出", "monitor:operlog:export", 3);

        log.info("初始化默认菜单完成, 共 {} 个节点", savedMenus.size());
    }

    private SysMenu newMenu(String name, Long parentId, int orderNum, String path, String component,
                            String menuType, String visible, String status, String icon, String perms) {
        SysMenu m = new SysMenu();
        m.setMenuName(name);
        m.setParentId(parentId);
        m.setOrderNum(orderNum);
        m.setPath(path);
        m.setComponent(component);
        m.setIsFrame("1");
        m.setIsCache("0");
        m.setMenuType(menuType);
        m.setVisible(visible);
        m.setStatus(status);
        m.setIcon(icon);
        m.setPerms(perms);
        return m;
    }

    private void addButton(SysMenu parent, String name, String perms, int orderNum) {
        SysMenu btn = newMenu(name, parent.getMenuId(), orderNum, "", null, "F", "0", "0", "#", perms);
        menuService.save(btn);
    }

    private void initRoles() {
        SysRole admin = new SysRole();
        admin.setRoleName("超级管理员");
        admin.setRoleKey(UserConstants.ROLE_ADMIN);
        admin.setRoleSort(1);
        admin.setDataScope("1");
        admin.setStatus(UserConstants.NORMAL);
        roleService.save(admin);

        SysRole common = new SysRole();
        common.setRoleName("普通角色");
        common.setRoleKey(UserConstants.ROLE_COMMON);
        common.setRoleSort(2);
        common.setDataScope("5");
        common.setStatus(UserConstants.NORMAL);
        roleService.save(common);

        // admin -> 所有菜单
        List<Long> allMenuIds = menuService.list().stream().map(SysMenu::getMenuId).toList();
        roleService.updateMenuPermissions(admin.getRoleId(), allMenuIds);

        log.info("初始化默认角色完成");
    }

    private void initAdminUser() {
            SysUser admin = new SysUser();
            admin.setUserName(UserConstants.USER_ADMIN);
            admin.setNickName("超级管理员");
            String hash = SecurityUtils.encryptPassword("admin123");
            log.info("admin password BCrypt hash: {}", hash);
            admin.setPassword(hash);
            admin.setStatus(UserConstants.NORMAL);
            admin.setEmail("admin@zlinks.com");
            admin.setPhonenumber("15888888888");
            admin.setSex("0");
            admin.setAvatar("");
            // 部门 = 集团总公司
            SysDept rootDept = deptService.getOne(new LambdaQueryWrapper<SysDept>().eq(SysDept::getDeptName, "集团总公司"));
            if (rootDept != null) admin.setDeptId(rootDept.getDeptId());
            admin.setRoleIds(new Long[]{1L});
            userService.insertUser(admin);
            log.info("初始化默认用户 admin/admin123 完成");
        }
}