namespace Contextrion;

internal sealed class FolderIconPickerForm : Form
{
    private const int SearchDebounceDelayMs = 300;
    private const int ThumbnailSize = 48;
    private const int PreviewSize = 256;
    private const int VisibleRowPreload = 2;

    private readonly string _folderPath;
    private readonly ComboBox _categoryComboBox;
    private readonly TextBox _searchTextBox;
    private readonly VisibleRangeListView _iconListView;
    private readonly PictureBox _previewPictureBox;
    private readonly Label _selectionLabel;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _searchDebounceTimer;
    private readonly Dictionary<string, string> _categoryFilterMap = [];
    private List<FolderIconEntry> _allEntries = [];
    private List<FolderIconEntry> _filteredEntries = [];
    private readonly Dictionary<string, int> _thumbnailIndices = [];
    private readonly HashSet<string> _queuedThumbnailKeys = [];
    private readonly HashSet<string> _loadedThumbnailKeys = [];
    private CancellationTokenSource? _entriesLoadCancellation;
    private CancellationTokenSource? _filterCancellation;
    private CancellationTokenSource? _thumbnailCancellation;
    private CancellationTokenSource? _previewCancellation;
    private bool _isInitializing;
    private nint _nativeImageListHandle;
    private int _placeholderImageIndex;

    public FolderIconPickerForm(string folderPath)
    {
        _folderPath = folderPath;

        Text = Translate("folder-picker.title", "Customize Folder - {0}", Path.GetFileName(folderPath));
        Icon = AppIconProvider.GetIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);
        ClientSize = new Size(1080, 720);

