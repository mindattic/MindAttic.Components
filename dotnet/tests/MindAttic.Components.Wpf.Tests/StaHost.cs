using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>One test on its own STA thread and Dispatcher, with an off-screen themed window (<see cref="OffScreenWindow"/>)
/// whose root panel the test adds controls to.</summary>
internal sealed class StaHost
{
    private readonly StackPanel _root = new();

    public Window Window { get; }

    private StaHost(double width, double height)
    {
        Window = OffScreenWindow.Show(width, height, _root);
        Pump();
    }

    public T Show<T>(T element) where T : FrameworkElement
    {
        _root.Children.Add(element);
        Pump();
        element.ApplyTemplate();
        return element;
    }

    public void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    /// <summary>Pumps the dispatcher (timers included) until <paramref name="done"/> or the deadline.</summary>
    public bool PumpUntil(Func<bool> done, TimeSpan? within = null)
    {
        var deadline = DateTime.UtcNow + (within ?? TimeSpan.FromSeconds(5));
        while (!done())
        {
            if (DateTime.UtcNow > deadline) return false;
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, (s, _) =>
            {
                ((DispatcherTimer)s!).Stop();
                frame.Continue = false;
            }, Dispatcher.CurrentDispatcher);
            Dispatcher.PushFrame(frame);
        }
        return true;
    }

    /// <summary>Clicks a button the way a user or screen reader does, through its automation Invoke pattern.</summary>
    public void Click(Button button)
    {
        var peer = (IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke);
        peer.Invoke();
        Pump();
    }

    public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    public static void Run(Action<StaHost> test, double width = 600, double height = 400)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            StaHost? host = null;
            try
            {
                host = new StaHost(width, height);
                test(host);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                host?.Window.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
