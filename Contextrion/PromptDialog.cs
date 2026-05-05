namespace Contextrion;

internal static class PromptDialog
{
    public static string? Show(string prompt, string title, string defaultValue = "")
    {
        using var form = new Form
        {
            Width = 520,
            Height = 180,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Icon = AppIconProvider.GetIcon()
        };

        var promptLabel = new Label
        {
            Left = 12,
            Top = 12,
            Width = 480,
            Height = 48,
            Text = prompt
        };

        var inputBox = new TextBox
        {
            Left = 12,
            Top = 64,
            Width = 480,
            Text = defaultValue
        };

        var okButton = new Button
        {
            Text = Translate("common.ok", "OK"),
            Left = 336,
            Width = 75,
            Top = 100,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = Translate("common.cancel", "Cancel"),
            Left = 417,
            Width = 75,
            Top = 100,
            DialogResult = DialogResult.Cancel
        };

        form.Controls.Add(promptLabel);
        form.Controls.Add(inputBox);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        return form.ShowDialog() == DialogResult.OK
            ? inputBox.Text
            : null;
    }
}
