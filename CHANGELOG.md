# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.1.0-alpha] - 2026-05-20

### Added

- Settings **About** section with application and WebView2 Runtime versions
- Status bar soft warning when more than 10 tabs are open
- Settings **Clear browsing data** (WebView2 Profile cookies, cache, browsing/download history; sidecar JSON unchanged)
- `GM_openInTab` support for `active: false` (background tab without switching selection)
- `Ctrl+Shift+R` shortcut to reload all open tabs (user script workflow)
- Atomic JSON persistence (`JsonFileStoreBase`) for favorites, extensions, scripts, permissions, downloads, and related stores
- User-visible status bar messages for permission flush, session flush, and user-script host failures
- `ITabWebViewHost` / `FakeTabWebViewHost` for headless tests; Linux test suite aligned with Windows CI

### Known limitations

See [doc/development/alpha-known-limitations.md](doc/development/alpha-known-limitations.md) and [doc/development/alpha-s0-acceptance.md](doc/development/alpha-s0-acceptance.md) for Alpha acceptance and product limits.
