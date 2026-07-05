namespace AutoClicker;

internal static class InputSimulator
{
    public static void MoveMouse(int x, int y) => NativeMethods.SetCursorPos(x, y);

    public static void LeftClick()
    {
        SendMouse(NativeMethods.LeftDown);
        SendMouse(NativeMethods.LeftUp);
    }

    public static void RightClick()
    {
        SendMouse(NativeMethods.RightDown);
        SendMouse(NativeMethods.RightUp);
    }

    public static void MiddleClick()
    {
        SendMouse(NativeMethods.MiddleDown);
        SendMouse(NativeMethods.MiddleUp);
    }

    public static void MouseButton(string button, string action)
    {
        bool isUp = action.Equals("Up", StringComparison.OrdinalIgnoreCase);
        uint flag = button.ToLowerInvariant() switch
        {
            "right" => isUp ? NativeMethods.RightUp : NativeMethods.RightDown,
            "middle" => isUp ? NativeMethods.MiddleUp : NativeMethods.MiddleDown,
            _ => isUp ? NativeMethods.LeftUp : NativeMethods.LeftDown
        };
        SendMouse(flag);
    }

    public static void Scroll(int delta)
    {
        var input = new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            U = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.MouseInput
                {
                    MouseData = unchecked((uint)delta),
                    DwFlags = NativeMethods.MouseWheel
                }
            }
        };
        NativeMethods.SendInput(1, [input], NativeMethods.InputSize);
    }

    public static void KeyDown(int virtualKey) => SendKey((ushort)virtualKey, false);

    public static void KeyUp(int virtualKey) => SendKey((ushort)virtualKey, true);

    public static void CharKeyDown(char c, bool lowercase) => SendChar(c, lowercase, keyUp: false);

    public static void CharKeyUp(char c, bool lowercase) => SendChar(c, lowercase, keyUp: true);

    private static void SendMouse(uint flags)
    {
        var input = new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            U = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.MouseInput { DwFlags = flags }
            }
        };
        NativeMethods.SendInput(1, [input], NativeMethods.InputSize);
    }

    private static void SendKey(ushort virtualKey, bool keyUp)
    {
        var input = new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    WVk = virtualKey,
                    DwFlags = keyUp ? NativeMethods.KeyeventfKeyup : 0
                }
            }
        };
        NativeMethods.SendInput(1, [input], NativeMethods.InputSize);
    }

    private static void SendChar(char c, bool lowercase, bool keyUp)
    {
        char keyChar = ToKeyChar(c, lowercase);
        short scan = NativeMethods.VkKeyScan(keyChar);
        if (scan == -1)
        {
            return;
        }

        ushort virtualKey = (ushort)(scan & 0xFF);
        bool needShift = (scan >> 8 & 1) != 0;

        if (!keyUp)
        {
            if (needShift)
            {
                KeyDown((int)Keys.ShiftKey);
            }

            KeyDown(virtualKey);
            return;
        }

        KeyUp(virtualKey);
        if (needShift)
        {
            KeyUp((int)Keys.ShiftKey);
        }
    }

    private static char ToKeyChar(char c, bool lowercase)
    {
        if (c is ':' or '\'' or ';')
        {
            return c;
        }

        if (char.IsDigit(c) || !char.IsLetter(c))
        {
            return c;
        }

        return lowercase ? char.ToLowerInvariant(c) : c;
    }
}
