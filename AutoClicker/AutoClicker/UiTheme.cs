namespace AutoClicker;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(24, 26, 34);
    public static readonly Color Sidebar = Color.FromArgb(32, 35, 48);
    public static readonly Color Surface = Color.FromArgb(42, 46, 61);
    public static readonly Color SurfaceAlt = Color.FromArgb(52, 57, 74);
    public static readonly Color Accent = Color.FromArgb(108, 140, 255);
    public static readonly Color AccentHover = Color.FromArgb(132, 160, 255);
    public static readonly Color AccentActive = Color.FromArgb(84, 118, 235);
    public static readonly Color Success = Color.FromArgb(72, 199, 142);
    public static readonly Color Danger = Color.FromArgb(235, 96, 110);
    public static readonly Color Text = Color.FromArgb(236, 240, 255);
    public static readonly Color TextMuted = Color.FromArgb(164, 172, 196);
    public static readonly Color Border = Color.FromArgb(68, 74, 96);

    public static void ApplyForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F);
    }

    public static Button CreateNavButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            ForeColor = Text,
            BackColor = Sidebar,
            Margin = new Padding(8, 0, 8, 3),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SurfaceAlt;
        button.FlatAppearance.MouseDownBackColor = AccentActive;
        button.Click += onClick;
        return button;
    }

    public static void SetNavActive(Button button, bool active)
    {
        button.BackColor = active ? Accent : Sidebar;
        button.ForeColor = active ? Color.White : Text;
    }

    public static Button CreateActionButton(string text, Color backColor, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 10, 10),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
        button.Click += onClick;
        return button;
    }

    public static Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(12),
            Margin = new Padding(0)
        };
    }

    public static Label CreateTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
        ForeColor = Text,
        Margin = new Padding(0, 0, 0, 6)
    };

    public static Label CreateSubtitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = TextMuted,
        Margin = new Padding(0, 0, 0, 16)
    };

    public static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = TextMuted,
        Margin = new Padding(0, 8, 8, 8),
        Anchor = AnchorStyles.Left
    };

    public static NumericUpDown CreateNumericInput(decimal minimum, decimal maximum, decimal value)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = Math.Min(maximum, Math.Max(minimum, value)),
            Width = 110,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            ThousandsSeparator = true
        };
    }

    public static TextBox CreateLogBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9.5F)
        };
    }

    public static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 110,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat
        };
    }

    public static TextBox CreateHotkeyBox()
    {
        return new TextBox
        {
            ReadOnly = true,
            Width = 72,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center
        };
    }

    public static ListBox CreateEventList()
    {
        return new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceAlt,
            ForeColor = Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9F),
            IntegralHeight = false
        };
    }
}
