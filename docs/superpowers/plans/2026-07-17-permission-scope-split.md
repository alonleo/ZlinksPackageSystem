# 权限管理拆分为后台/桌面 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 将权限组从单一 `groupPermission.modules` 拆分为 `backend` / `desktop` 两个 scope，用于分别控制 Web 后台管理系统与桌面应用（Avalonia）的模块可见性，并联动 Web `MainLayout.vue` 菜单过滤与桌面端 8 个 MenuItem 的 IsVisible。

**架构：**
- 数据层：方案 C —— 新建 `permission_scope(group_id, scope, modules_text)` 表（UNIQUE(group_id, scope)），保留 `permission_group.group_permission` 列作为 deprecated 兼容字段。
- 后端：新增 `PermissionScope` 子资源（`/api/permission-groups/{groupId}/scopes`）；`/api/auth/info` 与 `/api/getInfo` 增加合并后的 `backendModules` / `desktopModules` 字段。
- Web 前端：`PermissionListView.vue` 改为双 panel（后台模块 + 桌面端模块），`MainLayout.vue` 按 `userStore.backendModules` 过滤菜单。
- 桌面端：`AuthService` 增加预留 `FetchDesktopModulesAsync`，`MainViewModel` 暴露 `IsXxxVisible`，`MainWindow.axaml` 用 IsVisible 绑定；本地 admin/admin 仍全可见。

**技术栈：** Spring Boot 3.3.7 + MyBatis-Plus 3.5.7（后端）；Vue 3 + TS + Pinia（前端 Web）；Avalonia 11.2 + CommunityToolkit.Mvvm（桌面端）。

---

## 文件结构

后端新增：
- `entity/PermissionScope.java`
- `mapper/PermissionScopeMapper.java`
- `service/IPermissionScopeService.java`
- `service/impl/PermissionScopeServiceImpl.java`
- `dto/PermissionScopeRequest.java`
- `controller/PermissionScopeController.java`

后端修改：
- `controller/PermissionGroupController.java`
- `dto/PermissionGroupRequest.java`
- `dto/PermissionGroupExcelDTO.java`
- `controller/AuthController.java`
- `controller/RuoYiAuthController.java`
- `service/impl/AuthServiceImpl.java`
- `service/impl/UserServiceImpl.java`（如需查询 user-group 关联）
- `backend/src/main/resources/schema.sql`
- `scripts/init-database.sql`
- `scripts/init-database-pgsql.sql`

前端 Web 新增：无（修改现有）

前端 Web 修改：
- `types/permission-group.ts`
- `api/permission.ts`
- `views/system-mgmt/permissions/PermissionListView.vue`
- `stores/user.ts`
- `layouts/MainLayout.vue`
- `api/auth.ts`
- `types/user.ts`

桌面端修改：
- `Models/User.cs`
- `Services/AuthService.cs`
- `ViewModels/MainViewModel.cs`
- `Views/MainWindow.axaml`

文档：
- `docs/superpowers/specs/2026-07-17-permission-scope-split.md`

---

## 任务 1：后端 PermissionScope 实体与基础 CRUD 骨架

**文件：**
- 新增：`backend/src/main/java/com/zlinks/package_system/entity/PermissionScope.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/mapper/PermissionScopeMapper.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/IPermissionScopeService.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/impl/PermissionScopeServiceImpl.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/PermissionScopeRequest.java`

- [ ] **步骤 1：实体 PermissionScope.java**

```java
package com.zlinks.package_system.entity;

import com.baomidou.mybatisplus.annotation.IdType;
import com.baomidou.mybatisplus.annotation.TableField;
import com.baomidou.mybatisplus.annotation.TableId;
import com.baomidou.mybatisplus.annotation.TableName;
import lombok.Data;
import lombok.EqualsAndHashCode;

@Data
@EqualsAndHashCode(callSuper = true)
@TableName("permission_scope")
public class PermissionScope extends BaseEntity {

    @TableId(value = "id", type = IdType.AUTO)
    private Long id;

    @TableField("group_id")
    private Long groupId;

    private String scope;

    @TableField("modules_text")
    private String modulesText;
}
```

- [ ] **步骤 2：Mapper**

```java
package com.zlinks.package_system.mapper;

import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.zlinks.package_system.entity.PermissionScope;
import org.apache.ibatis.annotations.Mapper;

@Mapper
public interface PermissionScopeMapper extends BaseMapper<PermissionScope> {
}
```

- [ ] **步骤 3：Service 接口**

```java
package com.zlinks.package_system.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.PermissionScope;

public interface IPermissionScopeService extends IService<PermissionScope> {
}
```

- [ ] **步骤 4：Service Impl**

```java
package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.PermissionScope;
import com.zlinks.package_system.mapper.PermissionScopeMapper;
import com.zlinks.package_system.service.IPermissionScopeService;
import org.springframework.stereotype.Service;

@Service
public class PermissionScopeServiceImpl extends ServiceImpl<PermissionScopeMapper, PermissionScope> implements IPermissionScopeService {
}
```

- [ ] **步骤 5：Request DTO**

