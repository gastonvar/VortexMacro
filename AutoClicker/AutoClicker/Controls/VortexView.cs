using System.Reflection;

namespace AutoClicker.Controls;

internal sealed class VortexView : UserControl
{
    private readonly Dictionary<string, NumericUpDown> _inputs = new();
    private readonly ComboBox _profileCombo;
    private readonly TextBox _pauseHotkeyBox;
    private readonly Button _startButton;
    private readonly Button _pauseButton;
    private readonly Button _stopButton;
    private NumericUpDown? _pickTarget;
    private string? _pickPropertyName;
    private bool _suppressProfileEvents;

    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? PauseRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? ResetRequested;
    public event EventHandler<string>? ProfileChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler? CoordinatePickRequested;

    public Button StartButton => _startButton;
    public Button PauseButton => _pauseButton;
    public Button StopButton => _stopButton;

    public VortexView()
    {
        Dock = DockStyle.Fill;
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        header.Controls.Add(UiTheme.CreateTitle("Vortex Nexus"));
        header.Controls.Add(UiTheme.CreateSubtitle("Configure and run the Vortex Nexus macro."));

        _profileCombo = UiTheme.CreateComboBox();
        _profileCombo.Width = 180;
        _profileCombo.SelectedIndexChanged += OnProfileSelected;

        var profileRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        profileRow.Controls.Add(UiTheme.CreateFieldLabel("Profile"));
        profileRow.Controls.Add(_profileCombo);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        _startButton = UiTheme.CreateActionButton("Start", UiTheme.Success, (_, _) => StartRequested?.Invoke(this, EventArgs.Empty));
        _pauseButton = UiTheme.CreateActionButton("Pause / Resume", UiTheme.Accent, (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty));
        _stopButton = UiTheme.CreateActionButton("Stop", UiTheme.Danger, (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        _pauseButton.Enabled = false;
        _stopButton.Enabled = false;
        actions.Controls.AddRange([_startButton, _pauseButton, _stopButton]);

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        topBar.Controls.Add(header);
        topBar.Controls.Add(profileRow);
        topBar.Controls.Add(actions);

        _pauseHotkeyBox = UiTheme.CreateHotkeyBox();
        _pauseHotkeyBox.Width = 110;
        _pauseHotkeyBox.Click += (_, _) => BeginPauseCapture();
        _pauseHotkeyBox.KeyDown += PauseBoxOnKeyDown;

        var configScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Surface };
        var configColumns = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = UiTheme.Surface
        };
        configColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        configColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        configColumns.Controls.Add(CreateLeftConfigTable(), 0, 0);
        configColumns.Controls.Add(CreateRightConfigTable(), 1, 0);
        configScroll.Controls.Add(configColumns);

        var saveRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        Button saveBtn = UiTheme.CreateActionButton("Save", UiTheme.Accent, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty));
        Button resetBtn = UiTheme.CreateActionButton("Reset Defaults", UiTheme.SurfaceAlt, (_, _) => ResetRequested?.Invoke(this, EventArgs.Empty));
        saveRow.Controls.Add(saveBtn);
        saveRow.Controls.Add(resetBtn);

