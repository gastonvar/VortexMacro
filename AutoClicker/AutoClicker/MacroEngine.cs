namespace AutoClicker;

internal sealed class MacroEngine
{
    private readonly Random _random = new();

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? MousePositionChanged;

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
        OnStatusChanged("Vortex macro started. Use Stop or ESC to end. Use the Up Arrow hotkey to pause/resume.");

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

            if (settings.ClearGoogleEveryCycles > 0 && cycle % settings.ClearGoogleEveryCycles == 0)
            {
                await ClearGoogleAsync(settings, cancellationToken);
            }

            PublishCurrentMousePosition();
            cycle++;
            await DelayAsync(settings.CycleDelayMs, cancellationToken);
        }

        OnStatusChanged("Vortex macro stopped.");
    }

    public async Task RunAutoClickerAsync(int intervalMs, int clickCount, bool rightClick, int startDelayMs, CancellationToken cancellationToken)
    {
        uint down = rightClick ? NativeMethods.RightDown : NativeMethods.LeftDown;
        uint up = rightClick ? NativeMethods.RightUp : NativeMethods.LeftUp;

        OnStatusChanged($"Autoclicker starts in {startDelayMs} ms.");
        await DelayAsync(startDelayMs, cancellationToken);

        for (int i = 0; i < clickCount && !cancellationToken.IsCancellationRequested; i++)
        {
            NativeMethods.mouse_event(down, 0, 0, 0, IntPtr.Zero);
            NativeMethods.mouse_event(up, 0, 0, 0, IntPtr.Zero);
            OnStatusChanged($"Autoclicker click {i + 1} of {clickCount}.");
            await DelayAsync(intervalMs, cancellationToken);
        }

        OnStatusChanged("Autoclicker finished.");
    }

    public async Task RunRecordedMacroAsync(RecordedMacro macro, CancellationToken cancellationToken)
    {
        OnStatusChanged($"Playing recorded macro \"{macro.Name}\" ({macro.Events.Count} events).");

        foreach (RecordedMacroEvent recordedEvent in macro.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (NativeMethods.GetAsyncKeyState(NativeMethods.EscapeKey) < 0)
            {
                OnStatusChanged("ESC pressed. Stopping recorded macro playback.");
                break;
            }

            if (recordedEvent.DelayMs > 0)
            {
                await DelayAsync(recordedEvent.DelayMs, cancellationToken);
            }

            switch (recordedEvent.Type)
            {
                case RecordedEventType.MouseMove:
                    NativeMethods.SetCursorPos(recordedEvent.X, recordedEvent.Y);
                    break;
                case RecordedEventType.MouseClick:
                    NativeMethods.SetCursorPos(recordedEvent.X, recordedEvent.Y);
                    SendMouseButton(recordedEvent.Button, recordedEvent.Action);
                    break;
                case RecordedEventType.MouseScroll:
                    NativeMethods.SetCursorPos(recordedEvent.X, recordedEvent.Y);
                    NativeMethods.mouse_event(
                        NativeMethods.MouseWheel,
                        0,
                        0,
                        unchecked((uint)recordedEvent.ScrollDelta),
                        IntPtr.Zero);
                    break;
                case RecordedEventType.KeyDown:
                    NativeMethods.keybd_event((byte)recordedEvent.VirtualKey, 0, 0, UIntPtr.Zero);
                    break;
                case RecordedEventType.KeyUp:
                    NativeMethods.keybd_event((byte)recordedEvent.VirtualKey, 0, 0x0002, UIntPtr.Zero);
                    break;
            }
        }

        OnStatusChanged("Recorded macro playback finished.");
    }

    public static string GetCurrentMousePositionText()
    {
        NativeMethods.GetCursorPos(out NativeMethods.Point point);
        return $"X={point.X}, Y={point.Y}";
    }

    private static void SendMouseButton(string button, string action)
    {
        bool isUp = action.Equals("Up", StringComparison.OrdinalIgnoreCase);
        uint flag = button.ToLowerInvariant() switch
        {
            "right" => isUp ? NativeMethods.RightUp : NativeMethods.RightDown,
            "middle" => isUp ? NativeMethods.MiddleUp : NativeMethods.MiddleDown,
            _ => isUp ? NativeMethods.LeftUp : NativeMethods.LeftDown
        };

        NativeMethods.mouse_event(flag, 0, 0, 0, IntPtr.Zero);
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
        NativeMethods.SetCursorPos(x, y);
        await DelayAsync(moveDelayMs, cancellationToken);
        MouseClick();
    }

    private static void MouseClick()
    {
        NativeMethods.mouse_event(NativeMethods.LeftDown, 0, 0, 0, IntPtr.Zero);
        NativeMethods.mouse_event(NativeMethods.LeftUp, 0, 0, 0, IntPtr.Zero);
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
        uint wheelData = direction.Equals("up", StringComparison.OrdinalIgnoreCase)
            ? (uint)scrollAmount
            : unchecked((uint)-scrollAmount);

        NativeMethods.mouse_event(NativeMethods.MouseWheel, 0, 0, wheelData, IntPtr.Zero);
    }

    private void PublishCurrentMousePosition() => MousePositionChanged?.Invoke(this, GetCurrentMousePositionText());

    private static bool IsKeyPressed(int virtualKey) => NativeMethods.GetAsyncKeyState(virtualKey) < 0;

    private static Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken)
    {
        return Task.Delay(Math.Max(0, millisecondsDelay), cancellationToken);
    }

    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
