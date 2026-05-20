using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class NavigationErrorPolicyTests
{
    [Fact]
    public void Evaluate_SuppressFlag_ResetsAndReturnsFalse()
    {
        var decision = NavigationErrorPolicy.Evaluate(
            BrowserErrorStatus.Timeout,
            suppressNavigationError: true,
            isClosing: false);

        Assert.False(decision.ShouldShow);
        Assert.True(decision.ResetSuppressFlag);
    }

    [Fact]
    public void Evaluate_OperationCanceled_ReturnsFalse()
    {
        var decision = NavigationErrorPolicy.Evaluate(
            BrowserErrorStatus.OperationCanceled,
            suppressNavigationError: false,
            isClosing: false);

        Assert.False(decision.ShouldShow);
        Assert.False(decision.ResetSuppressFlag);
    }

    [Fact]
    public void Evaluate_RealError_ReturnsTrue()
    {
        var decision = NavigationErrorPolicy.Evaluate(
            BrowserErrorStatus.HostNameNotResolved,
            suppressNavigationError: false,
            isClosing: false);

        Assert.True(decision.ShouldShow);
        Assert.False(decision.ResetSuppressFlag);
    }

    [Fact]
    public void Evaluate_IsClosing_ReturnsFalse()
    {
        var decision = NavigationErrorPolicy.Evaluate(
            BrowserErrorStatus.Timeout,
            suppressNavigationError: false,
            isClosing: true);

        Assert.False(decision.ShouldShow);
    }
}