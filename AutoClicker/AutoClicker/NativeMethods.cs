using System.Runtime.InteropServices;



namespace AutoClicker;



internal static class NativeMethods

{

    public const uint LeftDown = 0x0002;

    public const uint LeftUp = 0x0004;

    public const uint RightDown = 0x0008;

    public const uint RightUp = 0x0010;

    public const uint MiddleDown = 0x0020;

    public const uint MiddleUp = 0x0040;

    public const uint MouseWheel = 0x0800;

    public const uint MouseMove = 0x0001;

    public const uint KeyeventfKeyup = 0x0002;

    public const int InputMouse = 0;

    public const int InputKeyboard = 1;

    public const int EscapeKey = 0x1B;

    public const int WmHotkey = 0x0312;

    public const int WhMouseLl = 14;

    public const int WhKeyboardLl = 13;

    public const uint ModNone = 0x0000;

    public const uint ModAlt = 0x0001;

    public const uint ModControl = 0x0002;

    public const uint ModShift = 0x0004;



    public const int WmLButtonDown = 0x0201;

    public const int WmLButtonUp = 0x0202;

    public const int WmRButtonDown = 0x0204;

    public const int WmRButtonUp = 0x0205;

    public const int WmMButtonDown = 0x0207;

    public const int WmMButtonUp = 0x0208;

    public const int WmMouseWheel = 0x020A;

    public const int WmMouseMove = 0x0200;



    public static readonly int InputSize = Marshal.SizeOf<Input>();



    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);



    [DllImport("user32.dll", SetLastError = true)]

    public static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);



    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]

    public static extern bool SetCursorPos(int x, int y);



    [DllImport("user32.dll")]

    public static extern short GetAsyncKeyState(int vKey);



    [DllImport("user32.dll")]

    public static extern bool GetCursorPos(out Point lpPoint);



    [DllImport("user32.dll", SetLastError = true)]

    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);



    [DllImport("user32.dll", SetLastError = true)]

    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);



    [DllImport("user32.dll")]

    public static extern short VkKeyScan(char ch);



    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);



    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);



    [DllImport("user32.dll", SetLastError = true)]

    public static extern bool UnhookWindowsHookEx(IntPtr hhk);



    [DllImport("user32.dll", SetLastError = true)]

    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);



    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

    public static extern IntPtr GetModuleHandle(string? lpModuleName);



    [StructLayout(LayoutKind.Sequential)]

    public struct Point

    {

        public int X;

        public int Y;

    }



    [StructLayout(LayoutKind.Sequential)]

    public struct MsLlHookStruct

    {

        public Point Pt;

        public uint MouseData;

        public uint Flags;

        public uint Time;

        public IntPtr DwExtraInfo;

    }



    [StructLayout(LayoutKind.Sequential)]

    public struct KeyboardLlHookStruct

    {

        public uint VkCode;

        public uint ScanCode;

        public uint Flags;

        public uint Time;

        public IntPtr DwExtraInfo;

    }



    [StructLayout(LayoutKind.Sequential)]

    public struct Input

    {

        public int Type;

        public InputUnion U;

    }



    [StructLayout(LayoutKind.Explicit)]

    public struct InputUnion

    {

        [FieldOffset(0)] public MouseInput Mouse;

        [FieldOffset(0)] public KeyboardInput Keyboard;

    }



    [StructLayout(LayoutKind.Sequential)]

    public struct MouseInput

    {

        public int Dx;

        public int Dy;

        public uint MouseData;

        public uint DwFlags;

        public uint Time;

        public IntPtr DwExtraInfo;

    }



    [StructLayout(LayoutKind.Sequential)]

    public struct KeyboardInput

    {

        public ushort WVk;

        public ushort WScan;

        public uint DwFlags;

        public uint Time;

        public IntPtr DwExtraInfo;

    }

}