        var categoryLabel = new Label { Left = 16, Top = 18, Width = 70, Text = Translate("folder-picker.category", "Category") };
        _categoryComboBox = new ComboBox
        {
            Left = 90,
            Top = 14,
            Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _categoryComboBox.SelectedIndexChanged += async (_, _) =>
        {
            if (_isInitializing)
            {
                return;
            }

            await ApplyFiltersAsync();
        };

        var searchLabel = new Label { Left = 360, Top = 18, Width = 50, Text = Translate("common.search", "Search") };
        _searchTextBox = new TextBox { Left = 414, Top = 14, Width = 300 };
        _searchTextBox.TextChanged += (_, _) =>
        {
            if (_isInitializing)
            {
                return;
            }

            QueueSearch();
        };

        var importButton = new Button { Left = 732, Top = 12, Width = 110, Text = Translate("import.icons", "Import Icons") };
        importButton.Click += (_, _) => ImportIcons();

        var editorButton = new Button { Left = 850, Top = 12, Width = 110, Text = Translate("folder-picker.open-editor", "Open Editor") };
        editorButton.Click += (_, _) => OpenEditor();

        var installFolderButton = new Button { Left = 968, Top = 12, Width = 96, Text = Translate("common.open-assets", "Open Assets") };
        installFolderButton.Click += (_, _) => OpenAssetsFolder();

        _iconListView = new VisibleRangeListView
        {
            Left = 16,
            Top = 52,
            Width = 680,
            Height = 610,
            View = View.LargeIcon,
            MultiSelect = false,
            HideSelection = false
        };
        _iconListView.SelectedIndexChanged += async (_, _) => await UpdatePreviewAsync();
        _iconListView.DoubleClick += (_, _) => ApplyCurrentSelection();
        _iconListView.VisibleRangeChanged += (_, _) => QueueVisibleThumbnails();
        _iconListView.HandleCreated += (_, _) => AttachNativeImageList();

        var previewGroup = new GroupBox
        {
            Left = 712,
            Top = 52,
            Width = 352,
            Height = 440,
            Text = Translate("preview.group", "Preview")
        };

        _previewPictureBox = new PictureBox
        {
            Left = 24,
            Top = 36,
            Width = 300,
            Height = 300,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _selectionLabel = new Label
        {
            Left = 24,
            Top = 352,
            Width = 300,
            Height = 64,
            Text = Translate("folder-picker.select-preview", "Select an icon to preview it.")
        };

        previewGroup.Controls.Add(_previewPictureBox);
        previewGroup.Controls.Add(_selectionLabel);

        _statusLabel = new Label
        {
            Left = 16,
            Top = 668,
            Width = 680,
            Height = 24,
            Text = Translate("folder-picker.loading-icons", "Loading icons...")
        };

        var applyButton = new Button { Left = 808, Top = 520, Width = 180, Text = Translate("titles.customize-folder", "Customize Folder") };
        applyButton.Click += (_, _) => ApplyCurrentSelection();

        var restoreButton = new Button { Left = 808, Top = 564, Width = 180, Text = Translate("titles.restore-default", "Restore Default") };
        restoreButton.Click += (_, _) => RestoreDefault();

        var cancelButton = new Button { Left = 808, Top = 608, Width = 180, Text = Translate("common.close", "Close") };
        cancelButton.Click += (_, _) => Close();

        Controls.Add(categoryLabel);
        Controls.Add(_categoryComboBox);
        Controls.Add(searchLabel);
        Controls.Add(_searchTextBox);
        Controls.Add(importButton);
        Controls.Add(editorButton);
        Controls.Add(installFolderButton);
        Controls.Add(_iconListView);
        Controls.Add(_statusLabel);
        Controls.Add(previewGroup);
        Controls.Add(applyButton);
        Controls.Add(restoreButton);
        Controls.Add(cancelButton);

        _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = SearchDebounceDelayMs };
        _searchDebounceTimer.Tick += async (_, _) =>
        {
            _searchDebounceTimer.Stop();
            await ApplyFiltersAsync();
        };

        Shown += async (_, _) => await InitializeDataAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchDebounceTimer.Dispose();
            CancelPendingWork();
            DestroyNativeImageList();
            _previewPictureBox.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task InitializeDataAsync()
    {
        _isInitializing = true;
        SetInteractionEnabled(false);
        SetStatus(Translate("folder-picker.loading-catalog", "Loading icon catalog..."));

        _entriesLoadCancellation?.Cancel();
        _entriesLoadCancellation?.Dispose();
        _entriesLoadCancellation = new CancellationTokenSource();
        var cancellationToken = _entriesLoadCancellation.Token;

        try
        {
            var entries = await Task.Run(() => FolderIconCatalog.LoadAll().ToList(), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _allEntries = entries;

            _categoryComboBox.Items.Clear();
            _categoryFilterMap.Clear();
            foreach (var category in FolderIconCatalog.GetCategoryOrder().Where(category => _allEntries.Any(entry => entry.Category == category)))
            {
                var displayName = FolderIconCatalog.GetCategoryDisplayName(category);
                _categoryFilterMap[displayName] = category;
                _categoryComboBox.Items.Add(displayName);
            }

            if (_categoryComboBox.Items.Count > 0)
            {
                _categoryComboBox.SelectedItem = FolderIconCatalog.GetCategoryDisplayName("Current Windows");
                if (_categoryComboBox.SelectedIndex < 0)
                {
                    _categoryComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isInitializing = false;
            SetInteractionEnabled(true);
        }

        await ApplyFiltersAsync();
    }

    private void QueueSearch()
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
        SetStatus(Translate("folder-picker.waiting-search", "Waiting for search..."));
    }

    private async Task ApplyFiltersAsync()
    {
        if (_isInitializing)
        {
            return;
        }

        _searchDebounceTimer.Stop();
        CancelThumbnailLoading();
        CancelPreviewLoading();

        var selectedEntry = GetSelectedEntry();
        var categorySelection = _categoryComboBox.SelectedItem?.ToString();
        var category = categorySelection is not null && _categoryFilterMap.TryGetValue(categorySelection, out var rawCategory)
            ? rawCategory
            : categorySelection;
        var query = _searchTextBox.Text;

        _filterCancellation?.Cancel();
        _filterCancellation?.Dispose();
        _filterCancellation = new CancellationTokenSource();
        var cancellationToken = _filterCancellation.Token;

        SetStatus(Translate("folder-picker.filtering", "Filtering icons..."));

        List<FolderIconEntry> filteredEntries;
        try
        {
            filteredEntries = await Task.Run(() => FilterEntries(_allEntries, category, query), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || IsDisposed)
        {
            return;
        }

        BindFilteredEntries(filteredEntries, selectedEntry);
        QueueVisibleThumbnails();
        await UpdatePreviewAsync();
    }

    private void BindFilteredEntries(List<FolderIconEntry> entries, FolderIconEntry? previouslySelectedEntry)
    {
        _filteredEntries = entries;
        _thumbnailIndices.Clear();
        _queuedThumbnailKeys.Clear();
        _loadedThumbnailKeys.Clear();

        RecreateNativeImageList();

        _iconListView.BeginUpdate();
        _iconListView.Items.Clear();

        foreach (var entry in _filteredEntries)
        {
            _iconListView.Items.Add(new ListViewItem(entry.Label)
            {
                ImageIndex = _placeholderImageIndex,
                Tag = entry
            });
        }

        _iconListView.EndUpdate();

        if (previouslySelectedEntry is not null)
        {
            var selectedIndex = _filteredEntries.FindIndex(entry => entry == previouslySelectedEntry);
            if (selectedIndex >= 0)
            {
                _iconListView.Items[selectedIndex].Selected = true;
                _iconListView.EnsureVisible(selectedIndex);
            }
        }

        SetStatus(Translate("folder-picker.icons-found", "{0} icons found.", _filteredEntries.Count.ToString()));
    }

    private void QueueVisibleThumbnails()
    {
        if (IsDisposed || !_iconListView.IsHandleCreated || _filteredEntries.Count == 0)
        {
            return;
        }

        var visibleEntries = GetVisibleEntries();
        if (visibleEntries.Count == 0)
        {
            return;
        }

        var entriesToLoad = new List<FolderIconEntry>();
        foreach (var entry in visibleEntries)
        {
            var key = GetImageKey(entry);
            if (_loadedThumbnailKeys.Contains(key) || !_queuedThumbnailKeys.Add(key))
            {
                continue;
            }

            entriesToLoad.Add(entry);
        }

        if (entriesToLoad.Count == 0)
        {
            return;
        }

        _ = LoadVisibleThumbnailsAsync(entriesToLoad);
    }

    private List<FolderIconEntry> GetVisibleEntries()
    {
        var visibleEntries = new List<FolderIconEntry>();
        if (_iconListView.Items.Count == 0)
        {
            return visibleEntries;
        }

        var viewport = _iconListView.ClientRectangle;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return visibleEntries;
        }

        var rowHeight = Math.Max(1, ThumbnailSize + 24);
        var preloadViewport = Rectangle.FromLTRB(
            viewport.Left,
            viewport.Top,
            viewport.Right,
            viewport.Bottom + (VisibleRowPreload * rowHeight));

        for (var index = 0; index < _iconListView.Items.Count; index++)
        {
            var bounds = _iconListView.Items[index].Bounds;
            if (bounds.IntersectsWith(preloadViewport))
            {
                visibleEntries.Add(_filteredEntries[index]);
                continue;
            }

            if (visibleEntries.Count > 0 && bounds.Top > preloadViewport.Bottom)
            {
                break;
            }
        }

        return visibleEntries;
    }

    private async Task LoadVisibleThumbnailsAsync(IReadOnlyList<FolderIconEntry> entries)
    {
        _thumbnailCancellation ??= new CancellationTokenSource();
        var cancellationToken = _thumbnailCancellation.Token;

        try
        {
            await Parallel.ForEachAsync(
                entries,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2)
                },
                async (entry, token) =>
                {
                    var iconHandle = await Task.Run(() => LoadThumbnailIconHandle(entry), token);
                    if (token.IsCancellationRequested)
                    {
                        ReleaseIconHandle(iconHandle);
                        return;
                    }

                    TryBeginInvoke(() => ApplyThumbnail(entry, iconHandle));
                });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ApplyThumbnail(FolderIconEntry entry, nint iconHandle)
    {
        try
        {
            if (IsDisposed || _nativeImageListHandle == nint.Zero)
            {
                return;
            }

            var key = GetImageKey(entry);
            if (_loadedThumbnailKeys.Contains(key))
            {
                return;
            }

            var itemIndex = _filteredEntries.IndexOf(entry);
            if (itemIndex < 0 || itemIndex >= _iconListView.Items.Count)
            {
                return;
            }

            var imageIndex = iconHandle != nint.Zero
                ? NativeMethods.ImageList_ReplaceIcon(_nativeImageListHandle, -1, iconHandle)
                : _placeholderImageIndex;

            if (imageIndex < 0)
            {
                imageIndex = _placeholderImageIndex;
            }

            _thumbnailIndices[key] = imageIndex;
            _loadedThumbnailKeys.Add(key);
            _iconListView.Items[itemIndex].ImageIndex = imageIndex;
        }
        finally
        {
            _queuedThumbnailKeys.Remove(GetImageKey(entry));
            ReleaseIconHandle(iconHandle);
        }
    }

    private async Task UpdatePreviewAsync()
    {
        CancelPreviewLoading();

        if (_iconListView.SelectedIndices.Count == 0)
        {
            _previewPictureBox.Image?.Dispose();
            _previewPictureBox.Image = null;
            _selectionLabel.Text = Translate("folder-picker.select-preview", "Select an icon to preview it.");
            return;
        }

        var entry = _filteredEntries[_iconListView.SelectedIndices[0]];
        _selectionLabel.Text = Translate("folder-picker.loading-preview", "Loading preview...{0}{1}", Environment.NewLine, entry.Label);

        _previewCancellation = new CancellationTokenSource();
        var cancellationToken = _previewCancellation.Token;

        try
        {
            var previewBitmap = await Task.Run(() => FolderIconCatalog.LoadBitmap(entry, PreviewSize), cancellationToken);
            if (cancellationToken.IsCancellationRequested || IsDisposed)
            {
                previewBitmap.Dispose();
                return;
            }

            var oldImage = _previewPictureBox.Image;
            _previewPictureBox.Image = previewBitmap;
            oldImage?.Dispose();
            _selectionLabel.Text = Translate(
                "folder-picker.preview-details",
                "{0}{1}{2}",
                entry.Label,
                Environment.NewLine,
                FolderIconCatalog.GetCategoryDisplayName(entry.Category));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            _selectionLabel.Text = Translate("folder-picker.preview-unavailable", "{0}{1}Preview unavailable", entry.Label, Environment.NewLine);
        }
    }

    private FolderIconEntry? GetSelectedEntry()
    {
        if (_iconListView.SelectedIndices.Count == 0)
        {
            return null;
        }

        return _filteredEntries[_iconListView.SelectedIndices[0]];
    }

    private void ApplyCurrentSelection()
    {
        var entry = GetSelectedEntry();
        if (entry is null)
        {
            MessageBox.Show(
                this,
                Translate("folder-picker.select-first", "Select an icon first."),
                Translate("titles.customize-folder", "Customize Folder"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            FolderCustomizationService.ApplyIcon(_folderPath, entry);
            MessageBox.Show(
                this,
                Translate("folder-picker.updated", "Folder icon updated."),
                Translate("titles.customize-folder", "Customize Folder"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Translate("titles.customize-folder", "Customize Folder"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreDefault()
    {
        try
        {
            FolderCustomizationService.RestoreDefault(_folderPath);
            MessageBox.Show(
                this,
                Translate("folder-picker.restored", "The default folder icon was restored."),
                Translate("titles.customize-folder", "Customize Folder"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Translate("titles.customize-folder", "Customize Folder"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ImportIcons()
    {
        var result = IconImportService.ImportFromDialog(this);
        if (result.CopiedCount == 0 && result.ConvertedCount == 0 && result.FailedFiles.Count == 0)
        {
            return;
        }

        await InitializeDataAsync();

        var summary = Translate(
            "import.summary",
            "Imported: {0}\nConverted: {1}",
            result.CopiedCount.ToString(),
            result.ConvertedCount.ToString());
        if (result.FailedFiles.Count > 0)
        {
            summary += Translate(
                "import.summary.failed",
                "\nFailed: {0}",
                string.Join(", ", result.FailedFiles.Take(5)));
        }

        MessageBox.Show(this, summary, Translate("import.icons", "Import Icons"), MessageBoxButtons.OK, result.FailedFiles.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private async void OpenEditor()
    {
        var entry = GetSelectedEntry();
        if (entry is null)
        {
            MessageBox.Show(
                this,
                Translate("folder-picker.select-base-icon", "Select a base icon first."),
                Translate("folder-picker.open-editor", "Open Editor"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var editor = new DerivedIconEditorForm(entry);
        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            await InitializeDataAsync();
        }
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

    private static List<FolderIconEntry> FilterEntries(IEnumerable<FolderIconEntry> entries, string? category, string? query)
    {
        IEnumerable<FolderIconEntry> filtered = entries;

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(entry => string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        var tokens = TokenizeQuery(query);
        if (tokens.Count == 0)
        {
            return filtered.ToList();
        }

        return filtered
            .Where(entry => tokens.All(token =>
                entry.Label.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                entry.Category.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static List<string> TokenizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return query
            .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(part => part.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetImageKey(FolderIconEntry entry)
    {
        return $"{entry.Category}|{entry.ResourcePath}|{entry.ResourceIndex}";
    }

    private static nint LoadThumbnailIconHandle(FolderIconEntry entry)
    {
        if (BundledAssetCatalog.IsBundledIconPath(entry.ResourcePath) ||
            entry.ResourcePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            using var icon = FolderIconCatalog.LoadIcon(entry, ThumbnailSize);
            return icon is null ? nint.Zero : NativeMethods.CopyIcon(icon.Handle);
        }

        var handles = new IntPtr[1];
        var extracted = NativeMethods.PrivateExtractIcons(
            entry.ResourcePath,
            entry.ResourceIndex,
            ThumbnailSize,
            ThumbnailSize,
            handles,
            null,
            1,
            0);

        if (extracted > 0 && handles[0] != nint.Zero)
        {
            return handles[0];
        }

        using var fallbackIcon = FolderIconCatalog.LoadIcon(entry, ThumbnailSize);
        return fallbackIcon is null ? nint.Zero : NativeMethods.CopyIcon(fallbackIcon.Handle);
    }

    private static void ReleaseIconHandle(nint iconHandle)
    {
        if (iconHandle != nint.Zero)
        {
            NativeMethods.DestroyIcon(iconHandle);
        }
    }

    private void RecreateNativeImageList()
    {
        DestroyNativeImageList();
        _nativeImageListHandle = NativeMethods.ImageList_Create(
            ThumbnailSize,
            ThumbnailSize,
            NativeMethods.IlcColor32 | NativeMethods.IlcMask,
            Math.Max(64, _filteredEntries.Count + 1),
            64);

        var placeholderHandle = NativeMethods.CopyIcon(SystemIcons.Application.Handle);
        try
        {
            _placeholderImageIndex = NativeMethods.ImageList_ReplaceIcon(_nativeImageListHandle, -1, placeholderHandle);
        }
        finally
        {
            ReleaseIconHandle(placeholderHandle);
        }

        AttachNativeImageList();
    }

    private void AttachNativeImageList()
    {
        if (_nativeImageListHandle == nint.Zero || !_iconListView.IsHandleCreated)
        {
            return;
        }

        NativeMethods.SendMessage(
            _iconListView.Handle,
            NativeMethods.LvmSetImageList,
            (nint)NativeMethods.LvsilNormal,
            _nativeImageListHandle);
    }

    private void DestroyNativeImageList()
    {
        if (_nativeImageListHandle == nint.Zero)
        {
            return;
        }

        NativeMethods.ImageList_Destroy(_nativeImageListHandle);
        _nativeImageListHandle = nint.Zero;
        _placeholderImageIndex = 0;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        _categoryComboBox.Enabled = enabled;
        _searchTextBox.Enabled = enabled;
        _iconListView.Enabled = enabled;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void CancelPendingWork()
    {
        _entriesLoadCancellation?.Cancel();
        _entriesLoadCancellation?.Dispose();
        _entriesLoadCancellation = null;

        _filterCancellation?.Cancel();
        _filterCancellation?.Dispose();
        _filterCancellation = null;

        CancelThumbnailLoading();
        CancelPreviewLoading();
    }

    private void CancelThumbnailLoading()
    {
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;
        _queuedThumbnailKeys.Clear();
    }

    private void CancelPreviewLoading()
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = null;
    }

    private void TryBeginInvoke(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed class VisibleRangeListView : ListView
    {
        public event EventHandler? VisibleRangeChanged;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case NativeMethods.WmSize:
                case NativeMethods.WmVScroll:
                case NativeMethods.WmHScroll:
                case NativeMethods.WmMouseWheel:
                case NativeMethods.WmKeyUp:
                    VisibleRangeChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}