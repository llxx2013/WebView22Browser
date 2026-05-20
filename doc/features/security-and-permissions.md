# 证书与权限

[← 文档索引](../README.md)

## HTTPS 证书错误

访问自签名或过期 HTTPS 时，`TabWebViewHost` 弹窗确认。用户选择「继续」后，**当前会话内**对该主机有效。

每次启动会调用 `ClearServerCertificateErrorActionsAsync`，**重启后需重新确认**。

## 站点权限

地理位置、摄像头等敏感权限首次请求时弹窗；用户选择由 [PermissionMemoryStore](../WebView22Browser.App/Services/PermissionMemoryStore.cs) 记忆，约 3 秒后批量写入 `permissions.json`。同站点、同权限类型再次访问时自动应用历史选择。

## 地址栏安全状态

HTTPS 页面安全状态由 [SecurityStateDevToolsParser](../WebView22Browser.Core/Services/SecurityStateDevToolsParser.cs) 解析，经 `BrowserTabViewModel` 显示在地址栏（[SecurityStateToGlyphConverter](../WebView22Browser.App/Converters/SecurityStateToGlyphConverter.cs)）。

## 测试

`PermissionMemoryStoreTests`、`SecurityStateDevToolsParserTests`。
