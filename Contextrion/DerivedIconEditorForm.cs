using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Contextrion;

internal sealed class DerivedIconEditorForm : Form
{
    private readonly FolderIconEntry _baseEntry;
    private readonly List<DerivedLayerModel> _layers = [];
    private readonly ListBox _layerListBox;
    private readonly PictureBox _previewPictureBox;
    private readonly NumericUpDown _opacityInput;
    private readonly NumericUpDown _scaleInput;
    private readonly NumericUpDown _offsetXInput;
    private readonly NumericUpDown _offsetYInput;
    private readonly NumericUpDown _rotationInput;
    private readonly TextBox _nameTextBox;
    private bool _syncingControls;

    public DerivedIconEditorForm(FolderIconEntry baseEntry)
    {
        _baseEntry = baseEntry;

        Text = Translate("derived-editor.title", "Derived Icon Editor - {0}", baseEntry.Label);
        Icon = AppIconProvider.GetIcon();
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 650);
        MinimumSize = new Size(940, 620);

        _previewPictureBox = new PictureBox
        {
            Left = 20,
            Top = 20,
            Width = 360,
            Height = 360,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _layerListBox = new ListBox
        {
            Left = 410,
            Top = 20,
            Width = 260,
            Height = 250
        };
        _layerListBox.SelectedIndexChanged += (_, _) => SyncControlsFromSelection();

        var addLayerButton = new Button { Left = 690, Top = 20, Width = 120, Text = Translate("derived-editor.add-layer", "Add Layer") };
        addLayerButton.Click += (_, _) => AddLayer();

        var removeLayerButton = new Button { Left = 690, Top = 56, Width = 120, Text = Translate("common.remove", "Remove") };
        removeLayerButton.Click += (_, _) => RemoveLayer();

        var upLayerButton = new Button { Left = 690, Top = 92, Width = 120, Text = Translate("common.move-up", "Move Up") };
        upLayerButton.Click += (_, _) => MoveLayer(-1);

        var downLayerButton = new Button { Left = 690, Top = 128, Width = 120, Text = Translate("common.move-down", "Move Down") };
        downLayerButton.Click += (_, _) => MoveLayer(1);

        var propertyPanel = new GroupBox
        {
            Left = 410,
            Top = 290,
            Width = 400,
            Height = 220,
            Text = Translate("derived-editor.selected-layer", "Selected Layer")
        };

        _opacityInput = CreateNumeric(propertyPanel, Translate("derived-editor.opacity", "Opacity"), 20, 30, 0, 100, 100);
        _scaleInput = CreateNumeric(propertyPanel, Translate("derived-editor.scale", "Scale %"), 20, 70, 10, 300, 100);
        _offsetXInput = CreateNumeric(propertyPanel, Translate("derived-editor.offset-x", "Offset X"), 20, 110, -256, 256, 0);
        _offsetYInput = CreateNumeric(propertyPanel, Translate("derived-editor.offset-y", "Offset Y"), 20, 150, -256, 256, 0);
        _rotationInput = CreateNumeric(propertyPanel, Translate("derived-editor.rotation", "Rotation"), 210, 30, -180, 180, 0);

        foreach (var numeric in new[] { _opacityInput, _scaleInput, _offsetXInput, _offsetYInput, _rotationInput })
        {
            numeric.ValueChanged += (_, _) => UpdateSelectedLayerFromControls();
        }

        var nameLabel = new Label { Left = 20, Top = 550, Width = 100, Text = Translate("preview.file-name", "File name") };
        _nameTextBox = new TextBox
        {
            Left = 100,
            Top = 546,
            Width = 360,
            Text = Translate("derived-editor.default-name", "{0} Overlay", _baseEntry.Label.ToFriendlyPathName())
        };

        var saveButton = new Button { Left = 500, Top = 544, Width = 140, Text = Translate("derived-editor.save", "Save Derived Icon") };
        saveButton.Click += (_, _) => SaveDerivedIcon();

        var closeButton = new Button { Left = 650, Top = 544, Width = 140, Text = Translate("common.close", "Close") };
        closeButton.Click += (_, _) => Close();

        Controls.Add(_previewPictureBox);
        Controls.Add(_layerListBox);
        Controls.Add(addLayerButton);
        Controls.Add(removeLayerButton);
        Controls.Add(upLayerButton);
        Controls.Add(downLayerButton);
        Controls.Add(propertyPanel);
        Controls.Add(nameLabel);
        Controls.Add(_nameTextBox);
        Controls.Add(saveButton);
        Controls.Add(closeButton);

        RefreshPreview();
    }

    private static NumericUpDown CreateNumeric(Control parent, string label, int left, int top, decimal min, decimal max, decimal value)
    {
        var caption = new Label { Left = left, Top = top + 4, Width = 70, Text = label };
        var input = new NumericUpDown
        {
            Left = left + 80,
            Top = top,
            Width = 80,
            Minimum = min,
            Maximum = max,
            Value = value
        };

        parent.Controls.Add(caption);
        parent.Controls.Add(input);
        return input;
    }

    private void AddLayer()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Translate("derived-editor.choose-overlay", "Choose overlay image"),
            Filter = Translate("file-dialog.supported-files", "Supported files|*.png;*.jpg;*.jpeg;*.ico;*.dll|All files|*.*")
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            using var bitmap = IconImportService.LoadImageForLayer(dialog.FileName);
            _layers.Add(new DerivedLayerModel
            {
                Name = Path.GetFileName(dialog.FileName),
                SourcePath = dialog.FileName,
                Bitmap = new Bitmap(bitmap),
                OpacityPercent = 100,
                ScalePercent = 100,
                OffsetX = 0,
                OffsetY = 0,
                RotationDegrees = 0
            });