```java
package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Data;

import java.util.List;

@Data
public class PermissionScopeRequest {
    @NotNull(message = "请选择权限组")
    private Long groupId;

    @NotBlank(message = "scope 不能为空")
    private String scope;

    private List<String> modules;
}
```

- [ ] **步骤 6：编译验证**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 7：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/entity/PermissionScope.java \
        backend/src/main/java/com/zlinks/package_system/mapper/PermissionScopeMapper.java \
        backend/src/main/java/com/zlinks/package_system/service/IPermissionScopeService.java \
        backend/src/main/java/com/zlinks/package_system/service/impl/PermissionScopeServiceImpl.java \
        backend/src/main/java/com/zlinks/package_system/dto/PermissionScopeRequest.java
git commit -m "feat(permission): add PermissionScope entity and base CRUD skeleton"
```

---

## 任务 2：PermissionScopeController（子资源风格）

**文件：**
- 新增：`backend/src/main/java/com/zlinks/package_system/controller/PermissionScopeController.java`

- [ ] **步骤 1：完整 Controller**

```java
package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zlinks.package_system.dto.PermissionScopeRequest;
import com.zlinks.package_system.entity.PermissionScope;
import com.zlinks.package_system.service.IPermissionScopeService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.Result;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.Collections;
import java.util.List;

@Tag(name = "权限组模块范围")
@RestController
@RequestMapping("/api/permission-groups/{groupId}/scopes")
@RequiredArgsConstructor
public class PermissionScopeController {
    private final IPermissionScopeService service;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Operation(summary = "列出某 group 全部 scope")
    @GetMapping
    public Result<List<PermissionScope>> list(@PathVariable Long groupId) {
        List<PermissionScope> scopes = service.list(new LambdaQueryWrapper<PermissionScope>().eq(PermissionScope::getGroupId, groupId));
        return Result.success(scopes);
    }

