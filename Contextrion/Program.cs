namespace Contextrion;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var command = CommandLineArguments.Parse(args);

        switch (command.Mode)
        {
            case AppMode.Paste:
                RunPaste(command.TargetPath);
                return;
            case AppMode.Install:
                RunInstall(showDialogs: true);
                return;
            case AppMode.Uninstall:
                RunUninstall(showDialogs: true);
                return;
            case AppMode.PickFolder:
                RunFolderPicker(command.TargetPath);
                return;
            case AppMode.RestoreFolder:
                RunRestoreFolder(command.TargetPath);
                return;
            case AppMode.NewTimestampFolder:
                RunNewTimestampFolder(command.TargetPath);
                return;
            case AppMode.FriendlyName:
            case AppMode.ToDataUrl:
            case AppMode.CopyPath:
            case AppMode.EnumRename:
            case AppMode.CombineVertical:
            case AppMode.CombineHorizontal:
            case AppMode.CleanEmpty:
            case AppMode.CopyContent:
            case AppMode.Grayscale:
            case AppMode.Watermark:
            case AppMode.Crop:
            case AppMode.Circle:
            case AppMode.Resize:
            case AppMode.InvertColor:
            case AppMode.CleanMetadata:
            case AppMode.Minify:
            case AppMode.Optimize:
                RunLegacyAction(command.Mode, command.TargetPaths);
                return;
            default:
                Application.Run(new InstallerForm());
                return;
        }
    }

    internal static string ExecutablePath => Application.ExecutablePath;

    private static void RunPaste(string? targetDirectory)
    {
        ClipboardContentSnapshot? snapshot = null;
        var dialogTitle = Translate("titles.paste-into-file", "Paste into File");

        try
        {
            var resolvedDirectory = ResolveTargetDirectory(targetDirectory);
            snapshot = ClipboardSaveService.CaptureClipboardContent();
            using var previewForm = new ClipboardSavePreviewForm(snapshot);
            if (previewForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var result = ClipboardSaveService.SaveClipboardContent(resolvedDirectory, previewForm.FileNameValue, snapshot);
            MessageBox.Show(
                Translate("program.paste.saved", "Content saved to:\n{0}", result.FullPath),
                dialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToFullExceptionString(),
                dialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            snapshot?.Dispose();
        }
    }

    private static void RunFolderPicker(string? targetFolder)
    {
        try
        {
            var resolvedFolder = ResolveTargetDirectory(targetFolder);
            using var form = new FolderIconPickerForm(resolvedFolder);
            form.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToFullExceptionString(),
                Translate("titles.customize-folder", "Customize Folder"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RunRestoreFolder(string? targetFolder)
    {
        try
        {
            var resolvedFolder = ResolveTargetDirectory(targetFolder);
            FolderCustomizationService.RestoreDefault(resolvedFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                Translate("titles.restore-default", "Restore Default"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RunNewTimestampFolder(string? targetFolder)
    {
        try
        {
            var resolvedFolder = ResolveTargetDirectory(targetFolder);
            var now = DateTime.Now;
            var timestampFolder = Path.Combine(
                resolvedFolder,
                now.Year.ToString("0000"),
                now.Month.ToString("00"),
                now.Day.ToString("00"));

            Directory.CreateDirectory(timestampFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToFullExceptionString(),
                Translate("titles.new-timestamp-folder", "New Timestamp Folder"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void RunLegacyAction(AppMode mode, IReadOnlyList<string> targetPaths)
    {
        try
        {
            LegacyContextActionService.Execute(mode, targetPaths);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToFullExceptionString(),
                Translate("titles.clipboard-files", "Clipboard Files"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    internal static void RunInstall(bool showDialogs)
    {
        try
        {
            ExplorerContextInstaller.Install();

            if (showDialogs)
            {
                MessageBox.Show(
                    Translate(
                        "program.install.completed",
                        "Context menus updated for:\n{0}",
                        ExplorerContextInstaller.GetContextMenuExecutablePath()),
                    Translate("titles.clipboard-files", "Clipboard Files"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            if (showDialogs)
            {
                MessageBox.Show(
                    ex.ToFullExceptionString(),
                    Translate("titles.clipboard-files", "Clipboard Files"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            throw;
        }
    }

    internal static void RunUninstall(bool showDialogs)
    {
        try
        {
            ExplorerContextInstaller.Uninstall();

            if (showDialogs)
            {
                MessageBox.Show(
                    Translate(
                        "program.uninstall.completed",
                        "Context menus removed. The ClickOnce application remains installed."),
                    Translate("titles.clipboard-files", "Clipboard Files"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            if (showDialogs)
            {
                MessageBox.Show(
                    ex.ToFullExceptionString(),
                    Translate("titles.clipboard-files", "Clipboard Files"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            throw;
        }
    }

    private static string ResolveTargetDirectory(string? targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        if (Directory.Exists(targetDirectory))
        {
            return Path.GetFullPath(targetDirectory);
        }

        throw new DirectoryNotFoundException(
            Translate("errors.invalid-directory", "Invalid directory: {0}", targetDirectory));
    }
}
