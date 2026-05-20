# 用户脚本

[← 文档索引](../README.md)

## 使用

1. 工具栏打开「用户脚本」侧栏（[UserScriptsPanel](../WebView22Browser.App/Views/UserScriptsPanel.xaml)），可手动添加或从 `.user.js` 导入。
2. 修改脚本列表后需**刷新已打开标签**方生效。

## 元数据

[UserScriptMetadataParser](../WebView22Browser.Core/Services/UserScriptMetadataParser.cs) 解析 `// ==UserScript== ... ==/UserScript==` 块，支持：

`@name`、`@match` / `@include`、`@exclude`、`@run-at`（`document-start` / `-end` / `-idle`）、`@noframes`、`@grant`、`@connect`、`@require`（外部 JS URL）、`@resource`（`名称 URL`，如 CSS）。

`@require` / `@resource` 的 URL 会写入 [UserScriptEntry](../WebView22Browser.Core/Models/UserScriptEntry.cs) 并随 `userscripts.json` 持久化。

URL 匹配语义与 [UserScriptUrlMatcher](../WebView22Browser.Core/Services/UserScriptUrlMatcher.cs) 一致；测试含 [UserScriptUrlMatcherParityTests](../../WebView22Browser.Tests/UserScriptUrlMatcherParityTests.cs)（与注入 JS 同构校验）。

## 注入与执行

- 脚本在 `document-created` 时由 [UserScriptService](../WebView22Browser.App/Services/UserScriptService.cs) / [UserScriptBridge](../WebView22Browser.App/Services/UserScriptBridge.cs) 注入。
- 用户代码经 `new Function('GM', 'unsafeWindow', code)` 包装，与页面**共享 JS 世界**（非 Tampermonkey 级隔离世界），但无法访问 bootstrap 闭包内的鉴权信息。
- 注入脚本由 [UserScriptBootstrapBuilder](../WebView22Browser.Core/Services/UserScriptBootstrapBuilder.cs) 生成（URL 匹配、GM API、每脚本 **nonce**）。
- 在每个 `@run-at` 调度点执行用户代码**之前**，按元数据顺序将已缓存的 `@require` 源码以 `<script>` 注入 `document.head`（与 Tampermonkey 一致）。
- `GM_getResourceText(name)` 返回已缓存的 `@resource` 文本（需声明 `@grant GM_getResourceText`）。

## `@require` / `@resource` 与依赖缓存

| 组件 | 职责 |
| --- | --- |
| [UserScriptDependencyCache](../WebView22Browser.App/Services/UserScriptDependencyCache.cs) | 宿主 `HttpClient` 下载并缓存到 `%LocalAppData%/WebView22Browser/script-deps/`（URL SHA-256 文件名） |
| [UserScriptDependencyResolver](../WebView22Browser.App/Services/UserScriptDependencyResolver.cs) | 解析脚本的全部 require/resource URL |
| [UserScriptImportService](../WebView22Browser.App/Services/UserScriptImportService.cs) | 导入成功后预取；失败写入确认对话框警告 |
| [UserScriptService](../WebView22Browser.App/Services/UserScriptService.cs) | `RefreshAllHostsAsync` 前批量预取，再注入各 WebView |

限制与策略：

- 仅 `http`/`https` URL；单文件上限 **5 MB**（`UserScriptDependencyCache.MaxFileBytes`）。
- 预取走宿主网络，**不**扩展脚本的 `@connect`（例如 `unpkg.com` 不必写入 `@connect`）。
- 缓存未命中或下载失败时，bootstrap 跳过对应项并在页面 `console.error`；导入/刷新时会在 UI 警告中列出。
- 修改脚本列表或依赖 URL 后需**刷新已打开标签**；侧栏保存会触发 `RefreshAllHostsAsync` 重新预取。

## `@grant` 支持的 GM API

| 类别 | API |
| --- | --- |
| 存储 | `GM_setValue`、`GM_getValue`、`GM_deleteValue`、`GM_listValues` |
| 网络 | `GM_xmlhttpRequest` |
| UI / 系统 | `GM_log`、`GM_info`、`GM_notification`、`GM_addStyle`、`GM_openInTab`、`GM_setClipboard`（仅 `text`）、`GM_registerMenuCommand`、`GM_unregisterMenuCommand`、`GM_getResourceText` |

