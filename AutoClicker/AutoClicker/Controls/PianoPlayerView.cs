namespace AutoClicker.Controls;

internal sealed class PianoPlayerView : UserControl
{
    private readonly ComboBox _songCombo;
    private readonly Label _pathLabel;
    private readonly NumericUpDown _noteMsInput;
    private readonly NumericUpDown _holdMsInput;
    private readonly NumericUpDown _pauseMsInput;
    private readonly NumericUpDown _lineGapMsInput;
    private readonly NumericUpDown _countdownInput;
    private readonly NumericUpDown _repeatsInput;
    private readonly CheckBox _lowercaseCheck;
    private readonly TextBox _previewBox;
    private readonly Button _playButton;
    private readonly Button _stopButton;

    private IReadOnlyList<IReadOnlyList<PianoToken>>? _song;
    private string? _loadedPath;

    public event EventHandler? PlayRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler<string>? StatusChanged;

    public Button PlayButton => _playButton;
    public Button StopButton => _stopButton;

    public PianoPlayerView()
    {
        Dock = DockStyle.Fill;
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(UiTheme.CreateTitle("Piano Player"), 0, 0);
        layout.Controls.Add(UiTheme.CreateSubtitle(
            "Reads .txt song notation and simulates keyboard input for in-game pianos. Focus the game before the countdown ends."), 0, 1);

        var filePanel = new Panel { Dock = DockStyle.Top, AutoSize = true };
        var fileGroup = CreateGroup("Song file");
        _pathLabel = new Label
        {
            Text = "No file selected",
            AutoSize = false,
            Height = 36,
            Dock = DockStyle.Top,
            ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _songCombo = UiTheme.CreateComboBox();
        _songCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _songCombo.SelectedIndexChanged += (_, _) => LoadSelectedSong();

        var fileRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        fileRow.Controls.Add(_songCombo);
        Button browseBtn = UiTheme.CreateActionButton("Browse…", UiTheme.SurfaceAlt, (_, _) => BrowseSong());
        fileRow.Controls.Add(browseBtn);
        fileGroup.Controls.Add(fileRow);
        fileGroup.Controls.Add(_pathLabel);
        filePanel.Controls.Add(fileGroup);
        layout.Controls.Add(filePanel, 0, 2);

        var settingsGroup = CreateGroup("Timing (milliseconds)");
        var settingsTable = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _noteMsInput = UiTheme.CreateNumericInput(20, 2000, 120);
        _holdMsInput = UiTheme.CreateNumericInput(20, 2000, 90);
        _pauseMsInput = UiTheme.CreateNumericInput(20, 2000, 80);
        _lineGapMsInput = UiTheme.CreateNumericInput(20, 5000, 400);
        _countdownInput = UiTheme.CreateNumericInput(0, 15, 3);
        _countdownInput.Increment = 0.5M;
        _countdownInput.DecimalPlaces = 1;
        _repeatsInput = UiTheme.CreateNumericInput(1, 99, 1);
        _lowercaseCheck = new CheckBox
        {
            Text = "Send lowercase keys (most games)",
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Checked = true,
            Margin = new Padding(0, 8, 0, 4)
        };

        AddSettingRow(settingsTable, 0, "Note duration", _noteMsInput);
        AddSettingRow(settingsTable, 1, "Extra hold per repeated letter", _holdMsInput);
        AddSettingRow(settingsTable, 2, "Pause (space / dash)", _pauseMsInput);
        AddSettingRow(settingsTable, 3, "Gap between lines", _lineGapMsInput);
        AddSettingRow(settingsTable, 4, "Countdown (seconds)", _countdownInput);
        AddSettingRow(settingsTable, 5, "Repeat song", _repeatsInput);
        settingsTable.Controls.Add(_lowercaseCheck, 0, 6);
        settingsTable.SetColumnSpan(_lowercaseCheck, 2);
        settingsGroup.Controls.Add(settingsTable);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
        _playButton = UiTheme.CreateActionButton("Play", UiTheme.Success, (_, _) => PlayRequested?.Invoke(this, EventArgs.Empty));
        _stopButton = UiTheme.CreateActionButton("Stop", UiTheme.Danger, (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        Button saveBtn = UiTheme.CreateActionButton("Save", UiTheme.Accent, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty));
        _stopButton.Enabled = false;
        actions.Controls.AddRange([_playButton, _stopButton, saveBtn]);

        var previewGroup = CreateGroup("Preview");
        _previewBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9F)
        };
        previewGroup.Controls.Add(_previewBox);
        previewGroup.Dock = DockStyle.Fill;

        var middle = new Panel { Dock = DockStyle.Fill };
        middle.Controls.Add(previewGroup);
        middle.Controls.Add(actions);
        middle.Controls.Add(settingsGroup);
        layout.Controls.Add(middle, 0, 3);

        var hint = new Label
        {
            Text = "Use Start / Stop hotkeys while on this tab. Tune timing if notes sound rushed or too slow.",
            AutoSize = true,
            ForeColor = UiTheme.TextMuted,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, 8, 0, 0)
        };
        layout.Controls.Add(hint, 0, 4);

