# 打包管理模块整合 + 广告参数完整 CRUD 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 将“游戏管理 / 产品管理 / 测试管理”整合为“打包管理”父模块下的子模块，新增“广告参数”子模块，并补齐荣耀/VIVO/华为三套参数的后端完整 CRUD。

**架构：** 前端硬编码菜单与路由（`MainLayout.vue` + `router/index.ts`），父级用 `<el-sub-menu>`，子项不带 icon，与 `monitor` 一致。后端按方案 A 三个独立 Controller（`/api/honor-params`、`/api/vivo-params`、`/api/huawei-params`），沿用 `GameController/ProductController` 的 Service/Impl/Result/PageResult/软删/审计模式；参数表 `game_id` 重命名为 `product_id`，与产品建立 1:N 关系。

**技术栈：** Vue 3 + Vue Router 4 + Element Plus + Pinia + axios + TypeScript（前端）；Spring Boot 3.3.7 + MyBatis-Plus 3.5.7 + Knife4j 4.5 + EasyExcel 3.3 + Bean Validation（H2/MySQL/PostgreSQL）。

**约定：** 父菜单插在“首页”之后；旧 `views/{game,product,test}` 删除；测试管理保留 15 行占位；`appSecret` 不做任何隐藏/屏蔽；`modules` 白名单将 `games/products/tests` 替换为 `package/ad-params`；默认权限组 `modules` JSON 本期不动（提示用户在权限管理页调整）。

---

## 文件结构（前置）

修改：
- `frontend/src/router/index.ts`
- `frontend/src/layouts/MainLayout.vue`
- `frontend/src/views/home/HomeView.vue`
- `frontend/src/views/permission/PermissionListView.vue`
- `frontend/src/api/product.ts`
- `frontend/src/stores/tabs.ts`（仅确认旧 path 是否需跳过）
- `backend/src/main/java/com/zlinks/package_system/entity/HonorParam.java`
- `backend/src/main/java/com/zlinks/package_system/entity/VivoParam.java`
- `backend/src/main/java/com/zlinks/package_system/entity/HuaweiParam.java`
- `backend/src/main/java/com/zlinks/package_system/controller/ProductController.java`
- `backend/src/main/resources/schema.sql`
- `scripts/init-database.sql`
- `scripts/init-database-pgsql.sql`

新增：
- `frontend/src/views/package/GameListView.vue`
- `frontend/src/views/package/ProductListView.vue`
- `frontend/src/views/package/TestListView.vue`
- `frontend/src/views/package/AdParamListView.vue`
- `frontend/src/api/ad-param.ts`
- `frontend/src/types/ad-param.ts`
- `backend/src/main/java/com/zlinks/package_system/dto/HonorParamRequest.java`
- `backend/src/main/java/com/zlinks/package_system/dto/VivoParamRequest.java`
- `backend/src/main/java/com/zlinks/package_system/dto/HuaweiParamRequest.java`
- `backend/src/main/java/com/zlinks/package_system/dto/HonorParamExcelDTO.java`
- `backend/src/main/java/com/zlinks/package_system/dto/VivoParamExcelDTO.java`
- `backend/src/main/java/com/zlinks/package_system/dto/HuaweiParamExcelDTO.java`
- `backend/src/main/java/com/zlinks/package_system/service/IHonorParamService.java`
- `backend/src/main/java/com/zlinks/package_system/service/impl/HonorParamServiceImpl.java`
- `backend/src/main/java/com/zlinks/package_system/service/IVivoParamService.java`
- `backend/src/main/java/com/zlinks/package_system/service/impl/VivoParamServiceImpl.java`
- `backend/src/main/java/com/zlinks/package_system/service/IHuaweiParamService.java`
- `backend/src/main/java/com/zlinks/package_system/service/impl/HuaweiParamServiceImpl.java`
- `backend/src/main/java/com/zlinks/package_system/controller/HonorParamController.java`
- `backend/src/main/java/com/zlinks/package_system/controller/VivoParamController.java`
- `backend/src/main/java/com/zlinks/package_system/controller/HuaweiParamController.java`

删除：
- `frontend/src/views/game/`、`frontend/src/views/product/`、`frontend/src/views/test/`

---

## 任务 1：迁移 3 个旧视图到 `views/package/`

**文件：**
- 创建：`frontend/src/views/package/GameListView.vue`（从 `views/game/GameListView.vue` 平移）
- 创建：`frontend/src/views/package/ProductListView.vue`（从 `views/product/ProductListView.vue` 平移）
- 创建：`frontend/src/views/package/TestListView.vue`（从 `views/test/TestListView.vue` 平移，保留占位）
- 删除：`frontend/src/views/game/`、`frontend/src/views/product/`、`frontend/src/views/test/`

- [ ] **步骤 1：复制 3 个视图**

```bash
mkdir -p frontend/src/views/package
cp frontend/src/views/game/GameListView.vue      frontend/src/views/package/GameListView.vue
cp frontend/src/views/product/ProductListView.vue frontend/src/views/package/ProductListView.vue
cp frontend/src/views/test/TestListView.vue      frontend/src/views/package/TestListView.vue
```

- [ ] **步骤 2：验证组件内 import 路径不变**

```bash
grep -n "@/api/\|@/types/" frontend/src/views/package/*.vue
```

预期：与 `views/game|product|test/` 旧版的 `import '@/api/...'@/types/...'` 完全一致（不需要改）。

- [ ] **步骤 3：删除旧目录**

```bash
rm -rf frontend/src/views/game frontend/src/views/product frontend/src/views/test
ls frontend/src/views | sort
```

预期：输出含 `package`，不再含 `game/product/test`。

- [ ] **步骤 4：Commit**

```bash
git add frontend/src/views/package frontend/src/views/game frontend/src/views/product frontend/src/views/test
git commit -m "refactor(views): migrate game/product/test views under views/package"
```

---

## 任务 2：路由改造

**文件：**
- 修改：`frontend/src/router/index.ts:24-41`

- [ ] **步骤 1：替换路由 children**

把 router/index.ts 中 L24-L41 三条 `games/products/tests` 路由替换为：

