using System.Reflection;

namespace AutoClicker;

internal enum AppView
{
    Vortex,
    AutoClicker,
    Recorder
}

internal sealed class MainForm : Form
{
    private readonly MacroEngine _engine = new();
    private readonly MacroRecorderService _recorder = new();
    private readonly string _settingsPath = Path.Combine(AppContext.BaseDirectory, "app-settings.json");
    private readonly Dictionary<string, NumericUpDown> _vortexSettingInputs = new();
    private readonly Dictionary<AppView, Button> _navButtons = new();
    private readonly Dictionary<AppView, Panel> _views = new();
    private readonly Dictionary<Button, string> _buttonBaseLabels = new();
    private readonly Dictionary<Button, Func<HotkeyChord>> _buttonHotkeyProviders = new();
    private readonly System.Windows.Forms.Timer _mouseTimer = new();

    private GlobalHotkeyService? _hotkeyService;
    private AppSettings _settings;
    private AppView _currentView = AppView.Vortex;
    private Panel _contentPanel = null!;
    private TextBox _logBox = null!;
    private Label _mousePositionLabel = null!;
    private ListBox _recordedEventsList = null!;

    private NumericUpDown _autoIntervalInput = null!;
    private NumericUpDown _autoClicksInput = null!;
    private NumericUpDown _autoStartDelayInput = null!;
    private ComboBox _autoClickTypeInput = null!;
    private TextBox _startHotkeyInput = null!;
    private TextBox _stopHotkeyInput = null!;
    private TextBox _playHotkeyInput = null!;
    private TextBox _macroNameInput = null!;

    private Button _vortexStartButton = null!;
    private Button _vortexPauseButton = null!;
    private Button _vortexStopButton = null!;
    private Button _vortexSaveButton = null!;
    private Button _vortexResetButton = null!;
    private Button _autoStartButton = null!;
    private Button _autoStopButton = null!;
    private Button _autoSaveButton = null!;
    private Button _recordToggleButton = null!;
    private Button _recordPlayButton = null!;
    private Button _recordStopButton = null!;
    private Button _recordImportButton = null!;
    private Button _recordExportButton = null!;
    private Button _recordClearButton = null!;
    private Button _saveHotkeysButton = null!;

    private CancellationTokenSource? _vortexCts;
    private CancellationTokenSource? _autoClickerCts;
    private CancellationTokenSource? _playbackCts;
    private TextBox? _activeHotkeyCapture;

    public MainForm()
    {
        _settings = AppSettings.Load(_settingsPath);

        Text = "Gasvar Macro";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 820;
        Height = 720;
        MinimumSize = new Size(680, 560);
        UiTheme.ApplyForm(this);

        BuildUi();
        LoadSettingsIntoInputs();
        ShowView(AppView.Vortex);

        _engine.StatusChanged += (_, message) => AppendLog(message);
        _engine.MousePositionChanged += (_, position) => SetMousePosition(position);
        _recorder.StatusChanged += (_, message) => AppendLog(message);
        _recorder.EventsChanged += (_, _) => RefreshRecordedEventsList();

        _mouseTimer.Interval = 100;
        _mouseTimer.Tick += (_, _) => SetMousePosition(MacroEngine.GetCurrentMousePositionText());
        _mouseTimer.Start();

        Load += (_, _) =>
        {
            _hotkeyService = new GlobalHotkeyService(Handle);
            RegisterHotkeys();
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _vortexCts?.Cancel();
        _autoClickerCts?.Cancel();
        _playbackCts?.Cancel();
        _hotkeyService?.Dispose();
        _recorder.Dispose();
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey)
        {
            _hotkeyService?.HandleHotkey(m.WParam.ToInt32());
        }

        base.WndProc(ref m);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Sidebar,
            Padding = new Padding(0, 20, 0, 20)
        };

        var brand = new Label
        {
            Text = "GASVAR\nMACRO",
            Dock = DockStyle.Top,
            Height = 72,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiTheme.Text
        };

        _mousePositionLabel = new Label
        {
            Text = "X=0, Y=0",
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = UiTheme.Accent
        };

