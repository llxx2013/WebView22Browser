# AGENTS.md

## Cursor Cloud specific instructions

This is a **WPF desktop browser** targeting `net8.0-windows`. The Cloud Agent VM runs Ubuntu (Linux), so there are platform-specific considerations.

### Environment setup (already done by update script)

- .NET 8 SDK is installed at `/usr/share/dotnet`
- `EnableWindowsTargeting=true` is set so `dotnet build/restore` works on Linux for `net8.0-windows` projects
- A `Microsoft.WindowsDesktop.App` shim (copied from `Microsoft.NETCore.App`) allows the test host to load

### Key commands

| Action | Command |
|--------|---------|
| Restore | `dotnet restore WebView22Browser.sln` |
| Build | `dotnet build WebView22Browser.sln` |
| Test | `dotnet test WebView22Browser.sln` |
| Lint/format check | `dotnet format WebView22Browser.sln --verify-no-changes` |
| Auto-fix format | `dotnet format WebView22Browser.sln` |
| Tool restore | `dotnet tool restore` |

### Platform caveats on Linux

- **359/373 tests pass.** 14 tests in `MainViewModelTests` fail because they instantiate `TabHostService`, which loads `PresentationFramework.dll` (WPF). This is expected on Linux.
- The WPF application (`dotnet run --project WebView22Browser.App`) **cannot** launch on Linux — WPF requires Windows.
- `dotnet build` and `dotnet format` work fully on Linux with `EnableWindowsTargeting=true`.

### CI parity

CI (`.github/workflows/ci.yml`) runs on `windows-latest`. Steps: restore → tool restore → format verify → build (Release) → test. The format check (`.cursor/rules/dotnet-format-ci.mdc`) must pass before pushing C# changes.
