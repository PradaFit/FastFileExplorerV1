# Fast File Explorer

A high-performance file explorer for Windows, built from scratch with speed as the primary focus. This application uses native Windows APIs and multi-threaded indexing to deliver near-instant file searches across your entire system.

## Features

### Core Functionality
- Native Win32 file enumeration for maximum speed
- Multi-threaded indexing with 64 parallel workers
- Real-time file search across all indexed drives
- Batch file renaming with pattern support
- Command palette for keyboard-driven navigation
- File preview panel for images and text files

### Performance
- Indexes 1M+ files in under 5 minutes
- Partitioned index for reduced lock contention
- Object pooling to minimize garbage collection
- Pre-allocated memory for large file systems

### Interface
- Modern dark theme interface
- Breadcrumb navigation bar
- Keyboard shortcuts for common operations
- Context menu integration
- Discord Rich Presence support

## System Requirements

- Windows 7 or later (64-bit)
- .NET Framework 4.8
- Minimum 4GB RAM (8GB recommended for large drives)

## Installation

1. Download the latest release from the Releases page
2. Extract the ZIP file to your preferred location
3. Run `Fastest_FileExplorer.exe`

No installation required. The application is fully portable.

## Building from Source

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.8 SDK

### Build Steps
```
git clone https://github.com/PradaFit/FastFileExplorerV1.git
cd FastFileExplorerV1
msbuild Fastest_FileExplorer.csproj /p:Configuration=Release /p:Platform=x64
```

The compiled executable will be in `bin\x64\Release\`.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Shift+P | Open command palette |
| Ctrl+F | Focus search box |
| Ctrl+F2 | Batch rename selected files |
| Ctrl+Shift+N | Create new folder |
| Ctrl+Shift+C | Copy current path |
| F5 | Refresh current directory |
| Alt+Left | Navigate back |
| Alt+Right | Navigate forward |
| Alt+Up | Navigate to parent folder |

## Command Palette

Press `Ctrl+Shift+P` to open the command palette. From here you can search and execute any available action. The palette supports fuzzy matching and displays keyboard shortcuts for each command.

## Batch Rename

Select multiple files and press `Ctrl+F2` to open the batch rename dialog. Available rename modes:

- Find and Replace (with regex support)
- Add Prefix
- Add Suffix
- Sequential Numbering
- Date Stamp
- Remove Characters
- Change Case

Use template variables like `{n}` for numbers, `{d}` for dates, and `{name}` for the original filename.

## Discord Integration

The application includes optional Discord Rich Presence support. To enable it:

1. Create an application at https://discord.com/developers/applications
2. Copy your Application ID
3. Replace the `DISCORD_CLIENT_ID` constant in `Form1.cs`
4. Upload your app icons to the Rich Presence Art Assets section

## Project Structure

```
Fastest_FileExplorer/
    Core/
        FileIndexer.cs          - Multi-threaded file indexing engine
        NativeFileEnumerator.cs - Win32 API file enumeration
        SearchEngine.cs         - Search query processing
        FileSystemCache.cs      - Directory caching layer
        PathSecurity.cs         - Path validation and sanitization
        DiscordRpcClient.cs     - Discord Rich Presence client
    UI/
        CommandPalette.cs       - Keyboard command interface
        BatchRenameDialog.cs    - Bulk file renaming
        TabManager.cs           - Multi-tab support
        BreadcrumbBar.cs        - Path navigation
        PreviewPanel.cs         - File preview
        Theme.cs                - Visual styling
    Form1.cs                    - Main application window
    Program.cs                  - Entry point
```

## Contributing

Contributions are welcome. Please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Known Issues

- Very large directories (100,000+ files) may take a moment to display
- Network drives are not indexed by default
- Some system folders are intentionally skipped during indexing

## License

This project is licensed under the PradaFit Open Source License. See the LICENSE file for details.

## Author

Developed by [PradaFit](https://github.com/PradaFit)

## Acknowledgments

- Windows API documentation from Microsoft
- The .NET community for performance optimization techniques