        var navStack = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
        Button recorderButton = CreateNavButton("Recorder", AppView.Recorder, () => _settings.Hotkeys.NavRecorder);
        Button autoButton = CreateNavButton("AutoClicker", AppView.AutoClicker, () => _settings.Hotkeys.NavAutoClicker);
        Button vortexButton = CreateNavButton("Vortex Nexus", AppView.Vortex, () => _settings.Hotkeys.NavVortex);

        navStack.Controls.Add(recorderButton);
        navStack.Controls.Add(autoButton);
        navStack.Controls.Add(vortexButton);

        _navButtons[AppView.Vortex] = vortexButton;
        _navButtons[AppView.AutoClicker] = autoButton;
        _navButtons[AppView.Recorder] = recorderButton;

        sidebar.Controls.Add(navStack);
        sidebar.Controls.Add(_mousePositionLabel);
        sidebar.Controls.Add(brand);

        var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = UiTheme.Background };

        var headerHotkeys = CreateHeaderHotkeysPanel();

        _contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        _logBox = UiTheme.CreateLogBox();

        var logCard = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 140,
            BackColor = UiTheme.Surface,
            Padding = new Padding(12)
        };
        logCard.Controls.Add(_logBox);

        _views[AppView.Vortex] = CreateVortexView();
        _views[AppView.AutoClicker] = CreateAutoClickerView();
        _views[AppView.Recorder] = CreateRecorderView();

        main.Controls.Add(_contentPanel);
        main.Controls.Add(logCard);
        main.Controls.Add(headerHotkeys);

        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(main, 1, 0);
        Controls.Add(root);
    }

    private Button CreateNavButton(string text, AppView view, Func<HotkeyChord> hotkeyProvider)
    {
        Button button = UiTheme.CreateNavButton(FormatNavButtonLabel(text, hotkeyProvider()), (_, _) => ShowView(view));
        button.Dock = DockStyle.Top;
        TrackButtonHotkey(button, text, hotkeyProvider);
        return button;
    }

    private void ShowView(AppView view)
    {
        _currentView = view;
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(_views[view]);

        foreach ((AppView key, Button button) in _navButtons)
        {
            UiTheme.SetNavActive(button, key == view);
        }

    }

    private Panel CreateHeaderHotkeysPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 0, 0, 6)
        };

        var hotkeys = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 0)
        };
        hotkeys.Controls.Add(CreateHotkeyField("Start hotkey", out _startHotkeyInput, _settings.Hotkeys.Start));
        hotkeys.Controls.Add(CreateHotkeyField("Stop hotkey", out _stopHotkeyInput, _settings.Hotkeys.Stop));
        hotkeys.Controls.Add(CreateHotkeyField("Play hotkey", out _playHotkeyInput, _settings.Hotkeys.Play));

        var saveHotkeysRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(4, 0, 0, 0)
        };
        var saveHotkeysSpacer = CreateHotkeyLabel("\u00a0");
        saveHotkeysSpacer.Margin = new Padding(0, 0, 0, 2);
        _saveHotkeysButton = CreateActionButton("Save Hotkeys", UiTheme.Accent, () => _settings.Hotkeys.Save, SaveSettings);
        saveHotkeysRow.Controls.Add(saveHotkeysSpacer);
        saveHotkeysRow.Controls.Add(_saveHotkeysButton);
        hotkeys.Controls.Add(saveHotkeysRow);

        panel.Controls.Add(hotkeys);
        return panel;
    }

    private FlowLayoutPanel CreateHotkeyField(string label, out TextBox box, HotkeyChord chord)
    {
        var field = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 10, 0)
        };

        var fieldLabel = CreateHotkeyLabel(label);
        fieldLabel.Margin = new Padding(0, 0, 0, 2);
        box = CreateHotkeyBox(chord);
        field.Controls.Add(fieldLabel);
        field.Controls.Add(box);
        return field;
    }

    private Panel CreateVortexView()
    {
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true
        };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false
        };
        headerStack.Controls.Add(UiTheme.CreateTitle("Vortex Nexus"));
        headerStack.Controls.Add(UiTheme.CreateSubtitle("Configura and run the Vortex Nexus Macro."));

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        _vortexStartButton = CreateActionButton("Start", UiTheme.Success, () => _settings.Hotkeys.Start, StartVortex);
        _vortexPauseButton = CreateActionButton("Pause / Resume", UiTheme.Accent, () => HotkeyChord.FromKey((Keys)_settings.Vortex.ToggleHotkeyVirtualKey), () => _engine.TogglePause());
        _vortexStopButton = CreateActionButton("Stop", UiTheme.Danger, () => _settings.Hotkeys.Stop, StopVortex);
        _vortexPauseButton.Enabled = false;
        _vortexStopButton.Enabled = false;
        actions.Controls.AddRange(new Control[] { _vortexStartButton, _vortexPauseButton, _vortexStopButton });

        left.Controls.Add(headerStack, 0, 0);
        left.Controls.Add(actions, 0, 1);

        var configScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Surface };
        configScroll.Controls.Add(CreateVortexConfigTable());

        var saveRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _vortexSaveButton = CreateActionButton("Save", UiTheme.Accent, () => _settings.Hotkeys.Save, SaveSettings);
        _vortexResetButton = CreateActionButton("Reset Defaults", UiTheme.SurfaceAlt, () => _settings.Hotkeys.Reset, ResetVortexDefaults);
        saveRow.Controls.Add(_vortexSaveButton);
        saveRow.Controls.Add(_vortexResetButton);

        layout.Controls.Add(left, 0, 0);
        layout.SetColumnSpan(left, 2);
        layout.Controls.Add(configScroll, 0, 1);
        layout.Controls.Add(saveRow, 1, 1);

        card.Controls.Add(layout);
        return card;
    }

    private TableLayoutPanel CreateVortexConfigTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = UiTheme.Surface
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        int row = 0;
        AddSection(table, ref row, "Timing");
        AddVortexSetting(table, ref row, nameof(MacroSettings.CycleDelayMs), "Cycle delay (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.MoveDelayMs), "Move delay (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.VortexDelayMs), "After Vortex click (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusAfterFirstClickDelayMs), "After Nexus click 1 (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusAfterSecondClickDelayMs), "After Nexus click 2 (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusDelayMs), "After Nexus click 3 (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.ClearGoogleDelayMs), "Clear Google delay (ms)", 0, 600000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.ClearGoogleEveryCycles), "Clear Google every cycles", 0, 100000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.ScrollDownSteps), "Scroll down steps", 0, 100000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.ScrollUpSteps), "Scroll up steps", 0, 100000);
        AddVortexSetting(table, ref row, nameof(MacroSettings.ToggleHotkeyVirtualKey), "Pause hotkey (VK code)", 0, 255);

        AddSection(table, ref row, "Vortex Area");
        AddVortexSetting(table, ref row, nameof(MacroSettings.VortexXStart), "Vortex X start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.VortexXEnd), "Vortex X end");
        AddVortexSetting(table, ref row, nameof(MacroSettings.VortexYStart), "Vortex Y start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.VortexYEnd), "Vortex Y end");

        AddSection(table, ref row, "Nexus Areas");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusXStart), "Nexus X start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusXEnd), "Nexus X end");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusYStart), "Nexus Y start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusYEnd), "Nexus Y end");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusLowerYStart), "Nexus lower Y start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusLowerYEnd), "Nexus lower Y end");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusLowerPlusYStart), "Nexus lower+ Y start");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusLowerPlusYEnd), "Nexus lower+ Y end");

        AddSection(table, ref row, "Single Clicks");
        AddVortexSetting(table, ref row, nameof(MacroSettings.CloseNexusReminderX), "Close Nexus reminder X");
        AddVortexSetting(table, ref row, nameof(MacroSettings.CloseNexusReminderY), "Close Nexus reminder Y");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusPageX), "Nexus page X");
        AddVortexSetting(table, ref row, nameof(MacroSettings.NexusPageY), "Nexus page Y");
        AddVortexSetting(table, ref row, nameof(MacroSettings.CloseGoogleX), "Close Google X");
        AddVortexSetting(table, ref row, nameof(MacroSettings.CloseGoogleY), "Close Google Y");

        return table;
    }

    private Panel CreateAutoClickerView()
    {
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(UiTheme.CreateTitle("AutoClicker"), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
        layout.Controls.Add(UiTheme.CreateSubtitle("Repeats clicks at the current cursor position after a short countdown."), 0, 1);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 1)!, 2);

        var config = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        config.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        config.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _autoIntervalInput = UiTheme.CreateNumericInput(1, 600000, _settings.AutoClicker.IntervalMs);
        _autoClicksInput = UiTheme.CreateNumericInput(1, 100000, _settings.AutoClicker.ClickCount);
        _autoStartDelayInput = UiTheme.CreateNumericInput(0, 600000, _settings.AutoClicker.StartDelayMs);
        _autoClickTypeInput = UiTheme.CreateComboBox();
        _autoClickTypeInput.Items.AddRange(new object[] { "Left", "Right" });
        _autoClickTypeInput.SelectedItem = _settings.AutoClicker.ClickType;

        AddConfigRow(config, 0, "Interval (ms)", _autoIntervalInput);
        AddConfigRow(config, 1, "Number of clicks", _autoClicksInput);
        AddConfigRow(config, 2, "Start delay (ms)", _autoStartDelayInput);
        AddConfigRow(config, 3, "Click type", _autoClickTypeInput);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
        _autoStartButton = CreateActionButton("Start Autoclicker", UiTheme.Success, () => _settings.Hotkeys.Start, StartAutoClicker);
        _autoStopButton = CreateActionButton("Stop", UiTheme.Danger, () => _settings.Hotkeys.Stop, StopAutoClicker);
        _autoSaveButton = CreateActionButton("Save", UiTheme.Accent, () => _settings.Hotkeys.Save, SaveSettings);
        _autoStopButton.Enabled = false;
        actions.Controls.AddRange(new Control[] { _autoStartButton, _autoStopButton, _autoSaveButton });

        var stack = new Panel { Dock = DockStyle.Fill };
        stack.Controls.Add(actions);
        stack.Controls.Add(config);

        layout.Controls.Add(stack, 0, 2);
        layout.SetColumnSpan(stack, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Panel CreateRecorderView()
    {
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(UiTheme.CreateTitle("Macro Recorder"), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
        layout.Controls.Add(UiTheme.CreateSubtitle("Records delays between mouse moves, clicks, scrolls, and keystrokes. Import and export as JSON."), 0, 1);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 1)!, 2);

        var left = new Panel { Dock = DockStyle.Fill };
        _macroNameInput = new TextBox
        {
            Text = "My Macro",
            Width = 220,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        var nameRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        nameRow.Controls.Add(UiTheme.CreateFieldLabel("Macro name"));
        nameRow.Controls.Add(_macroNameInput);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 10, 0, 0) };
        _recordToggleButton = CreateActionButton("Start Recording", UiTheme.Danger, () => _settings.Hotkeys.Start, ToggleRecording);
        _recordPlayButton = CreateActionButton("Play Macro", UiTheme.Success, () => _settings.Hotkeys.Play, PlayRecordedMacro);
        _recordStopButton = CreateActionButton("Stop Playback", UiTheme.Accent, () => _settings.Hotkeys.Stop, StopPlayback);
        _recordImportButton = CreateActionButton("Import", UiTheme.SurfaceAlt, () => _settings.Hotkeys.Import, ImportMacro);
        _recordExportButton = CreateActionButton("Export", UiTheme.SurfaceAlt, () => _settings.Hotkeys.Export, ExportMacro);
        _recordClearButton = CreateActionButton("Clear", UiTheme.SurfaceAlt, () => _settings.Hotkeys.Reset, () => _recorder.Clear());
        actions.Controls.AddRange(new Control[]
        {
            _recordToggleButton,
            _recordPlayButton,
            _recordStopButton,
            _recordImportButton,
            _recordExportButton,
            _recordClearButton
        });

        left.Controls.Add(actions);
        left.Controls.Add(nameRow);

        _recordedEventsList = UiTheme.CreateEventList();
        var right = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.SurfaceAlt, Padding = new Padding(10) };
        right.Controls.Add(_recordedEventsList);

        layout.Controls.Add(left, 0, 2);
        layout.Controls.Add(right, 1, 2);
        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateHotkeyLabel(string text) => UiTheme.CreateFieldLabel(text);

    private TextBox CreateHotkeyBox(HotkeyChord chord)
    {
        TextBox box = UiTheme.CreateHotkeyBox();
        box.Text = FormatHotkey(chord);
        box.Tag = chord;
        box.Click += (_, _) => BeginHotkeyCapture(box);
        box.KeyDown += HotkeyBoxOnKeyDown;
        return box;
    }

    private Button CreateActionButton(string label, Color backColor, Func<HotkeyChord> hotkeyProvider, Action action)
    {
        Button button = UiTheme.CreateActionButton(FormatButtonLabel(label, hotkeyProvider()), backColor, (_, _) => action());
        TrackButtonHotkey(button, label, hotkeyProvider);
        return button;
    }

    private void TrackButtonHotkey(Button button, string baseLabel, Func<HotkeyChord> hotkeyProvider)
    {
        _buttonBaseLabels[button] = baseLabel;
        _buttonHotkeyProviders[button] = hotkeyProvider;
    }

    private void RefreshButtonHotkeyLabel(Button button)
    {
        if (!_buttonBaseLabels.TryGetValue(button, out string? baseLabel) ||
            !_buttonHotkeyProviders.TryGetValue(button, out Func<HotkeyChord>? hotkeyProvider))
        {
            return;
        }

        bool isNav = _navButtons.Values.Contains(button);
        button.Text = isNav
            ? FormatNavButtonLabel(baseLabel, hotkeyProvider())
            : FormatButtonLabel(baseLabel, hotkeyProvider());
    }

    private void RefreshAllButtonHotkeyLabels()
    {
        foreach (Button button in _buttonBaseLabels.Keys)
        {
            RefreshButtonHotkeyLabel(button);
        }
    }

    private static string FormatButtonLabel(string baseLabel, HotkeyChord chord) =>
        chord.IsValid ? $"{baseLabel} ({FormatHotkey(chord)})" : baseLabel;

    private static string FormatNavButtonLabel(string baseLabel, HotkeyChord chord) =>
        chord.IsValid ? $"{baseLabel}\n{FormatHotkey(chord)}" : baseLabel;

    private void BeginHotkeyCapture(TextBox box)
    {
        _activeHotkeyCapture = box;
        box.Text = "Press a key...";
        box.Focus();
    }

    private void HotkeyBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || _activeHotkeyCapture != box)
        {
            return;
        }

        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Escape)
        {
            box.Text = FormatHotkey(box.Tag as HotkeyChord ?? new HotkeyChord());
            _activeHotkeyCapture = null;
            return;
        }

        if (e.KeyCode is Keys.ShiftKey or Keys.ControlKey or Keys.Menu)
        {
            return;
        }

        uint modifiers = 0;
        if (e.Control)
        {
            modifiers |= NativeMethods.ModControl;
        }

        if (e.Alt)
        {
            modifiers |= NativeMethods.ModAlt;
        }

        if (e.Shift)
        {
            modifiers |= NativeMethods.ModShift;
        }

        var chord = new HotkeyChord
        {
            Modifiers = modifiers,
            VirtualKey = (int)e.KeyCode
        };
        box.Tag = chord;
        box.Text = FormatHotkey(chord);
        _activeHotkeyCapture = null;
    }

    private async void StartVortex()
    {
        if (_vortexCts != null)
        {
            return;
        }

        ApplySettingsFromInputs();
        SaveSettings();
        _vortexCts = new CancellationTokenSource();
        SetVortexRunningState(true);

        try
        {
            await _engine.RunVortexNexusAsync(_settings.Vortex, _vortexCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Vortex macro stopped.");
        }
        finally
        {
            _vortexCts?.Dispose();
            _vortexCts = null;
            SetVortexRunningState(false);
        }
    }

    private async void StartAutoClicker()
    {
        if (_autoClickerCts != null)
        {
            return;
        }

        ApplySettingsFromInputs();
        SaveSettings();
        _autoClickerCts = new CancellationTokenSource();
        _autoStartButton.Enabled = false;
        _autoStopButton.Enabled = true;

        try
        {
            bool rightClick = string.Equals(_autoClickTypeInput.SelectedItem?.ToString(), "Right", StringComparison.OrdinalIgnoreCase);
            await _engine.RunAutoClickerAsync(
                (int)_autoIntervalInput.Value,
                (int)_autoClicksInput.Value,
                rightClick,
                (int)_autoStartDelayInput.Value,
                _autoClickerCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Autoclicker stopped.");
        }
        finally
        {
            _autoClickerCts?.Dispose();
            _autoClickerCts = null;
            _autoStartButton.Enabled = true;
            _autoStopButton.Enabled = false;
        }
    }

    private void ToggleRecording()
    {
        if (_recorder.IsRecording)
        {
            RecordedMacro macro = _recorder.StopRecording(_macroNameInput.Text);
            _buttonBaseLabels[_recordToggleButton] = "Start Recording";
            _recordToggleButton.BackColor = UiTheme.Danger;
            RefreshButtonHotkeyLabel(_recordToggleButton);
            AppendLog($"Saved recording \"{macro.Name}\" with {macro.Events.Count} events.");
            _recorder.LoadEvents(macro.Events);
            return;
        }

        ApplySettingsFromInputs();
        IEnumerable<int> suppressed = new[]
        {
            _settings.Hotkeys.Start.VirtualKey,
            _settings.Hotkeys.Stop.VirtualKey,
            _settings.Hotkeys.Play.VirtualKey
        };
        _recorder.StartRecording(suppressed);
        _buttonBaseLabels[_recordToggleButton] = "Stop Recording";
        _recordToggleButton.BackColor = UiTheme.Success;
        RefreshButtonHotkeyLabel(_recordToggleButton);
    }

    private async void PlayRecordedMacro()
    {
        if (_playbackCts != null || _recorder.Events.Count == 0)
        {
            if (_recorder.Events.Count == 0)
            {
                AppendLog("No recorded events to play.");
            }

            return;
        }

        _playbackCts = new CancellationTokenSource();
        var macro = new RecordedMacro
        {
            Name = _macroNameInput.Text,
            Events = _recorder.Events.ToList()
        };

        try
        {
            await _engine.RunRecordedMacroAsync(macro, _playbackCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Recorded macro playback stopped.");
        }
        finally
        {
            _playbackCts?.Dispose();
            _playbackCts = null;
        }
    }

    private void StopVortex() => _vortexCts?.Cancel();
    private void StopAutoClicker() => _autoClickerCts?.Cancel();
    private void StopPlayback() => _playbackCts?.Cancel();

    private void ImportMacro()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Macro JSON (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import recorded macro"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        RecordedMacro macro = RecordedMacro.Load(dialog.FileName);
        _macroNameInput.Text = macro.Name;
        _recorder.LoadEvents(macro.Events);
        AppendLog($"Imported macro \"{macro.Name}\" from {dialog.FileName}.");
    }

    private void ExportMacro()
    {
        if (_recorder.Events.Count == 0)
        {
            AppendLog("Nothing to export yet.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Macro JSON (*.json)|*.json",
            FileName = $"{_macroNameInput.Text}.json",
            Title = "Export recorded macro"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var macro = new RecordedMacro
        {
            Name = _macroNameInput.Text,
            Events = _recorder.Events.ToList()
        };
        macro.Save(dialog.FileName);
        AppendLog($"Exported macro to {dialog.FileName}.");
    }

    private void SaveSettings()
    {
        ApplySettingsFromInputs();
        _settings.Save(_settingsPath);
        RegisterHotkeys();
        AppendLog("Settings saved.");
    }

    private void ResetVortexDefaults()
    {
        _settings.Vortex = MacroSettings.CreateDefault();
        LoadVortexSettingsIntoInputs();
        SaveSettings();
        AppendLog("Vortex settings reset to defaults.");
    }

    private void ApplySettingsFromInputs()
    {
        foreach ((string propertyName, NumericUpDown input) in _vortexSettingInputs)
        {
            PropertyInfo? property = typeof(MacroSettings).GetProperty(propertyName);
            property?.SetValue(_settings.Vortex, (int)input.Value);
        }

        _settings.AutoClicker.IntervalMs = (int)_autoIntervalInput.Value;
        _settings.AutoClicker.ClickCount = (int)_autoClicksInput.Value;
        _settings.AutoClicker.StartDelayMs = (int)_autoStartDelayInput.Value;
        _settings.AutoClicker.ClickType = _autoClickTypeInput.SelectedItem?.ToString() ?? "Left";

        _settings.Hotkeys.Start = GetHotkeyValue(_startHotkeyInput);
        _settings.Hotkeys.Stop = GetHotkeyValue(_stopHotkeyInput);
        _settings.Hotkeys.Play = GetHotkeyValue(_playHotkeyInput);
    }

    private void LoadSettingsIntoInputs()
    {
        LoadVortexSettingsIntoInputs();
        _autoIntervalInput.Value = Math.Min(_autoIntervalInput.Maximum, Math.Max(_autoIntervalInput.Minimum, _settings.AutoClicker.IntervalMs));
        _autoClicksInput.Value = Math.Min(_autoClicksInput.Maximum, Math.Max(_autoClicksInput.Minimum, _settings.AutoClicker.ClickCount));
        _autoStartDelayInput.Value = Math.Min(_autoStartDelayInput.Maximum, Math.Max(_autoStartDelayInput.Minimum, _settings.AutoClicker.StartDelayMs));
        _autoClickTypeInput.SelectedItem = _settings.AutoClicker.ClickType;

        SetHotkeyBox(_startHotkeyInput, _settings.Hotkeys.Start);
        SetHotkeyBox(_stopHotkeyInput, _settings.Hotkeys.Stop);
        SetHotkeyBox(_playHotkeyInput, _settings.Hotkeys.Play);
    }

    private void LoadVortexSettingsIntoInputs()
    {
        foreach ((string propertyName, NumericUpDown input) in _vortexSettingInputs)
        {
            PropertyInfo? property = typeof(MacroSettings).GetProperty(propertyName);
            if (property?.GetValue(_settings.Vortex) is int value)
            {
                input.Value = Math.Min(input.Maximum, Math.Max(input.Minimum, value));
            }
        }
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyService == null)
        {
            return;
        }

        _hotkeyService.Dispose();
        _hotkeyService = new GlobalHotkeyService(Handle);

        RegisterHotkey(_settings.Hotkeys.NavVortex, () => ShowView(AppView.Vortex));
        RegisterHotkey(_settings.Hotkeys.NavAutoClicker, () => ShowView(AppView.AutoClicker));
        RegisterHotkey(_settings.Hotkeys.NavRecorder, () => ShowView(AppView.Recorder));

        RegisterHotkey(_settings.Hotkeys.Start, DispatchStart);
        RegisterHotkey(_settings.Hotkeys.Stop, DispatchStop);
        RegisterHotkey(_settings.Hotkeys.Play, DispatchPlay);
        RegisterHotkey(HotkeyChord.FromKey((Keys)_settings.Vortex.ToggleHotkeyVirtualKey), DispatchPause);

        RegisterHotkey(_settings.Hotkeys.Save, DispatchSave);
        RegisterHotkey(_settings.Hotkeys.Reset, DispatchReset);
        RegisterHotkey(_settings.Hotkeys.Import, DispatchImport);
        RegisterHotkey(_settings.Hotkeys.Export, DispatchExport);

        RefreshAllButtonHotkeyLabels();
    }

    private void DispatchStart()
    {
        switch (_currentView)
        {
            case AppView.Vortex:
                StartVortex();
                break;
            case AppView.AutoClicker:
                StartAutoClicker();
                break;
            case AppView.Recorder when !_recorder.IsRecording:
                ToggleRecording();
                break;
        }
    }

    private void DispatchStop()
    {
        if (_vortexCts != null)
        {
            StopVortex();
            return;
        }

        if (_autoClickerCts != null)
        {
            StopAutoClicker();
            return;
        }

        if (_playbackCts != null)
        {
            StopPlayback();
            return;
        }

        if (_recorder.IsRecording)
        {
            ToggleRecording();
            return;
        }

        switch (_currentView)
        {
            case AppView.Vortex:
                StopVortex();
                break;
            case AppView.AutoClicker:
                StopAutoClicker();
                break;
            case AppView.Recorder:
                StopPlayback();
                break;
        }
    }

    private void DispatchPlay()
    {
        if (_currentView == AppView.Recorder)
        {
            PlayRecordedMacro();
        }
    }

    private void DispatchPause()
    {
        if (_vortexCts != null)
        {
            _engine.TogglePause();
        }
    }

    private void DispatchSave()
    {
        SaveSettings();
    }

    private void DispatchReset()
    {
        switch (_currentView)
        {
            case AppView.Vortex:
                ResetVortexDefaults();
                break;
            case AppView.Recorder:
                _recorder.Clear();
                AppendLog("Recorded events cleared.");
                break;
        }
    }

    private void DispatchImport()
    {
        if (_currentView == AppView.Recorder)
        {
            ImportMacro();
        }
    }

    private void DispatchExport()
    {
        if (_currentView == AppView.Recorder)
        {
            ExportMacro();
        }
    }

    private void RegisterHotkey(HotkeyChord chord, Action action)
    {
        if (_hotkeyService == null || !chord.IsValid)
        {
            return;
        }

        if (!_hotkeyService.Register(chord, action))
        {
            AppendLog($"Could not register hotkey {FormatHotkey(chord)}.");
        }
    }

    private void SetVortexRunningState(bool running)
    {
        _vortexStartButton.Enabled = !running;
        _vortexPauseButton.Enabled = running;
        _vortexStopButton.Enabled = running;
    }

    private void RefreshRecordedEventsList()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshRecordedEventsList);
            return;
        }

        _recordedEventsList.Items.Clear();
        foreach (RecordedMacroEvent recordedEvent in _recorder.Events)
        {
            _recordedEventsList.Items.Add(recordedEvent.Describe());
        }
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
    }

    private void SetMousePosition(string position)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetMousePosition(position));
            return;
        }

        _mousePositionLabel.Text = position;
    }

    private static HotkeyChord GetHotkeyValue(TextBox box) => box.Tag as HotkeyChord ?? new HotkeyChord();

    private static void SetHotkeyBox(TextBox box, HotkeyChord chord)
    {
        box.Tag = chord;
        box.Text = FormatHotkey(chord);
    }

    private static string FormatHotkey(HotkeyChord chord)
    {
        if (!chord.IsValid)
        {
            return "None";
        }

        var parts = new List<string>();
        if ((chord.Modifiers & NativeMethods.ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((chord.Modifiers & NativeMethods.ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((chord.Modifiers & NativeMethods.ModShift) != 0)
        {
            parts.Add("Shift");
        }

        parts.Add(chord.Key.ToString());
        return string.Join("+", parts);
    }

    private static void AddConfigRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(label), 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void AddVortexSetting(TableLayoutPanel table, ref int row, string propertyName, string labelText, int minimum = -10000, int maximum = 10000)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(labelText), 0, row);

        NumericUpDown input = UiTheme.CreateNumericInput(minimum, maximum, 0);
        _vortexSettingInputs[propertyName] = input;
        table.Controls.Add(input, 1, row);
        row++;
    }

    private static void AddSection(TableLayoutPanel table, ref int row, string title)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 8)
        };
        table.Controls.Add(label, 0, row);
        table.SetColumnSpan(label, 2);
        row++;
    }
}
