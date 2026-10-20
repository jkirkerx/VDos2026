# vDos 2026

vDos 2026 runs legacy DOS business applications on Windows. This repository contains the vDos C++ application source and its required SDL and FreeType dependencies, maintained by [Jim Kirker](https://jkirkerx.com).

## Buy the packaged edition

[Buy vDos 2026 on the Microsoft Store](https://apps.microsoft.com/detail/9n122dmjrsn8?hl=en-US&gl=US).

The Store edition provides the packaged application and configuration interface. This source repository contains the vDos application; it does not include the installer, configurator, print monitor, or Windows notification helper projects.

## Latest memory fix: runaway REP string operations

The October 8, 2026 fix addresses intermittent memory corruption and crashes observed while running FoxPro/AccountMate, including during memo entry, scrolling, and nested screens.

The CPU execution loop decrements its cycle counter before executing an instruction, so the last instruction in a time slice can run with zero cycles remaining. The REP string-operation handler previously clamped its iteration count to that zero value. Its `do ... while (--count)` loops then underflowed the unsigned counter to `0xFFFFFFFF`, allowing a copy or fill to continue far beyond its intended buffer. Depending on direction and mappings, this could overwrite application data or page tables, fault on an unmapped page, or stall.

In `VDosApp/src/cpu/core_normal/string.h`, the handler now uses at least one available cycle:

```cpp
Bits cyclesAvail = CPU_Cycles > 0 ? CPU_Cycles : 1;
```

The clamp uses this positive value, and remaining work resumes through the existing instruction-restart path. This prevents a zero-count pass from entering those loops.

The project notes record an A/B test with the final REPSTALL reproducer: the pre-fix build stalled after approximately 13,000 REP operations, while the fixed Release build completed 300,000 operations with `OK`. These are recorded development results, not a claim that every production compatibility issue has been resolved.

Earlier paging corrections are also retained: CR3 reloads flush cached translations, paging-state changes invalidate caches, INVLPG invalidates translations, and accesses crossing 4 KB page boundaries translate both pages. Those fixes are separate from the REP counter-underflow bug.

## Build from source

Open `vDos.sln` in Visual Studio with the Desktop development with C++ workload, the v145 platform toolset, and Windows SDK 10.0.26100.0. Build **Release | Win32**, or run from a Visual Studio developer shell:

```powershell
msbuild vDos.vcxproj /p:Configuration=Release /p:Platform=Win32
```

The executable is written to `Release/vDos.exe`. The project copies `SDL.dll` and the default `config.txt` and `autoexec.txt` into the output folder when needed. Review those configuration files and set the working directory and DOS application commands for your installation.

## Source layout

- `VDosApp/src/` and `VDosApp/include/`: emulator source and headers.
- `VDosApp/Embedded/`: default runtime configuration files.
- `VDosApp/bin/SDL.dll`: SDL runtime.
- `VDosApp/SDL-1.2.15/` and `VDosApp/ft255/`: bundled build dependencies.
- `VDosApp/font/`: font resources.
- `vDos.vcxproj`, `vDos.vcxproj.filters`, and `vDos.sln`: C++ build files.

## License

See [COPYING](COPYING) for the GNU General Public License. Bundled dependencies retain their respective license notices.
