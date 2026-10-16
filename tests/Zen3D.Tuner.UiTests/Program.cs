using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Zen3D.Core;
using Zen3D.Tuner;

static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
string path = Path.Combine(Path.GetTempPath(), "zen3d-ui-" + Guid.NewGuid() + ".json");
try
{
    var store = new ProfileStore(path);
    var window = new MainWindow(store);
    window.Show();
    Dispatcher.UIThread.RunJobs();
    if (args.Length == 1)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame() ?? throw new Exception("No rendered UI frame");
        frame.Save(args[0], new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
    }
    T Control<T>(string name) where T : Control => window.FindControl<T>(name) ?? throw new Exception("Missing control " + name);
    void Click(string name) => Control<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Check(Control<NumericUpDown>("PptBox").Value is null, "New profile must leave PPT unchanged");
    Control<NumericUpDown>("PptBox").Value = 100;
    Control<NumericUpDown>("TdcBox").Value = 65;
    Control<NumericUpDown>("EdcBox").Value = 90;
    Control<TextBox>("ProfileNameBox").Text = "UI test";
    Click("PreviewButton");
    Check(Control<TextBlock>("StatusText").Text!.Contains("PPT=100 W"), "Preview must use visible power fields");
    Click("SaveButton");
    Check(store.LoadRequired().PptWatts == 100 && store.LoadRequired().EdcAmps == 90, "Save must persist power edits");
    Control<CheckBox>("StableProfileCheck").IsChecked = true;
    Control<NumericUpDown>("PptBox").Value = 110;
    Check(Control<CheckBox>("StableProfileCheck").IsChecked == false, "Changing values must invalidate stability confirmation");
    Click("ResetButton");
    Check(Control<NumericUpDown>("PptBox").Value == 110, "Reset offsets must preserve unsaved power edits");
    Control<NumericUpDown>("PptBox").Text = "not a number";
    Click("SaveButton");
    Check(Control<TextBlock>("StatusText").Text!.Contains("failed", StringComparison.OrdinalIgnoreCase), "Invalid visible text must fail instead of saving a stale value");
    Check(store.LoadRequired().PptWatts == 100, "Invalid text must not overwrite saved profile");
    Control<NumericUpDown>("PptBox").Value = 100;
    Control<NumericUpDown>("PptBox").Text = "100";
    Control<CheckBox>("ApplyOnLaunchCheck").IsChecked = true;
    Click("SaveButton");
    Check(!store.LoadRequired().ApplyOnLaunch, "Auto-apply requires explicit stability confirmation");
    Control<CheckBox>("ExperimentalCheck").IsChecked = true;
    Control<CheckBox>("StableProfileCheck").IsChecked = true;
    Click("SaveButton");
    Check(store.LoadRequired().ApplyOnLaunch, "Save persists explicit auto-apply selection");
    Control<CheckBox>("ApplyOnLaunchCheck").IsChecked = false;
    Control<CheckBox>("StableProfileCheck").IsChecked = false;
    Click("SaveButton");
    Check(!store.LoadRequired().ApplyOnLaunch, "Auto-apply can be disabled without confirmation");
    if (!OperatingSystem.IsWindows())
    {
        Check(!Control<Button>("ApplyButton").IsEnabled, "macOS cannot apply hardware settings");
        Check(!Control<Button>("EnableApplyStartupButton").IsEnabled, "macOS cannot enroll startup");
    }
    window.Close();
    Console.WriteLine("All headless UI tests passed; no hardware or user profile modified.");
}
finally { if (File.Exists(path)) File.Delete(path); }
