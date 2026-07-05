namespace AutoClicker;

internal static class PianoPlayer
{
    public static async Task PlayAsync(
        IReadOnlyList<IReadOnlyList<PianoToken>> song,
        PianoPlayerSettings settings,
        Action<string> onStatus,
        CancellationToken cancellationToken)
    {
        onStatus($"Starting in {settings.CountdownSeconds:0}s — focus the game window!");
        DateTime deadline = DateTime.UtcNow.AddSeconds(settings.CountdownSeconds);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
        }

        int repeats = Math.Max(1, settings.RepeatLines);
        for (int rep = 0; rep < repeats; rep++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int lineIdx = 0; lineIdx < song.Count; lineIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                onStatus($"Line {lineIdx + 1}/{song.Count}");

                foreach (PianoToken token in song[lineIdx])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await PlayTokenAsync(token, settings, cancellationToken);
                }

                if (lineIdx < song.Count - 1)
                {
                    await SleepMsAsync(settings.LineGapMs, cancellationToken);
                }
            }

            if (rep < repeats - 1)
            {
                await SleepMsAsync(settings.LineGapMs * 2, cancellationToken);
            }
        }

        onStatus("Finished.");
    }

    public static void ReleaseAllKeys(bool lowercase)
    {
        const string keyChars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:;'!@#$%^&*()_+-=,./<>?";
        foreach (char c in keyChars)
        {
            try
            {
                InputSimulator.CharKeyUp(c, lowercase);
            }
            catch
            {
                // Best effort.
            }
        }
    }

    private static async Task PlayTokenAsync(
        PianoToken token,
        PianoPlayerSettings settings,
        CancellationToken cancellationToken)
    {
        switch (token.Kind)
        {
            case PianoTokenKind.Pause:
                await SleepMsAsync(settings.PauseMs, cancellationToken);
                return;

            case PianoTokenKind.Note:
                int holdMs = settings.NoteMs + (token.RepeatCount - 1) * settings.HoldMultiplierMs;
                InputSimulator.CharKeyDown(token.NoteChar, settings.Lowercase);
                await SleepMsAsync(holdMs, cancellationToken);
                InputSimulator.CharKeyUp(token.NoteChar, settings.Lowercase);
                return;

            case PianoTokenKind.Chord:
                string[] keys = token.ChordKeys ?? [];
                foreach (string key in keys)
                {
                    if (key.Length > 0)
                    {
                        InputSimulator.CharKeyDown(key[0], settings.Lowercase);
                    }
                }

                await SleepMsAsync(settings.NoteMs, cancellationToken);

                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    if (keys[i].Length > 0)
                    {
                        InputSimulator.CharKeyUp(keys[i][0], settings.Lowercase);
                    }
                }

                return;
        }
    }

    private static async Task SleepMsAsync(int ms, CancellationToken cancellationToken)
    {
        if (ms <= 0)
        {
            return;
        }

        DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = (int)Math.Max(1, (end - DateTime.UtcNow).TotalMilliseconds);
            await Task.Delay(Math.Min(20, remaining), cancellationToken);
        }
    }
}
