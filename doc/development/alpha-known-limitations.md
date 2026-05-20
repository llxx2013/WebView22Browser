# Alpha 已知限制（Release Notes 草稿）

[← 文档索引](../README.md) · [Alpha 收尾计划](alpha-wrap-up.md)

本文档摘录 README 与 [doc/features/](../features/) 中**已接受**的产品与平台限制，供 `v0.1.0-alpha` GitHub Release 正文使用。收尾期**不**将下列项当作待修缺陷 backlog。

---

## 扩展

| 限制 | 说明 |
| --- | --- |
| 仅本地已解压目录 | 不支持 Chrome Web Store 在线安装；须包含 `manifest.json`（见 [extensions.md](../features/extensions.md)） |
| 启动重装 | 源路径记录在 `extensions.json`，启动时按注册表自动尝试重装 |

## 用户脚本

| 限制 | 说明 |
| --- | --- |
| 共享 JS 世界 | 用户代码与页面同源执行，非 Tampermonkey 级隔离世界（见 [user-scripts.md](../features/user-scripts.md)） |
| 修改后需刷新标签 | 侧栏保存会触发预取与 `RefreshAllHostsAsync`，已打开页面须 F5 或「刷新全部标签」 |
| `@require` / `@resource` | 仅 `http`/`https`；单文件 ≤ 5 MB；预取不走 `@connect` |
| GM 存储 | 跨标签不实时同步；宿主异常退出可能导致未持久化 |
| `GM_xmlhttpRequest` | 注入页面 Cookie；`Set-Cookie` 不写回；无流式 / `onprogress`；响应体 ≤ 10 MB |
| `GM_openInTab` | `active: false` 在后台打开标签且不切换当前选中标签 |
| 高权限风险 | 仅安装可信脚本；可借登录态访问已 `@connect` 的站点 |

## 标签与会话

| 限制 | 说明 |
| --- | --- |
| 活跃标签建议 ≤ 10 | 每标签独立 WebView2；超过 10 个标签时状态栏软提示 |
| 会话恢复粒度 | 仅 **URL 级** 线性历史栈；不恢复 SPA 表单、`pushState` 内存状态（见 [tab-sleep-and-session.md](../features/tab-sleep-and-session.md)） |
| `RestoreLastSession` | 须**完整重启浏览器**后生效 |
| 侧栏 JSON 原子写 | 收藏、扩展、脚本、权限、下载历史等经 `JsonFileStoreBase` 原子写入；崩溃时仍建议备份重要数据 |

## 安全与证书

| 限制 | 说明 |
| --- | --- |
| HTTPS 证书绕过 | 用户「继续」后**仅当前会话**有效；每次启动清除 `AlwaysAllow`（见 [security-and-permissions.md](../features/security-and-permissions.md)） |
| WebView2 证书 API | 仅 `AlwaysAllow` / `Cancel`，无细粒度策略 |

## 浏览与查找

| 限制 | 说明 |
| --- | --- |
| PDF 页内查找 | WebView2 限制，匹配计数与上一项/下一项可能不完整（见 [navigation-and-tabs.md](../features/navigation-and-tabs.md)） |
| 浏览历史上限 | 改设置后需新 Host 才完全生效（懒加载 `BrowserOptions`） |

## 工程与开发

| 限制 | 说明 |
| --- | --- |
| Linux 本地测试 | S2 后 Linux 与 Windows CI 测试一致（见 [AGENTS.md](../../AGENTS.md)） |
| 无 WebView2 E2E | `TabWebViewHost` 无自动化集成测试（最大回归盲区） |
| DevTools | 默认开启，适合 Alpha；正式版可考虑设置项 |

---

## 安全提示（Release 正文建议保留）

- **扩展与用户脚本**与宿主共享 Profile Cookie 与高权限桥接，请仅安装可信来源。
- **证书错误「继续」**不会持久到下次启动。

---

## 修订记录

| 日期 | 说明 |
| --- | --- |
| 2026-05-20 | S0：初版，供 Alpha Release Notes 引用 |
