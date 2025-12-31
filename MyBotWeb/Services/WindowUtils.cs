using System.Runtime.InteropServices;

namespace MyBotWeb.Services;

public static class WindowUtils
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(
        IntPtr hwndParent,
        IntPtr hwndChildAfter,
        string? lpszClass,
        string? lpszWindow
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(
        IntPtr hwndParent,
        EnumChildProc lpEnumFunc,
        IntPtr lParam
    );

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(
        IntPtr hWnd,
        System.Text.StringBuilder lpString,
        int nMaxCount
    );

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(
        IntPtr hWnd,
        System.Text.StringBuilder lpClassName,
        int nMaxCount
    );

    public delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    // Window Messages
    public const uint WM_CLOSE = 0x0010;
    public const uint BM_CLICK = 0x00F5;

    public static IntPtr FindWindowByTitles(params string[] titles)
    {
        foreach (var title in titles)
        {
            IntPtr window = FindWindow(null, title);
            if (window != IntPtr.Zero)
            {
                return window;
            }
        }
        return IntPtr.Zero;
    }

    public static bool CloseWindow(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
        {
            SendMessage(windowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        return false;
    }

    public static bool ClickButton(IntPtr buttonHandle)
    {
        if (buttonHandle != IntPtr.Zero)
        {
            SendMessage(buttonHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        return false;
    }
}
