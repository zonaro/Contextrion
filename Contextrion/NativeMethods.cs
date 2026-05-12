using System.Runtime.InteropServices;
using System.Text;

namespace Contextrion;

internal static class NativeMethods
{
    public const int WmSize = 0x0005;
    public const int WmKeyUp = 0x0101;
    public const int WmVScroll = 0x0115;
    public const int WmHScroll = 0x0114;
    public const int WmMouseWheel = 0x020A;
    public const uint ShcneAssocChanged = 0x08000000;
    public const uint ShcneUpdateDir = 0x00001000;
    public const uint ShcnfIdList = 0x0000;
    public const uint ShcnfPathW = 0x0005;
    public const uint ShcnfFlushNoWait = 0x2000;
    public const int WmSettingChange = 0x001A;
    public const uint SmtoAbortIfHung = 0x0002;
    public const int LvmSetImageList = 0x1003;
    public const int LvsilNormal = 0;
    public const int IlcMask = 0x0001;
    public const int IlcColor32 = 0x0020;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, string? dwItem1, nint dwItem2);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CommandLineToArgvW(string lpCmdLine, out int pNumArgs);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint SendMessageTimeout(
        nint hWnd,
        int msg,
        nint wParam,
        string? lParam,
        uint fuFlags,
        uint uTimeout,
        out nint lpdwResult);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern uint ExtractIconEx(
        string lpszFile,
        int nIconIndex,
        IntPtr[]? phiconLarge,
        IntPtr[]? phiconSmall,
        uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint PrivateExtractIcons(
        string szFileName,
        int nIconIndex,
        int cxIcon,
        int cyIcon,
        IntPtr[] phicon,
        uint[]? piconid,
        uint nIcons,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern IntPtr ImageList_Create(int cx, int cy, uint flags, int cInitial, int cGrow);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImageList_Destroy(IntPtr himl);

    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern int ImageList_ReplaceIcon(IntPtr himl, int i, IntPtr hicon);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetPrivateProfileString(
        string lpAppName,
        string lpKeyName,
        string lpDefault,
        StringBuilder lpReturnedString,
        uint nSize,
        string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WritePrivateProfileString(
        string lpAppName,
        string? lpKeyName,
        string? lpString,
        string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint LocalFree(nint hMem);

    public static string[] SplitCommandLineArguments(string commandLine)
    {
        var argv = CommandLineToArgvW(commandLine, out var argumentCount);
        if (argv == nint.Zero || argumentCount <= 0)
        {
            return Array.Empty<string>();
        }

        try
        {
            var result = new string[argumentCount];
            for (var index = 0; index < argumentCount; index++)
            {
                var argumentPointer = Marshal.ReadIntPtr(argv, index * IntPtr.Size);
                result[index] = Marshal.PtrToStringUni(argumentPointer) ?? string.Empty;
            }

            return result;
        }
        finally
        {
            _ = LocalFree(argv);
        }
    }

    public static string ReadIniValue(string section, string key, string filePath)
    {
        var builder = new StringBuilder(2048);
        _ = GetPrivateProfileString(section, key, string.Empty, builder, (uint)builder.Capacity, filePath);
        return builder.ToString();
    }
}
