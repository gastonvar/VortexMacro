using AutoClicker.Controls;

namespace AutoClicker;

internal enum AppView
{
    Vortex,
    AutoClicker,
    Recorder,
    Piano,
    Settings
}

internal sealed class MainForm : Form
{
    private readonly MacroEngine _engine = new();
    private readonly MacroRecorderService _recorder = new();
    private readonly MacroSession _session = new();
    private readonly MacroLibrary _macroLibrary = new();
    private readonly CoordinatePickerService _coordinatePicker = new();
    private readonly HotkeyButtonTracker _hotkeyButtons = new();
    private readonly string _settingsPath = SettingsPaths.PrepareSettingsFile();
    private readonly Dictionary<AppView, Button> _navButtons = new();
    private readonly Dictionary<AppView, Control> _views = new();
    private readonly System.Windows.Forms.Timer _mouseTimer = new();

    private GlobalHotkeyService? _hotkeyService;
    private TrayIconService? _tray;
    private MacroOverlayForm? _overlay;
    private AppSettings _settings = AppSettings.CreateDefault();
    private bool _uiReady;
    private AppView _currentView = AppView.Vortex;
    private Panel _contentPanel = null!;
    private TextBox _logBox = null!;
    private Label _mousePositionLabel = null!;
    private HotkeyPanel _hotkeyPanel = null!;
    private VortexView _vortexView = null!;
    private AutoClickerView _autoClickerView = null!;
    private RecorderView _recorderView = null!;
    private PianoPlayerView _pianoView = null!;
    private SettingsView _settingsView = null!;

    private CancellationTokenSource? _vortexCts;
    private CancellationTokenSource? _autoClickerCts;
    private CancellationTokenSource? _playbackCts;
    private CancellationTokenSource? _pianoCts;
    private bool _isPickingCoordinate;
    private bool _reallyExit;

    public MainForm()
    {
        Text = "Gasvar Macro";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 860;
        Height = 740;
        MinimumSize = new Size(700, 580);
        UiTheme.ApplyForm(this);

        BuildUi();

        _settings = AppSettings.Load(_settingsPath, AppendLog);
        AppendLog($"Settings loaded from: {_settingsPath}");

        WireEvents();
        LoadAllSettings();
        ShowView(AppView.Vortex);

        _engine.StatusChanged += (_, message) => AppendLog(message);
        _engine.MousePositionChanged += (_, position) => SetMousePosition(position);
        _engine.SessionStarted += OnSessionStarted;
        _engine.SessionEnded += OnSessionEnded;
        _recorder.StatusChanged += (_, message) => AppendLog(message);
        _recorder.EventsChanged += (_, _) => _recorderView.RefreshEvents(_recorder.Events);
        _recorder.Configure(_settings.Recorder);

        _mouseTimer.Interval = 100;
        _mouseTimer.Tick += (_, _) => SetMousePosition(MacroEngine.GetCurrentMousePositionText());
        _mouseTimer.Start();

        Load += (_, _) =>
        {
            _hotkeyService = new GlobalHotkeyService(Handle);
            RegisterHotkeys();
            ApplyBehaviorSettings();
        };

        _uiReady = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyExit && _settings.Behavior.CloseToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        SaveSettings();
        _vortexCts?.Cancel();
        _autoClickerCts?.Cancel();
        _playbackCts?.Cancel();
        _pianoCts?.Cancel();
        _hotkeyService?.Dispose();
        _tray?.Dispose();
        _overlay?.Close();
        _overlay?.Dispose();
        _coordinatePicker.Dispose();
        _recorder.Dispose();
        _session.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!_uiReady || !_settings.Behavior.MinimizeToTray || WindowState != FormWindowState.Minimized)
        {
            return;
        }