```ts
{
  path: 'package/games',
  name: 'package-games',
  component: () => import('@/views/package/GameListView.vue'),
  meta: { title: '游戏管理' },
},
{
  path: 'package/products',
  name: 'package-products',
  component: () => import('@/views/package/ProductListView.vue'),
  meta: { title: '产品管理' },
},
{
  path: 'package/tests',
  name: 'package-tests',
  component: () => import('@/views/package/TestListView.vue'),
  meta: { title: '测试管理' },
},
{
  path: 'package/ad-params',
  name: 'package-ad-params',
  component: () => import('@/views/package/AdParamListView.vue'),
  meta: { title: '广告参数' },
},
```

- [ ] **步骤 2：确认无残留**

```bash
grep -n "name: 'games'\\|name: 'products'\\|name: 'tests'" frontend/src/router/index.ts
grep -n "path: 'games'\\|path: 'products'\\|path: 'tests'" frontend/src/router/index.ts
```

预期：无输出。

- [ ] **步骤 3：Commit**

```bash
git add frontend/src/router/index.ts
git commit -m "refactor(router): rename management routes under /package/*"
```

---

## 任务 3：菜单改造

**文件：**
- 修改：`frontend/src/layouts/MainLayout.vue:6-26, 43-69, 116-145`

- [ ] **步骤 1：补充 `Box` icon import**

在 `MainLayout.vue:6-26` 的 `from '@element-plus/icons-vue'` 列表末尾添加 `Box`：

```ts
import {
  HomeFilled,
  Monitor,
  Goods,
  Connection,
  User,
  Lock,
  OfficeBuilding,
  Document,
  Bell,
  Key,
  SwitchButton,
  Fold,
  Expand,
  ArrowDown,
  Close,
  Tickets,
  Menu as IconMenu,
  Tools,
  Setting,
  Box,
} from '@element-plus/icons-vue'
```

- [ ] **步骤 2：从 menuItems 删除三项**

在 `MainLayout.vue:43-59` 的 `menuItems` 中删除下面三行：

```ts
{ index: '/games', icon: Monitor, title: '游戏管理' },
{ index: '/products', icon: Goods, title: '产品管理' },
{ index: '/tests', icon: Connection, title: '测试管理' },
```

- [ ] **步骤 3：新增 packageItems**

在 `monitorItems` 之后（`MainLayout.vue:69` 之后）新增：

```ts
const packageItems = [
  { index: '/package/games',     title: '游戏管理' },
  { index: '/package/products',  title: '产品管理' },
  { index: '/package/tests',     title: '测试管理' },
  { index: '/package/ad-params', title: '广告参数' },
]
```

- [ ] **步骤 4：插入新 el-sub-menu**

在 `MainLayout.vue` 中原 `<el-sub-menu index="monitor">` 之前（L132）插入：

```html
<el-sub-menu index="package">
  <template #title>
    <el-icon><Box /></el-icon>
    <span>打包管理</span>
  </template>
  <el-menu-item
    v-for="item in packageItems"
    :key="item.index"
    :index="item.index"
  >
    {{ item.title }}
  </el-menu-item>
</el-sub-menu>
```

- [ ] **步骤 5：Commit**

```bash
git add frontend/src/layouts/MainLayout.vue
git commit -m "feat(menu): add 打包管理 parent menu with 4 children"
```

---

## 任务 4：快捷入口与权限白名单

**文件：**
- 修改：`frontend/src/views/home/HomeView.vue:34-39`
- 修改：`frontend/src/views/permission/PermissionListView.vue:43-54`

- [ ] **步骤 1：合并 quickLinks**

把 `HomeView.vue:34-39`：

```ts
const quickLinks = ref([
  { id: 1, title: '游戏管理', icon: 'Monitor', path: '/games' },
  { id: 2, title: '产品管理', icon: 'Goods', path: '/products' },
  { id: 3, title: '测试管理', icon: 'Connection', path: '/tests' },
  { id: 4, title: '用户管理', icon: 'User', path: '/system/user' },
])
```

替换为：

```ts
const quickLinks = ref([
  { id: 1, title: '首页', icon: 'HomeFilled', path: '/' },
  { id: 2, title: '打包管理', icon: 'Box', path: '/package/games' },
  { id: 3, title: '用户管理', icon: 'User', path: '/system/user' },
  { id: 4, title: '系统监控', icon: 'Monitor', path: '/system/monitor/server' },
])
```

- [ ] **步骤 2：更新权限白名单**

把 `PermissionListView.vue:43-54`：

```ts
const moduleOptions = [
  { label: '全部模块', value: 'all' },
  { label: '首页', value: 'home' },
  { label: '游戏管理', value: 'games' },
  { label: '产品管理', value: 'products' },
  { label: '测试管理', value: 'tests' },
  { label: '用户管理', value: 'users' },
  { label: '权限管理', value: 'permissions' },
  { label: '公司管理', value: 'companies' },
  { label: '软著管理', value: 'copyrights' },
  { label: '通知管理', value: 'notifications' },
]
```

替换为：

```ts
const moduleOptions = [
  { label: '全部模块', value: 'all' },
  { label: '首页', value: 'home' },
  { label: '打包管理', value: 'package' },
  { label: '广告参数', value: 'ad-params' },
  { label: '用户管理', value: 'users' },
  { label: '权限管理', value: 'permissions' },
  { label: '公司管理', value: 'companies' },
  { label: '软著管理', value: 'copyrights' },
  { label: '通知管理', value: 'notifications' },
]
```

- [ ] **步骤 3：Commit**

```bash
git add frontend/src/views/home/HomeView.vue frontend/src/views/permission/PermissionListView.vue
git commit -m "feat(ui): sync quick links & module whitelist with package menu"
```

---

## 任务 5：实体改用 productId

**文件：**
- 修改：`backend/src/main/java/com/zlinks/package_system/entity/HonorParam.java`
- 修改：`backend/src/main/java/com/zlinks/package_system/entity/VivoParam.java`
- 修改：`backend/src/main/java/com/zlinks/package_system/entity/HuaweiParam.java`

- [ ] **步骤 1：调整 HonorParam 字段**

