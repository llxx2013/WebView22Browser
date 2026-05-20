# 收藏夹

[← 文档索引](../README.md)

## 使用

1. 工具栏打开左侧「收藏」侧栏（[FavoritesPanel](../WebView22Browser.App/Views/FavoritesPanel.xaml)）。
2. 将当前页加入收藏、删除条目、双击打开。
3. 数据写入 `%LocalAppData%\WebView22Browser\favorites.json`。

## 实现

| 组件 | 路径 |
| --- | --- |
| ViewModel | [FavoritesViewModel](../WebView22Browser.App/ViewModels/FavoritesViewModel.cs) |
| 持久化 | [JsonFavoritesStore](../WebView22Browser.Core/Stores/JsonFavoritesStore.cs) |
| 模型 | `FavoriteItem`（标题、URL 等） |

导航回调在 [App.xaml.cs](../WebView22Browser.App/App.xaml.cs) 启动时绑定到 `MainViewModel.OpenFavoriteCommand`。

## 测试

`JsonFavoritesStoreTests`、`FavoritesViewModelTests`（见 [testing.md](../development/testing.md)）。
