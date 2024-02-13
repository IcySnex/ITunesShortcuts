#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable CA1401 // P/Invokes should not be visible

using System.Runtime.InteropServices;

namespace ITunesShortcuts.Helpers;

public abstract class Win32
{
    public delegate IntPtr WinProc(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    public delegate int SUBCLASSPROC(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam,
        IntPtr uIdSubclass,
        uint dwRefData);

    public delegate IntPtr HookProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);


    public static WinProc? NewWndProc { get; set; }
    public static IntPtr OldWndProc { get; set; } = IntPtr.Zero;

    public static int MinWidth = 0;
    public static int MinHeight = 0;

    public const uint MB_OK = 0x00000000;
    public const uint MB_YESNO = 0x00000004;
    public const uint MB_ICONINFORMATION = 0x00000040;
    public const uint MB_ICONERROR = 0x00000010;

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_SYSKEYDOWN = 0x104;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYUP = 0x105;


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPATCHERQUEUEOPTIONS
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }


    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDpiForWindow(
        IntPtr hwnd);

    [DllImport("user32")]
    public static extern IntPtr SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        WinProc newProc);

    [DllImport("user32", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        WinProc newProc);

    [DllImport("user32.dll")]
    public static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("winbrand.dll", CharSet = CharSet.Unicode)]
    public static extern string BrandingFormatString(
        string format);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(
        int id,
        HookProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(
        string lpModuleName);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);



    public static IntPtr SetWindowLongAll(
        IntPtr hWnd,
        int nIndex,
        WinProc newProc) =>
        IntPtr.Size == 8 ? SetWindowLongPtr(hWnd, nIndex, newProc) : SetWindowLong(hWnd, nIndex, newProc);

    public static IntPtr NewWindowProc(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (msg == 36)
        {
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.x = MinWidth;
            minMaxInfo.ptMinTrackSize.y = MinHeight;
            Marshal.StructureToPtr(minMaxInfo, lParam, true);

        }
        return CallWindowProc(OldWndProc, hWnd, msg, wParam, lParam);
    }
}