        card.Controls.Add(layout);
        Controls.Add(card);

        RefreshSongList();
    }

    public IReadOnlyList<IReadOnlyList<PianoToken>>? LoadedSong => _song;

    public void LoadSettings(PianoPlayerSettings settings)
    {
        _noteMsInput.Value = Clamp(_noteMsInput, settings.NoteMs);
        _holdMsInput.Value = Clamp(_holdMsInput, settings.HoldMultiplierMs);
        _pauseMsInput.Value = Clamp(_pauseMsInput, settings.PauseMs);
        _lineGapMsInput.Value = Clamp(_lineGapMsInput, settings.LineGapMs);
        _countdownInput.Value = Clamp(_countdownInput, (decimal)settings.CountdownSeconds);
        _repeatsInput.Value = Clamp(_repeatsInput, settings.RepeatLines);
        _lowercaseCheck.Checked = settings.Lowercase;

        if (!string.IsNullOrWhiteSpace(settings.LastSongPath) && File.Exists(settings.LastSongPath))
        {
            TryLoadSong(settings.LastSongPath, silent: true);
            SelectComboPath(settings.LastSongPath);
        }
    }

    public void ApplyTo(PianoPlayerSettings settings)
    {
        settings.NoteMs = (int)_noteMsInput.Value;
        settings.HoldMultiplierMs = (int)_holdMsInput.Value;
        settings.PauseMs = (int)_pauseMsInput.Value;
        settings.LineGapMs = (int)_lineGapMsInput.Value;
        settings.CountdownSeconds = (double)_countdownInput.Value;
        settings.RepeatLines = (int)_repeatsInput.Value;
        settings.Lowercase = _lowercaseCheck.Checked;
        settings.LastSongPath = _loadedPath;
    }

    public PianoPlayerSettings BuildPlaybackSettings()
    {
        var settings = new PianoPlayerSettings();
        ApplyTo(settings);
        return settings;
    }

    public void SetRunningState(bool running)
    {
        _playButton.Enabled = !running;
        _stopButton.Enabled = running;
        _songCombo.Enabled = !running;
    }

    public void ReportStatus(string message) => StatusChanged?.Invoke(this, message);

    public void RefreshSongList()
    {
        string? previous = _loadedPath;
        _songCombo.Items.Clear();
        _songCombo.Items.Add("(Select a song)");

        if (Directory.Exists(SettingsPaths.SongsDirectory))
        {
            foreach (string file in Directory.GetFiles(SettingsPaths.SongsDirectory, "*.txt").OrderBy(Path.GetFileName))
            {
                _songCombo.Items.Add(file);
            }
        }

        _songCombo.SelectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(previous))
        {
            SelectComboPath(previous);
        }

        UpdateSongComboWidth();
    }

    private void BrowseSong()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select song file",
            InitialDirectory = Directory.Exists(SettingsPaths.SongsDirectory)
                ? SettingsPaths.SongsDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        TryLoadSong(dialog.FileName);
        SelectComboPath(dialog.FileName);
    }

    private void LoadSelectedSong()
    {
        if (_songCombo.SelectedItem is not string path || path.StartsWith('('))
        {
            return;
        }

        TryLoadSong(path);
    }

    private void TryLoadSong(string path, bool silent = false)
    {
        try
        {
            string text = File.ReadAllText(path);
            IReadOnlyDictionary<string, string> header = PianoSongParser.ParseHeaderSettings(text);
            if (header.TryGetValue("symbols", out string? symbols) &&
                symbols is "0" or "false" or "no" or "off")
            {
                text = PianoSongParser.SanitizeForNormalKeys(text);
            }

            _song = PianoSongParser.ParseSong(text);
            _loadedPath = path;
            _pathLabel.Text = path;
            _previewBox.Text = text;
            ApplyHeaderSettings(header);
            if (!silent)
            {
                ReportStatus($"Loaded {_song.Count} lines from {Path.GetFileName(path)}.");
            }
        }
        catch (Exception ex)
        {
            _song = null;
            _loadedPath = null;
            _pathLabel.Text = "Failed to load file";
            _previewBox.Text = string.Empty;
            if (!silent)
            {
                MessageBox.Show(ex.Message, "Song load error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void ApplyHeaderSettings(IReadOnlyDictionary<string, string> header)
    {
        ApplyHeaderInt(header, "note_ms", _noteMsInput);
        ApplyHeaderInt(header, "hold_ms", _holdMsInput);
        ApplyHeaderInt(header, "pause_ms", _pauseMsInput);
        ApplyHeaderInt(header, "line_gap_ms", _lineGapMsInput);
        ApplyHeaderInt(header, "repeats", _repeatsInput);

        if (header.TryGetValue("countdown", out string? countdown) &&
            decimal.TryParse(countdown, out decimal countdownValue))
        {
            _countdownInput.Value = Clamp(_countdownInput, countdownValue);
        }

        if (header.TryGetValue("lowercase", out string? lowercase))
        {
            _lowercaseCheck.Checked = lowercase is "1" or "true" or "yes" or "on";
        }
    }

    private static void ApplyHeaderInt(IReadOnlyDictionary<string, string> header, string key, NumericUpDown input)
    {
        if (header.TryGetValue(key, out string? value) && int.TryParse(value, out int parsed))
        {
            input.Value = Clamp(input, parsed);
        }
    }

    private void SelectComboPath(string path)
    {
        for (int i = 0; i < _songCombo.Items.Count; i++)
        {
            if (_songCombo.Items[i] is string item &&
                item.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                _songCombo.SelectedIndex = i;
                return;
            }
        }

        _songCombo.Items.Add(path);
        _songCombo.SelectedIndex = _songCombo.Items.Count - 1;
        UpdateSongComboWidth();
    }

    private void UpdateSongComboWidth()
    {
        int maxWidth = 0;
        using Graphics graphics = _songCombo.CreateGraphics();
        foreach (object item in _songCombo.Items)
        {
            if (item?.ToString() is not { Length: > 0 } text)
            {
                continue;
            }

            Size size = TextRenderer.MeasureText(graphics, text, _songCombo.Font, Size.Empty, TextFormatFlags.NoPadding);
            maxWidth = Math.Max(maxWidth, size.Width);
        }

        const int chromePadding = 28;
        int targetWidth = Math.Max(110, maxWidth + chromePadding);
        _songCombo.Width = targetWidth;
        _songCombo.DropDownWidth = targetWidth;
    }

    private static GroupBox CreateGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            ForeColor = UiTheme.Text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10, 16, 10, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private static void AddSettingRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(label), 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static decimal Clamp(NumericUpDown input, decimal value) =>
        Math.Min(input.Maximum, Math.Max(input.Minimum, value));
}
