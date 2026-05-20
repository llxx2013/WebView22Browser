# 下载

[← 文档索引](../README.md)

## 行为

- 下载开始时弹出 Windows「另存为」对话框。
- 底部 [DownloadsPanel](../WebView22Browser.App/Views/DownloadsPanel.xaml) 展示进度，支持暂停、取消、「在文件夹中显示」、打开文件。
- 历史写入 `download-history.json`（默认上限 200 条，可在设置页调整）。

## 实现

| 组件 | 路径 |
| --- | --- |
| WebView2 下载事件 | [WebView2DownloadService](../WebView22Browser.App/Services/WebView2DownloadService.cs) |
| ViewModel | [DownloadsViewModel](../WebView22Browser.App/ViewModels/DownloadsViewModel.cs)、[DownloadItemViewModel](../WebView22Browser.App/ViewModels/DownloadItemViewModel.cs) |
| 历史存储 | [JsonDownloadHistoryStore](../WebView22Browser.Core/Stores/JsonDownloadHistoryStore.cs) |
| 进度文案 | [DownloadProgressFormatter](../WebView22Browser.Core/Services/DownloadProgressFormatter.cs) |

进行中的下载会阻止对应标签进入休眠（见 [tab-sleep-and-session.md](tab-sleep-and-session.md)）。

## 测试

`JsonDownloadHistoryStoreTests`、`DownloadItemViewModelTests`、`DownloadProgressFormatterTests`。
