using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Zen3D.Tuner;

/// <summary>A short-lived desktop toast, also available to the logon helper without opening the editor.</summary>
internal sealed class ToastWindow : Window
{
    public ToastWindow(string message)
    {
        Title = "Zen3D Tuner";
        Width = 400; Height = 160;
        CanResize = false; ShowInTaskbar = false; Topmost = true;
        WindowDecorations = WindowDecorations.None;
        Background = Brush.Parse("#192132");
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "ZEN3D TUNER", Foreground = Brush.Parse("#55D5B2"), FontWeight = FontWeight.Bold });
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White });
        var close = new Button { Content = "Dismiss", HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => Close();
        panel.Children.Add(close);
        Content = panel;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        timer.Tick += (_, _) => { timer.Stop(); Close(); };
        Closed += (_, _) => timer.Stop();
        Opened += (_, _) =>
        {
            var screen = Screens.Primary;
            if (screen is not null)
                Position = new PixelPoint(screen.WorkingArea.Right - (int)(Width * screen.Scaling) - 24,
                    screen.WorkingArea.Bottom - (int)(Height * screen.Scaling) - 24);
            timer.Start();
        };
    }
}

