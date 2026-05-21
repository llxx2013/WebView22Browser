# 本地数据存储

[← 文档索引](README.md)

## 默认根目录

`%LocalAppData%\WebView22Browser\`

路径解析与默认值由 [BrowserOptions.cs](../WebView22Browser.Core/BrowserOptions.cs) 的 `Get*` 方法提供；设置页「数据路径」为只读展示（[SettingsViewModel](../WebView22Browser.App/ViewModels/SettingsViewModel.cs)）。

## 文件与目录

| 路径 | 内容 | 实现 |
| --- | --- | --- |
| `UserData\Profile\` | WebView2 用户数据（Cookie、缓存、Session、已安装扩展） | [WebView2EnvironmentService](../WebView22Browser.App/Services/WebView2EnvironmentService.cs) |
| `favorites.json` | 收藏夹列表 | `JsonFavoritesStore`（原子写） |
| `extensions.json` | 已安装扩展的源目录注册表（启动重装） | `JsonExtensionSourceStore`（原子写） |
| `permissions.json` | 站点权限记忆（URI + 权限类型） | [PermissionMemoryStore](../WebView22Browser.App/Services/PermissionMemoryStore.cs)（原子写） |
| `download-history.json` | 下载历史（默认上限 200 条，可在设置页调整） | `JsonDownloadHistoryStore`（原子写） |
| `browsing-history.json` | 浏览访问历史（默认上限 2000 条） | `JsonBrowsingHistoryStore`（原子写） |
| `userscripts.json` | 用户脚本元数据与代码 | `JsonUserScriptStore`（原子写） |
| `tabs-session.json` | 标签列表、选中标签、每标签 URL 历史栈 | `JsonTabSessionStore`（原子写） |
| `user-settings.json` | 应用内设置页覆盖项 | `JsonUserSettingsStore`（原子写） |
| `gm-storage/<scriptId>.json` | 各脚本的 GM 键值存储 | `JsonGmStorageStore`（原子写） |

WebView2 环境创建时启用 `AreBrowserExtensionsEnabled = true`。

## 写入约定

- **侧栏与用户数据 JSON**：上表所列 `*.json` 均经 [JsonFileStoreBase](../WebView22Browser.Core/Stores/JsonFileStoreBase.cs)（继承类调用实例 `WriteAtomicAsync`，或等价调用静态 `JsonFileStoreBase.WriteAtomicAsync`）以 **临时文件 + 同名替换** 落盘，降低进程崩溃时半写损坏风险。
- **权限记忆**：首次弹窗结果约 3 秒后批量写入 `permissions.json`（见 [security-and-permissions.md](features/security-and-permissions.md)）。
- **GM 存储**：写入先更新内存，再异步落盘；异常退出可能导致未持久化（见 [user-scripts.md](features/user-scripts.md)）。

## 高级覆盖

`BrowserOptions` 支持代码级覆盖 `UserDataRoot`、各 JSON 文件路径、`GmStorageDirectoryPath`（单元测试与高级部署用）。常规用户通过设置页查看路径，不编辑这些键。

## 可选：主要模型字段

### `userscripts.json`（`UserScriptEntry`）

名称、`MatchPatterns` / `ExcludePatterns`、`RunAt`、`Noframes`、`Grants`、`Connect`、`Code`、`IsEnabled`、`Id` 等。解析见 `UserScriptMetadataParser`。

### `tabs-session.json`（`TabSessionSnapshot`）

标签 ID、URL、标题、选中标签、每标签 URL 历史栈（供 `TabHistoryRestorer` 唤醒）。构建见 `TabSessionSnapshotBuilder`。
