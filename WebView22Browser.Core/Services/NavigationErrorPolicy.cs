using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class NavigationErrorPolicy
{
    public static NavigationErrorDecision Evaluate(
        BrowserErrorStatus errorStatus,
        bool suppressNavigationError,
        bool isClosing)
    {
        if (suppressNavigationError)
            return new NavigationErrorDecision(ShouldShow: false, ResetSuppressFlag: true);

        if (isClosing)
            return new NavigationErrorDecision(false, false);

        if (errorStatus == BrowserErrorStatus.OperationCanceled)
            return new NavigationErrorDecision(false, false);

        return new NavigationErrorDecision(true, false);
    }
}