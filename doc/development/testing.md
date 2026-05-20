# 测试

[← 文档索引](../README.md)

## 策略

- 测试项目：[WebView22Browser.Tests](../../WebView22Browser.Tests/WebView22Browser.Tests.csproj)，目标 `net8.0-windows`。
- **引用 App 项目**：以便测试 ViewModel 与 App 层服务（GM 消息处理、权限记忆、错误映射等），**不**启动真实 WebView2 控件。
- `dotnet test` **不需要**安装 WebView2 运行时。

## Fakes（`WebView22Browser.Tests/Fakes/`）

| Fake | 用途 |
| --- | --- |
| `FakeFavoritesStore` | 收藏夹内存存储 |
| `FakeBrowsingHistoryStore` | 浏览历史 |
| `FakeDownloadHistoryStore` | 下载历史 |
| `FakeUserSettingsStore` | 用户设置 |
| `FakeDialogService` | 对话框 |
| `FakeRuntimeBrowserSettingsApplier` | 运行时设置应用 |
| `FakeGmXhrReplyChannel` | GM XHR 回传通道 |
| `FakeTabWebViewHost` | 标签 WebView 宿主能力（休眠单测） |

组合测试范例：[MainViewModelTests.cs](../../WebView22Browser.Tests/MainViewModelTests.cs)（临时 JSON 路径 + 上述 Fakes 构建完整 `MainViewModel`）。

## 未覆盖区域

- `TabWebViewHost` 与 WebView2 运行时集成
- XAML 布局与视觉回归
- 端到端浏览器场景

相关逻辑通过 Core 策略类与 ViewModel 命令间接验证。

### translate 回归

- **自动化**：`TranslateScriptCompatibilityTests`（`Fixtures/TranslateCompatFixture.cs`，内联 GM / XHR / 菜单模式，不依赖外网）。
- **手动**：见 [user-scripts.md § translate 手动验收清单](../features/user-scripts.md#translate-手动验收清单)（含 [验收界面截图](../images/user-scripts-translate.png)）。

## 覆盖范围（按主题）

| 主题 | 测试类 | 覆盖范围 |
| --- | --- | --- |
| 导航 | `NavigationServiceTests`、`NavigationErrorPolicyTests`、`NavigationErrorFormatterTests` | URI 解析、搜索回退、`localhost` / `file`、错误文案 |
| 收藏 | `JsonFavoritesStoreTests`、`FavoritesViewModelTests` | 持久化与 ViewModel |
| 扩展 | `JsonExtensionSourceStoreTests`、`ExtensionPathValidatorTests`、`ExtensionManifestReaderTests` | 注册表、路径、`manifest.json` |
| 用户脚本 | `JsonUserScriptStoreTests`、`UserScriptMetadataParserTests`、`UserScriptUrlMatcherTests`、`UserScriptUrlMatcherParityTests`、`UserScriptBootstrapBuilderTests`、`UserScriptDependencyCacheTests`、`UserScriptDependencyResolverTests`、`UserScriptDependencyStatusTests`、`TranslateScriptCompatibilityTests`、`UserScriptMessageValidatorTests`、`UserScriptConnectMatcherTests`、`UserScriptConflictDetectorTests` | 元数据、URL 匹配（含 JS 同构）、注入、依赖缓存、`translate` 裁剪 fixture、鉴权、`@connect`、冲突 |
| GM | `JsonGmStorageStoreTests`、`GmStorageMessageHandlerTests`、`GmXhrServiceTests`、`GmXhrMessageHandlerTests`、`GmMenuCommandRegistryTests` | 存储配额、XHR |
| 历史 | `JsonBrowsingHistoryStoreTests`、`BrowsingHistoryPolicyTests`、`BrowsingHistoryGrouperTests`、`BrowsingHistorySearchTests`、`BrowsingHistoryTitleFormatterTests`、`HistoryViewModelTests` | 写入、分组、搜索 |
| 下载 | `JsonDownloadHistoryStoreTests`、`DownloadItemViewModelTests`、`DownloadProgressFormatterTests` | 历史与进度文案 |
| 标签 / 休眠 / 会话 | `TabSleepPolicyTests`、`TabSleepPolicyDecideTests`、`TabSleepCycleProcessorTests`、`TabNavigationHistoryTests`、`TabHistoryRestorerTests`、`TabSessionSnapshotBuilderTests`、`JsonTabSessionStoreTests` | 休眠判定、定时 tick、历史栈、会话 |
| 主 VM / 配置 | `MainViewModelTests`、`BrowserOptionsTests`、`BrowserOptionsLoaderTests`、`BrowserSettingsValidatorTests`、`JsonUserSettingsStoreTests`、`JsonUserSettingsStoreLoadTests`、`SettingsViewModelTests`、`PermissionMemoryStoreTests`、`WebView2ErrorMapperTests`、`SecurityStateDevToolsParserTests` | 标签命令、配置、设置页、权限、安全状态 |

## 运行

```powershell
dotnet test WebView22Browser.sln
```

CI 会上传 TRX 与覆盖率产物（见 [ci-and-release.md](ci-and-release.md)）。
