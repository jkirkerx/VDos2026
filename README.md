# vDos 2026 — Source Code

vDos 2026 runs legacy DOS business applications on Windows, with compatibility and memory-handling fixes maintained by [Jim Kirker](https://jkirkerx.com).

## Want to try vDos 2026?

**[Get vDos 2026 from the Microsoft Store](https://apps.microsoft.com/detail/9n122dmjrsn8?hl=en-US&gl=US)** to download and use the packaged application, including its configuration interface.

## Purpose of this repository

This repository publishes the vDos application source code to meet the source-sharing requirements of its open-source license. It is a source-code repository, not the distribution channel for the packaged product. To try or use vDos 2026, please download it from the Microsoft Store using the link above.

The source includes the C++ application, its SDL and FreeType dependencies, and Visual Studio build files. The installer, configurator, print monitor, and Windows notification helper projects are not included here.

## What is fixed?

### Runaway memory copies and intermittent crashes

The October 8, 2026 fix corrects a REP string-operation bug associated with intermittent memory corruption, stalls, and crashes observed in FoxPro/AccountMate during memo entry, scrolling, and nested screens.

At the end of a CPU time slice, the emulator could reduce a repeated copy or fill operation to zero iterations. Its loop counter then underflowed, allowing the operation to continue far beyond the intended buffer. That could overwrite application data or page tables, or reach unmapped memory.

The fix in `VDosApp/src/cpu/core_normal/string.h` ensures that each execution pass has at least one available iteration and defers the remaining work through the existing instruction-restart path.

In the recorded REPSTALL A/B test, the pre-fix build stalled after approximately 13,000 REP operations; the fixed Release build completed 300,000 operations with `OK`. This verifies the reproduced bug fix; it does not establish that every application compatibility issue is resolved.

### Protected-mode memory translation

The source also retains corrections to cached address translations and paging:

- CR3 reloads flush cached translations.
- Paging-state changes and INVLPG invalidate cached translations.
- Memory accesses crossing a 4 KB page boundary translate both pages correctly.

These corrections are separate from the REP loop-counter fix.

## Source and license

Application source is under `VDosApp/src/` and `VDosApp/include/`. Bundled dependencies, fonts, and default configuration files are also under `VDosApp/`; the Visual Studio C++ build files are at the repository root.

See [COPYING](COPYING) for the GNU General Public License. Bundled dependencies retain their respective license notices.
