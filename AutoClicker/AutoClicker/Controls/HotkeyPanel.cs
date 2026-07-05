namespace AutoClicker.Controls;

internal sealed class HotkeyPanel : UserControl
{
    private TextBox? _activeCapture;
    private readonly TextBox _startBox;
    private readonly TextBox _stopBox;
    private readonly TextBox _playBox;

    public event EventHandler? SaveRequested;

    public HotkeyPanel()
    {
        Dock = DockStyle.Top;
        AutoSize = true;
        BackColor = UiTheme.Background;
        Padding = new Padding(0, 0, 0, 6);

        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 0)
        };

        root.Controls.Add(CreateField("Start", out _startBox));
        root.Controls.Add(CreateField("Stop", out _stopBox));
        root.Controls.Add(CreateField("Play", out _playBox));

        var saveRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(4, 0, 0, 0)
        };
        saveRow.Controls.Add(UiTheme.CreateFieldLabel("\u00a0"));
        Button saveButton = UiTheme.CreateActionButton("Save Hotkeys", UiTheme.Accent, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty));
        saveRow.Controls.Add(saveButton);
        root.Controls.Add(saveRow);

        Controls.Add(root);
    }

    public void LoadHotkeys(HotkeySettings hotkeys)
    {
        SetBox(_startBox, hotkeys.Start);
        SetBox(_stopBox, hotkeys.Stop);
        SetBox(_playBox, hotkeys.Play);
    }

    public void ApplyTo(HotkeySettings hotkeys)
    {
        hotkeys.Start = GetBox(_startBox);
        hotkeys.Stop = GetBox(_stopBox);
        hotkeys.Play = GetBox(_playBox);
    }

    private FlowLayoutPanel CreateField(string label, out TextBox box)
    {
        var field = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        field.Controls.Add(UiTheme.CreateFieldLabel(label));
        box = CreateBox();
        field.Controls.Add(box);
        return field;
    }

    private TextBox CreateBox()
    {
        TextBox box = UiTheme.CreateHotkeyBox();
        box.Width = 88;
        box.Click += (_, _) => BeginCapture(box);
        box.KeyDown += BoxOnKeyDown;
        return box;
    }

    private void BeginCapture(TextBox box)
    {
        _activeCapture = box;
        box.Text = "Press key...";
        box.Focus();
    }

    private void BoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || _activeCapture != box)
        {
            return;
        }

        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Escape)
        {
            box.Text = HotkeyFormatting.Format(box.Tag as HotkeyChord ?? new HotkeyChord());
            _activeCapture = null;
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

        uint modifiers = 0;
        if (e.Control) modifiers |= NativeMethods.ModControl;
        if (e.Alt) modifiers |= NativeMethods.ModAlt;
        if (e.Shift) modifiers |= NativeMethods.ModShift;

        var chord = new HotkeyChord { Modifiers = modifiers, VirtualKey = (int)e.KeyCode };
        SetBox(box, chord);
        _activeCapture = null;
    }

    private static void SetBox(TextBox box, HotkeyChord chord)
    {
        box.Tag = chord;
        box.Text = HotkeyFormatting.Format(chord);
    }

    private static HotkeyChord GetBox(TextBox box) => box.Tag as HotkeyChord ?? new HotkeyChord();
}