    @Operation(summary = "取单个 scope 的 modules")
    @GetMapping("/{scope}")
    public Result<List<String>> getModules(@PathVariable Long groupId, @PathVariable String scope) {
        PermissionScope existing = service.getOne(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        if (existing == null) return Result.success(Collections.emptyList());
        return Result.success(parseModules(existing.getModulesText()));
    }

    @Operation(summary = "upsert scope")
    @PutMapping("/{scope}")
    public Result<PermissionScope> upsert(@PathVariable Long groupId,
                                          @PathVariable String scope,
                                          @Valid @RequestBody PermissionScopeRequest req) {
        if (!scope.equals(req.getScope())) throw new BusinessException("scope 路径与请求体不一致");
        if (!"backend".equals(scope) && !"desktop".equals(scope)) throw new BusinessException("scope 必须为 backend 或 desktop");
        String text = serialize(req.getModules());
        PermissionScope existing = service.getOne(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        if (existing == null) {
            PermissionScope entity = new PermissionScope();
            entity.setGroupId(groupId);
            entity.setScope(scope);
            entity.setModulesText(text);
            service.save(entity);
            return Result.success(entity);
        } else {
            existing.setModulesText(text);
            service.updateById(existing);
            return Result.success(existing);
        }
    }

    @Operation(summary = "删除单个 scope")
    @DeleteMapping("/{scope}")
    public Result<Void> delete(@PathVariable Long groupId, @PathVariable String scope) {
        service.remove(new LambdaQueryWrapper<PermissionScope>()
                .eq(PermissionScope::getGroupId, groupId)
                .eq(PermissionScope::getScope, scope));
        return Result.success();
    }

    private String serialize(List<String> modules) {
        if (modules == null) return "[]";
        try {
            return objectMapper.writeValueAsString(modules);
        } catch (Exception e) {
            throw new BusinessException("模块列表序列化失败");
        }
    }

    private List<String> parseModules(String modulesText) {
        if (modulesText == null || modulesText.isEmpty()) return Collections.emptyList();
        try {
            return objectMapper.readValue(modulesText, new TypeReference<List<String>>() {});
        } catch (Exception e) {
            return Collections.emptyList();
        }
    }
}
```

- [ ] **步骤 2：编译验证**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 3：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/controller/PermissionScopeController.java
git commit -m "feat(permission): add PermissionScope REST endpoints"
```

---

## 任务 3：DB schema 增加 permission_scope 表（3 份脚本）

**文件：**
- 修改：`backend/src/main/resources/schema.sql`
- 修改：`scripts/init-database.sql`
- 修改：`scripts/init-database-pgsql.sql`

- [ ] **步骤 1：H2 schema（H2 嵌入式）**

在 `backend/src/main/resources/schema.sql` 中 `permission_group` 表（约 L4-15）后追加新表：

```sql
-- 权限组模块范围（按 scope 拆分）
CREATE TABLE IF NOT EXISTS `permission_scope` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `group_id` BIGINT NOT NULL,
    `scope` VARCHAR(16) NOT NULL,
    `modules_text` TEXT,
    `create_by` VARCHAR(64) DEFAULT '',
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_by` VARCHAR(64) DEFAULT '',
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0,
    UNIQUE KEY `uk_group_scope` (`group_id`, `scope`)
);
```

- [ ] **步骤 2：MySQL init 脚本**

在 `scripts/init-database.sql` 中 `permission_group` 表（约 L192-203）后追加新表 + 4 行默认 scope 数据（紧跟默认权限组 4 行 INSERT 之后）：

```sql
-- 权限组模块范围
CREATE TABLE IF NOT EXISTS `permission_scope` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `group_id` BIGINT NOT NULL,
    `scope` VARCHAR(16) NOT NULL,
    `modules_text` TEXT,
    `create_by` VARCHAR(64) DEFAULT '',
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_by` VARCHAR(64) DEFAULT '',
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0,
    UNIQUE KEY `uk_group_scope` (`group_id`, `scope`)
);

-- 默认权限组的模块范围（与默认权限组 INSERT 之后，按 id 顺序绑定）
INSERT INTO `permission_scope` (`group_id`, `scope`, `modules_text`, `create_by`) VALUES
(1, 'backend', '["all"]', 'system'),
(1, 'desktop',  '["all"]', 'system'),
(2, 'backend',  '["home","package"]', 'system'),
(2, 'desktop',   '["home","products"]', 'system'),
(3, 'backend',  '["home","package"]', 'system'),
(3, 'desktop',   '["home","tests"]', 'system'),
(4, 'backend',  '["home","package"]', 'system'),
(4, 'desktop',   '["home","products"]', 'system');
```

> 注：插入顺序需对应默认权限组的 id。如果默认权限组 id 不确定，可改为：先 SELECT id 拿到 groupId 后再插入；或使用子查询 `INSERT ... SELECT id FROM permission_group WHERE group_name='管理员组'`。

- [ ] **步骤 3：PostgreSQL init 脚本**

在 `scripts/init-database-pgsql.sql` 中追加（PG 用 `BIGINT GENERATED BY DEFAULT AS IDENTITY`）：

```sql
CREATE TABLE IF NOT EXISTS permission_scope (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    group_id BIGINT NOT NULL,
    scope VARCHAR(16) NOT NULL,
    modules_text TEXT,
    create_by VARCHAR(64) DEFAULT '',
    create_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    update_by VARCHAR(64) DEFAULT '',
    update_time TIMESTAMP,
    is_deleted SMALLINT DEFAULT 0,
    CONSTRAINT uk_permission_scope UNIQUE (group_id, scope)
);

INSERT INTO permission_scope (group_id, scope, modules_text, create_by) VALUES
(1, 'backend', '["all"]', 'system'),
(1, 'desktop',  '["all"]', 'system'),
(2, 'backend',  '["home","package"]', 'system'),
(2, 'desktop',   '["home","products"]', 'system'),
(3, 'backend',  '["home","package"]', 'system'),
(3, 'desktop',   '["home","tests"]', 'system'),
(4, 'backend',  '["home","package"]', 'system'),
(4, 'desktop',   '["home","products"]', 'system');
```

- [ ] **步骤 4：Commit**

```bash
git add backend/src/main/resources/schema.sql scripts/init-database.sql scripts/init-database-pgsql.sql
git commit -m "feat(db): add permission_scope table and seed default scopes"
```

---

## 任务 4：PermissionGroupController / Request / ExcelDTO 适配（移除 groupPermission 写入）

**文件：**
- 修改：`backend/src/main/java/com/zlinks/package_system/dto/PermissionGroupRequest.java`
- 修改：`backend/src/main/java/com/zlinks/package_system/dto/PermissionGroupExcelDTO.java`
- 修改：`backend/src/main/java/com/zlinks/package_system/controller/PermissionGroupController.java`

- [ ] **步骤 1：读取现状**

```bash
ls backend/src/main/java/com/zlinks/package_system/dto/PermissionGroup*
grep -n "groupPermission" backend/src/main/java/com/zlinks/package_system/dto/*.java backend/src/main/java/com/zlinks/package_system/controller/PermissionGroupController.java
```

- [ ] **步骤 2：PermissionGroupRequest.java 移除 groupPermission 字段**

保留 `groupName (NotBlank)` 与 `remark`；删除 `groupPermission` 字段。最终内容：

```java
package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class PermissionGroupRequest {

    @NotBlank(message = "权限组名称不能为空")
    private String groupName;

    private String remark;
}
```

- [ ] **步骤 3：PermissionGroupExcelDTO.java 移除 groupPermission 列**

保留 `groupName / groupAccounts / remark`；删除 `groupPermission` 字段。最终内容：

```java
package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class PermissionGroupExcelDTO {
    @ExcelProperty("权限组名称") private String groupName;
    @ExcelProperty("关联账号") private String groupAccounts;
    @ExcelProperty("备注") private String remark;
}
```

- [ ] **步骤 4：PermissionGroupController.java 移除 groupPermission 处理**

- 删除以下行（具体行号通过 grep 确认）：
  - `import com.alibaba.excel.annotation.ExcelProperty;`（如果不再用）
  - `req.setGroupPermission(...)`（如果存在）
  - `dto.setGroupPermission(...)`（Excel 导出循环内）
  - 模板生成 `dto.setGroupPermission(...)`
  - import 路径中 `JSONObject`（如果仅用于 groupPermission）
- 保留：导入导出端点骨架、`getList/getById/create/update/import/export/template/delete`、用户关联端点。

- [ ] **步骤 5：编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 6：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/dto/PermissionGroupRequest.java \
        backend/src/main/java/com/zlinks/package_system/dto/PermissionGroupExcelDTO.java \
        backend/src/main/java/com/zlinks/package_system/controller/PermissionGroupController.java
git commit -m "refactor(permission): drop groupPermission from PG request/excel/controller"
```

---

## 任务 5：AuthController / AuthServiceImpl 扩展（/auth/info 增加 desktopModules）

**文件：**
- 修改：`backend/src/main/java/com/zlinks/package_system/entity/User.java`（如果未含 groupIds）
- 修改：`backend/src/main/java/com/zlinks/package_system/service/impl/AuthServiceImpl.java`
- 修改：`backend/src/main/java/com/zlinks/package_system/controller/AuthController.java`

- [ ] **步骤 1：读取现状**

```bash
grep -n "getCurrentUser\|getInfo\|return User\|user.set" backend/src/main/java/com/zlinks/package_system/service/impl/AuthServiceImpl.java backend/src/main/java/com/zlinks/package_system/controller/AuthController.java
```

- [ ] **步骤 2：AuthServiceImpl 增加 mergeModulesByUser 工具方法**

定位 `AuthServiceImpl` 类内，在末尾新增：

```java
public List<String> mergeModulesByUser(Long userId, String scope) {
    if (userId == null) return Collections.emptyList();
    LambdaQueryWrapper<UserGroup> ugW = new LambdaQueryWrapper<UserGroup>().eq(UserGroup::getUserId, userId);
    List<UserGroup> ugs = userGroupService.list(ugW);
    if (ugs.isEmpty()) return Collections.emptyList();
    List<Long> groupIds = ugs.stream().map(UserGroup::getGroupId).collect(Collectors.toList());
    LambdaQueryWrapper<PermissionScope> psW = new LambdaQueryWrapper<PermissionScope>()
            .in(PermissionScope::getGroupId, groupIds)
            .eq(PermissionScope::getScope, scope);
    List<PermissionScope> scopes = permissionScopeService.list(psW);
    Set<String> merged = new LinkedHashSet<>();
    for (PermissionScope s : scopes) {
        if (s.getModulesText() == null) continue;
        try {
            List<String> mods = new ObjectMapper().readValue(s.getModulesText(), new TypeReference<List<String>>() {});
            if (mods.contains("all")) return List.of("all");
            merged.addAll(mods);
        } catch (Exception ignored) {}
    }
    return new ArrayList<>(merged);
}
```

需要的 imports：`com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper`、`com.zlinks.package_system.entity.UserGroup`、`com.zlinks.package_system.entity.PermissionScope`、`com.zlinks.package_system.service.IPermissionScopeService`、`com.zlinks.package_system.service.IUserGroupService`、`com.fasterxml.jackson.databind.ObjectMapper`、`com.fasterxml.jackson.core.type.TypeReference`、`java.util.*`。

并在类字段中加 `private final IPermissionScopeService permissionScopeService;` 与 `private final IUserGroupService userGroupService;`（如果使用 `@RequiredArgsConstructor` 会自动注入；否则手动 `@Autowired`）。

- [ ] **步骤 3：修改 `getCurrentUser` 返回结构**

定位 `getCurrentUser` 方法（`AuthServiceImpl.java:62-87`），在返回前增加：

```java
UserVO vo = new UserVO();
BeanUtils.copyProperties(user, vo);
vo.setGroupIds(user.getGroupIds());
vo.setGroupNames(user.getGroupNames());
vo.setDesktopModules(mergeModulesByUser(user.getId(), "desktop"));
return Result.success(vo);
```

若现方法返回 `User`，改为返回新的 `UserVO`（含 desktopModules）；并新增 `UserVO` 类（若不存在）。`UserVO` 可直接复用 `entity/User` 加字段，或新建一个独立 VO 类。

- [ ] **步骤 4：AuthController 调整返回类型**

```bash
grep -n "@GetMapping\|public Result" backend/src/main/java/com/zlinks/package_system/controller/AuthController.java
```

把 `/api/auth/info` 端点返回值改为 `Result<UserVO>`。

- [ ] **步骤 5：编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 6：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/entity/User.java \
        backend/src/main/java/com/zlinks/package_system/service/impl/AuthServiceImpl.java \
        backend/src/main/java/com/zlinks/package_system/controller/AuthController.java
git commit -m "feat(auth): include desktopModules in /api/auth/info response"
```

---

## 任务 6：RuoYiAuthController / getInfo 增加 modules 合并

**文件：**
- 修改：`backend/src/main/java/com/zlinks/package_system/controller/RuoYiAuthController.java`

- [ ] **步骤 1：读取现状**

```bash
grep -n "getInfo\|getRouters" backend/src/main/java/com/zlinks/package_system/controller/RuoYiAuthController.java
```

- [ ] **步骤 2：扩展 getInfo 返回**

定位 `getInfo` 方法（`RuoYiAuthController.java:118-144`），在构造 `Map<String, Object>` 返回值时增加：

```java
Long userId = loginUser.getUserId();
Map<String, Object> modules = new HashMap<>();
modules.put("backend", authService.mergeModulesByUser(userId, "backend"));
modules.put("desktop", authService.mergeModulesByUser(userId, "desktop"));
ajax.put("modules", modules);
```

- [ ] **步骤 3：编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 4：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/controller/RuoYiAuthController.java
git commit -m "feat(auth): include backend/desktop modules in /api/getInfo response"
```

---

## 任务 7：前端类型与 API 扩展

**文件：**
- 修改：`frontend/src/types/permission-group.ts`
- 修改：`frontend/src/types/user.ts`
- 修改：`frontend/src/api/permission.ts`
- 修改：`frontend/src/api/auth.ts`

- [ ] **步骤 1：types/permission-group.ts**

```ts
export interface PermissionScope {
  id?: number
  groupId: number
  scope: 'backend' | 'desktop'
  modulesText?: string
  modules?: string[]
}

export interface PermissionGroup {
  id: number
  groupName: string
  groupPermission: string
  groupAccounts: string
  remark: string
  userCount: number
  createTime: string
  updateTime: string
  scopes?: PermissionScope[]
}

export const BACKEND_MODULE_OPTIONS: { label: string; value: string }[] = [
  { label: '首页', value: 'home' },
  { label: '系统管理', value: 'system-mgmt' },
  { label: '系统设置', value: 'system-settings' },
  { label: '打包管理', value: 'package' },
  { label: '系统监控', value: 'monitor' },
  { label: '全部', value: 'all' },
]

export const DESKTOP_MODULE_OPTIONS: { label: string; value: string }[] = [
  { label: '首页', value: 'home' },
  { label: '游戏管理', value: 'games' },
  { label: '产品管理', value: 'products' },
  { label: '参数管理', value: 'parameters' },
  { label: '测试管理', value: 'tests' },
  { label: '工具库', value: 'tool-library' },
  { label: '消息中心', value: 'notification' },
  { label: '设置', value: 'settings' },
  { label: '全部', value: 'all' },
]
```

- [ ] **步骤 2：types/user.ts 增加 modules**

```ts
export interface UserInfoModules {
  backend: string[]
  desktop: string[]
}

export interface UserInfo {
  user: {
    userId?: number
    userName: string
    nickName?: string
    avatar?: string
    [k: string]: unknown
  }
  roles: string[]
  permissions: string[]
  modules: UserInfoModules
}
```

> 若 `frontend/src/types/user.ts` 已存在 UserInfo，按需合并；不存在则新建。

- [ ] **步骤 3：api/permission.ts 增加 scope 端点**

在现有 export 后追加：

```ts
export const permissionScopeApi = {
  list(groupId: number): Promise<{ data: PermissionScope[] }> {
    return api.get(`/permission-groups/${groupId}/scopes`)
  },
  getModules(groupId: number, scope: string): Promise<{ data: string[] }> {
    return api.get(`/permission-groups/${groupId}/scopes/${scope}`)
  },
  upsert(groupId: number, scope: string, modules: string[]): Promise<{ data: PermissionScope }> {
    return api.put(`/permission-groups/${groupId}/scopes/${scope}`, { groupId, scope, modules })
  },
  remove(groupId: number, scope: string): Promise<void> {
    return api.delete(`/permission-groups/${groupId}/scopes/${scope}`)
  },
}
```

- [ ] **步骤 4：api/auth.ts 调整 UserInfoResponse 类型**

如果 UserInfoResponse 有显式类型，扩展 modules 字段；否则交由 `stores/user` 处理。

- [ ] **步骤 5：Commit**

```bash
git add frontend/src/types/permission-group.ts \
        frontend/src/types/user.ts \
        frontend/src/api/permission.ts \
        frontend/src/api/auth.ts
git commit -m "feat(frontend): add backend/desktop module types and scope APIs"
```

---

## 任务 8：stores/user.ts 增加 backendModules / desktopModules

**文件：**
- 修改：`frontend/src/stores/user.ts`

- [ ] **步骤 1：读取现状**

```bash
cat frontend/src/stores/user.ts
```

- [ ] **步骤 2：扩展 state 与 fetchUserInfo**

```ts
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi } from '@/api/auth'
import type { UserInfoModules } from '@/types/user'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>('')
  const userInfo = ref<any>(null)
  const roles = ref<string[]>([])
  const permissions = ref<string[]>([])
  const backendModules = ref<string[]>([])
  const desktopModules = ref<string[]>([])

  async function fetchUserInfo() {
    try {
      const res = await authApi.getInfo()
      const data = res.data || {}
      userInfo.value = data.user || null
      roles.value = data.roles || []
      permissions.value = data.permissions || []
      const modules: UserInfoModules = data.modules || { backend: ['all'], desktop: ['all'] }
      backendModules.value = modules.backend || ['all']
      desktopModules.value = modules.desktop || ['all']
    } catch (e) {
      console.error('fetchUserInfo failed', e)
    }
  }

  function resetModules() {
    backendModules.value = ['all']
    desktopModules.value = ['all']
  }

  return { token, userInfo, roles, permissions, backendModules, desktopModules, fetchUserInfo, resetModules }
})
```

- [ ] **步骤 3：Commit**

```bash
git add frontend/src/stores/user.ts
git commit -m "feat(store): track backend/desktop modules from /getInfo"
```

---

## 任务 9：MainLayout.vue 按 backendModules 过滤菜单

**文件：**
- 修改：`frontend/src/layouts/MainLayout.vue`

- [ ] **步骤 1：增加过滤计算属性**

在 `<script setup>` 内（`userStore` 已有引用），增加：

```ts
const canShow = (key: string) => {
  const mods = userStore.backendModules || []
  if (mods.length === 0) return true
  if (mods.includes('all')) return true
  return mods.includes(key)
}
```

- [ ] **步骤 2：模板 v-if 包裹 4 个父菜单**

在 4 个 `<el-sub-menu>` 上分别增加 `v-if`：

```html
<el-sub-menu v-if="canShow('system-mgmt')" index="system-mgmt">
<el-sub-menu v-if="canShow('package')" index="package">
<el-sub-menu v-if="canShow('system-settings')" index="system-settings">
<el-sub-menu v-if="canShow('monitor')" index="monitor">
```

- [ ] **步骤 3：Commit**

```bash
git add frontend/src/layouts/MainLayout.vue
git commit -m "feat(menu): filter parent menus by backendModules"
```

---

## 任务 10：PermissionListView.vue 双 panel 重构

**文件：**
- 修改：`frontend/src/views/system-mgmt/permissions/PermissionListView.vue`

- [ ] **步骤 1：替换 moduleOptions 为从常量导入**

在 script setup 顶部：

```ts
import { BACKEND_MODULE_OPTIONS, DESKTOP_MODULE_OPTIONS } from '@/types/permission-group'
import { permissionScopeApi } from '@/api/permission'
```

并删除原 `const moduleOptions = [...]`。

- [ ] **步骤 2：新增 scopes 状态**

```ts
const backendModulesSelection = ref<string[]>([])
const desktopModulesSelection = ref<string[]>([])
```

- [ ] **步骤 3：handleAdd/handleUpdate 加载 scope 数据**

```ts
const handleUpdate = async (row?: any) => {
  const id = row?.id ?? ids.value[0]
  if (!id) return
  try {
    const { data } = await permissionApi.getById(id)
    form.groupName = data.groupName
    form.remark = data.remark || ''
    const { data: scopes } = await permissionScopeApi.list(id)
    const backend = scopes.find((s: any) => s.scope === 'backend')
    const desktop = scopes.find((s: any) => s.scope === 'desktop')
    backendModulesSelection.value = backend?.modules || []
    desktopModulesSelection.value = desktop?.modules || []
    title.value = '修改权限组'; open.value = true
  } catch { ElMessage.error('获取权限详情失败') }
}
```

- [ ] **步骤 4：handleSubmit 写入 scope**

```ts
const handleSubmit = async () => {
  try {
    const data = { groupName: form.groupName, remark: form.remark }
    let groupId: number
    if (ids.value.length === 1) {
      await permissionApi.update(ids.value[0], data)
      groupId = ids.value[0]
    } else {
      const { data: created } = await permissionApi.create(data)
      groupId = created.id
    }
    await permissionScopeApi.upsert(groupId, 'backend', backendModulesSelection.value)
    await permissionScopeApi.upsert(groupId, 'desktop', desktopModulesSelection.value)
    ElMessage.success('保存成功')
    open.value = false; fetchList()
  } catch { ElMessage.error('操作失败') }
}
```

- [ ] **步骤 5：弹窗模板替换**

把 `handleModuleChange` 旧的多选 `<el-select multiple>` 替换为：

```html
<el-form-item label="后台模块">
  <el-checkbox-group v-model="backendModulesSelection">
    <el-checkbox v-for="o in BACKEND_MODULE_OPTIONS" :key="o.value" :value="o.value">{{ o.label }}</el-checkbox>
  </el-checkbox-group>
