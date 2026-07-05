# SnapMark

SnapMark is a lightweight Windows screenshot tool with quick region capture, clipboard export, local saving, and basic annotations.

## Features

- System tray app with configurable global hotkey
- Region screenshot selection with live preview
- Copy screenshot to clipboard
- Save screenshots as PNG files
- Rectangle, ellipse, pen, and note annotations
- Editable annotation selection, movement, and resize handles

## Requirements

- Windows
- .NET 10 SDK for development

## Build

```powershell
dotnet build ScreenCaptureTool.slnx
```

## Test

```powershell
dotnet test ScreenCaptureTool.slnx
```

## Publish

```powershell
dotnet publish src/ScreenCaptureTool/ScreenCaptureTool.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o artifacts/win-x64
```

The published executable is written to:

```text
artifacts/win-x64/ScreenCaptureTool.exe
```
