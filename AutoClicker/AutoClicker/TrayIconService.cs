namespace AutoClicker;

internal sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Form _owner;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(Form owner)
    {
        _owner = owner;
        _notifyIcon = new NotifyIcon
        {
            Text = "Gasvar Macro",
            Icon = SystemIcons.Application,
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowBalloon(string message) =>
        _notifyIcon.ShowBalloonTip(2000, "Gasvar Macro", message, ToolTipIcon.Info);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