</el-form-item>
<el-form-item label="桌面端模块">
  <el-checkbox-group v-model="desktopModulesSelection">
    <el-checkbox v-for="o in DESKTOP_MODULE_OPTIONS" :key="o.value" :value="o.value">{{ o.label }}</el-checkbox>
  </el-checkbox-group>
</el-form-item>
```

- [ ] **步骤 6：编译**

```bash
cd frontend && npx vue-tsc --noEmit
```

预期：0 error。

- [ ] **步骤 7：Commit**

```bash
git add frontend/src/views/system-mgmt/permissions/PermissionListView.vue
git commit -m "feat(permission): split permission dialog into backend/desktop panels"
```

---

## 任务 11：桌面端 Models/User.cs 增加 DesktopModules

**文件：**
- 修改：`desktop/ZlinksPackageSystem.Desktop/Models/User.cs`

- [ ] **步骤 1：读取现状**

```bash
cat desktop/ZlinksPackageSystem.Desktop/Models/User.cs
```

- [ ] **步骤 2：增加字段**

```csharp
public class User {
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? GroupId { get; set; }
    public string Remark { get; set; } = string.Empty;
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public List<string> DesktopModules { get; set; } = new();
}
```

- [ ] **步骤 3：构建**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet build -c Debug
```

预期：Build succeeded。

