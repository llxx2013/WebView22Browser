using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class TabHistoryRestorerTests
{
    [Fact]
    public void BuildSteps_Empty_ReturnsEmpty()
    {
        var steps = TabHistoryRestorer.BuildSteps(Array.Empty<string>(), 0);
        Assert.Empty(steps);
    }

    [Fact]
    public void BuildSteps_SingleEntry_NavigatesOnce()
    {
        var steps = TabHistoryRestorer.BuildSteps(new[] { "https://a.com" }, 0);

        Assert.Single(steps);
        Assert.Equal(TabHistoryRestoreActionKind.Navigate, steps[0].Kind);
        Assert.Equal("https://a.com", steps[0].Uri);
    }

    [Fact]
    public void BuildSteps_LastIndex_NavigatesAllWithoutGoBack()
    {
        var steps = TabHistoryRestorer.BuildSteps(
            new[] { "https://a.com", "https://b.com", "https://c.com" }, 2);

        Assert.Equal(3, steps.Count);
        Assert.All(steps, s => Assert.Equal(TabHistoryRestoreActionKind.Navigate, s.Kind));
    }

    [Fact]
    public void BuildSteps_MiddleIndex_NavigatesAllThenGoBack()
    {
        var steps = TabHistoryRestorer.BuildSteps(
            new[] { "https://a.com", "https://b.com", "https://c.com" }, 1);

        Assert.Equal(4, steps.Count);
        Assert.Equal(TabHistoryRestoreActionKind.Navigate, steps[0].Kind);
        Assert.Equal(TabHistoryRestoreActionKind.Navigate, steps[2].Kind);
        Assert.Equal(TabHistoryRestoreActionKind.GoBack, steps[3].Kind);
    }

    [Fact]
    public void BuildSteps_FirstIndex_NavigatesAllThenGoBackTwice()
    {
        var steps = TabHistoryRestorer.BuildSteps(
            new[] { "https://a.com", "https://b.com", "https://c.com" }, 0);

        Assert.Equal(5, steps.Count);
        Assert.Equal(TabHistoryRestoreActionKind.GoBack, steps[3].Kind);
        Assert.Equal(TabHistoryRestoreActionKind.GoBack, steps[4].Kind);
    }
}