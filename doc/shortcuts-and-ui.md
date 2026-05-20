# 快捷键与界面

[← 文档索引](README.md)

全局快捷键在 [MainWindow.xaml.cs](../WebView22Browser.App/MainWindow.xaml.cs) 的 `Window_PreviewKeyDown` 中处理。

## 键盘快捷键

| 输入 | 操作 |
| --- | --- |
| `Ctrl + T` | 新建标签 |
| `Ctrl + W` | 关闭当前标签 |
| `Ctrl + Tab` | 切换到下一个标签 |
| `Ctrl + H` | 打开 / 关闭历史记录全屏页 |
| `Ctrl + F` | 页内查找（仅浏览器内容可见时；WebView2 内置查找栏） |
| `Ctrl + Shift + R` | 刷新全部已打开标签（用户脚本侧栏同功能；仅浏览器内容可见时） |
| `Ctrl + ,` | 打开 / 关闭设置页 |
| `Esc` | 历史页或设置页打开时关闭 |
| `F12` | 打开 / 关闭当前标签开发者工具 |
| 地址栏 `Enter` | 导航或搜索 |
| `Ctrl + 点击` 链接 | 在新标签打开 |
| 中键点击链接 | 在新标签打开 |

## 主界面布局

[MainWindow.xaml](../WebView22Browser.App/MainWindow.xaml) 为四列网格：

| 列 | 内容 |
| --- | --- |
| 0 | 收藏夹侧栏（可折叠） |
| 1 | 主浏览区：标签条、工具栏、地址栏、WebView 宿主、状态栏、下载底栏 |
| 2 | 用户脚本侧栏 |
| 3 | 扩展侧栏 |

叠加层：历史记录全屏页、设置全屏页（与侧栏互斥显示逻辑由 ViewModel 控制）。

## 工具栏要点

- **导航**：后退、前进、刷新/停止、主页。
- **地址栏**：安全状态图标、URL、收藏当前页。
- **右侧**：收藏/扩展/用户脚本/历史/设置等入口；「更多」菜单含页内查找等。
- **脚本命令**：显示当前选中且已就绪标签中，各脚本通过 `GM_registerMenuCommand` 注册的命令（详见 [user-scripts.md](features/user-scripts.md)）。

## 状态栏

显示进行中的下载摘要（`DownloadsViewModel`）、标签数软提示（超过 10 个标签时），以及脚本/会话等全局状态消息。