- [ ] **步骤 4：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/Models/User.cs
git commit -m "feat(desktop): add DesktopModules to User model"
```

---

## 任务 12：桌面端 AuthService 增加 FetchDesktopModulesAsync

**文件：**
- 修改：`desktop/ZlinksPackageSystem.Desktop/Services/AuthService.cs`

- [ ] **步骤 1：读取现状**

```bash
grep -n "LoginAsync\|GetCurrentUserAsync\|HttpClient\|_apiService" desktop/ZlinksPackageSystem.Desktop/Services/AuthService.cs
```

- [ ] **步骤 2：新增预留方法**

在 `AuthService` 类内增加：

```csharp
public async Task<List<string>> FetchDesktopModulesAsync()
{
    try
    {
        var user = await GetCurrentUserAsync();
        return user?.DesktopModules ?? new List<string>();
    }
    catch
    {
        return new List<string> { "all" };
    }
}
```

- [ ] **步骤 3：LoginAsync 内本地 admin 跳过**

定位 `LoginAsync`（`AuthService.cs:21-52`），在 `_useLocalAccount = true` 之后添加注释：`// 本地 admin/admin：默认桌面端全可见，模块列表保持 ["all"]`，不需要再调 FetchDesktopModulesAsync。

- [ ] **步骤 4：构建**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet build -c Debug
```

预期：Build succeeded。

- [ ] **步骤 5：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/Services/AuthService.cs
git commit -m "feat(desktop): add FetchDesktopModulesAsync stub in AuthService"
```

