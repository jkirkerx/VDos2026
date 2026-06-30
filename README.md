# vDos 2026

vDos 2026 is a maintained Windows build of vDos focused on running legacy DOS business applications and making network printing practical for modern customer environments.

The original vDos project provided the DOS compatibility layer. Printing support was historically weak and inconsistent, especially for network printers. This fork keeps the vDos application working while improving the raw TCP printing path used by printers and print servers that accept direct port 9100-style jobs.

## Why This Exists

This project exists because keeping legacy DOS business software alive on modern Windows should not require a decade of failed workarounds.

Over the years, solutions like VMware, DOSBox, and other compatibility layers were tested for real business use, especially around printing. They either failed outright, were too fragile, or could not reliably handle the kind of raw printer output that older DOS accounting and check-printing systems still depend on.

vDos 2026 is focused on the practical parts that matter in the field: launching the DOS application, preserving usable configuration files, supporting raw network printing, supporting direct LPT/COM device output, and packaging everything in an installer that can be deployed on customer machines.

Maintained by [Jim Kirker](https://jkirkerx.com), this fork is intended to save other people from having to rediscover the same fixes the hard way.

## Support This Project

If vDos 2026 saves you time or helps keep a legacy DOS application running, you can support ongoing maintenance through [GitHub Sponsors](https://github.com/sponsors/jkirkerx). Donations help cover testing time, packaging work, and continued compatibility fixes.

## Maintainer Credit

This vDos 2026 fork is maintained by [Jim Kirker](https://jkirkerx.com). The current work includes cleaning up the project structure, modernizing the Visual Studio/WiX build, packaging the installer, and making direct TCP network printing usable for real customer environments.

## Repository Layout

### Root Files

- `vDos.sln` - Visual Studio 2026 solution file.
- `vDos.vcxproj` - C++ project for the main vDos executable.
- `vDos.vcxproj.filters` - Visual Studio file grouping for the C++ project.
- `COPYING` - Original license file.
- `README.md` - This file.
- `.gitignore` - Keeps generated build output out of Git.

### `VDosApp/`

Main vDos application source and required runtime assets.

- `VDosApp/src/` - C++ source code for vDos.
- `VDosApp/include/` - Header files used by the vDos C++ project.
- `VDosApp/Embedded/` - Default `autoexec.txt` and `config.txt` files installed or copied beside the app.
- `VDosApp/bin/SDL.dll` - Required SDL runtime DLL for vDos.
- `VDosApp/SDL-1.2.15/` - SDL headers and x86 import libraries used by the build.
- `VDosApp/ft255/` - FreeType library files used by the build.
- `VDosApp/font/` - Font-related files used by vDos.

### `vDosPrintMonitor/`

Windows notification/helper application for vDos printing.

This is a .NET 8 Windows project. The installer publishes it as a self-contained `win-x86` executable so customer machines do not need to install the .NET 8 runtime separately.

Important files:

- `vDosPrintMonitor/vDosPrintMonitor.csproj`
- `vDosPrintMonitor/Program.cs`

Generated folders such as `bin/`, `obj/`, and `publish/` are intentionally ignored by Git.

### `vDosInstaller/`

WiX installer project for vDos 2026.

Important files:

- `vDosInstaller/vDosInstaller.wixproj` - Builds the app, publishes the print monitor, and creates the MSI.
- `vDosInstaller/Package.wxs` - WiX package definition.

The installer includes:

- `vDos.exe`
- `SDL.dll`
- default `autoexec.txt`
- default `config.txt`
- self-contained `vDosPrintMonitor.exe`
- Start Menu/Desktop shortcuts
- permissions allowing users to edit `autoexec.txt` and `config.txt`

Installer build output under `vDosInstaller/bin/` and `vDosInstaller/obj/` is ignored by Git.

## Printing

vDos 2026 supports direct raw TCP printing to network printers and print servers. Typical raw printing uses TCP port `9100`.

Example `config.txt` entries:

```text
LPT1 = TCP 192.168.1.154:9100
LPT2 = TCP 192.168.1.111:9101
LPT3 = TCP 192.168.1.111:9102
```

This avoids depending on Windows printer shares when direct network printing is the better fit.

## Build Notes

The project is currently set up for Visual Studio 2026:

- Platform toolset: `v145`
- Platform: `Win32`
- Main app output: `Debug/vDos.exe` or `Release/vDos.exe`
- Installer output: `vDosInstaller/bin/Release/vDos2026.msi`

Build the installer project to produce the full MSI package:

```powershell
msbuild vDosInstaller\vDosInstaller.wixproj /p:Configuration=Release /p:Platform=Win32
```

The installer project will build `vDos.vcxproj`, publish `vDosPrintMonitor` as self-contained `win-x86`, and then run WiX.

## Git Notes

Generated build output is intentionally excluded from GitHub. This keeps the repository small and source-focused.

Ignored examples:

- `Debug/`
- `Release/`
- `bin/`
- `obj/`
- `vDosInstaller/bin/`
- `vDosInstaller/obj/`
- `vDosPrintMonitor/bin/`
- `vDosPrintMonitor/obj/`
- `vDosPrintMonitor/publish/`
- `.vs/`
- `*.suo`, `*.pdb`, `*.obj`, `*.wixpdb`, `*.cab`

The built MSI should be treated as a release artifact, not normal source code.
