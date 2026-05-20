# 导航与多标签

[← 文档索引](../README.md)

## 地址栏输入

[NavigationService](../WebView22Browser.Core/Services/NavigationService.cs) 将地址栏文本转为最终 URI：

| 输入类型 | 行为 |
| --- | --- |
| `http://` / `https://` | 直链导航 |
| `localhost`、IP、含 `.` 的主机名 | 自动补 `https://` |
| `file://` | 本地文件 |
| 其余 | 使用可配置搜索引擎（`SearchUrlTemplate`，`{0}` 为 `Uri.EscapeDataString` 编码的查询词） |

配置见 [configuration.md](../configuration.md)。

## 多标签

- 每个标签对应独立 `WebView2` 控件（[TabWebViewHost](../WebView22Browser.App/Controls/TabWebViewHost.xaml.cs)）。
- 全部标签共享同一 [CoreWebView2Environment](../WebView22Browser.App/Services/WebView2EnvironmentService.cs) 与用户数据目录，Cookie / 登录态跨标签共享。
- 关闭最后一个标签时自动打开主页。

## 新标签手势

[NewTabGestureDetector](../WebView22Browser.App/WebView2/NewTabGestureDetector.cs) 与右键菜单（[WebView2ContextMenuHelper](../WebView22Browser.App/WebView2/WebView2ContextMenuHelper.cs)）支持：

- `Ctrl + 点击` 链接
- 中键点击链接
- 右键「在新标签页中打开」

## 导航错误

[NavigationErrorPolicy](../WebView22Browser.Core/Services/NavigationErrorPolicy.cs) / [NavigationErrorFormatter](../WebView22Browser.Core/Services/NavigationErrorFormatter.cs) 决定是否展示错误及文案；WebView2 原始错误经 [WebView2ErrorMapper](../WebView22Browser.App/WebView2/WebView2ErrorMapper.cs) 映射。

## 页内查找

`Ctrl+F` 或菜单触发，使用 WebView2 内置查找 API。PDF 内页查找受 WebView2 限制，匹配计数与上一项/下一项可能不完整。

## 渲染进程恢复

渲染进程崩溃时，单标签最多自动恢复 3 次（`TabWebViewHost`）。
