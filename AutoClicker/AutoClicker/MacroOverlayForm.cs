namespace AutoClicker;

internal sealed class MacroOverlayForm : Form
{
    private readonly Label _statusLabel;

    public MacroOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(24, 26, 34);
        Opacity = 0.92;
        Width = 280;
        Height = 64;
        Padding = new Padding(12);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.Text,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Macro running"
        };

        Controls.Add(_statusLabel);
        Location = new Point(16, Screen.PrimaryScreen?.WorkingArea.Height - Height - 16 ?? 16);
    }

    public void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text));
            return;
        }

        _statusLabel.Text = text;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
            return cp;
        }
    }
}
