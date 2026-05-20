# WebView22Browser

基于 [Microsoft WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) 的 WPF 多标签浏览器。采用 **MVVM** 与 **多项目分层**，通过 **Microsoft.Extensions.DependencyInjection** 装配，将可测试的核心逻辑与 WebView2 宿主解耦。

> 🤖 **重要说明**：本项目由 AI 主导代码编写、代码审查与功能演进，旨在探索 AI 主导工程的边界，仓库全部文本内容（除本说明与 LICENSE 以外）由 AI 生成。

---

## 功能一览

| 类别 | 能力 |
| --- | --- |
| **多标签** | 每个标签独立 `WebView2` 实例，`Ctrl+T` / `Ctrl+W` / `Ctrl+Tab` 切换；关闭最后一个标签时自动打开主页 |
| **地址栏** | `http(s)://` 直链；`localhost`、IP、含 `.` 的主机名自动补 `https://`；`file://` 本地文件；其余输入走可配置的搜索引擎 |
| **导航** | 图标化工具栏（后退 / 前进 / 刷新·停止 / 主页）、宽地址栏（安全状态 + 收藏）、右侧快捷入口与「更多」菜单；状态栏显示加载和下载摘要；**页内查找**（`Ctrl+F` 或菜单，WebView2 内置查找栏） |
| **新标签手势** | `Ctrl + 点击`、中键点击、右键菜单「在新标签页中打开」 |
| **共享会话** | 全部标签共用同一 `CoreWebView2Environment` 与用户数据目录，Cookie / 登录态跨标签共享 |
| **收藏夹** | 左侧可折叠面板，添加 / 删除 / 双击打开，写入 `favorites.json` |
| **Chromium 扩展** | 右侧可折叠面板，安装**本地已解压**的扩展（含 `manifest.json`）；启动时按注册表自动重装 |
| **用户脚本** | 右侧可折叠面板，按 URL 匹配规则注入 JavaScript；支持 `@grant` / `@connect`；可检查与已启用扩展的 URL 冲突 |
| **下载** | 弹出 Windows「另存为」；底部下载中心展示进度、暂停 / 取消、「在文件夹中显示」、打开文件；历史写入 `download-history.json` |
| **浏览历史** | 全屏历史页（工具栏按钮或 `Ctrl+H`），按日期分组、关键字搜索、单条删除或清空，写入 `browsing-history.json` |
| **证书与权限** | HTTPS 证书错误弹窗确认（仅当会话有效）；地理位置、摄像头等首次弹窗，结果按站点 + 权限类型记忆 |
| **稳定性** | 渲染进程崩溃最多自动恢复 3 次；`F12` 开发者工具；PDF 内页查找由 WebView2 限制，匹配计数与上一项/下一项可能不完整 |
| **标签页休眠** | 分层节能（降内存 → `TrySuspend` → 销毁控件）+ 系统压力自适应；会话写入 `tabs-session.json` 支持崩溃/重启后懒唤醒恢复标签与历史栈 |

---

## 技术栈

| 组件 | 版本 / 说明 |
| --- | --- |
| .NET | 8.0（App 为 `net8.0-windows` + WPF） |
| WebView2 SDK | `Microsoft.Web.WebView2`（见 [WebView22Browser.App.csproj](WebView22Browser.App/WebView22Browser.App.csproj)） |
| MVVM | `CommunityToolkit.Mvvm` |
| 配置与 DI | `Microsoft.Extensions.Configuration.Json`、`DependencyInjection` |
| 测试 | xUnit（`WebView22Browser.Tests`，不依赖 WebView2 运行时） |

---

## 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Evergreen 常青版）

---

## 快速开始

```powershell
dotnet build
dotnet test
dotnet run --project WebView22Browser.App
```

生产发布：

```powershell
dotnet publish WebView22Browser.App -c Release -r win-x64 --self-contained false
```

更多说明（格式检查、故障排查、性能建议）见 [doc/getting-started.md](doc/getting-started.md)。

---

## 文档

详细文档位于 [doc/](doc/) 目录，从 **[doc/README.md](doc/README.md)** 进入：

| 分类 | 文档 |
| --- | --- |
| 入门 | [快速开始](doc/getting-started.md)、[快捷键与界面](doc/shortcuts-and-ui.md) |
| 架构与配置 | [架构](doc/architecture.md)、[配置](doc/configuration.md)、[本地数据](doc/data-storage.md) |
| 功能 | [导航与标签](doc/features/navigation-and-tabs.md)、[休眠与会话](doc/features/tab-sleep-and-session.md)、[收藏夹](doc/features/favorites.md)、[扩展](doc/features/extensions.md)、[用户脚本](doc/features/user-scripts.md)、[下载](doc/features/downloads.md)、[历史](doc/features/history.md)、[证书与权限](doc/features/security-and-permissions.md) |
| 开发 | [贡献](doc/development/contributing.md)、[测试](doc/development/testing.md)、[CI 与发布](doc/development/ci-and-release.md) |

---

## 许可证

MIT License — 见 [LICENSE](LICENSE)。
