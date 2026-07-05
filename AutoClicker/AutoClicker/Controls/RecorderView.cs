namespace AutoClicker.Controls;

internal sealed class RecorderView : UserControl
{
    private const int MinContentWidth = 760;
    private const int LeftColumnWidth = 420;

    private readonly TextBox _macroNameInput;
    private readonly ListBox _eventsList;
    private readonly ComboBox _libraryCombo;
    private readonly NumericUpDown _loopsInput;
    private readonly Button _recordToggleButton;
    private readonly Button _playButton;
    private readonly Button _stopButton;
    private readonly Panel _scroll;
    private readonly TableLayoutPanel _layout;
    private readonly FlowLayoutPanel _left;

    public event EventHandler? ToggleRecordingRequested;
    public event EventHandler? PlayRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? SaveToLibraryRequested;
    public event EventHandler? LoadFromLibraryRequested;
    public event EventHandler? DeleteFromLibraryRequested;
    public event EventHandler<int>? RemoveEventRequested;

    public Button RecordToggleButton => _recordToggleButton;
    public Button PlayButton => _playButton;
    public Button StopButton => _stopButton;

    public RecorderView()
    {
        Dock = DockStyle.Fill;
        var card = UiTheme.CreateCard();
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.Surface };
        _layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = UiTheme.Surface,
            MinimumSize = new Size(MinContentWidth, 0)
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LeftColumnWidth));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _layout.Controls.Add(UiTheme.CreateTitle("Macro Recorder"), 0, 0);
        _layout.SetColumnSpan(_layout.GetControlFromPosition(0, 0)!, 2);
        _layout.Controls.Add(UiTheme.CreateSubtitle("Records delays between mouse moves, clicks, scrolls, and keystrokes. Supports relative coordinates and loop playback."), 0, 1);
        _layout.SetColumnSpan(_layout.GetControlFromPosition(0, 1)!, 2);

        _left = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            MinimumSize = new Size(LeftColumnWidth, 0)
        };

        _macroNameInput = new TextBox
        {
            Text = "My Macro",
            Width = 220,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        _loopsInput = UiTheme.CreateNumericInput(1, 10000, 1);
        _loopsInput.Width = 80;

        var nameRow = CreateFieldRow("Macro name", _macroNameInput);
        var loopRow = CreateFieldRow("Playback loops", _loopsInput);

        _libraryCombo = UiTheme.CreateComboBox();
        _libraryCombo.Width = 180;
        var libraryRow = CreateFieldRow("Library", _libraryCombo);

        var libraryActions = CreateButtonRow();
        libraryActions.Controls.Add(UiTheme.CreateActionButton("Load", UiTheme.SurfaceAlt, (_, _) => LoadFromLibraryRequested?.Invoke(this, EventArgs.Empty)));
        libraryActions.Controls.Add(UiTheme.CreateActionButton("Save", UiTheme.SurfaceAlt, (_, _) => SaveToLibraryRequested?.Invoke(this, EventArgs.Empty)));
        libraryActions.Controls.Add(UiTheme.CreateActionButton("Delete", UiTheme.SurfaceAlt, (_, _) => DeleteFromLibraryRequested?.Invoke(this, EventArgs.Empty)));

        _eventsList = UiTheme.CreateEventList();

        var actions = CreateButtonRow();
        actions.Padding = new Padding(0, 10, 0, 0);
        _recordToggleButton = UiTheme.CreateActionButton("Start Recording", UiTheme.Danger, (_, _) => ToggleRecordingRequested?.Invoke(this, EventArgs.Empty));
        _playButton = UiTheme.CreateActionButton("Play Macro", UiTheme.Success, (_, _) => PlayRequested?.Invoke(this, EventArgs.Empty));
        _stopButton = UiTheme.CreateActionButton("Stop Playback", UiTheme.Accent, (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        actions.Controls.AddRange([
            _recordToggleButton, _playButton, _stopButton,
            UiTheme.CreateActionButton("Import", UiTheme.SurfaceAlt, (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty)),
            UiTheme.CreateActionButton("Export", UiTheme.SurfaceAlt, (_, _) => ExportRequested?.Invoke(this, EventArgs.Empty)),
            UiTheme.CreateActionButton("Clear", UiTheme.SurfaceAlt, (_, _) => ClearRequested?.Invoke(this, EventArgs.Empty)),
            UiTheme.CreateActionButton("Remove Event", UiTheme.SurfaceAlt, (_, _) =>
            {
                if (_eventsList.SelectedIndex >= 0)
                {
                    RemoveEventRequested?.Invoke(this, _eventsList.SelectedIndex);
                }
            })
        ]);

        _left.Controls.Add(nameRow);
        _left.Controls.Add(loopRow);
        _left.Controls.Add(libraryRow);
        _left.Controls.Add(libraryActions);
        _left.Controls.Add(actions);

        var right = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SurfaceAlt,
            Padding = new Padding(10),
            MinimumSize = new Size(320, 280)
        };
        right.Controls.Add(_eventsList);

        _layout.Controls.Add(_left, 0, 2);
        _layout.Controls.Add(right, 1, 2);

        _scroll.Controls.Add(_layout);
        _scroll.Resize += (_, _) => SyncScrollLayout();
        card.Controls.Add(_scroll);
        Controls.Add(card);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        SyncScrollLayout();
    }

    private FlowLayoutPanel CreateFieldRow(string label, Control input)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true,
            Width = LeftColumnWidth - 4,
            Margin = new Padding(0, 0, 0, 6)
        };
        row.Controls.Add(UiTheme.CreateFieldLabel(label));
        row.Controls.Add(input);
        return row;
    }

    private FlowLayoutPanel CreateButtonRow()
    {
        return new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true,
            Width = LeftColumnWidth - 4,
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private void SyncScrollLayout()
    {
        _layout.Width = Math.Max(_scroll.ClientSize.Width, MinContentWidth);
        int rowWidth = LeftColumnWidth - 4;
        foreach (Control control in _left.Controls)
        {
            if (control is FlowLayoutPanel row)
            {
                row.Width = rowWidth;
            }
        }
    }

    public string MacroName
    {
        get => _macroNameInput.Text;
        set => _macroNameInput.Text = value;
    }

    public void LoadSettings(RecorderSettings settings)
    {
        _loopsInput.Value = Math.Min(_loopsInput.Maximum, Math.Max(_loopsInput.Minimum, settings.PlaybackLoops));
    }

    public void ApplyTo(RecorderSettings settings)
    {
        settings.PlaybackLoops = (int)_loopsInput.Value;
    }

    public void SetRecordingState(bool recording)
    {
        _recordToggleButton.Text = recording ? "Stop Recording" : "Start Recording";
        _recordToggleButton.BackColor = recording ? UiTheme.Success : UiTheme.Danger;
    }

    public void RefreshEvents(IReadOnlyList<RecordedMacroEvent> events)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => RefreshEvents(events));
            return;
        }

        _eventsList.Items.Clear();
        foreach (RecordedMacroEvent e in events)
        {
            _eventsList.Items.Add(e.Describe());
        }
    }

    public void RefreshLibrary(IReadOnlyList<string> names, string? selected = null)
    {
        _libraryCombo.Items.Clear();
        foreach (string name in names)
        {
            _libraryCombo.Items.Add(name);
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            int idx = _libraryCombo.Items.IndexOf(selected);
            if (idx >= 0)
            {
                _libraryCombo.SelectedIndex = idx;
            }
        }
    }

    public string? SelectedLibraryMacro =>
        _libraryCombo.SelectedItem as string;
}