            RefreshLayerList();
            _layerListBox.SelectedIndex = _layers.Count - 1;
            RefreshPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Translate("titles.derived-icon-editor", "Derived Icon Editor"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveLayer()
    {
        if (_layerListBox.SelectedIndex < 0)
        {
            return;
        }

        _layers[_layerListBox.SelectedIndex].Bitmap.Dispose();
        _layers.RemoveAt(_layerListBox.SelectedIndex);
        RefreshLayerList();
        RefreshPreview();
    }

    private void MoveLayer(int delta)
    {
        var index = _layerListBox.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        var newIndex = index + delta;
        if (newIndex < 0 || newIndex >= _layers.Count)
        {
            return;
        }

        (_layers[index], _layers[newIndex]) = (_layers[newIndex], _layers[index]);
        RefreshLayerList();
        _layerListBox.SelectedIndex = newIndex;
        RefreshPreview();
    }

    private void RefreshLayerList()
    {
        _layerListBox.Items.Clear();
        foreach (var layer in _layers)
        {
            _layerListBox.Items.Add(layer);
        }
    }

    private void SyncControlsFromSelection()
    {
        _syncingControls = true;
        try
        {
            if (_layerListBox.SelectedIndex < 0)
            {
                _opacityInput.Value = 100;
                _scaleInput.Value = 100;
                _offsetXInput.Value = 0;
                _offsetYInput.Value = 0;
                _rotationInput.Value = 0;
                return;
            }

            var layer = _layers[_layerListBox.SelectedIndex];
            _opacityInput.Value = layer.OpacityPercent;
            _scaleInput.Value = layer.ScalePercent;
            _offsetXInput.Value = layer.OffsetX;
            _offsetYInput.Value = layer.OffsetY;
            _rotationInput.Value = layer.RotationDegrees;
        }
        finally
        {
            _syncingControls = false;
        }
    }

    private void UpdateSelectedLayerFromControls()
    {
        if (_syncingControls || _layerListBox.SelectedIndex < 0)
        {
            return;
        }

        var layer = _layers[_layerListBox.SelectedIndex];
        layer.OpacityPercent = (int)_opacityInput.Value;
        layer.ScalePercent = (int)_scaleInput.Value;
        layer.OffsetX = (int)_offsetXInput.Value;
        layer.OffsetY = (int)_offsetYInput.Value;
        layer.RotationDegrees = (int)_rotationInput.Value;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _previewPictureBox.Image?.Dispose();
        _previewPictureBox.Image = ComposeBitmap();
    }

    private Bitmap ComposeBitmap()
    {
        using var baseBitmap = FolderIconCatalog.LoadBitmap(_baseEntry, 256);
        var composed = new Bitmap(256, 256, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(composed);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);
        graphics.DrawImage(baseBitmap, new Rectangle(0, 0, 256, 256));

        foreach (var layer in _layers)
        {
            DrawLayer(graphics, layer);
        }

        return composed;
    }

    private static void DrawLayer(Graphics graphics, DerivedLayerModel layer)
    {
        var width = layer.Bitmap.Width * layer.ScalePercent / 100f;
        var height = layer.Bitmap.Height * layer.ScalePercent / 100f;

        var state = graphics.Save();
        graphics.TranslateTransform(128 + layer.OffsetX, 128 + layer.OffsetY);
        graphics.RotateTransform(layer.RotationDegrees);
        graphics.TranslateTransform(-width / 2f, -height / 2f);

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix
        {
            Matrix33 = layer.OpacityPercent / 100f
        };
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        graphics.DrawImage(
            layer.Bitmap,
            new Rectangle(0, 0, (int)Math.Round(width), (int)Math.Round(height)),
            0,
            0,
            layer.Bitmap.Width,
            layer.Bitmap.Height,
            GraphicsUnit.Pixel,
            attributes);

        graphics.Restore(state);
    }

    private void SaveDerivedIcon()
    {
        var fileName = _nameTextBox.Text.ToFriendlyPathName();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            MessageBox.Show(
                this,
                Translate("derived-editor.enter-valid-file-name", "Enter a valid file name."),
                Translate("titles.derived-icon-editor", "Derived Icon Editor"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppPaths.EnsureDirectory(AppPaths.UserIconsDirectory);
        var outputPath = Path.Combine(AppPaths.UserIconsDirectory, $"{fileName}.ico");

        try
        {
            using var bitmap = ComposeBitmap();
            IconImportService.SaveBitmapAsIcon(bitmap, outputPath);
            MessageBox.Show(
                this,
                Translate("common.saved-to", "Saved to:{0}{1}", Environment.NewLine, outputPath),
                Translate("titles.derived-icon-editor", "Derived Icon Editor"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Translate("titles.derived-icon-editor", "Derived Icon Editor"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewPictureBox.Image?.Dispose();
            foreach (var layer in _layers)
            {
                layer.Bitmap.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private sealed class DerivedLayerModel
    {
        public required string Name { get; init; }
        public required string SourcePath { get; init; }
        public required Bitmap Bitmap { get; init; }
        public int OpacityPercent { get; set; }
        public int ScalePercent { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public int RotationDegrees { get; set; }

        public override string ToString() => Name;
    }
}
