namespace WebView22Browser.Core.Services;

public readonly record struct NavigationErrorDecision(bool ShouldShow, bool ResetSuppressFlag);