---

## 任务 13：桌面端 MainViewModel 增加 IsXxxVisible 计算属性

**文件：**
- 修改：`desktop/ZlinksPackageSystem.Desktop/ViewModels/MainViewModel.cs`

- [ ] **步骤 1：读取现状**

```bash
grep -n "NavigateTo\|IsLoggedIn\|CurrentUser" desktop/ZlinksPackageSystem.Desktop/ViewModels/MainViewModel.cs
```

- [ ] **步骤 2：增加计算属性**

在 MainViewModel 类内增加：

```csharp
public bool IsHomeVisible => CheckModule("home");
public bool IsGamesVisible => CheckModule("games");
public bool IsProductsVisible => CheckModule("products");
public bool IsParametersVisible => CheckModule("parameters");
public bool IsTestsVisible => CheckModule("tests");
public bool IsToolLibraryVisible => CheckModule("tool-library");
public bool IsNotificationVisible => CheckModule("notification");
public bool IsSettingsVisible => CheckModule("settings");

private bool CheckModule(string key)
{
    var user = _authService.CurrentUser;
    if (user == null) return true;
    var mods = user.DesktopModules;
    if (mods == null || mods.Count == 0) return true;
    if (mods.Contains("all")) return true;
    return mods.Contains(key);
}
```

如果 `_authService.CurrentUser` 不存在，可改为通过静态字段或事件传值；最简方案是直接读 `SettingsViewModel` 或在 LoginAsync 成功时缓存一个 CurrentUser 字段。

