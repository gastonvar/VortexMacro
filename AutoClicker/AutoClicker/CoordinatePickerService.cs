using System.Runtime.InteropServices;

namespace AutoClicker;

internal sealed class CoordinatePickerService : IDisposable
{
    private IntPtr _hook;
    private NativeMethods.LowLevelMouseProc? _proc;
    private TaskCompletionSource<(int X, int Y)>? _tcs;

    public async Task<(int X, int Y)?> PickAsync(CancellationToken cancellationToken = default)
    {
        if (_tcs != null)
        {
            return null;
        }

        _tcs = new TaskCompletionSource<(int X, int Y)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));

        _proc = HookCallback;
        IntPtr module = NativeMethods.GetModuleHandle(null);
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _proc, module, 0);

        if (_hook == IntPtr.Zero)
        {
            _tcs = null;
            return null;
        }

        try
        {
            return await _tcs.Task;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            Unhook();
            _tcs = null;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _tcs != null)
        {
            int message = wParam.ToInt32();
            if (message is NativeMethods.WmLButtonDown or NativeMethods.WmRButtonDown)
            {
                var hook = Marshal.PtrToStructure<NativeMethods.MsLlHookStruct>(lParam);
                _tcs.TrySetResult((hook.Pt.X, hook.Pt.Y));
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void Unhook()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    public void Dispose() => Unhook();
}
