# 标签页休眠与会话恢复

[← 文档索引](../README.md)

## 概述

[TabSleepService](../WebView22Browser.App/Services/TabSleepService.cs) 按闲置时长与系统压力（内存 / 本进程 CPU）对**非选中**后台标签执行三层动作；策略判定在 Core 层 [TabSleepPolicy](../WebView22Browser.Core/Services/TabSleepPolicy.cs)。

## 三层休眠

| 阶段 | 动作 | 说明 |
| --- | --- | --- |
| 1 | `MemoryUsageTargetLevel = Low` | 闲置 ≥ 0.5×有效超时 |
| 2 | `TrySuspendAsync` + `Resume` | 闲置 ≥ 1×有效超时；轻量挂起，唤醒几乎无等待，保留 DOM / SPA 状态 |
| 3 | 销毁 WebView2 控件 | 闲置 ≥ 2×有效超时，或 High 压力下 ≥ 1.5×有效超时；冻结 URL 历史快照（标签条 💤） |

**有效超时**：Normal 为 `TabSleepTimeoutMinutes`；系统压力 Elevated 时 ×0.5；High 时 ×0.2。

## 唤醒

再次选中标签时 `WakeAsync()`：

- 若仅轻量挂起：`Resume()`。
- 若已销毁：重建 WebView2，由 [TabHistoryRestorer](../WebView22Browser.Core/Services/TabHistoryRestorer.cs) 重放 `Navigate` / `GoBack`（栈深度上限 `TabHistoryMaxEntries`，默认 50）。

应用层 URL 历史由 [TabNavigationHistory](../WebView22Browser.Core/Services/TabNavigationHistory.cs) 在 `NavigationCompleted` 时维护。

## 不会进入休眠

- 当前选中标签
- 正在加载（`IsLoading`）
- 正在播放音频（`CoreWebView2.IsDocumentPlayingAudio`）
- 存在进行中的下载（`ActiveDownloadCount > 0`）

## 会话恢复

退出时将标签与历史栈写入 `tabs-session.json`（[JsonTabSessionStore](../WebView22Browser.Core/Stores/JsonTabSessionStore.cs)，原子替换）。

| `RestoreLastSession` | 行为 |
| --- | --- |
| `true`（默认） | 恢复全部标签；**当前选中标签立即初始化**，其余以休眠占位懒唤醒（点击后 `WakeAsync()`） |
| `false` | 仅打开主页 |

该选项需**重启浏览器**后才会影响启动行为。详见 [configuration.md](../configuration.md)。

## 历史恢复限制（WebView2 无 Travellog API）

- 仅恢复 **URL 级** 线性历史，不恢复 SPA 内存状态、未提交表单或仅 `pushState` 无完整导航的条目。
- 轻量挂起可保留页面内存状态；销毁控件路径依赖 URL 重放。
- 共享 Profile 的 Cookie / 登录态仍保留在 `UserData\Profile\` 中。