把 `HonorParam.java:18` 的 `private Long gameId;` 替换为：

```java
@TableField("product_id")
private Long productId;
```

并在 imports 区加入：

```java
import com.baomidou.mybatisplus.annotation.TableField;
```

- [ ] **步骤 2：调整 VivoParam 字段**

把 `VivoParam.java:16` 的 `private Long gameId;` 替换为：

```java
@TableField("product_id")
private Long productId;
```

并加入 `import com.baomidou.mybatisplus.annotation.TableField;`。

- [ ] **步骤 3：调整 HuaweiParam 字段**

把 `HuaweiParam.java:16` 的 `private Long gameId;` 替换为：

```java
@TableField("product_id")
private Long productId;
```

并加入 `import com.baomidou.mybatisplus.annotation.TableField;`。

- [ ] **步骤 4：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/entity/HonorParam.java \
        backend/src/main/java/com/zlinks/package_system/entity/VivoParam.java \
        backend/src/main/java/com/zlinks/package_system/entity/HuaweiParam.java
git commit -m "refactor(entity): rename param gameId to productId"
```

---

## 任务 6：数据库 schema 调整

**文件：**
- 修改：`backend/src/main/resources/schema.sql:144-195`
- 修改：`scripts/init-database.sql:89-146`
- 修改：`scripts/init-database-pgsql.sql:148-211`

- [ ] **步骤 1：H2 schema**

把 `backend/src/main/resources/schema.sql` 中 `honor_param / vivo_param / huawei_param` 三表的 `game_id BIGINT` 改为 `product_id BIGINT NOT NULL`，并在 `remark` 后增加：

```sql
`create_by` VARCHAR(64) DEFAULT '',
`update_by` VARCHAR(64) DEFAULT '',
```

最终 H2 表结构（HonorParam 示例，其它两表类比）：

```sql
-- 荣耀参数表
CREATE TABLE IF NOT EXISTS `honor_param` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `product_id` BIGINT NOT NULL,
    `package_name` VARCHAR(100),
    `app_id` VARCHAR(100),
    `app_secret` VARCHAR(100),
    `media_id` VARCHAR(100),
    `agconnect_path` VARCHAR(255),
    `td_app_id` VARCHAR(100),
    `ad_param_status` VARCHAR(20),
    `list_status` VARCHAR(20),
    `operator` VARCHAR(50),
    `remark` TEXT,
    `create_by` VARCHAR(64) DEFAULT '',
    `create_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `update_by` VARCHAR(64) DEFAULT '',
    `update_time` TIMESTAMP,
    `is_deleted` TINYINT DEFAULT 0
);
```

- [ ] **步骤 2：MySQL init 脚本**

对 `scripts/init-database.sql` 中三表执行相同改动：列名 `game_id` → `product_id`，并补充 `create_by / update_by`。

- [ ] **步骤 3：PostgreSQL init 脚本**

对 `scripts/init-database-pgsql.sql` 中三表执行相同改动：列名 `game_id` → `product_id`，补充 `create_by / update_by`；注意 PG 的注释 `COMMENT ON COLUMN` 与触发器函数引用需保持一致。

- [ ] **步骤 4：Commit**

```bash
git add backend/src/main/resources/schema.sql scripts/init-database.sql scripts/init-database-pgsql.sql
git commit -m "chore(db): rename param.game_id to product_id and add audit cols"
```

---

## 任务 7：新增 Honor/Vivo/Huawei Param DTO 与 Service/Impl

**文件：**
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/HonorParamRequest.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/VivoParamRequest.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/HuaweiParamRequest.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/HonorParamExcelDTO.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/VivoParamExcelDTO.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/dto/HuaweiParamExcelDTO.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/IHonorParamService.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/impl/HonorParamServiceImpl.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/IVivoParamService.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/impl/VivoParamServiceImpl.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/IHuaweiParamService.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/service/impl/HuaweiParamServiceImpl.java`

- [ ] **步骤 1：新增 HonorParamRequest**

```java
package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class HonorParamRequest {
    @NotNull(message = "请选择产品")
    private Long productId;
    private String packageName;
    private String appId;
    private String appSecret;
    private String mediaId;
    private String agconnectPath;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}
```

- [ ] **步骤 2：新增 VivoParamRequest**

```java
package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class VivoParamRequest {
    @NotNull(message = "请选择产品")
    private Long productId;
    private String appId;
    private String contractStatus;
    private String mediaId;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}
```

- [ ] **步骤 3：新增 HuaweiParamRequest**

```java
package com.zlinks.package_system.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class HuaweiParamRequest {
    @NotNull(message = "请选择产品")
    private Long productId;
    private String packageName;
    private String appId;
    private String agconnectPath;
    private String tdAppId;
    private String adParamStatus;
    private String listStatus;
    private String operator;
    private String remark;
}
```

- [ ] **步骤 4：新增 HonorParamExcelDTO**

```java
package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class HonorParamExcelDTO {
    @ExcelProperty("产品ID") private Long productId;
    @ExcelProperty("包名") private String packageName;
    @ExcelProperty("AppId") private String appId;
    @ExcelProperty("AppSecret") private String appSecret;
    @ExcelProperty("MediaId") private String mediaId;
    @ExcelProperty("AGConnect路径") private String agconnectPath;
    @ExcelProperty("TDAppId") private String tdAppId;
    @ExcelProperty("广告参数状态") private String adParamStatus;
    @ExcelProperty("上架状态") private String listStatus;
    @ExcelProperty("操作人") private String operator;
    @ExcelProperty("备注") private String remark;
}
```

- [ ] **步骤 5：新增 VivoParamExcelDTO**

```java
package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class VivoParamExcelDTO {
    @ExcelProperty("产品ID") private Long productId;
    @ExcelProperty("AppId") private String appId;
    @ExcelProperty("合同状态") private String contractStatus;
    @ExcelProperty("MediaId") private String mediaId;
    @ExcelProperty("TDAppId") private String tdAppId;
    @ExcelProperty("广告参数状态") private String adParamStatus;
    @ExcelProperty("上架状态") private String listStatus;
    @ExcelProperty("操作人") private String operator;
    @ExcelProperty("备注") private String remark;
}
```

- [ ] **步骤 6：新增 HuaweiParamExcelDTO**

```java
package com.zlinks.package_system.dto;

