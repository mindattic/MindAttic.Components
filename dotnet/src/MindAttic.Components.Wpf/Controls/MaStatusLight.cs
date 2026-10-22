using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The state an <see cref="MaStatusLight"/> shows, each drawn in a theme brush.</summary>
public enum MaStatus
{
    /// <summary>Running, healthy, done: <c>MaGood</c> (green).</summary>
    Good,

    /// <summary>Needs attention soon (e.g. a usage limit close): <c>MaWarn</c> (orange).</summary>
    Warning,

    /// <summary>Failed, stopped, out of limit: <c>MaBad</c> (red).</summary>
    Bad,

    /// <summary>Anything else (idle, waiting, unknown): <c>MaOther</c> (purple).</summary>
    Other,
}

/// <summary>
/// A status light: a small dot whose colour is a theme token for its <see cref="Status"/>
/// (<see cref="MaStatus.Good"/> <c>MaGood</c>, <see cref="MaStatus.Warning"/> <c>MaWarn</c>,
/// <see cref="MaStatus.Bad"/> <c>MaBad</c>, <see cref="MaStatus.Other"/> <c>MaOther</c>), so a
/// host never hand-sets a colour and high contrast or an app's re-keyed brush follows live.
/// <code>
/// &lt;ctl:MaStatusLight Status="{Binding Health}" Label="{Binding HealthText}" /&gt;
/// </code>
/// <para>
/// Colour is never the only cue (WCAG 1.4.1): the light's accessible name (and its tooltip, unless
/// one is set) is <see cref="Label"/>, or the status word ("Good", "Warning", "Bad", "Other") when
/// no label is given; <c>AutomationProperties.Name</c> wins over both. It's not a tab stop. Its
/// look is the implicit <c>MaStatusLight</c> style in Theme.xaml: an 8px circle.
/// </para>
/// </summary>
public class MaStatusLight : Control
{
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(MaStatus), typeof(MaStatusLight),
        new FrameworkPropertyMetadata(MaStatus.Other, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptionChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(MaStatusLight),
        new FrameworkPropertyMetadata(null, OnDescriptionChanged));

    private static readonly DependencyPropertyKey DescriptionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Description), typeof(string), typeof(MaStatusLight), new FrameworkPropertyMetadata(Describe(MaStatus.Other)));

    /// <summary>The read-only <see cref="Description"/> property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DescriptionPropertyKey.DependencyProperty;

    static MaStatusLight()
    {
        // A light is a mark, not a stop in the tab order.
        FocusableProperty.OverrideMetadata(typeof(MaStatusLight), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaStatusLight), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The state shown; <see cref="MaStatus.Other"/> by default.</summary>
    public MaStatus Status
    {
        get => (MaStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>What the light means in words (e.g. "Running", "Out of limit"): its accessible name and
    /// tooltip. Null or empty (the default) uses the status word.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary><see cref="Label"/>, or the status word when there's no label: what a screen reader
    /// reads and the default tooltip shows.</summary>
    public string Description => (string)GetValue(DescriptionProperty);

    /// <summary>The theme brush key a status is drawn in: MaGood, MaWarn, MaBad or MaOther.</summary>
    public static string BrushKey(MaStatus status) => status switch
    {
        MaStatus.Good => "MaGood",
        MaStatus.Warning => "MaWarn",
        MaStatus.Bad => "MaBad",
        _ => "MaOther",
    };

    /// <summary>The status as a word: "Good", "Warning", "Bad" or "Other".</summary>
    public static string Describe(MaStatus status) => status switch
    {
        MaStatus.Good => "Good",
        MaStatus.Warning => "Warning",
        MaStatus.Bad => "Bad",
        _ => "Other",
    };

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var light = (MaStatusLight)d;
        var label = light.Label;
        light.SetValue(DescriptionPropertyKey, string.IsNullOrEmpty(label) ? Describe(light.Status) : label);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MaStatusLightAutomationPeer(this);

    /// <summary>A light reads as a labelled image: its name is <c>AutomationProperties.Name</c>, else
    /// <see cref="Description"/>; its item status is always the status word, so a screen reader that
    /// asks gets the state even when a custom label is set.</summary>
    private sealed class MaStatusLightAutomationPeer(MaStatusLight owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaStatusLight);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

        protected override string GetNameCore()
        {
            var name = AutomationProperties.GetName(Owner);
            return string.IsNullOrEmpty(name) ? ((MaStatusLight)Owner).Description : name;
        }

        protected override string GetItemStatusCore()
        {
            var status = AutomationProperties.GetItemStatus(Owner);
            return string.IsNullOrEmpty(status) ? Describe(((MaStatusLight)Owner).Status) : status;
        }

        protected override List<AutomationPeer>? GetChildrenCore() => null;
    }
}
