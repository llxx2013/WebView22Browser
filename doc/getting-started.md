# 快速开始

[← 文档索引](README.md)

## 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Evergreen 常青版）

目标框架与包版本以 [WebView22Browser.App/WebView22Browser.App.csproj](../WebView22Browser.App/WebView22Browser.App.csproj) 为准。

## 构建与运行

在仓库根目录执行：

```powershell
dotnet build
dotnet test
dotnet run --project WebView22Browser.App
```

`dotnet test` 仅运行 Core 与 ViewModel/App 服务层逻辑，**无需**安装 WebView2 运行时。

## 代码格式（与 CI 一致）

```powershell
dotnet tool restore
dotnet format WebView22Browser.sln
dotnet format WebView22Browser.sln --verify-no-changes
```

详见 [development/contributing.md](development/contributing.md)。

## 生产发布

```powershell
dotnet publish WebView22Browser.App -c Release -r win-x64 --self-contained false
```

发布包为框架依赖部署，目标机器须已安装 WebView2 常青运行时。GitHub Actions 发布流程见 [development/ci-and-release.md](development/ci-and-release.md)。

## 常见故障

| 现象 | 处理 |
| --- | --- |
| 启动时提示 WebView2 运行时未安装 | 安装 [WebView2 Evergreen Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)；错误文案由 `TabWebViewHost` 在初始化失败时展示 |
| `dotnet test` 通过但 `dotnet run` 失败 | 多为本机缺少 WebView2 运行时或 WPF 环境异常，与测试无关 |
| 扩展/脚本不生效 | 用户脚本与扩展列表变更后需**刷新已打开标签**；扩展需本地已解压目录 |

## 性能建议

每个标签对应独立 WebView2 实例以保证隔离与稳定。受内存限制，**建议同时打开的标签不超过 10 个**；其余可借助 [标签页休眠](features/tab-sleep-and-session.md) 降低开销。
