# 配置

[← 文档索引](README.md)

## 加载顺序

1. `WebView22Browser.App/appsettings.json`（出厂默认，随构建复制到输出目录）
2. `%LocalAppData%\WebView22Browser\user-settings.json`（设置页保存的覆盖项）
3. 合并为运行时 [BrowserOptions](../WebView22Browser.Core/BrowserOptions.cs)（[BrowserOptionsLoader](../WebView22Browser.Core/Services/BrowserOptionsLoader.cs)）

启动时在 [App.xaml.cs](../WebView22Browser.App/App.xaml.cs) 读取配置并注册 DI。

## appsettings.json

编辑 [appsettings.json](../WebView22Browser.App/appsettings.json) 的 `Browser` 节：

```json
{
  "Browser": {
    "HomeUrl": "https://www.bing.com",
    "SearchUrlTemplate": "https://www.bing.com/search?q={0}",
    "TabSleepTimeoutMinutes": 5,
    "TabSleepCheckIntervalSeconds": 30,
    "RestoreLastSession": true,
    "PressureElevatedMemoryPercent": 70,
    "PressureHighMemoryPercent": 85,
    "PressureHighCpuPercent": 80,
    "PressureSampleWindowSeconds": 15
  }
}
```

| 键 | 说明 | 设置页可编辑 |
| --- | --- | --- |
| `HomeUrl` | 主页与新建标签默认地址 | 是 |
| `SearchUrlTemplate` | 非 URL 输入的搜索模板，`{0}` 为编码后的查询词 | 是 |
| `TabSleepTimeoutMinutes` | 非选中标签闲置多少分钟后进入休眠；`0` 禁用 | 是 |
| `TabSleepCheckIntervalSeconds` | 休眠扫描间隔（秒） | 是 |
| `RestoreLastSession` | 启动时从 `tabs-session.json` 恢复标签 | 是（**需重启生效**） |
| `PressureElevatedMemoryPercent` | 系统内存达此百分比 → Elevated（有效超时 ×0.5） | 是 |
| `PressureHighMemoryPercent` | 系统内存达此百分比 → 可升 High（×0.2，加速销毁） | 是 |
| `PressureHighCpuPercent` | 本进程 CPU 与 Elevated 内存组合升 High 的辅助阈值 | 是 |
| `PressureSampleWindowSeconds` | 压力采样 EMA 窗口（秒） | 是 |

以下项**仅**在 `user-settings.json` / 设置页中配置（不在默认 `appsettings.json` 中）：

| 键 | 说明 | 默认值 |
| --- | --- | --- |
| `TabHistoryMaxEntries` | 每标签 URL 历史栈上限（唤醒恢复） | 50 |
| `BrowsingHistoryMaxEntries` | 浏览历史上限 | 2000 |
| `DownloadHistoryMaxEntries` | 下载历史上限 | 200 |

## 设置页（`Ctrl+,`）

[SettingsViewModel](../WebView22Browser.App/ViewModels/SettingsViewModel.cs) 编辑上述全部 12 项，保存至 `user-settings.json`。

### 生效时机

| 类型 | 行为 |
| --- | --- |
| 多数选项 | 保存后由 [TabSleepService](../WebView22Browser.App/Services/TabSleepService.cs) 作为 `IRuntimeBrowserSettingsApplier` **立即**更新休眠扫描间隔等 |
| `RestoreLastSession` | 仅影响**下次启动**的会话恢复逻辑 |
| 历史上限 | 保存后立即 `TrimToMaxEntries` 并持久化对应 JSON |

### 校验规则（[BrowserSettingsValidator](../WebView22Browser.Core/Services/BrowserSettingsValidator.cs)）

| 字段 | 规则 |
| --- | --- |
| `HomeUrl` | 非空；有效 `http`/`https` URL |
| `SearchUrlTemplate` | 非空；含 `{0}`；`string.Format` 后须为合法绝对 URI |
| `TabSleepTimeoutMinutes` | ≥ 0 |
| `TabSleepCheckIntervalSeconds` | ≥ 1 |
| `TabHistoryMaxEntries` | ≥ 1 |
| `PressureElevatedMemoryPercent` | 1–100 |
| `PressureHighMemoryPercent` | 1–100，且 ≥ Elevated 阈值 |
| `PressureHighCpuPercent` | 1–100 |
| `PressureSampleWindowSeconds` | ≥ 1 |
| `BrowsingHistoryMaxEntries` | ≥ 1 |
| `DownloadHistoryMaxEntries` | ≥ 1 |

### 恢复默认

「恢复默认」会删除 `user-settings.json`，用 `appsettings.json` 默认值重写并保存，再应用运行时设置。

### 数据路径（只读）

设置页展示 WebView2 Profile、各 JSON 路径、GM 存储目录；可点击在资源管理器中打开。路径定义见 [data-storage.md](data-storage.md)。

## 高级覆盖

`BrowserOptions` 支持代码级覆盖 `UserDataRoot`、各 JSON 文件路径（单元测试与高级部署）。设置页不编辑这些属性。