        HideToTray();
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

        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Sidebar, Padding = new Padding(0, 20, 0, 20) };
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
        Button vortexNav = CreateNavButton("Vortex Nexus", AppView.Vortex, HotkeyRestrictions.NavVortex);
        Button autoNav = CreateNavButton("AutoClicker", AppView.AutoClicker, HotkeyRestrictions.NavAutoClicker);
        Button recorderNav = CreateNavButton("Recorder", AppView.Recorder, HotkeyRestrictions.NavRecorder);
        Button pianoNav = CreateNavButton("Piano Player", AppView.Piano, HotkeyRestrictions.NavPiano);
        Button settingsNav = CreateNavButton("Settings", AppView.Settings, null);
        navStack.Controls.AddRange([settingsNav, pianoNav, recorderNav, autoNav, vortexNav]);
        _navButtons[AppView.Vortex] = vortexNav;
        _navButtons[AppView.AutoClicker] = autoNav;
        _navButtons[AppView.Recorder] = recorderNav;
        _navButtons[AppView.Piano] = pianoNav;
        _navButtons[AppView.Settings] = settingsNav;

        sidebar.Controls.Add(navStack);
        sidebar.Controls.Add(_mousePositionLabel);
        sidebar.Controls.Add(brand);

        var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = UiTheme.Background };
        _hotkeyPanel = new HotkeyPanel();
        _contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        _logBox = UiTheme.CreateLogBox();
        var logCard = new Panel { Dock = DockStyle.Bottom, Height = 140, BackColor = UiTheme.Surface, Padding = new Padding(12) };
        logCard.Controls.Add(_logBox);

        _vortexView = new VortexView();
        _autoClickerView = new AutoClickerView();
        _recorderView = new RecorderView();
        _pianoView = new PianoPlayerView();
        _settingsView = new SettingsView();
        _views[AppView.Vortex] = _vortexView;
        _views[AppView.AutoClicker] = _autoClickerView;
        _views[AppView.Recorder] = _recorderView;
        _views[AppView.Piano] = _pianoView;
        _views[AppView.Settings] = _settingsView;

        main.Controls.Add(_contentPanel);
        main.Controls.Add(logCard);
        main.Controls.Add(_hotkeyPanel);
        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(main, 1, 0);
        Controls.Add(root);

        TrackViewButtons();
    }

    private void TrackViewButtons()
    {
        _hotkeyButtons.Track(_vortexView.StartButton, "Start", () => _settings.Hotkeys.Start);
        _hotkeyButtons.Track(_vortexView.PauseButton, "Pause / Resume", () => HotkeyChord.FromKey((Keys)_settings.Vortex.ToggleHotkeyVirtualKey));
        _hotkeyButtons.Track(_vortexView.StopButton, "Stop", () => _settings.Hotkeys.Stop);
        _hotkeyButtons.Track(_autoClickerView.StartButton, "Start Autoclicker", () => _settings.Hotkeys.Start);
        _hotkeyButtons.Track(_autoClickerView.StopButton, "Stop", () => _settings.Hotkeys.Stop);
        _hotkeyButtons.Track(_recorderView.RecordToggleButton, "Start Recording", () => _settings.Hotkeys.Start);
        _hotkeyButtons.Track(_recorderView.PlayButton, "Play Macro", () => _settings.Hotkeys.Play);
        _hotkeyButtons.Track(_recorderView.StopButton, "Stop Playback", () => _settings.Hotkeys.Stop);

        _hotkeyButtons.Track(_pianoView.PlayButton, "Play Song", () => _settings.Hotkeys.Start);
        _hotkeyButtons.Track(_pianoView.StopButton, "Stop Song", () => _settings.Hotkeys.Stop);

        foreach ((AppView view, Button button) in _navButtons)
        {
            if (view == AppView.Settings)
            {
                continue;
            }

            Func<HotkeyChord> provider = view switch
            {
                AppView.Vortex => () => HotkeyRestrictions.NavVortex,
                AppView.AutoClicker => () => HotkeyRestrictions.NavAutoClicker,
                AppView.Piano => () => HotkeyRestrictions.NavPiano,
                _ => () => HotkeyRestrictions.NavRecorder
            };
            _hotkeyButtons.Track(button, button.Text.Split('\n')[0], provider, isNav: true);
        }
    }

    private void WireEvents()
    {
        _hotkeyPanel.SaveRequested += (_, _) => SaveSettings();
        _vortexView.StartRequested += (_, _) => _ = RunSafe(StartVortexAsync);
        _vortexView.StopRequested += (_, _) => StopVortex();
        _vortexView.PauseRequested += (_, _) => _engine.TogglePause();
        _vortexView.SaveRequested += (_, _) => SaveSettings();
        _vortexView.ResetRequested += (_, _) => ResetVortexDefaults();
        _vortexView.ProfileChanged += (_, name) => SwitchVortexProfile(name);
        _vortexView.LogMessage += (_, msg) => AppendLog(msg);
        _vortexView.CoordinatePickRequested += (_, _) => _ = RunSafe(PickCoordinateAsync);

        _autoClickerView.StartRequested += (_, _) => _ = RunSafe(StartAutoClickerAsync);
        _autoClickerView.StopRequested += (_, _) => StopAutoClicker();
        _autoClickerView.SaveRequested += (_, _) => SaveSettings();

        _recorderView.ToggleRecordingRequested += (_, _) => ToggleRecording();
        _recorderView.PlayRequested += (_, _) => _ = RunSafe(PlayRecordedMacroAsync);
        _recorderView.StopRequested += (_, _) => StopPlayback();
        _recorderView.ImportRequested += (_, _) => ImportMacro();
        _recorderView.ExportRequested += (_, _) => ExportMacro();
        _recorderView.ClearRequested += (_, _) => _recorder.Clear();
        _recorderView.SaveToLibraryRequested += (_, _) => SaveMacroToLibrary();
        _recorderView.LoadFromLibraryRequested += (_, _) => LoadMacroFromLibrary();
        _recorderView.DeleteFromLibraryRequested += (_, _) => DeleteMacroFromLibrary();
        _recorderView.RemoveEventRequested += (_, index) => _recorder.RemoveEventAt(index);

        _pianoView.PlayRequested += (_, _) => _ = RunSafe(StartPianoAsync);
        _pianoView.StopRequested += (_, _) => StopPiano();
        _pianoView.SaveRequested += (_, _) => SaveSettings();
        _pianoView.StatusChanged += (_, msg) => AppendLog(msg);

        _settingsView.SaveRequested += (_, _) => SaveSettings();
        _settingsView.ExportAllRequested += (_, _) => ExportAllSettings();
        _settingsView.ImportAllRequested += (_, _) => ImportAllSettings();
    }

    private Button CreateNavButton(string text, AppView view, HotkeyChord? hotkey)
    {
        string label = hotkey is { IsValid: true } chord ? $"{text}\n{HotkeyFormatting.Format(chord)}" : text;
        Button button = UiTheme.CreateNavButton(label, (_, _) => ShowView(view));
        button.Dock = DockStyle.Top;
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

    private void LoadAllSettings()
    {
        _vortexView.LoadSettings(_settings.Vortex);
        _vortexView.LoadProfiles(_settings.VortexProfiles.Select(p => p.Name), _settings.ActiveVortexProfile);
        _autoClickerView.LoadSettings(_settings.AutoClicker);
        _recorderView.LoadSettings(_settings.Recorder);
        _pianoView.LoadSettings(_settings.Piano);
        _settingsView.LoadSettings(_settings.Behavior, _settings.Recorder);
        _settingsView.SetSettingsPath(_settingsPath);
        _hotkeyPanel.LoadHotkeys(_settings.Hotkeys);
        _recorderView.RefreshLibrary(_macroLibrary.ListMacroNames());
        _recorderView.RefreshEvents(_recorder.Events);
    }

    private void ApplySettingsFromInputs()
    {
        _vortexView.ApplyTo(_settings.Vortex);
        _autoClickerView.ApplyTo(_settings.AutoClicker);
        _recorderView.ApplyTo(_settings.Recorder);
        _pianoView.ApplyTo(_settings.Piano);
        _settingsView.ApplyTo(_settings.Behavior, _settings.Recorder);
        _hotkeyPanel.ApplyTo(_settings.Hotkeys);
        _settings.SaveActiveProfile();
        _recorder.Configure(_settings.Recorder);
    }

    private void SaveSettings()
    {
        ApplySettingsFromInputs();
        _settings.Save(_settingsPath);
        RegisterHotkeys();
        ApplyBehaviorSettings();
        AppendLog("Settings saved.");
    }

    private void ApplyBehaviorSettings()
    {
        StartupService.SetEnabled(_settings.Behavior.StartWithWindows);
    }

    private void SwitchVortexProfile(string name)
    {
        ApplySettingsFromInputs();
        _settings.ActiveVortexProfile = name;
        _settings.ApplyActiveProfile();
        _vortexView.LoadSettings(_settings.Vortex);
        AppendLog($"Switched to profile \"{name}\".");
    }

    private void ResetVortexDefaults()
    {
        _settings.Vortex = MacroSettings.CreateDefault();
        _vortexView.LoadSettings(_settings.Vortex);
        SaveSettings();
        AppendLog("Vortex settings reset to defaults.");
    }

    private async Task StartVortexAsync()
    {
        if (_vortexCts != null || !_session.TryBegin(MacroSessionKind.Vortex))
        {
            AppendLog("Another macro is already running.");
            return;
        }

        ApplySettingsFromInputs();
        SaveSettings();
        _vortexCts = new CancellationTokenSource();
        _vortexView.SetRunningState(true);

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
            _session.End(MacroSessionKind.Vortex);
            _vortexView.SetRunningState(false);
        }
    }

    private async Task StartAutoClickerAsync()
    {
        if (_autoClickerCts != null || !_session.TryBegin(MacroSessionKind.AutoClicker))
        {
            AppendLog("Another macro is already running.");
            return;
        }

        ApplySettingsFromInputs();
        SaveSettings();
        _autoClickerCts = new CancellationTokenSource();
        _autoClickerView.SetRunningState(true);

        try
        {
            await _engine.RunAutoClickerAsync(_settings.AutoClicker, _autoClickerCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Autoclicker stopped.");
        }
        finally
        {
            _autoClickerCts?.Dispose();
            _autoClickerCts = null;
            _session.End(MacroSessionKind.AutoClicker);
            _autoClickerView.SetRunningState(false);
        }
    }

    private void ToggleRecording()
    {
        if (_recorder.IsRecording)
        {
            RecordedMacro macro = _recorder.StopRecording(_recorderView.MacroName);
            _recorderView.SetRecordingState(false);
            _hotkeyButtons.SetBaseLabel(_recorderView.RecordToggleButton, "Start Recording");
            _hotkeyButtons.Refresh(_recorderView.RecordToggleButton);
            AppendLog($"Saved recording \"{macro.Name}\" with {macro.Events.Count} events.");
            _recorder.LoadEvents(macro.Events);
            return;
        }

        if (_session.Active != MacroSessionKind.None)
        {
            AppendLog("Stop the running macro before recording.");
            return;
        }

        ApplySettingsFromInputs();
        int[] suppressed =
        [
            _settings.Hotkeys.Start.VirtualKey,
            _settings.Hotkeys.Stop.VirtualKey,
            _settings.Hotkeys.Play.VirtualKey
        ];
        _recorder.StartRecording(suppressed);
        _recorderView.SetRecordingState(true);
        _hotkeyButtons.SetBaseLabel(_recorderView.RecordToggleButton, "Stop Recording");
        _hotkeyButtons.Refresh(_recorderView.RecordToggleButton);
    }

    private async Task PlayRecordedMacroAsync()
    {
        if (_playbackCts != null || _recorder.Events.Count == 0)
        {
            if (_recorder.Events.Count == 0)
            {
                AppendLog("No recorded events to play.");
            }

            return;
        }

        if (!_session.TryBegin(MacroSessionKind.Playback))
        {
            AppendLog("Another macro is already running.");
            return;
        }

        _playbackCts = new CancellationTokenSource();
        ApplySettingsFromInputs();
        var macro = new RecordedMacro
        {
            Name = _recorderView.MacroName,
            Events = _recorder.Events.ToList(),
            RecordedScreenWidth = MacroEngine.GetPrimaryScreenSize().Width,
            RecordedScreenHeight = MacroEngine.GetPrimaryScreenSize().Height,
            UseRelativeCoordinates = true
        };

        try
        {
            await _engine.RunRecordedMacroAsync(macro, _settings.Recorder, _playbackCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Recorded macro playback stopped.");
        }
        finally
        {
            _playbackCts?.Dispose();
            _playbackCts = null;
            _session.End(MacroSessionKind.Playback);
        }
    }

    private void StopVortex() => _vortexCts?.Cancel();
    private void StopAutoClicker() => _autoClickerCts?.Cancel();
    private void StopPlayback() => _playbackCts?.Cancel();
    private void StopPiano() => _pianoCts?.Cancel();

    private async Task StartPianoAsync()
    {
        if (_pianoCts != null)
        {
            return;
        }

        if (_pianoView.LoadedSong == null)
        {
            AppendLog("Select a .txt song file first.");
            return;
        }

        if (!_session.TryBegin(MacroSessionKind.Piano))
        {
            AppendLog("Another macro is already running.");
            return;
        }

        ApplySettingsFromInputs();
        SaveSettings();
        _pianoCts = new CancellationTokenSource();
        _pianoView.SetRunningState(true);
        PianoPlayerSettings playbackSettings = _pianoView.BuildPlaybackSettings();

        try
        {
            await PianoPlayer.PlayAsync(
                _pianoView.LoadedSong,
                playbackSettings,
                message => _pianoView.ReportStatus(message),
                _pianoCts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Piano playback stopped.");
        }
        finally
        {
            PianoPlayer.ReleaseAllKeys(playbackSettings.Lowercase);
            _pianoCts?.Dispose();
            _pianoCts = null;
            _session.End(MacroSessionKind.Piano);
            _pianoView.SetRunningState(false);
        }
    }

    private async Task PickCoordinateAsync()
    {
        if (_isPickingCoordinate)
        {
            return;
        }

        _isPickingCoordinate = true;
        AppendLog("Click anywhere on screen to pick a coordinate (ESC cancels).");
        WindowState = FormWindowState.Minimized;

        try
        {
            (int X, int Y)? point = await _coordinatePicker.PickAsync();
            if (point.HasValue)
            {
                _vortexView.ApplyPickedCoordinate(point.Value.X, point.Value.Y);
            }
            else
            {
                _vortexView.CancelPick();
                AppendLog("Coordinate pick cancelled.");
            }
        }
        finally
        {
            _isPickingCoordinate = false;
            ShowFromTray();
        }
    }

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
        _recorderView.MacroName = macro.Name;
        _recorder.LoadEvents(macro.Events);
        AppendLog($"Imported macro \"{macro.Name}\".");
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
            FileName = $"{_recorderView.MacroName}.json",
            Title = "Export recorded macro"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        new RecordedMacro
        {
            Name = _recorderView.MacroName,
            Events = _recorder.Events.ToList()
        }.Save(dialog.FileName);
        AppendLog($"Exported macro to {dialog.FileName}.");
    }

    private void SaveMacroToLibrary()
    {
        if (_recorder.Events.Count == 0)
        {
            AppendLog("Nothing to save to library.");
            return;
        }

        var macro = new RecordedMacro
        {
            Name = _recorderView.MacroName,
            Events = _recorder.Events.ToList(),
            RecordedScreenWidth = MacroEngine.GetPrimaryScreenSize().Width,
            RecordedScreenHeight = MacroEngine.GetPrimaryScreenSize().Height,
            UseRelativeCoordinates = true
        };
        _macroLibrary.Save(macro);
        _recorderView.RefreshLibrary(_macroLibrary.ListMacroNames(), macro.Name);
        AppendLog($"Saved \"{macro.Name}\" to macro library.");
    }

    private void LoadMacroFromLibrary()
    {
        string? name = _recorderView.SelectedLibraryMacro;
        if (string.IsNullOrWhiteSpace(name) || !_macroLibrary.Exists(name))
        {
            AppendLog("Select a macro from the library first.");
            return;
        }

        RecordedMacro macro = _macroLibrary.Load(name);
        _recorderView.MacroName = macro.Name;
        _recorder.LoadEvents(macro.Events);
        AppendLog($"Loaded \"{macro.Name}\" from library.");
    }

    private void DeleteMacroFromLibrary()
    {
        string? name = _recorderView.SelectedLibraryMacro;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _macroLibrary.Delete(name);
        _recorderView.RefreshLibrary(_macroLibrary.ListMacroNames());
        AppendLog($"Deleted \"{name}\" from library.");
    }

    private void ExportAllSettings()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "gasvar-macro-settings.json",
            Title = "Export all settings"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        ApplySettingsFromInputs();
        AppSettings.ExportAll(dialog.FileName, _settings);
        AppendLog($"Exported all settings to {dialog.FileName}.");
    }

    private void ImportAllSettings()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Title = "Import all settings"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _settings = AppSettings.ImportAll(dialog.FileName, AppendLog);
        LoadAllSettings();
        RegisterHotkeys();
        ApplyBehaviorSettings();
        SaveSettings();
        AppendLog("Imported all settings.");
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyService == null)
        {
            return;
        }

        _hotkeyService.Dispose();
        _hotkeyService = new GlobalHotkeyService(Handle);

        Register("Nav Vortex", HotkeyRestrictions.NavVortex, () => ShowView(AppView.Vortex));
        Register("Nav AutoClicker", HotkeyRestrictions.NavAutoClicker, () => ShowView(AppView.AutoClicker));
        Register("Nav Recorder", HotkeyRestrictions.NavRecorder, () => ShowView(AppView.Recorder));
        Register("Nav Piano", HotkeyRestrictions.NavPiano, () => ShowView(AppView.Piano));
        Register("Start", _settings.Hotkeys.Start, DispatchStart);
        Register("Stop", _settings.Hotkeys.Stop, DispatchStop);
        Register("Play", _settings.Hotkeys.Play, DispatchPlay);
        Register("Pause", HotkeyChord.FromKey((Keys)_settings.Vortex.ToggleHotkeyVirtualKey), DispatchPause);
        Register("Save", _settings.Hotkeys.Save, () => SaveSettings());
        Register("Reset", _settings.Hotkeys.Reset, DispatchReset);
        Register("Import", _settings.Hotkeys.Import, DispatchImport);
        Register("Export", _settings.Hotkeys.Export, DispatchExport);

        _hotkeyButtons.RefreshAll();

        if (_hotkeyService.Conflicts.Count > 0)
        {
            AppendLog("Hotkey conflicts: " + string.Join(" | ", _hotkeyService.Conflicts));
        }
    }

    private void Register(string owner, HotkeyChord chord, Action action)
    {
        if (_hotkeyService == null || !chord.IsValid)
        {
            return;
        }

        if (!_hotkeyService.Register(owner, chord, action))
        {
            AppendLog($"Could not register hotkey {HotkeyFormatting.Format(chord)} for {owner}.");
        }
    }

    private void DispatchStart()
    {
        switch (_currentView)
        {
            case AppView.Vortex:
                _ = RunSafe(StartVortexAsync);
                break;
            case AppView.AutoClicker:
                _ = RunSafe(StartAutoClickerAsync);
                break;
            case AppView.Recorder when !_recorder.IsRecording:
                ToggleRecording();
                break;
            case AppView.Piano:
                _ = RunSafe(StartPianoAsync);
                break;
        }
    }

    private void DispatchStop()
    {
        if (_vortexCts != null) { StopVortex(); return; }
        if (_autoClickerCts != null) { StopAutoClicker(); return; }
        if (_playbackCts != null) { StopPlayback(); return; }
        if (_pianoCts != null) { StopPiano(); return; }
        if (_recorder.IsRecording) { ToggleRecording(); return; }

        switch (_currentView)
        {
            case AppView.Vortex: StopVortex(); break;
            case AppView.AutoClicker: StopAutoClicker(); break;
            case AppView.Recorder: StopPlayback(); break;
            case AppView.Piano: StopPiano(); break;
        }
    }

    private void DispatchPlay()
    {
        if (_currentView == AppView.Recorder)
        {
            _ = RunSafe(PlayRecordedMacroAsync);
        }
    }

    private void DispatchPause()
    {
        if (_vortexCts != null)
        {
            _engine.TogglePause();
        }
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

    private void OnSessionStarted(object? sender, MacroSessionKind kind)
    {
        if (!_settings.Behavior.ShowOverlay)
        {
            return;
        }

        _overlay ??= new MacroOverlayForm();
        _overlay.SetStatus($"{kind} running — ESC to stop");
        if (!_overlay.Visible)
        {
            _overlay.Show();
        }
    }

    private void OnSessionEnded(object? sender, MacroSessionKind kind)
    {
        _overlay?.Hide();
    }

    private void EnsureTray()
    {
        if (_tray != null)
        {
            return;
        }

        _tray = new TrayIconService(this);
        _tray.ShowRequested += (_, _) => ShowFromTray();
        _tray.ExitRequested += (_, _) =>
        {
            _reallyExit = true;
            Close();
        };
    }

    private void HideToTray()
    {
        EnsureTray();
        Hide();
        _tray?.ShowBalloon("Gasvar Macro is still running in the tray.");
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task RunSafe(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
    }

    private void AppendLog(string message)
    {
        if (IsDisposed || _logBox == null)
        {
            return;
        }

        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => AppendLog(message));
            }

            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        _overlay?.SetStatus(message);
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
}
