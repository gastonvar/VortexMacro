namespace AutoClicker;

internal sealed class MacroEngine
{
    private readonly Random _random = new();

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? MousePositionChanged;
    public event EventHandler<MacroSessionKind>? SessionStarted;
    public event EventHandler<MacroSessionKind>? SessionEnded;

    public bool IsPaused { get; private set; }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        OnStatusChanged(IsPaused ? "Vortex macro paused." : "Vortex macro resumed.");
    }

    public async Task RunVortexNexusAsync(MacroSettings settings, CancellationToken cancellationToken)
    {
        IsPaused = false;
        int cycle = 0;
        SessionStarted?.Invoke(this, MacroSessionKind.Vortex);
        OnStatusChanged("Vortex macro started. Press ESC or Stop to end. Use the pause hotkey to pause/resume.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsKeyPressed(NativeMethods.EscapeKey))
                {
                    OnStatusChanged("ESC pressed. Stopping Vortex macro.");
                    break;
                }

                if (IsKeyPressed(settings.ToggleHotkeyVirtualKey))
                {
                    TogglePause();
                    await DelayAsync(300, cancellationToken);
                }

                if (IsPaused)
                {
                    await DelayAsync(100, cancellationToken);
                    continue;
                }

                await ClickVortexAsync(settings, cancellationToken);
                await ClickNexusAsync(settings, cancellationToken);

                if (settings.ClearGoogleEveryCycles > 0 && cycle > 0 && cycle % settings.ClearGoogleEveryCycles == 0)
                {
                    await ClearGoogleAsync(settings, cancellationToken);
                }

                PublishCurrentMousePosition();
                cycle++;
                await DelayAsync(settings.CycleDelayMs, cancellationToken);
            }

            OnStatusChanged("Vortex macro stopped.");
        }
        finally
        {
            SessionEnded?.Invoke(this, MacroSessionKind.Vortex);
        }
    }

    public async Task RunAutoClickerAsync(AutoClickerSettings settings, CancellationToken cancellationToken)
    {
        SessionStarted?.Invoke(this, MacroSessionKind.AutoClicker);
        OnStatusChanged($"Autoclicker starts in {settings.StartDelayMs} ms.");
        await DelayAsync(settings.StartDelayMs, cancellationToken);

        int clickIndex = 0;
        bool infinite = settings.ClickCount <= 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested && (infinite || clickIndex < settings.ClickCount))
            {
                PerformClick(settings.ClickType);
                clickIndex++;
                OnStatusChanged(infinite
                    ? $"Autoclicker click {clickIndex}."
                    : $"Autoclicker click {clickIndex} of {settings.ClickCount}.");

                int interval = settings.IntervalMs;
                if (settings.JitterMs > 0)
                {
                    interval += _random.Next(-settings.JitterMs, settings.JitterMs + 1);
                }

                await DelayAsync(Math.Max(0, interval), cancellationToken);
            }

            OnStatusChanged("Autoclicker finished.");
        }
        finally
        {
            SessionEnded?.Invoke(this, MacroSessionKind.AutoClicker);
        }
    }

    public async Task RunRecordedMacroAsync(RecordedMacro macro, RecorderSettings settings, CancellationToken cancellationToken)
    {
        SessionStarted?.Invoke(this, MacroSessionKind.Playback);
        int loops = Math.Max(1, settings.PlaybackLoops);
        int screenW = Screen.PrimaryScreen?.Bounds.Width ?? macro.RecordedScreenWidth;
        int screenH = Screen.PrimaryScreen?.Bounds.Height ?? macro.RecordedScreenHeight;

        try
        {
            for (int loop = 0; loop < loops && !cancellationToken.IsCancellationRequested; loop++)
            {
                if (loops > 1)
                {
                    OnStatusChanged($"Playing loop {loop + 1} of {loops}.");
                }

                OnStatusChanged($"Playing \"{macro.Name}\" ({macro.Events.Count} events).");

                foreach (RecordedMacroEvent recordedEvent in macro.Events)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsKeyPressed(NativeMethods.EscapeKey))
                    {
                        OnStatusChanged("ESC pressed. Stopping playback.");
                        return;
                    }

                    if (recordedEvent.DelayMs > 0)
                    {
                        await DelayAsync(recordedEvent.DelayMs, cancellationToken);
                    }

                    switch (recordedEvent.Type)
                    {
                        case RecordedEventType.MouseMove:
                            InputSimulator.MoveMouse(ResolveX(recordedEvent, screenW, macro), ResolveY(recordedEvent, screenH, macro));
                            break;
                        case RecordedEventType.MouseClick:
                            InputSimulator.MoveMouse(ResolveX(recordedEvent, screenW, macro), ResolveY(recordedEvent, screenH, macro));
                            InputSimulator.MouseButton(recordedEvent.Button, recordedEvent.Action);
                            break;
                        case RecordedEventType.MouseScroll:
                            InputSimulator.MoveMouse(ResolveX(recordedEvent, screenW, macro), ResolveY(recordedEvent, screenH, macro));
                            InputSimulator.Scroll(recordedEvent.ScrollDelta);
                            break;
                        case RecordedEventType.KeyDown:
                            InputSimulator.KeyDown(recordedEvent.VirtualKey);
                            break;
                        case RecordedEventType.KeyUp:
                            InputSimulator.KeyUp(recordedEvent.VirtualKey);
                            break;
                    }
                }
            }

            OnStatusChanged("Recorded macro playback finished.");
        }
        finally
        {
            SessionEnded?.Invoke(this, MacroSessionKind.Playback);
        }
    }

    public static string GetCurrentMousePositionText()
    {
        NativeMethods.GetCursorPos(out NativeMethods.Point point);
        return $"X={point.X}, Y={point.Y}";
    }

    public static (int Width, int Height) GetPrimaryScreenSize()
    {
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        return (bounds.Width, bounds.Height);
    }

    private static int ResolveX(RecordedMacroEvent e, int screenW, RecordedMacro macro)
    {
        if (e.UseRelative && macro.RecordedScreenWidth > 0)
        {
            return (int)Math.Round(e.RelativeX * screenW);
        }

        return e.X;
    }

    private static int ResolveY(RecordedMacroEvent e, int screenH, RecordedMacro macro)
    {
        if (e.UseRelative && macro.RecordedScreenHeight > 0)
        {
            return (int)Math.Round(e.RelativeY * screenH);
        }

        return e.Y;
    }

    private static void PerformClick(string clickType)
    {
        switch (clickType.ToLowerInvariant())
        {
            case "right":
                InputSimulator.RightClick();
                break;
            case "middle":
                InputSimulator.MiddleClick();
                break;
            default:
                InputSimulator.LeftClick();
                break;
        }
    }

    private async Task ClickVortexAsync(MacroSettings settings, CancellationToken cancellationToken)
    {
        (int x, int y) = PickPosition(settings.VortexXStart, settings.VortexXEnd, settings.VortexYStart, settings.VortexYEnd);
        OnStatusChanged($"Vortex click at X={x}, Y={y}.");
        await MoveAndClickAsync(x, y, settings.MoveDelayMs, cancellationToken);
        await DelayAsync(settings.VortexDelayMs, cancellationToken);
    }

    private async Task ClickNexusAsync(MacroSettings settings, CancellationToken cancellationToken)
    {
        await MoveAndClickAsync(settings.CloseNexusReminderX, settings.CloseNexusReminderY, settings.MoveDelayMs, cancellationToken);
        await MoveAndClickAsync(settings.NexusPageX, settings.NexusPageY, settings.MoveDelayMs, cancellationToken);
        ScrollPage("down", settings.ScrollDownSteps);
        await DelayAsync(500, cancellationToken);
        ScrollPage("up", settings.ScrollUpSteps);
        await DelayAsync(1000, cancellationToken);

        (int x, int y) = PickPosition(settings.NexusXStart, settings.NexusXEnd, settings.NexusYStart, settings.NexusYEnd);
        await MoveAndClickAsync(x, y, settings.MoveDelayMs, cancellationToken);
        await DelayAsync(settings.NexusAfterFirstClickDelayMs, cancellationToken);

        (int x2, int y2) = PickPosition(settings.NexusXStart, settings.NexusXEnd, settings.NexusLowerYStart, settings.NexusLowerYEnd);
        await MoveAndClickAsync(x2, y2, settings.MoveDelayMs, cancellationToken);
        await DelayAsync(settings.NexusAfterSecondClickDelayMs, cancellationToken);

        (int x3, int y3) = PickPosition(settings.NexusXStart, settings.NexusXEnd, settings.NexusLowerPlusYStart, settings.NexusLowerPlusYEnd);
        await MoveAndClickAsync(x3, y3, settings.MoveDelayMs, cancellationToken);
        await DelayAsync(settings.NexusDelayMs, cancellationToken);
    }

    private async Task ClearGoogleAsync(MacroSettings settings, CancellationToken cancellationToken)
    {
        OnStatusChanged("Running Google close click.");
        await MoveAndClickAsync(settings.CloseGoogleX, settings.CloseGoogleY, settings.MoveDelayMs, cancellationToken);
        await DelayAsync(settings.ClearGoogleDelayMs, cancellationToken);
    }

    private async Task MoveAndClickAsync(int x, int y, int moveDelayMs, CancellationToken cancellationToken)
    {
        InputSimulator.MoveMouse(x, y);
        await DelayAsync(moveDelayMs, cancellationToken);
        InputSimulator.LeftClick();
    }

    private (int X, int Y) PickPosition(int x1, int x2, int y1, int y2)
    {
        int randomX = _random.Next(Math.Min(x1, x2), Math.Max(x1, x2) + 1);
        int randomY = _random.Next(Math.Min(y1, y2), Math.Max(y1, y2) + 1);
        return (randomX, randomY);
    }

    private static void ScrollPage(string direction, int steps)
    {
        int scrollAmount = steps * 120;
        int delta = direction.Equals("up", StringComparison.OrdinalIgnoreCase) ? scrollAmount : -scrollAmount;
        InputSimulator.Scroll(delta);
    }

    private void PublishCurrentMousePosition() => MousePositionChanged?.Invoke(this, GetCurrentMousePositionText());

    private static bool IsKeyPressed(int virtualKey) => NativeMethods.GetAsyncKeyState(virtualKey) < 0;

    private static Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken)
        => Task.Delay(Math.Max(0, millisecondsDelay), cancellationToken);

    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
