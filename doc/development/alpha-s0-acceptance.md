# Alpha S0 验收记录（冻结与基线）

[← 文档索引](../README.md) · [Alpha 收尾计划](alpha-wrap-up.md) · [已知限制](alpha-known-limitations.md)

**Sprint**：S0 — 冻结与基线  
**记录日期**：2026-05-20  
**总体结论**：工程基线已建立；**手工验收部分通过**（人工确认）。未勾选项在 S3 发布前补测或记入 Release 说明。

---

## 工程基线

| 项 | 状态 | 证据 |
| --- | --- | --- |
| `dotnet format --verify-no-changes` | 通过 | 本地 2026-05-20；与 CI 步骤一致 |
| Release build | 通过 | 本地 `dotnet build -c Release` |
| 单元测试（Windows 权威） | 通过 | `main` 最近全量 CI：[run 26171136283](https://github.com/llxx2013/WebView22Browser/actions/runs/26171136283)（373/373） |
| 单元测试（Linux 开发） | 已知差异 | 359/373；14 条 `MainViewModelTests` 预期失败（WPF） |
| CodeQL `main` | 通过 | 最近 push：[run 26178576889](https://github.com/llxx2013/WebView22Browser/actions/runs/26178576889) |
| 文档-only push 不触发 CI | 已说明 | 见 [ci-and-release.md](ci-and-release.md)；`main` 当前 HEAD 为 doc 合并，代码基线与上次绿 CI 一致 |

> **说明**：`main` 在合并 Alpha 收尾文档（PR #8）后未重新触发 Build & Test（`paths-ignore: doc/**`）。自上次代码变更以来无 C# diff；本地 format/build 已复验。

---

## translate 手动验收

依据 [user-scripts.md § translate 手动验收清单](../features/user-scripts.md#translate-手动验收清单)，脚本：[test-scripts/translate/translate.user.js](../../test-scripts/translate/translate.user.js)。

| # | 步骤 | 状态 | 备注 |
| --- | --- | --- | --- |
| 1 | 导入 translate；侧栏 `Requires 3 · Resources 1 · 缓存就绪` | 通过 | 人工确认 |
| 2 | 打开 Bing/Google 顶层页并刷新标签 | 通过 | 人工确认 |
| 3 | 「脚本命令」4 条设置项；控制台无 `Swal` / `GM_*` ReferenceError | 通过 | 人工确认 |
| 4 | 选中英文按 F9，SweetAlert2 翻译窗显示译文 | 通过 | 见 [验收截图](../images/user-scripts-translate.png) |
| 5 | `GM_xmlhttpRequest` + `responseType: 'json'` 解析 `{ code: 200, data }` | 待补测 | 部分通过批次未单独勾选；离线见 `TranslateScriptCompatibilityTests` |

自动化（不访问外网）：`TranslateScriptCompatibilityTests` 在 CI 中持续回归裁剪 fixture。

---

## Windows 冒烟路径

路径：冷启动 → 多标签 → 休眠 → 杀进程 → 会话恢复 → 扩展重装 → 脚本 → 下载 → 历史。

| 步骤 | 状态 | 备注 |
| --- | --- | --- |
| 冷启动 | 通过 | |
| 多标签（新建 / 切换 / 关闭） | 通过 | |
| 标签休眠（后台闲置 → 唤醒） | 通过 | |
| 杀进程后 `tabs-session.json` 会话恢复 | 通过 | |
| 扩展：本地文件夹安装 + 重启后重装 | 通过 | |
| 用户脚本：导入 + 刷新全部标签 | 通过 | 含 translate 路径 |
| 下载：另存为 + 下载中心 | 通过 | |
| 浏览历史：`Ctrl+H` 打开 / 搜索 / 删除 | 待补测 | 部分通过批次未单独复测 |
| HTTPS 证书弹窗 + 权限记忆 | 待补测 | Alpha 门禁要求各 1 次；建议 S3 前补勾 |

---

## S0 出口核对

| 交付物 | 状态 |
| --- | --- |
| [alpha-known-limitations.md](alpha-known-limitations.md)（限制说明草稿） | 完成 |
| 本验收勾选表 | 完成（手工项部分待补） |
| 可附于 GitHub Release | 是 |

---

## 修订记录

| 日期 | 说明 |
| --- | --- |
| 2026-05-20 | S0 基线：工程 CI 记录 + 人工部分通过验收 |