import com.alibaba.excel.annotation.ExcelProperty;
import lombok.Data;

@Data
public class HuaweiParamExcelDTO {
    @ExcelProperty("产品ID") private Long productId;
    @ExcelProperty("包名") private String packageName;
    @ExcelProperty("AppId") private String appId;
    @ExcelProperty("AGConnect路径") private String agconnectPath;
    @ExcelProperty("TDAppId") private String tdAppId;
    @ExcelProperty("广告参数状态") private String adParamStatus;
    @ExcelProperty("上架状态") private String listStatus;
    @ExcelProperty("操作人") private String operator;
    @ExcelProperty("备注") private String remark;
}
```

- [ ] **步骤 7：新增 IHonorParamService**

```java
package com.zlinks.package_system.service;

import com.baomidou.mybatisplus.extension.service.IService;
import com.zlinks.package_system.entity.HonorParam;

public interface IHonorParamService extends IService<HonorParam> {
}
```

- [ ] **步骤 8：新增 HonorParamServiceImpl**

```java
package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.entity.HonorParam;
import com.zlinks.package_system.mapper.HonorParamMapper;
import com.zlinks.package_system.service.IHonorParamService;
import org.springframework.stereotype.Service;

@Service
public class HonorParamServiceImpl extends ServiceImpl<HonorParamMapper, HonorParam> implements IHonorParamService {
}
```

- [ ] **步骤 9：重复步骤 7-8 创建 IVivoParamService + VivoParamServiceImpl / IHuaweiParamService + HuaweiParamServiceImpl**

按相同结构生成四个文件，泛型替换为 `VivoParam` / `HuaweiParam` 与对应 Mapper。

- [ ] **步骤 10：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/dto backend/src/main/java/com/zlinks/package_system/service
git commit -m "feat(param): add request/excel DTO and service skeleton for honor/vivo/huawei"
```

---

## 任务 8：新增 Honor/Vivo/Huawei Param Controller

**文件：**
- 新增：`backend/src/main/java/com/zlinks/package_system/controller/HonorParamController.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/controller/VivoParamController.java`
- 新增：`backend/src/main/java/com/zlinks/package_system/controller/HuaweiParamController.java`

- [ ] **步骤 1：参考 ProductController 复制 HonorParamController 骨架**

完整 `HonorParamController.java` 内容：

```java
package com.zlinks.package_system.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zlinks.package_system.dto.HonorParamExcelDTO;
import com.zlinks.package_system.dto.HonorParamRequest;
import com.zlinks.package_system.entity.HonorParam;
import com.zlinks.package_system.service.IHonorParamService;
import com.zlinks.package_system.util.BusinessException;
import com.zlinks.package_system.util.PageResult;
import com.zlinks.package_system.util.Result;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.beans.BeanUtils;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@Tag(name = "荣耀参数")
@RestController
@RequestMapping("/api/honor-params")
@RequiredArgsConstructor
public class HonorParamController {
    private final IHonorParamService service;

    @Operation(summary = "统计")
    @GetMapping("/counts")
    public Result<Map<String, Long>> counts() {
        long total = service.count();
        long active = service.count(new LambdaQueryWrapper<HonorParam>().eq(HonorParam::getAdParamStatus, "active"));
        Map<String, Long> map = new LinkedHashMap<>();
        map.put("total", total);
        map.put("active", active);
        return Result.success(map);
    }

    @Operation(summary = "分页")
    @GetMapping
    public Result<PageResult<HonorParam>> page(@RequestParam(defaultValue = "1") long current,
                                               @RequestParam(defaultValue = "10") long size,
                                               @RequestParam(required = false) Long productId,
                                               @RequestParam(required = false) String adParamStatus,
                                               @RequestParam(required = false) String listStatus) {
        LambdaQueryWrapper<HonorParam> wrapper = new LambdaQueryWrapper<>();
        if (productId != null) wrapper.eq(HonorParam::getProductId, productId);
        if (adParamStatus != null && !adParamStatus.isEmpty()) wrapper.eq(HonorParam::getAdParamStatus, adParamStatus);
        if (listStatus != null && !listStatus.isEmpty()) wrapper.eq(HonorParam::getListStatus, listStatus);
        wrapper.orderByDesc(HonorParam::getUpdateTime);
        Page<HonorParam> page = service.page(new Page<>(current, size), wrapper);
        return Result.success(PageResult.of(page));
    }

    @Operation(summary = "筛选项")
    @GetMapping("/options")
    public Result<Map<String, Object>> options() {
        Map<String, Object> map = new LinkedHashMap<>();
        map.put("adParamStatuses", List.of("pending", "active", "inactive"));
        map.put("listStatuses", List.of("listed", "unlisted", "paused"));
        return Result.success(map);
    }

    @Operation(summary = "详情")
    @GetMapping("/{id}")
    public Result<HonorParam> getById(@PathVariable Long id) {
        HonorParam entity = service.getById(id);
        if (entity == null) throw new BusinessException("荣耀参数不存在");
        return Result.success(entity);
    }

    @Operation(summary = "新增")
    @PostMapping
    public Result<HonorParam> create(@Valid @RequestBody HonorParamRequest req) {
        HonorParam entity = new HonorParam();
        BeanUtils.copyProperties(req, entity);
        service.save(entity);
        return Result.success(entity);
    }

    @Operation(summary = "更新")
    @PutMapping("/{id}")
    public Result<HonorParam> update(@PathVariable Long id, @Valid @RequestBody HonorParamRequest req) {
        HonorParam entity = service.getById(id);
        if (entity == null) throw new BusinessException("荣耀参数不存在");
        BeanUtils.copyProperties(req, entity);
        entity.setId(id);
        service.updateById(entity);
        return Result.success(entity);
    }

    @Operation(summary = "删除")
    @DeleteMapping("/{id}")
    public Result<Void> delete(@PathVariable Long id) {
        if (service.getById(id) == null) throw new BusinessException("荣耀参数不存在");
        service.removeById(id);
        return Result.success();
    }

    @Operation(summary = "导入")
    @PostMapping("/import")
    public Result<String> importFile(MultipartFile file) {
        if (file == null || file.isEmpty()) throw new BusinessException("文件不能为空");
        try (var in = file.getInputStream();
             var excel = new com.alibaba.excel.EasyExcel(in, HonorParamExcelDTO.class).sheet().headRowNumber(1).doReadStream()) {
            int count = 0;
            var list = new ArrayList<HonorParamExcelDTO>();
            excel.forEach(list::add);
            for (HonorParamExcelDTO dto : list) {
                HonorParam entity = new HonorParam();
                BeanUtils.copyProperties(dto, entity);
                service.save(entity);
                count++;
            }
            return Result.success("导入成功 " + count + " 条");
        } catch (IOException e) {
            throw new BusinessException("导入失败: " + e.getMessage());
        }
    }

    @Operation(summary = "导出")
    @GetMapping("/export")
    public void export(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse resp) throws IOException {
        List<HonorParam> all = service.list();
        List<HonorParamExcelDTO> rows = new ArrayList<>();
        for (HonorParam entity : all) {
            HonorParamExcelDTO dto = new HonorParamExcelDTO();
            BeanUtils.copyProperties(entity, dto);
            rows.add(dto);
        }
        resp.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        resp.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("honor-params.xlsx", StandardCharsets.UTF_8).replace("\\+", "%20");
        resp.setHeader("Content-disposition", "attachment;filename*=utf-8''" + fileName);
        com.alibaba.excel.EasyExcel.write(resp.getOutputStream(), HonorParamExcelDTO.class).sheet("Honor").doWrite(rows);
    }

    @Operation(summary = "模板")
    @GetMapping("/template")
    public void template(@RequestParam(defaultValue = "xlsx") String format, HttpServletResponse resp) throws IOException {
        resp.setContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        resp.setCharacterEncoding("utf-8");
        String fileName = URLEncoder.encode("honor-params-template.xlsx", StandardCharsets.UTF_8).replace("\\+", "%20");
        resp.setHeader("Content-disposition", "attachment;filename*=utf-8''" + fileName);
        com.alibaba.excel.EasyExcel.write(resp.getOutputStream(), HonorParamExcelDTO.class).sheet("Honor").doWrite(new ArrayList<>());
    }
}
```

