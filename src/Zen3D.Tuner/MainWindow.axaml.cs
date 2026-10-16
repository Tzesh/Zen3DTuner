using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Zen3D.Core;

namespace Zen3D.Tuner;

public partial class MainWindow : Window
{
    private readonly ProfileStore _store;
    private readonly IHardwareBackend _backend = new PreviewOnlyBackend();
    private CurveProfile _profile = new();
    private bool _loadingStartup;
    private HardwareSnapshot? _snapshot;
    private readonly bool _startupOperations;
    private bool _busy;

    public MainWindow() : this(new ProfileStore(), true) { }

    public MainWindow(ProfileStore store, bool startupOperations = false)
    {
        _store = store;
        _startupOperations = startupOperations;
        InitializeComponent();
        ProfileNameBox.TextChanged += (_, _) => StableProfileCheck.IsChecked = false;
        foreach (var box in new[] { PptBox, TdcBox, EdcBox })
            box.PropertyChanged += (_, change) =>
            {
                if (change.Property == NumericUpDown.ValueProperty || change.Property == NumericUpDown.TextProperty)
                    StableProfileCheck.IsChecked = false;
            };
        CpuNameText.Text = CpuInfo.Detect();
        bool windows = OperatingSystem.IsWindows();
        ApplyButton.IsEnabled = windows;
        ReadCurrentButton.IsEnabled = windows;
        ApplyOnLaunchCheck.IsEnabled = windows;
        ExperimentalCheck.IsEnabled = windows;
        StableProfileCheck.IsEnabled = windows;
        EnableApplyStartupButton.IsEnabled = windows;
        DisableApplyStartupButton.IsEnabled = windows;
        try
        {
            _profile = _store.Load();
            StatusText.Text = "Saved profile loaded. Current CPU settings will be read separately.";
        }
        catch (Exception e)
        {
            StatusText.Text = "Profile could not be loaded (using safe zero offsets): " + e.Message;
        }
        PopulateProfile();
        _loadingStartup = true;
        AutostartCheck.IsEnabled = WindowsStartupService.IsAvailable;
        if (WindowsStartupService.IsAvailable)
        {
            try { AutostartCheck.IsChecked = WindowsStartupService.IsEnabled(); }
            catch (Exception e) { StatusText.Text = "Startup state unavailable: " + e.Message; }
        }
        _loadingStartup = false;
        Opened += async (_, _) =>
        {
            if (!_startupOperations || !OperatingSystem.IsWindows()) return;
            // Capture the baseline before any optional write. Reading does not save the editor.
            bool apply = _profile.ApplyOnLaunch;
            await RunElevated("--read-current");
            if (apply) await RunElevated("--apply-profile");
        };
    }

