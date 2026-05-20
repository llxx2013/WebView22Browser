namespace WebView22Browser.Core.Models;

public sealed record GmCookie(string Name, string Value, string? Domain = null, string? Path = null);
