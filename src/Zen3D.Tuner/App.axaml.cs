using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Zen3D.Tuner;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = Program.NotificationMessage is string message
                ? new ToastWindow(message, Program.NotificationFailed ? "Automatic application failed" : "Profile applied",
                    Program.NotificationFailed ? NoticeKind.Error : NoticeKind.Success)
                : new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

