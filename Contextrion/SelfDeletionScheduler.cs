using Extensions;
namespace Contextrion;


internal static class SelfDeletionScheduler
{
    public static void ScheduleDirectoryDeletion(string directoryPath, string executablePath)
    {
        var tempScriptPath = Path.Combine(Path.GetTempPath(), $"Contextrion_Uninstall_{Guid.NewGuid():N}.cmd");
        var scriptContents = $$"""
@echo off
set TARGET={{Util.Quote(directoryPath)}}
set EXE={{Util.Quote(executablePath)}}
:waitloop
del %EXE% >nul 2>nul
if exist %EXE% (
    timeout /t 1 /nobreak >nul
    goto waitloop
)
rmdir /s /q %TARGET%
del "%~f0"
""";

        File.WriteAllText(tempScriptPath, scriptContents);

        var startInfo = new ProcessStartInfo
        {
            FileName = tempScriptPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
    }

  
}