- [ ] **步骤 3：构建**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet build -c Debug
```

预期：Build succeeded。

- [ ] **步骤 4：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/ViewModels/MainViewModel.cs
git commit -m "feat(desktop): expose IsXxxVisible for menu filtering"
```

---

## 任务 14：桌面端 MainWindow.axaml 绑定 IsVisible

**文件：**
- 修改：`desktop/ZlinksPackageSystem.Desktop/Views/MainWindow.axaml`

- [ ] **步骤 1：定位 8 个 MenuItem**

```bash
grep -n "MenuItem\|NavigateCommand" desktop/ZlinksPackageSystem.Desktop/Views/MainWindow.axaml
```

- [ ] **步骤 2：为每个 MenuItem 加 IsVisible**

把 8 个 `<MenuItem>` 分别加上：

```xml
<MenuItem Header="🏠 首页" Command="{Binding NavigateToHomeCommand}" IsVisible="{Binding IsHomeVisible}" />
<MenuItem Header="🎮 游戏" Command="{Binding NavigateToGamesCommand}" IsVisible="{Binding IsGamesVisible}" />
<MenuItem Header="📦 产品" Command="{Binding NavigateToProductsCommand}" IsVisible="{Binding IsProductsVisible}" />
<MenuItem Header="📋 参数" Command="{Binding NavigateToParametersCommand}" IsVisible="{Binding IsParametersVisible}" />
<MenuItem Header="🧪 测试" Command="{Binding NavigateToTestsCommand}" IsVisible="{Binding IsTestsVisible}" />
<MenuItem Header="🔧 工具库" Command="{Binding NavigateToToolLibraryCommand}" IsVisible="{Binding IsToolLibraryVisible}" />
<MenuItem Header="🔔 消息中心" Command="{Binding NavigateToNotificationCommand}" IsVisible="{Binding IsNotificationVisible}" />
<MenuItem Header="⚙️ 设置" Command="{Binding NavigateToSettingsCommand}" IsVisible="{Binding IsSettingsVisible}" />
```

