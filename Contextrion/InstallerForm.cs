namespace Contextrion;

internal sealed class InstallerForm : Form
{
    private readonly Label _statusLabel;
    private readonly Button _installButton;
    private readonly Button _uninstallButton;
    private readonly Button _openAssetsButton;
    private readonly Button _importIconsButton;

    public InstallerForm()
    {
        Text = Translate("titles.app-name", "Contextrion");
        Icon = AppIconProvider.GetIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(640, 310);

        var titleLabel = new Label
        {
            Left = 20,
            Top = 20,
            Width = 590,
            Font = new Font(Font, FontStyle.Bold),
            Text = Translate(
                "installer.heading",
                "Contextrion installer and folder customization tools")
        };

        var descriptionLabel = new Label
        {
            Left = 20,
            Top = 56,
            Width = 590,
            Height = 76,
            Text = Translate(
                "installer.description",
                "Installs the application into Program Files, keeps the existing 'Paste Into File' flow, enables folder customization, and adds the integrated file tools as Explorer context menu commands.")
        };

        var locationLabel = new Label
        {
            Left = 20,
            Top = 134,
            Width = 590,
            Text = Translate(
                "installer.destination",
                "Destination: {0}",
                ExplorerContextInstaller.InstallDirectory)
        };

        var menuLabel = new Label
        {
            Left = 20,
            Top = 162,
            Width = 590,
            Height = 80,
            Text = Translate(
                "installer.menu-preview",
                "Explorer submenu:\nContextrion > Paste Into File\nContextrion > Customize Folder\nContextrion > Restore Default\nContextrion > File tools (rename, Data URL, combine, watermark, minify, and more)")
        };

        _statusLabel = new Label
        {
            Left = 20,
            Top = 232,
            Width = 590,
            Text = string.Empty
        };

        _installButton = new Button
        {
            Left = 150,
            Top = 262,
            Width = 110,
            Text = Translate("common.install", "Install")
        };
        _installButton.Click += (_, _) =>
        {
            Program.RunInstall(showDialogs: true);
            RefreshStatus();
        };

        _uninstallButton = new Button
        {
            Left = 272,
            Top = 262,
            Width = 110,
            Text = Translate("common.uninstall", "Uninstall")
        };
        _uninstallButton.Click += (_, _) =>
        {
            Program.RunUninstall(showDialogs: true);
            RefreshStatus();
        };

        _openAssetsButton = new Button
        {
            Left = 394,
            Top = 262,
            Width = 110,
            Text = Translate("common.open-assets", "Open Assets")
        };
        _openAssetsButton.Click += (_, _) => OpenAssetsFolder();

        _importIconsButton = new Button
        {
            Left = 516,
            Top = 262,
            Width = 100,
            Text = Translate("common.import", "Import")
        };
        _importIconsButton.Click += (_, _) => ImportIcons();

        Controls.Add(titleLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(locationLabel);
        Controls.Add(menuLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_installButton);
        Controls.Add(_uninstallButton);
        Controls.Add(_openAssetsButton);
        Controls.Add(_importIconsButton);

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var installed = ExplorerContextInstaller.IsInstalled();
        _statusLabel.Text = installed
            ? Translate("installer.status.installed", "Status: installed")
            : Translate("installer.status.not-installed", "Status: not installed");

        _installButton.Text = installed
            ? Translate("common.update", "Update")
            : Translate("common.install", "Install");
        _installButton.Enabled = true;
        _uninstallButton.Enabled = installed;
        _openAssetsButton.Enabled = true;
        _importIconsButton.Enabled = true;
    }

    private void OpenAssetsFolder()
    {
        var assetsFolder = AppPaths.EnsureDirectory(AppPaths.UserDataFolderAssetsDirectory);
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = assetsFolder,
            UseShellExecute = true
        });
    }

    private void ImportIcons()
    {
        var result = IconImportService.ImportFromDialog(this);
        if (result.CopiedCount == 0 && result.ConvertedCount == 0 && result.FailedFiles.Count == 0)
        {
            return;
        }

        var message = Translate(
            "import.summary",
            "Imported: {0}\nConverted: {1}",
            result.CopiedCount.ToString(),
            result.ConvertedCount.ToString());
        if (result.FailedFiles.Count > 0)
        {
            message += Translate(
                "import.summary.failed",
                "\nFailed: {0}",
                string.Join(", ", result.FailedFiles.Take(5)));
        }

        MessageBox.Show(
            this,
            message,
            Translate("titles.app-name", "Contextrion"),
            MessageBoxButtons.OK,
            result.FailedFiles.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}