    private void PopulateProfile()
    {
        ProfileNameBox.Text = _profile.Name;
        PptBox.Value = _profile.PptWatts;
        TdcBox.Value = _profile.TdcAmps;
        EdcBox.Value = _profile.EdcAmps;
        ApplyOnLaunchCheck.IsChecked = _profile.ApplyOnLaunch;
        CorePanel.Children.Clear();
        for (int core = 0; core < _profile.Offsets.Length; core++)
        {
            int index = core;
            var val = new TextBlock
            {
                Text = _profile.Offsets[core].ToString(),
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse("#55D5B2"),
                FontSize = 14
            };
            var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            heading.Children.Add(new TextBlock
            {
                Text = $"CORE {core}", FontWeight = FontWeight.SemiBold,
                FontSize = 12, Foreground = Brush.Parse("#CFD7E8")
            });
            Grid.SetColumn(val, 1);
            heading.Children.Add(val);

            var slider = new Slider
            {
                Minimum = -30, Maximum = 0,
                Value = _profile.Offsets[core],
                TickFrequency = 1, IsSnapToTickEnabled = true,
                Width = 185, HorizontalAlignment = HorizontalAlignment.Left
            };
            slider.PropertyChanged += (_, args) =>
            {
                if (args.Property != Slider.ValueProperty) return;
                int offset = (int)Math.Round(slider.Value);
                _profile.Offsets[index] = offset;
                StableProfileCheck.IsChecked = false;
                val.Text = offset.ToString();
            };
            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(heading);
            stack.Children.Add(slider);
            var reading = _snapshot?.Cores.FirstOrDefault(c => c.Core == core);
            stack.Children.Add(new TextBlock { Text = "Current: " + (reading?.Value?.ToString() ?? "unavailable"),
                Foreground = Brush.Parse("#9BA9C3"), FontSize = 12 });
            CorePanel.Children.Add(new Border
            {
                Width = 235,
                CornerRadius = new CornerRadius(12),
                Background = Brush.Parse("#253044"),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 10, 10),
                Child = stack
            });
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ReadEditors();
            if (_profile.ApplyOnLaunch && (ExperimentalCheck.IsChecked != true || StableProfileCheck.IsChecked != true))
                throw new InvalidOperationException("Enable hardware writes and confirm these exact settings before saving automatic application.");
            _store.Save(_profile);
            StatusText.Text = "Saved locally. No offsets were applied to the CPU.";
        }
        catch (Exception ex) { StatusText.Text = "Save failed: " + ex.Message; }
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ReadEditors();
            StableProfileCheck.IsChecked = false;
            _profile.Offsets = new int[8];
            PopulateProfile();
            StatusText.Text = "All offsets reset to 0 in the editor. Power limits preserved. Save to persist.";
        }
        catch (Exception ex) { StatusText.Text = "Reset failed: " + ex.Message; }
    }

    private void Preview_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ReadEditors();
            StatusText.Text = _backend.Preview(_profile);
        }
        catch (Exception ex) { StatusText.Text = "Invalid profile: " + ex.Message; }
    }


    private void ReadEditors()
    {
        static int? Read(NumericUpDown box)
        {
            // Read visible text too: invalid/uncommitted text must never silently apply an older value.
            if (string.IsNullOrWhiteSpace(box.Text)) return null;
            if (!decimal.TryParse(box.Text, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture, out decimal value) || value != decimal.Truncate(value))
                throw new ArgumentException("Power limits must be whole numbers, or blank to leave unchanged.");
            return checked((int)value);
        }
        var edited = _profile.Clone();
        edited.Name = ProfileNameBox.Text?.Trim() ?? "";
        edited.PptWatts = Read(PptBox);
        edited.TdcAmps = Read(TdcBox);
        edited.EdcAmps = Read(EdcBox);
        edited.ApplyOnLaunch = ApplyOnLaunchCheck.IsChecked == true;
        CurveProfileValidator.Validate(edited);
        _profile = edited;
    }

    private void ClearPower_Click(object? sender, RoutedEventArgs e)
    {
        StableProfileCheck.IsChecked = false;
        PptBox.Value = TdcBox.Value = EdcBox.Value = null;
        PptBox.Text = TdcBox.Text = EdcBox.Text = "";
        StatusText.Text = "Power limits left unchanged. This does not restore CPU firmware defaults.";
    }

    private async void Apply_Click(object? sender, RoutedEventArgs e)
    {
        if (ExperimentalCheck.IsChecked != true)
        {
            StatusText.Text = "Enable experimental hardware writes first. Apply requires Windows x64, PawnIO and administrator rights.";
            return;
        }
        await RunElevated("--apply-profile");
    }

    private async void EnableApplyStartup_Click(object? sender, RoutedEventArgs e)
    {
        if (ExperimentalCheck.IsChecked != true || StableProfileCheck.IsChecked != true)
        {
            StatusText.Text = "Enable hardware writes and confirm that you tested these exact settings before saving a startup snapshot.";
            return;
        }
        await RunElevated("--startup-enable");
    }

    private async void DisableApplyStartup_Click(object? sender, RoutedEventArgs e) => await RunElevated("--startup-disable");

    private async void ReadCurrent_Click(object? sender, RoutedEventArgs e) => await RunElevated("--read-current");

    private void UseCurrent_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _profile = (_snapshot ?? throw new InvalidOperationException("Read current settings first.")).ToProfile(ProfileNameBox.Text ?? "Current settings");
            PopulateProfile();
            StableProfileCheck.IsChecked = false;
            StatusText.Text = "Current CPU settings copied to the editor. Save to keep them; Apply to write changes.";
        }
        catch (Exception ex) { StatusText.Text = "Cannot import current settings: " + ex.Message; }
    }

    private void DisplaySnapshot(HardwareSnapshot snapshot)
    {
        _snapshot = snapshot;
        static string Value(double? value, string unit) => value is null ? "unavailable" : $"{value:0.##} {unit}";
        CurrentPowerText.Text = $"Current PPT: {Value(snapshot.Power.PptWatts, "W")} • TDC: {Value(snapshot.Power.TdcAmps, "A")} • EDC: {Value(snapshot.Power.EdcAmps, "A")}";
        ReadbackText.Text = $"Read at {snapshot.CapturedAt.ToLocalTime():HH:mm:ss} • {(snapshot.Complete ? "All values available" : "Partial readback")} • PM table 0x{snapshot.Power.TableVersion:X}. {snapshot.Power.Error}";
        // Preserve unsaved editor values; refreshing must never overwrite a saved profile.
        PopulateProfile();
    }

    private async Task RunElevated(string action)
    {
        if (!OperatingSystem.IsWindows()) { StatusText.Text = "Windows only; macOS is preview-only."; return; }
        if (_busy) return;
        _busy = true;
        EditorPanel.IsEnabled = false;
        bool applying = action == "--apply-profile";
        bool reading = action == "--read-current";
        string? requestPath = null;
        try
        {
            if (applying)
            {
                _snapshot = null;
                CurrentPowerText.Text = "Current power limits: awaiting post-apply readback";
                ReadbackText.Text = "Previous readings invalidated by application.";
            }
            if (reading)
            {
                ReadEditors();
                _snapshot = null;
                CurrentPowerText.Text = "Current power limits: reading…";
                ReadbackText.Text = "Reading current CPU settings…";
                PopulateProfile();
            }
            var executable = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executable) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Run the Windows Zen3D.Tuner.exe build first.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas" };
            start.ArgumentList.Add(action);
            if (action != "--startup-disable" && !reading)
            {
                ReadEditors();
                // Applying is not saving: keep persisted auto-apply choices unchanged.
                requestPath = Path.Combine(Path.GetDirectoryName(_store.FilePath)!, $"request-{Guid.NewGuid():N}.json");
                new ProfileStore(requestPath).Save(_profile);
                start.ArgumentList.Add(Path.GetFullPath(requestPath));
            }
            start.ArgumentList.Add("--no-gui");
            start.ArgumentList.Add("--expected-user");
            start.ArgumentList.Add(WindowsIdentity.GetCurrent().User!.Value);
            if (action == "--startup-enable") start.ArgumentList.Add("--confirm-stable");
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Zen3DTuner", $"apply-{WindowsIdentity.GetCurrent().User!.Value}.log");
            int previousLogLines = File.Exists(logPath) ? File.ReadLines(logPath).Count() : 0;
            if (applying) SetHardwareStatus("APPLY IN PROGRESS", "#EBCB8B");
            StatusText.Text = "Waiting for Windows administrator approval and operation result…";
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the hardware helper.");
            await process.WaitForExitAsync();
            if (reading && process.ExitCode != 0) throw new InvalidOperationException("Hardware helper failed. Check the protected activity log for details.");
            if ((reading || applying) && process.ExitCode == 0)
            {
                const string marker = "SUCCESS HARDWARE_SNAPSHOT ";
                string? line = File.Exists(logPath) ? File.ReadLines(logPath).Skip(previousLogLines).LastOrDefault(l => l.Contains(marker)) : null;
                if (line is null && reading) throw new InvalidOperationException("The helper returned no hardware snapshot.");
                if (line is not null)
                {
                var snapshot = JsonSerializer.Deserialize<HardwareSnapshot>(line[(line.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..])
                    ?? throw new InvalidDataException("Invalid hardware snapshot.");
                DisplaySnapshot(snapshot);
                if (reading && !File.Exists(_store.FilePath) && snapshot.Complete)
                {
                    try { _profile = snapshot.ToProfile("Current settings"); PopulateProfile(); }
                    catch (Exception) { /* Display values even if outside the editor's supported range. */ }
                }
                }
            }
            StatusText.Text = reading ? "Current firmware values refreshed. Saved profile unchanged." : $"Operation exited with code {process.ExitCode}. " +
                (File.Exists(logPath) ? File.ReadLines(logPath).Skip(previousLogLines).LastOrDefault() ?? "No new result log available." : "No result log available; run the command in an administrator terminal for details.");
            if (applying)
            {
                SetHardwareStatus(process.ExitCode == 0 ? "LAST APPLY: ACKNOWLEDGED" : "LAST APPLY: FAILED / CHECK LOG",
                    process.ExitCode == 0 ? "#78D8B5" : "#F0A0A0");
                if (_snapshot is null)
                {
                    CurrentPowerText.Text = "Current power limits: unavailable";
                    ReadbackText.Text = "Post-apply readback unavailable. Refresh current values to retry.";
                }
                PopulateProfile();
                new ToastWindow(process.ExitCode == 0 ? $"Profile '{_profile.Name}' applied. SMU commands acknowledged." : "Profile application failed. Some settings may have changed; check Activity.").Show(this);
            }
            if (process.ExitCode == 0 && action == "--startup-enable") StableProfileCheck.IsChecked = false;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Operation failed or was cancelled: " + ex.Message;
            if (applying) SetHardwareStatus("APPLY INCOMPLETE / CHECK LOG", "#F0A0A0");
            if (applying) new ToastWindow("Profile application failed or was cancelled. " + ex.Message).Show(this);
            if (reading) { ReadbackText.Text = "Readback unavailable: " + ex.Message; CurrentPowerText.Text = "Current power limits: unavailable"; }
        }
        finally
        {
            if (requestPath is not null) { try { File.Delete(requestPath); } catch { } }
            EditorPanel.IsEnabled = true;
            _busy = false;
        }
    }

    private void SetHardwareStatus(string text, string color)
    {
        HardwareStatusText.Text = text;
        HardwareStatusText.Foreground = Brush.Parse(color);
    }

    private void Autostart_Changed(object? sender, RoutedEventArgs e)
    {
        if (_loadingStartup || !WindowsStartupService.IsAvailable) return;
        try
        {
            WindowsStartupService.SetEnabled(AutostartCheck.IsChecked == true);
            StatusText.Text = AutostartCheck.IsChecked == true
                ? "GUI launch-on-login enabled. The saved app-open preference controls automatic application."
                : "GUI launch-on-login disabled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Startup setting failed: " + ex.Message;
            _loadingStartup = true;
            AutostartCheck.IsChecked = WindowsStartupService.IsEnabled();
            _loadingStartup = false;
        }
    }
}