- [ ] **步骤 2：复制为 VivoParamController**

```bash
cp backend/src/main/java/com/zlinks/package_system/controller/HonorParamController.java \
   backend/src/main/java/com/zlinks/package_system/controller/VivoParamController.java
```

然后用 `sed`/手动替换：类名 `HonorParamController` → `VivoParamController`、`HonorParam` → `VivoParam`、`HonorParamRequest` → `VivoParamRequest`、`HonorParamExcelDTO` → `VivoParamExcelDTO`、`IVivoParamService`、`"honor-params"` → `"vivo-params"`、`"荣耀参数"` → `"VIVO参数"`、`"荣耀参数不存在"` → `"VIVO参数不存在"`、`"honor-params.xlsx"` → `"vivo-params.xlsx"`、`"Vivo"` 等。

- [ ] **步骤 3：复制为 HuaweiParamController**

同上：替换为 `HuaweiParamController / HuaweiParam / HuaweiParamRequest / HuaweiParamExcelDTO / IHuaweiParamService / "huawei-params" / "华为参数" / "华为参数不存在" / "huawei-params.xlsx" / "Huawei"`。

- [ ] **步骤 4：编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：`BUILD SUCCESS`，无 error。

- [ ] **步骤 5：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/controller/HonorParamController.java \
        backend/src/main/java/com/zlinks/package_system/controller/VivoParamController.java \
        backend/src/main/java/com/zlinks/package_system/controller/HuaweiParamController.java
git commit -m "feat(controller): add Honor/Vivo/Huawei param CRUD endpoints"
```

---

## 任务 9：ProductController 新增 `/api/products/all`

**文件：**
- 修改：`backend/src/main/java/com/zlinks/package_system/controller/ProductController.java`

- [ ] **步骤 1：在 `@GetMapping` 集合中追加 `all` 端点**

定位 `ProductController` 中 `getAll()` 类似位置（或 `@GetMapping("/platforms")` 之后），新增：

```java
@Operation(summary = "全部产品（用于下拉）")
@GetMapping("/all")
public Result<List<Product>> all() {
    return Result.success(productService.list(new LambdaQueryWrapper<Product>().orderByAsc(Product::getId)));
}
```

（若类中没有 `ProductService productService`，请确认现有 import 与字段名后再调整；确保 import `LambdaQueryWrapper` 与 `Product`。）

- [ ] **步骤 2：编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：`BUILD SUCCESS`。

- [ ] **步骤 3：Commit**

```bash
git add backend/src/main/java/com/zlinks/package_system/controller/ProductController.java
git commit -m "feat(product): add /api/products/all for dropdown usage"
```

---

## 任务 10：前端 API/类型新增 ad-param

**文件：**
- 新增：`frontend/src/api/ad-param.ts`
- 新增：`frontend/src/types/ad-param.ts`
- 修改：`frontend/src/api/product.ts`

- [ ] **步骤 1：在 productApi 中添加 getAll**

在 `frontend/src/api/product.ts:65-66` 后追加：

```ts
getAll(): Promise<{ data: { id: number; packageName: string; gameName?: string }[] }> {
  return api.get('/products/all')
},
```

- [ ] **步骤 2：新增 types/ad-param.ts**

```ts
export interface HonorParam {
  id: number
  productId: number
  productName?: string
  packageName: string
  appId: string
  appSecret: string
  mediaId: string
  agconnectPath: string
  tdAppId: string
  adParamStatus: string
  listStatus: string
  operator: string
  remark: string
  createTime?: string
  updateTime?: string
}

export interface VivoParam {
  id: number
  productId: number
  productName?: string
  appId: string
  contractStatus: string
  mediaId: string
  tdAppId: string
  adParamStatus: string
  listStatus: string
  operator: string
  remark: string
  createTime?: string
  updateTime?: string
}

