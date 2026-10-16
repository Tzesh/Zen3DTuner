using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Zen3D.Tuner;

public enum NoticeKind { Information, Success, Warning, Error }

/// <summary>App-rendered desktop toast. It is not a Windows Action Center notification.</summary>
public sealed class ToastWindow : Window
{
    public ToastWindow(string message, string heading = "Zen3D Tuner", NoticeKind kind = NoticeKind.Information)
    {
        Title = "Zen3D Tuner notification";
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Zen3D.Tuner/Assets/zen3d.ico")));
        Width = 420; SizeToContent = SizeToContent.Height;
        CanResize = false; ShowInTaskbar = false; ShowActivated = false; Topmost = true;
        WindowDecorations = WindowDecorations.None;
        Background = Brush.Parse("#101B28");
        string accent = kind switch { NoticeKind.Success => "#54DFC3", NoticeKind.Warning => "#E8BE73", NoticeKind.Error => "#FF929C", _ => "#8EBBFF" };
        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("4,*") };
        content.Children.Add(new Border { Background = Brush.Parse(accent) });
        var panel = new StackPanel { Margin = new Thickness(18,16), Spacing = 10 };
        Grid.SetColumn(panel, 1);
        content.Children.Add(panel);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 10 };
        header.Children.Add(new Image { Source = new Bitmap(AssetLoader.Open(new Uri("avares://Zen3D.Tuner/Assets/zen3d.png"))), Width = 30, Height = 30 });
        var brand = new TextBlock { Text = "ZEN3D TUNER", Foreground = Brush.Parse(accent), FontSize = 11, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(brand, 1); header.Children.Add(brand);
        var close = new Button { Content = "×", Padding = new Thickness(8,0), Background = Brushes.Transparent, FontSize = 20 };
        ToolTip.SetTip(close, "Dismiss notification");
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 2); header.Children.Add(close);
        panel.Children.Add(header);
        panel.Children.Add(new TextBlock { Name = "NoticeTitle", Text = heading, FontSize = 17, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Name = "NoticeBody", Text = message, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#B6C7D8"), MaxWidth = 370 });
        panel.Children.Add(new TextBlock { Text = "Closes automatically • Details in Activity", FontSize = 10, Foreground = Brush.Parse("#7E95AC") });
        Content = content;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(kind is NoticeKind.Error or NoticeKind.Warning ? 12 : 7) };
        timer.Tick += (_, _) => Close();
        Closed += (_, _) => timer.Stop();
        // Give users time to read long failures without racing the timeout.
        PointerEntered += (_, _) => timer.Stop();
        PointerExited += (_, _) => timer.Start();
        void PositionToast()
        {
            var screen = Owner is not null ? Screens.ScreenFromWindow(Owner) : Screens.Primary;
            if (screen is not null)
                Position = new PixelPoint(screen.WorkingArea.Right - (int)(Bounds.Width * screen.Scaling) - 20,
                    screen.WorkingArea.Bottom - (int)(Bounds.Height * screen.Scaling) - 20);
        }
        Opened += (_, _) => { PositionToast(); timer.Start(); };
        SizeChanged += (_, _) => PositionToast();
    }
}
