using System.Windows;
using System.Windows.Media.Animation;

namespace WebView22Browser.App.Animations;

public sealed class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(GridLength?), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength? From
    {
        get => (GridLength?)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        if (animationClock.CurrentProgress == 0 && From.HasValue)
            return From.Value;

        var fromValue = From?.Value ?? ((GridLength)defaultOriginValue).Value;
        var toValue = To.Value;
        var progress = animationClock.CurrentProgress ?? 0;
        return new GridLength(fromValue + (toValue - fromValue) * progress, GridUnitType.Pixel);
    }
}