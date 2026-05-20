namespace WebView22Browser.Core.Services;

public sealed class BrowserSettingsValidationResult
{
    public static BrowserSettingsValidationResult Success { get; } = new([]);

    public BrowserSettingsValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public string ErrorMessage => string.Join(Environment.NewLine, Errors);
}