- [ ] **步骤 3：构建**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet build -c Debug
```

预期：Build succeeded。

- [ ] **步骤 4：Commit**

```bash
git add desktop/ZlinksPackageSystem.Desktop/Views/MainWindow.axaml
git commit -m "feat(desktop): bind MenuItem.IsVisible to module permissions"
```

---

## 任务 15：设计文档

**文件：**
- 新增：`docs/superpowers/specs/2026-07-17-permission-scope-split.md`

- [ ] **步骤 1：写入设计规格文档**

```markdown
# 权限组拆分：后台 / 桌面 规格说明

## 目标

将权限组从单一 `groupPermission.modules` 拆分为两个独立的 scope：`backend`（Web 后台管理系统）与 `desktop`（桌面端）。用于限制不同用户/用户组在两个系统中的模块可见性与操作能力。

## 数据模型

### 新表 permission_scope

| 列 | 类型 | 约束 |
|---|---|---|
| id | BIGINT | PK AUTO |
| group_id | BIGINT | FK → permission_group(id) |
| scope | VARCHAR(16) | 'backend' \| 'desktop' |
| modules_text | TEXT | JSON 数组 |
| create_by / create_time / update_by / update_time / is_deleted | 标准审计 | — |
| UNIQUE(group_id, scope) | | 防止重复 |

### 兼容旧字段

`permission_group.group_permission` 列保留（deprecated），前端不再写入。

## 模块白名单

### backend
home, system-mgmt, system-settings, package, monitor, all

### desktop
home, games, products, parameters, tests, tool-library, notification, settings, all

## 合并规则（用户属于多组时）

- 取所有 `permission_scope.modules_text` 解析后数组的**并集**
- 任一 group 含 `'all'` → 该 scope 视作 `'all'`

## API 端点

```
GET    /api/permission-groups/{groupId}/scopes
GET    /api/permission-groups/{groupId}/scopes/{scope}
PUT    /api/permission-groups/{groupId}/scopes/{scope}   # upsert { modules: [...] }
DELETE /api/permission-groups/{groupId}/scopes/{scope}
```

登录端点扩展：
- `/api/auth/info` 返回增加 `desktopModules`
- `/api/getInfo` 返回增加 `modules: { backend, desktop }`

## 前端 UI

- PermissionListView 双 panel（后台 + 桌面端）
- MainLayout 按 backendModules 过滤 4 个父菜单
- 桌面端 MainWindow 8 个 MenuItem 按 desktopModules 过滤

## 默认权限组迁移

| groupName | backend | desktop |
|---|---|---|
| 管理员组 | ["all"] | ["all"] |
| 开发组 | ["home","package"] | ["home","products"] |
| 测试组 | ["home","package"] | ["home","tests"] |
| 运营组 | ["home","package"] | ["home","products"] |

## 风险与缓解

| 风险 | 缓解 |
|---|---|
| 旧 groupPermission JSON 残留 | 保留列、前端不再写入；提供迁移文档 |
| 多组合并语义模糊 | 默认并集；`all` 通配优先 |
| 父子菜单不联动 | 同时按 `parent` 全模块 value 检查 |
| 桌面端 admin 本地登录无模块 | MainViewModel 强制 IsXxxVisible=true |
| 旧 auth/info 路径无 permissions | 后端在两路径都增加 modules 合并 |
```

- [ ] **步骤 2：Commit**

```bash
git add docs/superpowers/specs/2026-07-17-permission-scope-split.md
git commit -m "docs(permission): add scope-split design spec"
```

---

## 任务 16：最终验证

**文件：** 无（仅运行命令）

- [ ] **步骤 1：后端编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：BUILD SUCCESS。

- [ ] **步骤 2：前端 typecheck + build**

```bash
cd frontend && npx vue-tsc --noEmit && bun run build
```

预期：0 error，构建成功。

- [ ] **步骤 3：桌面端 build**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet build -c Debug
```

预期：Build succeeded。

- [ ] **步骤 4：启动后端（H2）+ Web 冒烟**

```bash
# 启动后端
cd backend && java -jar target/package-system-1.0.0.jar --spring.profiles.active=h2 &
# 启动前端
cd frontend && bun run dev
```

浏览器访问：
1. 登录 admin → 权限管理 → 双 panel 可见
2. 勾选/取消某模块，提交，刷新页面仍生效
3. 退出 admin，使用其他用户登录 → 左侧菜单按 backendModules 过滤
4. 调用 `/api/auth/info` → 响应包含 `desktopModules`

- [ ] **步骤 5：桌面端冒烟**

```bash
cd desktop/ZlinksPackageSystem.Desktop && dotnet run
```

登录 admin/admin → 8 个 MenuItem 全部可见（本地账户通配）。

---

## 自检

- 后端：`permission_scope` 表已建；Controller 端点齐全；`/auth/info` 与 `/getInfo` 返回 modules。
- 前端：双 panel 写入正常；MainLayout 过滤生效；类型与 API 一致。
- 桌面端：模型预留字段；AuthService 预留方法；MainViewModel 计算属性；MainWindow 绑定。
- 文档：specs 文件提交。

---

## 执行选项

1. 子代理驱动（推荐）：按任务顺序派遣 1-16，每任务配 spec + quality 两阶段审查。
2. 内联执行：在当前会话中按顺序执行。