namespace AutoClicker.Controls;

internal sealed class AutoClickerView : UserControl
{
    private readonly NumericUpDown _intervalInput;
    private readonly NumericUpDown _clicksInput;
    private readonly NumericUpDown _startDelayInput;
    private readonly NumericUpDown _jitterInput;
    private readonly ComboBox _clickTypeInput;
    private readonly CheckBox _infiniteCheck;
    private readonly Button _startButton;
    private readonly Button _stopButton;

    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? SaveRequested;

    public Button StartButton => _startButton;
    public Button StopButton => _stopButton;

    public AutoClickerView()
    {
        Dock = DockStyle.Fill;
        var card = UiTheme.CreateCard();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(UiTheme.CreateTitle("AutoClicker"), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
        layout.Controls.Add(UiTheme.CreateSubtitle("Repeats clicks at the current cursor position. Set click count to 0 or enable infinite for continuous clicking."), 0, 1);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 1)!, 2);

        _intervalInput = UiTheme.CreateNumericInput(1, 600000, 500);
        _clicksInput = UiTheme.CreateNumericInput(0, 100000, 10);
        _startDelayInput = UiTheme.CreateNumericInput(0, 600000, 3000);
        _jitterInput = UiTheme.CreateNumericInput(0, 10000, 0);
        _clickTypeInput = UiTheme.CreateComboBox();
        _clickTypeInput.Items.AddRange(["Left", "Right", "Middle"]);
        _clickTypeInput.SelectedIndex = 0;
        _infiniteCheck = new CheckBox
        {
            Text = "Infinite mode",
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 8, 0, 8)
        };
        _infiniteCheck.CheckedChanged += (_, _) => _clicksInput.Enabled = !_infiniteCheck.Checked;

        var config = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        config.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        config.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(config, 0, "Interval (ms)", _intervalInput);
        AddRow(config, 1, "Number of clicks (0 = infinite)", _clicksInput);
        AddRow(config, 2, "Start delay (ms)", _startDelayInput);
        AddRow(config, 3, "Jitter (+/- ms)", _jitterInput);
        AddRow(config, 4, "Click type", _clickTypeInput);
        AddRow(config, 5, "", _infiniteCheck);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
        _startButton = UiTheme.CreateActionButton("Start Autoclicker", UiTheme.Success, (_, _) => StartRequested?.Invoke(this, EventArgs.Empty));
        _stopButton = UiTheme.CreateActionButton("Stop", UiTheme.Danger, (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        Button saveBtn = UiTheme.CreateActionButton("Save", UiTheme.Accent, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty));
        _stopButton.Enabled = false;
        actions.Controls.AddRange([_startButton, _stopButton, saveBtn]);

        var stack = new Panel { Dock = DockStyle.Fill };
        stack.Controls.Add(actions);
        stack.Controls.Add(config);
        layout.Controls.Add(stack, 0, 2);
        layout.SetColumnSpan(stack, 2);
        card.Controls.Add(layout);
        Controls.Add(card);
    }

    public void LoadSettings(AutoClickerSettings settings)
    {
        _intervalInput.Value = Clamp(_intervalInput, settings.IntervalMs);
        _clicksInput.Value = Clamp(_clicksInput, settings.ClickCount);
        _startDelayInput.Value = Clamp(_startDelayInput, settings.StartDelayMs);
        _jitterInput.Value = Clamp(_jitterInput, settings.JitterMs);
        _infiniteCheck.Checked = settings.InfiniteMode || settings.ClickCount <= 0;
        _clickTypeInput.SelectedItem = settings.ClickType;
        if (_clickTypeInput.SelectedIndex < 0)
        {
            _clickTypeInput.SelectedIndex = 0;
        }
    }

    public void ApplyTo(AutoClickerSettings settings)
    {
        settings.IntervalMs = (int)_intervalInput.Value;
        settings.InfiniteMode = _infiniteCheck.Checked;
        settings.ClickCount = settings.InfiniteMode ? 0 : (int)_clicksInput.Value;
        settings.StartDelayMs = (int)_startDelayInput.Value;
        settings.JitterMs = (int)_jitterInput.Value;
        settings.ClickType = _clickTypeInput.SelectedItem?.ToString() ?? "Left";
    }

    public void SetRunningState(bool running)
    {
        _startButton.Enabled = !running;
        _stopButton.Enabled = running;
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateFieldLabel(label), 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static decimal Clamp(NumericUpDown input, int value) =>
        Math.Min(input.Maximum, Math.Max(input.Minimum, value));
}
