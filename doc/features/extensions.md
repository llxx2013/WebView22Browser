# Chromium 扩展

[← 文档索引](../README.md)

## 使用步骤

1. 工具栏打开右侧「扩展」侧栏（[ExtensionsPanel](../WebView22Browser.App/Views/ExtensionsPanel.xaml)）。
2. 点击「从文件夹安装」，选择**已解压**的扩展根目录（必须包含 `manifest.json`）。
3. 通过 WebView2 Profile API 安装；源路径记录在 `extensions.json`，下次启动由 [BrowserExtensionService](../WebView22Browser.App/Services/BrowserExtensionService.cs) 自动尝试重装。

**不支持** Chrome Web Store 在线安装；仅本地文件夹。

## Core 校验

| 组件 | 职责 |
| --- | --- |
| [ExtensionPathValidator](../WebView22Browser.Core/Services/ExtensionPathValidator.cs) | 校验扩展目录结构 |
| [ExtensionManifestReader](../WebView22Browser.Core/Services/ExtensionManifestReader.cs) | 读取 `manifest.json` |
| [JsonExtensionSourceStore](../WebView22Browser.Core/Stores/JsonExtensionSourceStore.cs) | 持久化源路径注册表 |

## 与用户脚本的关系

保存或导入用户脚本时，[UserScriptExtensionConflictService](../WebView22Browser.App/Services/UserScriptExtensionConflictService.cs) 会比对已启用扩展的 content script `matches`，提示 URL 重叠。详见 [user-scripts.md](user-scripts.md)。

## 测试

`JsonExtensionSourceStoreTests`、`ExtensionPathValidatorTests`、`ExtensionManifestReaderTests`。
