namespace Contextrion;

internal sealed class ClipboardSavePreviewForm : Form
{
    private readonly TextBox _fileNameTextBox;

    public ClipboardSavePreviewForm(ClipboardContentSnapshot snapshot)
    {
        Text = Translate("titles.paste-into-file", "Paste into File");
        Icon = AppIconProvider.GetIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(760, 560);

        var descriptionLabel = new Label
        {
            Left = 16,
            Top = 16,
            Width = 720,
            Text = Translate(
                "preview.description",
                "Choose the base file name and review the clipboard content before saving.")
        };

        var typeLabel = new Label
        {
            Left = 16,
            Top = 42,
            Width = 720,
            Text = Translate(
                "preview.detected-type",
                "Detected type: {0} ({1})",
                snapshot.DisplayName,
                snapshot.Extension)
        };

        var detailsLabel = new Label
        {
            Left = 16,
            Top = 66,
            Width = 720,
            Text = snapshot.PromptMessage
        };

        var previewGroup = new GroupBox
        {
            Left = 16,
            Top = 96,
            Width = 720,
            Height = 360,
            Text = Translate("preview.group", "Preview")
        };

        var previewControl = CreatePreviewControl(snapshot);
        previewGroup.Controls.Add(previewControl);

        var nameLabel = new Label
        {
            Left = 16,
            Top = 472,
            Width = 100,
            Text = Translate("preview.file-name", "File name")
        };

        _fileNameTextBox = new TextBox
        {
            Left = 16,
            Top = 494,
            Width = 540,
            Text = DefaultNames.Create()
        };

        var extensionLabel = new Label
        {
            Left = 566,
            Top = 497,
            Width = 60,
            Text = snapshot.Extension
        };

        var saveButton = new Button
        {
            Text = Translate("common.save", "Save"),
            Left = 580,
            Top = 524,
            Width = 75,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = Translate("common.cancel", "Cancel"),
            Left = 661,
            Top = 524,
            Width = 75,
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(descriptionLabel);
        Controls.Add(typeLabel);
        Controls.Add(detailsLabel);
        Controls.Add(previewGroup);
        Controls.Add(nameLabel);
        Controls.Add(_fileNameTextBox);
        Controls.Add(extensionLabel);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    public string FileNameValue => _fileNameTextBox.Text;

    private static Control CreatePreviewControl(ClipboardContentSnapshot snapshot)
    {
        return snapshot.Kind switch
        {
            ClipboardContentKind.Image => CreateImagePreview(snapshot.ImagePreview),
            ClipboardContentKind.Text => CreateTextPreview(snapshot.TextPreview),
            ClipboardContentKind.Archive => CreateFileListPreview(snapshot.FileItems),
            ClipboardContentKind.Audio => CreateAudioPreview(snapshot.BinaryPayload),
            _ => CreateBinaryPreview(snapshot.BinaryPayload),
        };
    }

    private static Control CreateImagePreview(Image? image)
    {
        if (image is null)
        {
            return CreateFallbackLabel(Translate("preview.image.unavailable", "No image preview is available."));
        }

        return new PictureBox
        {
            Left = 12,
            Top = 24,
            Width = 696,
            Height = 324,
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Control CreateTextPreview(string? text)
    {
        return new TextBox
        {
            Left = 12,
            Top = 24,
            Width = 696,
            Height = 324,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Text = text ?? string.Empty
        };
    }

    private static Control CreateFileListPreview(IReadOnlyList<string> items)
    {
        var listView = new ListView
        {
            Left = 12,
            Top = 24,
            Width = 696,
            Height = 324,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add(Translate("common.name", "Name"), 260);
        listView.Columns.Add(Translate("common.type", "Type"), 100);
        listView.Columns.Add(Translate("common.path", "Path"), 320);

        foreach (var item in items)
        {
            var itemType = Directory.Exists(item)
                ? Translate("common.folder", "Folder")
                : Translate("common.file", "File");
            var row = new ListViewItem(Path.GetFileName(item));
            row.SubItems.Add(itemType);
            row.SubItems.Add(item);
            listView.Items.Add(row);
        }

        return listView;
    }

    private static Control CreateBinaryPreview(byte[]? bytes)
    {
        var message = bytes is null
            ? Translate("preview.binary.unavailable", "No binary preview is available.")
            : Translate(
                "preview.binary.size",
                "Binary payload size: {0} bytes",
                bytes.Length.ToString("N0"));

        return CreateFallbackLabel(message);
    }

    private static Control CreateAudioPreview(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return CreateFallbackLabel(Translate("preview.audio.unavailable", "No audio preview is available."));
        }

        var lines = new List<string>
        {
            Translate("preview.audio.size", "Audio payload size: {0} bytes", bytes.Length.ToString("N0"))
        };

        if (TryReadWaveDuration(bytes, out var duration))
        {
            lines.Add(Translate("preview.audio.duration", "Estimated duration: {0}", duration.ToString(@"mm\:ss\.fff")));
        }
        else
        {
            lines.Add(Translate("preview.audio.duration-unknown", "Duration could not be determined from the WAV header."));
        }

        lines.Add(Translate("preview.audio.saved-as", "The clipboard audio will be saved as a .wav file."));
        return CreateFallbackLabel(string.Join(Environment.NewLine, lines));
    }

    private static bool TryReadWaveDuration(byte[] bytes, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;

        if (bytes.Length < 44)
        {
            return false;
        }

        if (!MatchesAscii(bytes, 0, "RIFF") || !MatchesAscii(bytes, 8, "WAVE"))
        {
            return false;
        }

        var offset = 12;
        uint? dataSize = null;
        ushort? channels = null;
        uint? sampleRate = null;
        ushort? bitsPerSample = null;

        while (offset + 8 <= bytes.Length)
        {
            var chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToUInt32(bytes, offset + 4);
            var chunkDataOffset = offset + 8;

            if (chunkDataOffset + chunkSize > bytes.Length)
            {
                return false;
            }

            if (chunkId == "fmt " && chunkSize >= 16)
            {
                channels = BitConverter.ToUInt16(bytes, chunkDataOffset + 2);
                sampleRate = BitConverter.ToUInt32(bytes, chunkDataOffset + 4);
                bitsPerSample = BitConverter.ToUInt16(bytes, chunkDataOffset + 14);
            }
            else if (chunkId == "data")
            {
                dataSize = chunkSize;
            }

            offset = chunkDataOffset + (int)chunkSize;
            if ((chunkSize & 1) == 1)
            {
                offset++;
            }
        }

        if (dataSize is null || channels is null || sampleRate is null || bitsPerSample is null)
        {
            return false;
        }

        var bytesPerSecond = sampleRate.Value * channels.Value * bitsPerSample.Value / 8d;
        if (bytesPerSecond <= 0)
        {
            return false;
        }

        duration = TimeSpan.FromSeconds(dataSize.Value / bytesPerSecond);
        return true;
    }

    private static bool MatchesAscii(byte[] bytes, int offset, string value)
    {
        if (offset + value.Length > bytes.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (bytes[offset + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private static Control CreateFallbackLabel(string text)
    {
        return new Label
        {
            Left = 12,
            Top = 24,
            Width = 696,
            Height = 324,
            Text = text
        };
    }
}