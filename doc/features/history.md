# 浏览历史

[← 文档索引](../README.md)

## 使用

- 工具栏按钮或 `Ctrl+H` 打开全屏 [HistoryPage](../WebView22Browser.App/Views/HistoryPage.xaml)。
- `Esc` 关闭。
- 按日期分组、关键字搜索、单条删除或清空。
- 数据写入 `browsing-history.json`（默认上限 2000 条，可在设置页调整）。

## 实现

| 层 | 组件 |
| --- | --- |
| 写入策略 | [BrowsingHistoryPolicy](../WebView22Browser.Core/Services/BrowsingHistoryPolicy.cs)、[BrowsingHistoryService](../WebView22Browser.App/Services/BrowsingHistoryService.cs) |
| 分组 / 搜索 / 标题 | `BrowsingHistoryGrouper`、`BrowsingHistorySearch`、`BrowsingHistoryTitleFormatter` |
| 存储 | [JsonBrowsingHistoryStore](../WebView22Browser.Core/Stores/JsonBrowsingHistoryStore.cs) |
| UI | [HistoryViewModel](../WebView22Browser.App/ViewModels/HistoryViewModel.cs) |

设置页保存历史上限后会 `TrimToMaxEntries` 并刷新历史页（[SettingsViewModel](../WebView22Browser.App/ViewModels/SettingsViewModel.cs)）。

## 测试

`JsonBrowsingHistoryStoreTests`、`BrowsingHistoryPolicyTests`、`BrowsingHistoryGrouperTests`、`BrowsingHistorySearchTests`、`BrowsingHistoryTitleFormatterTests`、`HistoryViewModelTests`。
