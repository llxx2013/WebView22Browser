# 贡献指南

[← 文档索引](../README.md)

## 环境

- Windows（WPF 目标）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 本地运行 App 时需 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

## 代码风格

仓库通过以下文件统一风格：

| 文件 | 作用 |
| --- | --- |
| [Directory.Build.props](../../Directory.Build.props) | 可空引用、分析器、ImplicitUsings |
| [.editorconfig](../../.editorconfig) | 格式化与 .NET 代码风格 |

## 格式化

与 [CI](../../.github/workflows/ci.yml) 一致：

```powershell
dotnet tool restore
dotnet format WebView22Browser.sln
dotnet format WebView22Browser.sln --verify-no-changes
```

提交前请确保 `verify-no-changes` 通过。

## 构建与测试

```powershell
dotnet build
dotnet test
```

## 文档变更约定

新增或修改用户可见功能时，请同步更新文档：

| 变更类型 | 需更新 |
| --- | --- |
| 新功能 / 行为变更 | 对应 [doc/features/](../features/) 专题 + 根 [README.md](../../README.md)「功能一览」表一行摘要 |
| 配置项 / 设置页字段 | [configuration.md](../configuration.md) |
| 本地数据文件 / 路径 | [data-storage.md](../data-storage.md) |
| 架构 / DI / 新项目 | [architecture.md](../architecture.md) |
| 快捷键 / UI | [shortcuts-and-ui.md](../shortcuts-and-ui.md) |
| 测试策略 | [testing.md](testing.md) |
| CI / 发布流程 | [ci-and-release.md](ci-and-release.md) |

技术栈版本号以 `WebView22Browser.App.csproj` 为单源；README 技术栈表可写「见 csproj」，避免多处手工改版本不一致。

纯文档 PR 修改 `doc/**` 或 `*.md` 时，CI 的 `paths-ignore` 会跳过构建（见 [ci-and-release.md](ci-and-release.md)）。

## 许可证

贡献内容遵循仓库 [MIT License](../../LICENSE)。