未声明 `@grant` 等同 `@grant none`，所有 GM 函数不可见。完整列表以 [UserScriptGrantCatalog](../WebView22Browser.Core/Services/UserScriptGrantCatalog.cs) 为准。

### 脚本命令菜单

`GM_registerMenuCommand` 注册的命令显示在工具栏 **「脚本命令」** 菜单（[UserScriptCommandsViewModel](../WebView22Browser.App/ViewModels/UserScriptCommandsViewModel.cs)）。仅**当前选中且已就绪**标签、本页导航后重新注册的项可见；导航开始时会清空旧注册。

## GM 存储语义

- 注入时从 `gm-storage/<scriptId>.json` 预加载到闭包；读取走内存（同步）。
- 写入先更内存，再 `postMessage` 异步落盘；宿主异常退出可能导致未持久化。
- 同脚本在其他已打开标签中**不会实时看到**新写入；需刷新页面后重新注入。

### 配额（[GmStorageQuota](../WebView22Browser.Core/Models/GmStorageQuota.cs)）

| 限制 | 默认值 |
| --- | --- |
| 单值大小 | ≤ 256 KB |
| 单脚本总量 | ≤ 5 MB |
| 键名长度 | ≤ 256 字符 |

超限写入被 [GmStorageMessageHandler](../WebView22Browser.App/Services/GmStorageMessageHandler.cs) 拒绝并记入 Debug 日志。

## `@connect` 白名单

[UserScriptConnectMatcher](../WebView22Browser.Core/Services/UserScriptConnectMatcher.cs)：

- 未声明 `@connect` 时等价于 `@connect self`。
- 支持 `*` / `self` / `example.com` / `*.example.com` 等。
- 不在白名单内的跨站请求由宿主拒绝，经 `__wv2dispatch` 回传 error。

## 桥接与安全

- 特权消息（`gm.*`）须携带 `{ scriptId, nonce }`，由 [UserScriptMessageValidator](../WebView22Browser.Core/Services/UserScriptMessageValidator.cs) 校验。
- 消息来源须为 `http(s)://` 或 `file://` 页面 URL。
- **`GM_xmlhttpRequest`**（[GmXhrService](../WebView22Browser.App/Services/GmXhrService.cs)）：
  - 宿主用源标签 `CookieManager` 读取目标 URL 的 Cookie 注入请求头；响应 `Set-Cookie` **不写回**。
  - 响应体上限 **10 MB**（`MaxResponseBytes`）；`HttpClient` 总超时 **2 分钟**。
  - 不支持：流式 / `onprogress`、`responseType=document`、`Set-Cookie` 回写、`binary` / `synchronous`。
- `__wv2dispatch` 通过不可配置的 `Object.defineProperty` 注册；XHR 结果用 `ExecuteScriptAsync` 投递。
- **共享 Cookie 风险**：脚本可借用户登录态访问已 `@connect` 的站点（与 Tampermonkey 同级高权限能力）。

## 冲突检查

[UserScriptConflictDetector](../WebView22Browser.Core/Services/UserScriptConflictDetector.cs) / [UserScriptExtensionConflictService](../WebView22Browser.App/Services/UserScriptExtensionConflictService.cs)：保存或导入时比对已启用 Chromium 扩展的 content script `matches`。

## 测试

`JsonUserScriptStoreTests`、`UserScriptMetadataParserTests`、`UserScriptUrlMatcherTests`、`UserScriptBootstrapBuilderTests`、`UserScriptDependencyCacheTests`、`UserScriptDependencyResolverTests`、`UserScriptMessageValidatorTests`、`UserScriptConnectMatcherTests`、`UserScriptConflictDetectorTests`、`JsonGmStorageStoreTests`、`GmStorageMessageHandlerTests`、`GmXhrServiceTests`、`GmXhrMessageHandlerTests`、`GmMenuCommandRegistryTests`。
