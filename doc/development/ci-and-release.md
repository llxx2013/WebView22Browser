# CI 与发布

[← 文档索引](../README.md)

## CI（[ci.yml](../../.github/workflows/ci.yml)）

**触发**：`push` / `pull_request` 到 `main`。

**路径忽略**（不触发构建）：`**.md`、`doc/**`。

**步骤**（`windows-latest`，.NET 8.0.x）：

1. `dotnet restore WebView22Browser.sln`
2. `dotnet tool restore`
3. `dotnet format WebView22Browser.sln --verify-no-changes`
4. `dotnet build -c Release`
5. `dotnet test`（TRX + XPlat Code Coverage 产物）

## Release（[release.yml](../../.github/workflows/release.yml)）

**触发**：

- 推送标签 `v*`
- `workflow_dispatch`（手动）

**构建**：

```powershell
dotnet publish WebView22Browser.App -c Release -r win-x64 --self-contained false -o ./publish
```

**产物**：

- 标签推送：`WebView22Browser-{tag}-win-x64.zip` 上传 GitHub Release（`softprops/action-gh-release`）
- 手动 / 非标签：同名 zip 为 Workflow Artifact（保留 30 天）

发布包依赖目标机已安装 WebView2 常青运行时。

## CodeQL（[codeql.yml](../../.github/workflows/codeql.yml)）

- `main` 上 push/PR 及每周一 06:00 UTC
- C#：`dotnet build` Release 后分析

## Dependabot（[dependabot.yml](../../.github/dependabot.yml)）

每周一检查：

- NuGet（分组：`Microsoft.Extensions.*`、`Microsoft.Web.WebView2`、xunit/coverlet）
- `github-actions`
