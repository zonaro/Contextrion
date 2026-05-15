# Contextrion

A C# WinForms (.NET 9.0) desktop application that integrates into the Windows Explorer context menu to provide clipboard-to-file saving, folder icon customization, and an extensive suite of file/image manipulation tools.

## What it does

### Clipboard to File ("Paste Into File")

Right-click in any folder background to save clipboard content as a file:

- **Text** → `.txt`
- **Images** (PNG/JPEG format detected) → `.png` or `.jpg`
- **Files/Folders** copied in Explorer → `.zip` archive (preserves directory structure)
- **Audio** → `.wav` (with duration parsed from header)
- **Other binary formats** → `.bin`

A preview dialog lets you review content and choose the filename before saving. Default name: `Clipboard_YYYYMMDD_HHMMSS`.

### Folder Customization

Right-click a folder to access:

- **Customize Folder** — browse and apply icons from a built-in catalog (Windows 11, 10, 7/8 styles) or import your own `.ico`/`.dll`/`.png`/`.jpg` files
- **Derived Icon Editor** — compose composite icons by layering images with opacity, scale, offset, and rotation controls
- **Restore Default** — removes custom icon and restores Windows default
- **New Timestamp Folder** — creates a `YYYY/MM/DD` folder hierarchy inside the target

### File Tools (available on selected items)

**All files:**
- Rename to friendly URL (lowercase, removes accents, replaces special chars)
- Copy as Data URL (Base64 Data URI)
- Copy file path to clipboard
- Bulk rename with enumeration (pattern-based, e.g. `file (#)` → `file (1).ext`, `file (2).ext`)
- Copy file content to clipboard (text as text, images combined, audio as stream)

**Images (`.jpg`, `.jpeg`, `.png`):**
- Combine images vertically or horizontally
- Convert to grayscale (BT.601 weights)
- Apply watermark (text or image, 50% alpha, centered)
- Crop (from center, specify dimensions)
- Crop to circle (square crop + elliptical clipping)
- Resize (maintains aspect ratio)
- Invert colors (RGB channel inversion)
- Clean metadata (strips EXIF, preserves resolution)
- Optimize for web (resize to max dimension, configurable JPEG quality, flatten transparency)

**CSS/JS files:**
- Minify (removes comments, collapses whitespace)

**Folders:**
- Clean empty directories (recursive removal)

## Context Menu Setup

When you run the app without arguments, it opens a context menu management window:

- **Install Context Menu / Update Context Menu** — registers or refreshes all Explorer context menu entries for the current ClickOnce executable
- **Remove Context Menus** — removes only Contextrion's registry entries; it does not uninstall the ClickOnce application
- **Open Assets** — opens the user icons directory (`%LOCALAPPDATA%\Contextrion\FolderAssets\UserIcons`)
- **Import Icons** — bulk import `.ico`/`.dll`/`.png`/`.jpg` files into the icon catalog

The context menu updater also cleans up legacy registry entries from the old "ClipboardFiles" name.

## Internal Commands

| Argument | Description |
|---|---|
| *(none)* | Opens the context menu management UI |
| `--install` | Registers or refreshes context menu entries |
| `--uninstall` | Removes Contextrion context menu entries |
| `--paste "<directory>"` | Opens the filename preview and saves clipboard content |
| `--pick --folder "<dir>"` | Opens the folder icon picker |
| `--restore-folder --folder "<dir>"` | Restores default folder icon |
| `--new-timestamp-folder --folder "<dir>"` | Creates `YYYY/MM/DD` folder hierarchy |
| `--friendly-name` | Rename files to URL-friendly names |
| `--tobase64` | Copy file as Base64 Data URL |
| `--copy-path` | Copy file path to clipboard |
| `--enum` | Bulk rename with enumeration pattern |
| `--combine-vertical` | Stack selected images vertically |
| `--combine-horizontal` | Place selected images horizontally |
| `--clean-empty` | Remove empty subdirectories |
| `--copy-content` | Copy file content to clipboard |
| `--grayscale` | Convert images to grayscale |
| `--watermark` | Apply watermark to images |
| `--crop` | Crop images (prompts for size) |
| `--circle` | Crop images to circle |
| `--resize` | Resize images (prompts for dimensions) |
| `--invert-color` | Invert image colors |
| `--clean-metadata` | Strip EXIF metadata from images |
| `--minify` | Minify JS or CSS files |
| `--optimize` | Optimize images for web |

## Build

```powershell
dotnet build D:\GIT\Contextrion\Contextrion.slnx
```

## Localization

UI strings are available in English, Portuguese (`pt`), and Brazilian Portuguese (`pt-BR`). Localization files live in `Contextrion/Localization/` and use JSON with dot-notation keys.

## Dependencies

- **.NET 9.0 Windows** (WinForms)
- **InnerLibs** — shared library (`D:\GIT\InnerLibs\InnerLibsCommon`) providing extension methods for image operations, string manipulation, file type detection, and directory utilities