        layout.Controls.Add(topBar, 0, 0);
        layout.Controls.Add(configScroll, 0, 1);
        layout.Controls.Add(saveRow, 0, 2);
        card.Controls.Add(layout);
        Controls.Add(card);
    }

    public void LoadProfiles(IEnumerable<string> names, string active)
    {
        _suppressProfileEvents = true;
        try
        {
            _profileCombo.Items.Clear();
            foreach (string name in names)
            {
                _profileCombo.Items.Add(name);
            }

            int idx = _profileCombo.Items.IndexOf(active);
            _profileCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally
        {
            _suppressProfileEvents = false;
        }
    }

    public void LoadSettings(MacroSettings settings)
    {
        foreach ((string propertyName, NumericUpDown input) in _inputs)
        {
            if (propertyName == nameof(MacroSettings.ToggleHotkeyVirtualKey))
            {
                continue;
            }

            PropertyInfo? property = typeof(MacroSettings).GetProperty(propertyName);
            if (property?.GetValue(settings) is int value)
            {
                input.Value = Math.Min(input.Maximum, Math.Max(input.Minimum, value));
            }
        }

        SetPauseHotkey(settings.ToggleHotkeyVirtualKey);
    }

    public void ApplyTo(MacroSettings settings)
    {
        foreach ((string propertyName, NumericUpDown input) in _inputs)
        {
            if (propertyName == nameof(MacroSettings.ToggleHotkeyVirtualKey))
            {
                continue;
            }

            PropertyInfo? property = typeof(MacroSettings).GetProperty(propertyName);
            property?.SetValue(settings, (int)input.Value);
        }

        if (_pauseHotkeyBox.Tag is HotkeyChord chord && chord.IsValid)
        {
            settings.ToggleHotkeyVirtualKey = chord.VirtualKey;
        }
    }

    public void SetRunningState(bool running)
    {
        _startButton.Enabled = !running;
        _pauseButton.Enabled = running;
        _stopButton.Enabled = running;
    }

    public void ApplyPickedCoordinate(int x, int y)
    {
        if (_pickTarget == null)
        {
            return;
        }

        bool isY = _pickPropertyName?.Contains('Y', StringComparison.Ordinal) == true;
        int value = isY ? y : x;
        _pickTarget.Value = Math.Min(_pickTarget.Maximum, Math.Max(_pickTarget.Minimum, value));
        LogMessage?.Invoke(this, $"Coordinate picked: X={x}, Y={y} → set {(isY ? "Y" : "X")}={value}");
        _pickTarget = null;
        _pickPropertyName = null;
    }

    public void CancelPick()
    {
        _pickTarget = null;
        _pickPropertyName = null;
    }

    internal int GetInputValue(string propertyName)
    {
        return _inputs.TryGetValue(propertyName, out NumericUpDown? input)
            ? (int)input.Value
            : throw new KeyNotFoundException(propertyName);
    }

    private void OnProfileSelected(object? sender, EventArgs e)
    {
        if (_suppressProfileEvents || _profileCombo.SelectedItem is not string name)
        {
            return;
        }

        ProfileChanged?.Invoke(this, name);
    }

    private TableLayoutPanel CreateConfigTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            BackColor = UiTheme.Surface
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return table;
    }

    private TableLayoutPanel CreateLeftConfigTable()
    {
        var table = CreateConfigTable();
        int row = 0;

        AddSection(table, ref row, "Timing");
        AddSetting(table, ref row, nameof(MacroSettings.CycleDelayMs), "Cycle delay (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.MoveDelayMs), "Move delay (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.VortexDelayMs), "After Vortex click (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.NexusAfterFirstClickDelayMs), "After Nexus click 1 (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.NexusAfterSecondClickDelayMs), "After Nexus click 2 (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.NexusDelayMs), "After Nexus click 3 (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.ClearGoogleDelayMs), "Clear Google delay (ms)", 0, 600000);
        AddSetting(table, ref row, nameof(MacroSettings.ClearGoogleEveryCycles), "Clear Google every cycles", 0, 100000);
        AddSetting(table, ref row, nameof(MacroSettings.ScrollDownSteps), "Scroll down steps", 0, 100000);
        AddSetting(table, ref row, nameof(MacroSettings.ScrollUpSteps), "Scroll up steps", 0, 100000);

        AddSection(table, ref row, "Pause Hotkey");
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel("Pause / resume key"), 0, row);
        table.Controls.Add(_pauseHotkeyBox, 1, row);
        row++;

        AddSection(table, ref row, "Vortex Area");
        AddCoordSetting(table, ref row, nameof(MacroSettings.VortexXStart), "Vortex X start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.VortexXEnd), "Vortex X end");
        AddCoordSetting(table, ref row, nameof(MacroSettings.VortexYStart), "Vortex Y start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.VortexYEnd), "Vortex Y end");

        return table;
    }

    private TableLayoutPanel CreateRightConfigTable()
    {
        var table = CreateConfigTable();
        int row = 0;

        AddSection(table, ref row, "Nexus Areas");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusXStart), "Nexus X start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusXEnd), "Nexus X end");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusYStart), "Nexus Y start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusYEnd), "Nexus Y end");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusLowerYStart), "Nexus lower Y start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusLowerYEnd), "Nexus lower Y end");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusLowerPlusYStart), "Nexus lower+ Y start");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusLowerPlusYEnd), "Nexus lower+ Y end");

        AddSection(table, ref row, "Single Clicks");
        AddCoordSetting(table, ref row, nameof(MacroSettings.CloseNexusReminderX), "Close Nexus reminder X");
        AddCoordSetting(table, ref row, nameof(MacroSettings.CloseNexusReminderY), "Close Nexus reminder Y");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusPageX), "Nexus page X");
        AddCoordSetting(table, ref row, nameof(MacroSettings.NexusPageY), "Nexus page Y");
        AddCoordSetting(table, ref row, nameof(MacroSettings.CloseGoogleX), "Close Google X");
        AddCoordSetting(table, ref row, nameof(MacroSettings.CloseGoogleY), "Close Google Y");

        return table;
    }

    private void AddSetting(TableLayoutPanel table, ref int row, string propertyName, string label, int min, int max)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(label), 0, row);
        NumericUpDown input = UiTheme.CreateNumericInput(min, max, 0);
        _inputs[propertyName] = input;
        table.Controls.Add(input, 1, row);
        row++;
    }

    private void AddCoordSetting(TableLayoutPanel table, ref int row, string propertyName, string label)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(label), 0, row);
        NumericUpDown input = UiTheme.CreateNumericInput(-10000, 10000, 0);
        _inputs[propertyName] = input;
        table.Controls.Add(input, 1, row);
        Button pick = UiTheme.CreateActionButton("Pick", UiTheme.SurfaceAlt, (_, _) =>
        {
            _pickTarget = input;
            _pickPropertyName = propertyName;
            CoordinatePickRequested?.Invoke(this, EventArgs.Empty);
        });
        pick.Margin = new Padding(6, 8, 0, 8);
        table.Controls.Add(pick, 2, row);
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
        table.SetColumnSpan(label, 3);
        row++;
    }

    private void SetPauseHotkey(int virtualKey)
    {
        var chord = HotkeyChord.FromKey((Keys)virtualKey);
        _pauseHotkeyBox.Tag = chord;
        _pauseHotkeyBox.Text = HotkeyFormatting.Format(chord);
    }

    private void BeginPauseCapture()
    {
        _pauseHotkeyBox.Text = "Press key...";
        _pauseHotkeyBox.Focus();
    }

    private void PauseBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Escape)
        {
            if (_pauseHotkeyBox.Tag is HotkeyChord existing)
            {
                _pauseHotkeyBox.Text = HotkeyFormatting.Format(existing);
            }

            return;
        }

        if (e.KeyCode is Keys.ShiftKey or Keys.ControlKey or Keys.Menu)
        {
            return;
        }

        if (HotkeyRestrictions.IsReservedNavigationKey(e.KeyCode))
        {
            return;
        }

        var chord = HotkeyChord.FromKey(e.KeyCode);
        _pauseHotkeyBox.Tag = chord;
        _pauseHotkeyBox.Text = HotkeyFormatting.Format(chord);
    }
}