export interface HuaweiParam {
  id: number
  productId: number
  productName?: string
  packageName: string
  appId: string
  agconnectPath: string
  tdAppId: string
  adParamStatus: string
  listStatus: string
  operator: string
  remark: string
  createTime?: string
  updateTime?: string
}
```

- [ ] **步骤 3：新增 api/ad-param.ts**

```ts
import api from '@/api'
import type { HonorParam, VivoParam, HuaweiParam } from '@/types/ad-param'
import type { PageResult } from '@/types/common'

interface ListParams {
  current?: number
  size?: number
  productId?: number
  adParamStatus?: string
  listStatus?: string
}

function buildCrud<T>(path: string) {
  return {
    getList(params: ListParams): Promise<{ data: PageResult<T> }> {
      return api.get(path, { params })
    },
    getById(id: number): Promise<{ data: T }> {
      return api.get(`${path}/${id}`)
    },
    create(data: Partial<T>): Promise<{ data: T }> {
      return api.post(path, data)
    },
    update(id: number, data: Partial<T>): Promise<{ data: T }> {
      return api.put(`${path}/${id}`, data)
    },
    delete(id: number): Promise<void> {
      return api.delete(`${path}/${id}`)
    },
    importFile(file: File): Promise<{ data: string }> {
      const form = new FormData()
      form.append('file', file)
      return api.post(`${path}/import`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    exportFile(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
      return api.get(`${path}/export`, { params: { format }, responseType: 'blob' })
    },
    downloadTemplate(format: 'xlsx' | 'json' = 'xlsx'): Promise<Blob> {
      return api.get(`${path}/template`, { params: { format }, responseType: 'blob' })
    },
  }
}

export const honorApi = buildCrud<HonorParam>('/honor-params')
export const vivoApi = buildCrud<VivoParam>('/vivo-params')
export const huaweiApi = buildCrud<HuaweiParam>('/huawei-params')
```

- [ ] **步骤 4：Commit**

```bash
git add frontend/src/api/ad-param.ts frontend/src/types/ad-param.ts frontend/src/api/product.ts
git commit -m "feat(api): add ad-param CRUD wrappers and product getAll"
```

---

## 任务 11：AdParamListView 实现

**文件：**
- 新增：`frontend/src/views/package/AdParamListView.vue`

- [ ] **步骤 1：复制 GameListView.vue 作为骨架**

```bash
cp frontend/src/views/package/GameListView.vue frontend/src/views/package/AdParamListView.vue
```

- [ ] **步骤 2：替换脚本区**

把 `AdParamListView.vue` 顶部 `<script setup lang="ts">` 区（保留 ElementPlus 引入）替换为：

```ts
<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, Download, ArrowDown, Upload } from '@element-plus/icons-vue'
import { honorApi, vivoApi, huaweiApi } from '@/api/ad-param'
import { productApi } from '@/api/product'
import type { HonorParam, VivoParam, HuaweiParam } from '@/types/ad-param'

type Platform = 'honor' | 'vivo' | 'huawei'
type AnyParam = HonorParam | VivoParam | HuaweiParam

const activeTab = ref<Platform>('honor')
const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const fileInput = ref<HTMLInputElement>()
const list = ref<AnyParam[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const open = ref(false)
const title = ref('')
const ids = ref<number[]>([])
const single = ref(true)
const multiple = ref(true)
const products = ref<{ id: number; packageName: string }[]>([])
const searchForm = reactive({ productId: undefined as number | undefined, adParamStatus: '', listStatus: '' })

const apiMap: Record<Platform, ReturnType<typeof wrapHonor>> = {
  honor: wrapHonor(),
  vivo: wrapVivo(),
  huawei: wrapHuawei(),
}
function wrapHonor() { return {
  getList: honorApi.getList.bind(honorApi),
  getById: honorApi.getById.bind(honorApi),
  create: honorApi.create.bind(honorApi),
  update: honorApi.update.bind(honorApi),
  delete: honorApi.delete.bind(honorApi),
} }
function wrapVivo() { return {
  getList: vivoApi.getList.bind(vivoApi),
  getById: vivoApi.getById.bind(vivoApi),
  create: vivoApi.create.bind(vivoApi),
  update: vivoApi.update.bind(vivoApi),
  delete: vivoApi.delete.bind(vivoApi),
} }
function wrapHuawei() { return {
  getList: huaweiApi.getList.bind(huaweiApi),
  getById: huaweiApi.getById.bind(huaweiApi),
  create: huaweiApi.create.bind(huaweiApi),
  update: huaweiApi.update.bind(huaweiApi),
  delete: huaweiApi.delete.bind(huaweiApi),
} }

const form = reactive<Record<string, any>>({})

const rules = {
  productId: [{ required: true, message: '请选择产品', trigger: 'change' }],
}

const statusLabels: Record<string, string> = { pending: '待配置', active: '已配置', inactive: '已下线' }
const listLabels: Record<string, string> = { listed: '上架', unlisted: '下架', paused: '暂停' }

const tabLabels: { value: Platform; label: string }[] = [
  { value: 'honor', label: '荣耀参数' },
  { value: 'vivo', label: 'VIVO参数' },
  { value: 'huawei', label: '华为参数' },
]

const columnsByTab: Record<Platform, { key: string; label: string }[]> = {
  honor: [
    { key: 'productId', label: '产品ID' },
    { key: 'packageName', label: '包名' },
    { key: 'appId', label: 'AppId' },
    { key: 'mediaId', label: 'MediaId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
  vivo: [
    { key: 'productId', label: '产品ID' },
    { key: 'appId', label: 'AppId' },
    { key: 'contractStatus', label: '合同状态' },
    { key: 'mediaId', label: 'MediaId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
  huawei: [
    { key: 'productId', label: '产品ID' },
    { key: 'packageName', label: '包名' },
    { key: 'appId', label: 'AppId' },
    { key: 'tdAppId', label: 'TDAppId' },
    { key: 'adParamStatus', label: '广告参数' },
    { key: 'listStatus', label: '上架状态' },
    { key: 'operator', label: '操作人' },
  ],
}

const adStatusOptions = [
  { label: '待配置', value: 'pending' },
  { label: '已配置', value: 'active' },
  { label: '已下线', value: 'inactive' },
]
const listStatusOptions = [
  { label: '上架', value: 'listed' },
  { label: '下架', value: 'unlisted' },
  { label: '暂停', value: 'paused' },
]

const fetchList = async () => {
  loading.value = true
  try {
    const api = apiMap[activeTab.value]
    const params: Record<string, unknown> = { current: currentPage.value, size: pageSize.value }
    if (searchForm.productId) params.productId = searchForm.productId
    if (searchForm.adParamStatus) params.adParamStatus = searchForm.adParamStatus
    if (searchForm.listStatus) params.listStatus = searchForm.listStatus
    const { data } = await api.getList(params)
    list.value = data.records as AnyParam[]
    total.value = data.total
  } catch { ElMessage.error('获取参数列表失败') }
  finally { loading.value = false }
}

const fetchProducts = async () => {
  try {
    const { data } = await productApi.getAll()
    products.value = data.map((p: any) => ({ id: p.id, packageName: p.packageName }))
  } catch { ElMessage.error('获取产品列表失败') }
}

const handleTabChange = (val: Platform) => { activeTab.value = val; currentPage.value = 1; fetchList() }

const handleSearch = () => { currentPage.value = 1; fetchList() }
const handleReset = () => { searchForm.productId = undefined; searchForm.adParamStatus = ''; searchForm.listStatus = ''; handleSearch() }

const handleSelectionChange = (rows: AnyParam[]) => {
  ids.value = rows.map((r: any) => r.id).filter(Boolean) as number[]
  single.value = rows.length !== 1
  multiple.value = !rows.length
}

const resetForm = () => {
  Object.keys(form).forEach((k) => delete form[k])
}

const handleAdd = () => {
  resetForm()
  title.value = `新增${tabLabels.find(t => t.value === activeTab.value)?.label}`
  open.value = true
}

const handleUpdate = async (row?: AnyParam) => {
  const id = (row?.id ?? ids.value[0]) as number
  if (!id) return
  try {
    const api = apiMap[activeTab.value]
    const { data } = await api.getById(id)
    Object.keys(form).forEach((k) => delete form[k])
    Object.assign(form, data)
    title.value = `修改${tabLabels.find(t => t.value === activeTab.value)?.label}`
    open.value = true
  } catch { ElMessage.error('获取参数详情失败') }
}

const handleSubmit = async () => {
  try {
    const api = apiMap[activeTab.value]
    if (form.id) await api.update(form.id, form)
    else await api.create(form)
    ElMessage.success(form.id ? '修改成功' : '新增成功')
    open.value = false
    fetchList()
  } catch { ElMessage.error('操作失败') }
}

const handleDelete = async (row?: AnyParam) => {
  const list = row ? [row.id] : ids.value
  try {
    await ElMessageBox.confirm(`确认删除选中 ${list.length} 条？`, '提示', { type: 'warning' })
    const api = apiMap[activeTab.value]
    for (const id of list) await api.delete(id)
    ElMessage.success('删除成功')
    fetchList()
  } catch { /* cancelled or failed */ }
}

const handleExport = async (format: 'xlsx' | 'json') => {
  exporting.value = true
  try {
    const api = apiMap[activeTab.value]
    const blob = await api.exportFile(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${activeTab.value}-params.${format}`
    a.click()
    URL.revokeObjectURL(url)
  } finally { exporting.value = false }
}

const handleDownloadTemplate = async (format: 'xlsx' | 'json') => {
  exporting.value = true
  try {
    const api = apiMap[activeTab.value]
    const blob = await api.downloadTemplate(format)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${activeTab.value}-params-template.${format}`
    a.click()
    URL.revokeObjectURL(url)
  } finally { exporting.value = false }
}

const triggerImport = () => fileInput.value?.click()
const handleImport = async (e: Event) => {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  importing.value = true
  try {
    const api = apiMap[activeTab.value]
    const { data } = await api.importFile(file)
    ElMessage.success(data)
    fetchList()
  } finally {
    importing.value = false
    target.value = ''
  }
}

onMounted(() => { fetchProducts(); fetchList() })
</script>
```

- [ ] **步骤 3：替换模板区**

把 `<template>` 区（保留外层 `<div class="app-container">` 与分页结构）替换为：

```html
<template>
  <div class="app-container">
    <el-tabs v-model="activeTab" @tab-change="handleTabChange">
      <el-tab-pane v-for="t in tabLabels" :key="t.value" :label="t.label" :name="t.value" />
    </el-tabs>

    <el-form :inline="true" :model="searchForm" class="search-form">
      <el-form-item label="产品">
        <el-select v-model="searchForm.productId" placeholder="全部" clearable filterable style="width:200px">
          <el-option v-for="p in products" :key="p.id" :label="`${p.id} - ${p.packageName}`" :value="p.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="广告参数">
        <el-select v-model="searchForm.adParamStatus" placeholder="全部" clearable style="width:150px">
          <el-option v-for="o in adStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="上架状态">
        <el-select v-model="searchForm.listStatus" placeholder="全部" clearable style="width:150px">
          <el-option v-for="o in listStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button :icon="Search" type="primary" @click="handleSearch">搜索</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <div class="toolbar">
      <el-button :icon="Plus" type="primary" @click="handleAdd">新增</el-button>
      <el-button :icon="Edit" :disabled="single" @click="handleUpdate()">修改</el-button>
      <el-button :icon="Delete" :disabled="multiple" @click="handleDelete()">删除</el-button>
      <el-button :icon="Upload" :loading="importing" @click="triggerImport">导入</el-button>
      <input ref="fileInput" type="file" accept=".xlsx,.json" style="display:none" @change="handleImport" />
      <el-dropdown @command="(c: 'xlsx' | 'json') => handleExport(c)">
        <el-button :icon="Download" :loading="exporting">导出<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="xlsx">Excel</el-dropdown-item>
            <el-dropdown-item command="json">JSON</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <el-dropdown @command="(c: 'xlsx' | 'json') => handleDownloadTemplate(c)">
        <el-button :icon="Download">模板<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="xlsx">Excel</el-dropdown-item>
            <el-dropdown-item command="json">JSON</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <el-table v-loading="loading" :data="list" border @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="50" />
      <el-table-column type="index" label="#" width="60" />
      <el-table-column v-for="c in columnsByTab[activeTab]" :key="c.key" :prop="c.key" :label="c.label" min-width="140" />
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="handleUpdate(row)">编辑</el-button>
          <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="currentPage"
      v-model:page-size="pageSize"
      :total="total"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      style="margin-top:16px;justify-content:flex-end;display:flex"
      @current-change="fetchList"
      @size-change="fetchList"
    />

    <el-dialog v-model="open" :title="title" width="640px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="产品" prop="productId">
          <el-select v-model="form.productId" placeholder="选择产品" filterable style="width:100%">
            <el-option v-for="p in products" :key="p.id" :label="`${p.id} - ${p.packageName}`" :value="p.id" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="activeTab !== 'vivo'" label="包名">
          <el-input v-model="form.packageName" />
        </el-form-item>
        <el-form-item label="AppId">
          <el-input v-model="form.appId" />
        </el-form-item>
        <el-form-item v-if="activeTab === 'honor'" label="AppSecret">
          <el-input v-model="form.appSecret" show-password />
        </el-form-item>
        <el-form-item v-if="activeTab !== 'huawei'" label="MediaId">
          <el-input v-model="form.mediaId" />
        </el-form-item>
        <el-form-item v-if="activeTab === 'vivo'" label="合同状态">
          <el-input v-model="form.contractStatus" />
        </el-form-item>
        <el-form-item v-if="activeTab !== 'vivo'" label="AGConnect路径">
          <el-input v-model="form.agconnectPath" />
        </el-form-item>
        <el-form-item label="TDAppId">
          <el-input v-model="form.tdAppId" />
        </el-form-item>
        <el-form-item label="广告参数">
          <el-select v-model="form.adParamStatus" placeholder="选择状态" style="width:100%">
            <el-option v-for="o in adStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="上架状态">
          <el-select v-model="form.listStatus" placeholder="选择状态" style="width:100%">
            <el-option v-for="o in listStatusOptions" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人">
          <el-input v-model="form.operator" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="open = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>
```

- [ ] **步骤 4：保留 `<style>` 区或重置为 `.app-container { padding: 20px }`**

- [ ] **步骤 5：类型检查**

```bash
cd frontend && bunx vue-tsc --noEmit
```

预期：除可能存在的子模块旧 TS 错误外，**本文件 0 error**（重点检查 `form: Record<string, any>` 与 `activeTab` 严格性）。

- [ ] **步骤 6：Commit**

```bash
git add frontend/src/views/package/AdParamListView.vue
git commit -m "feat(ad-param): add platform-tabbed CRUD view"
```

---

## 任务 12：tabs store 兼容旧 path

**文件：**
- 修改：`frontend/src/stores/tabs.ts`

- [ ] **步骤 1：检查现有 tabsStore**

```bash
grep -n "addTab\|removeTab\|path" frontend/src/stores/tabs.ts
```

预期：仅缓存 `route.fullPath` 与 `meta.title`。

- [ ] **步骤 2：若 addTab 跳过不存在的 path 路由**

定位 `addTab` 函数（疑似第 5-15 行），若发现 `router.hasRoute(path)` 检测则无需修改；否则保持现状（旧 tab 会在切换时自动跳过）→ **本任务通常为 no-op**。

- [ ] **步骤 3：Commit（若改动）**

```bash
git add frontend/src/stores/tabs.ts
git diff --cached --quiet || git commit -m "fix(tabs): skip stale package routes on mount"
```

---

## 任务 13：整体验证

**文件：**
- 无（只运行命令）

- [ ] **步骤 1：前端 lint**

```bash
cd frontend && bun run lint
```

预期：除已存在的仓库警告外，无新 error。

- [ ] **步骤 2：前端 typecheck/build**

```bash
cd frontend && bun run build
```

预期：`vue-tsc --noEmit` 与 `vite build` 均成功；输出 `dist/` 重新生成。

- [ ] **步骤 3：后端编译**

```bash
cd backend && ./mvnw -DskipTests -q compile
```

预期：`BUILD SUCCESS`。

- [ ] **步骤 4：手工冒烟测试**

```bash
# 启动后端（H2 配置）
cd backend && ./mvnw -DskipTests spring-boot:run &
# 启动前端
cd frontend && bun run dev
```

浏览器访问 `http://localhost:5173`，登录 admin，依次验证：

- 左侧“打包管理”展开 4 个子项；
- 切换路由 `/package/games`、`/package/products`、`/package/tests` 渲染正常；
- `/package/ad-params` 显示荣耀/VIVO/华为三个 tab，新增一条荣耀参数（选择产品，填写其他字段），确认保存后在列表出现；
- 删除、编辑、导入（先用导出模板）、导出 Excel/JSON 正常；
- 权限管理勾选 `package` / `ad-params` 保存，重新登录可见对应菜单。

- [ ] **步骤 5：最后提交（如有遗漏）**

```bash
git status --short
# 如有遗漏改动，按主题 commit
```

---

## 自检

- 路由：`/package/{games,products,tests,ad-params}` 4 条全部存在，名字 `package-*`。
- 菜单：`MainLayout.vue` 含 `packageItems` 与 `<el-sub-menu index="package">`，插在首页之后。
- 数据库：`product_id` 列就位、审计列补齐、三份脚本一致。
- 后端：3 Service/Impl/Controller 各自对应请求路径；`ProductController.getAll` 存在。
- 前端：`ad-param.ts / ad-param.ts types / AdParamListView.vue` 完整；`ProductApi.getAll` 可用。
- 权限：`moduleOptions` 含 `package` 与 `ad-params`，无 `games/products/tests`。

---

## 执行选项

1. 子代理驱动（推荐） - 使用 `superpowers:subagent-driven-development`
2. 内联执行 - 使用 `superpowers:executing-plans`