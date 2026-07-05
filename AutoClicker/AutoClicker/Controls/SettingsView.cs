namespace AutoClicker.Controls;

internal sealed class SettingsView : UserControl
{
    private readonly CheckBox _minimizeToTray;
    private readonly CheckBox _closeToTray;
    private readonly CheckBox _showOverlay;
    private readonly CheckBox _startWithWindows;
    private readonly NumericUpDown _moveThrottle;
    private readonly CheckBox _filterMoves;
    private readonly Label _settingsPathLabel;

    public event EventHandler? SaveRequested;
    public event EventHandler? ImportAllRequested;
    public event EventHandler? ExportAllRequested;

    public SettingsView()
    {
        Dock = DockStyle.Fill;
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
        layout.Controls.Add(UiTheme.CreateTitle("Settings"), 0, 0);
        layout.Controls.Add(UiTheme.CreateSubtitle("Application behavior, startup, and full settings backup."), 0, 1);

        _minimizeToTray = CreateCheck("Minimize to system tray");
        _closeToTray = CreateCheck("Close button minimizes to tray");
        _showOverlay = CreateCheck("Show status overlay while macros run");
        _startWithWindows = CreateCheck("Start with Windows");
        _moveThrottle = UiTheme.CreateNumericInput(0, 1000, 30);
        _moveThrottle.Width = 80;
        _filterMoves = CreateCheck("Filter redundant mouse moves while recording");

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        options.Controls.AddRange([_minimizeToTray, _closeToTray, _showOverlay, _startWithWindows]);

        var recorderGroup = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 12, 0, 0) };
        recorderGroup.Controls.Add(UiTheme.CreateFieldLabel("Recorder move throttle (ms)"));
        recorderGroup.Controls.Add(_moveThrottle);
        options.Controls.Add(recorderGroup);
        options.Controls.Add(_filterMoves);

        _settingsPathLabel = new Label
        {
            AutoSize = false,
            Width = 640,
            Height = 40,
            ForeColor = UiTheme.TextMuted,
            Margin = new Padding(0, 14, 0, 0)
        };
        options.Controls.Add(_settingsPathLabel);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 16, 0, 0) };
        actions.Controls.Add(UiTheme.CreateActionButton("Save Settings", UiTheme.Accent, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty)));
        actions.Controls.Add(UiTheme.CreateActionButton("Export All Settings", UiTheme.SurfaceAlt, (_, _) => ExportAllRequested?.Invoke(this, EventArgs.Empty)));
        actions.Controls.Add(UiTheme.CreateActionButton("Import All Settings", UiTheme.SurfaceAlt, (_, _) => ImportAllRequested?.Invoke(this, EventArgs.Empty)));

        layout.Controls.Add(options, 0, 2);
        layout.Controls.Add(actions, 0, 3);
        card.Controls.Add(layout);
        Controls.Add(card);
    }

    public void SetSettingsPath(string path)
    {
        _settingsPathLabel.Text = $"Settings file: {path}";
    }

    public void LoadSettings(AppBehaviorSettings behavior, RecorderSettings recorder)
    {
        _minimizeToTray.Checked = behavior.MinimizeToTray;
        _closeToTray.Checked = behavior.CloseToTray;
        _showOverlay.Checked = behavior.ShowOverlay;
        _startWithWindows.Checked = behavior.StartWithWindows;
        _moveThrottle.Value = Math.Min(_moveThrottle.Maximum, Math.Max(_moveThrottle.Minimum, recorder.MoveThrottleMs));
        _filterMoves.Checked = recorder.FilterRedundantMoves;
    }

    public void ApplyTo(AppBehaviorSettings behavior, RecorderSettings recorder)
    {
        behavior.MinimizeToTray = _minimizeToTray.Checked;
        behavior.CloseToTray = _closeToTray.Checked;
        behavior.ShowOverlay = _showOverlay.Checked;
        behavior.StartWithWindows = _startWithWindows.Checked;
        recorder.MoveThrottleMs = (int)_moveThrottle.Value;
        recorder.FilterRedundantMoves = _filterMoves.Checked;
    }

    private static CheckBox CreateCheck(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = UiTheme.Text,
        Margin = new Padding(0, 6, 0, 6)
    };
}
