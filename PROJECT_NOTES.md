# Project notes

## 2026-10-05: GitHub publication and memory change notes

Added `CHANGELOG.md` and a README memory-compatibility summary describing the
current CR3 reload, paging-state, instruction-fetch-cache, INVLPG, and 4 KB
boundary-access behavior. The notes explicitly retain the zero-CR3 compatibility
exception and distinguish the later INVLPG instruction from the 386's CR3 reload
mechanism. They do not claim complete 386 emulation or production crash resolution.

Validation repeated today: `tests/run-paging-memory.ps1` passed all 16,384 access
cases, missing-page checks, diagnostic safety, and translation-history checks
with zero failures. Release/Win32 built successfully. The first harness attempt
hit duplicate PATH/Path entries in the host process environment; normalizing the
child build environment resolved it without changing the source or test runner.
The harness does not cover CR3/INVLPG invalidation or CPU/IDT fault delivery.
Production remarks/Page Down behavior still needs confirmation.

## 2026-09-09: Sales-order page fault

Recorded on 2026-09-11 from the user-provided `VdosError.jpg` and
`vDos-pagefault.log`. Status: unresolved; root cause is not yet established.

### Evidence

- Crash timestamp: Wednesday, 2026-09-09 at 10:43:22.074, as recorded by the crashing machine (timezone not recorded).
- Screenshot: vDos 2026, S.T.T. Southland Tile Tool Mfg., Create Sales Order, customer 2500; visible subtotal 226.38. The exact action that triggered the fault is unknown.
- The dialog reports writing `C:\Program Files\vDos 2026\vDos-pagefault.log`.
- The supplied log is preserved verbatim as [the incident log](docs/incidents/2026-09-09-vDos-pagefault.txt). Original supplied path: `D:\Users\jkirkerx\Desktop\vDos-pagefault.log`.

| Diagnostic | Value |
| --- | --- |
| Fault | `table entry not present` |
| Linear address | `02973003` |
| CR3 | `00109000` |
| Page-directory offset (`pdAddr`) | `00000028` |
| Page-directory entry | `00252007` |
| Page-table entry address | `002525CC` |
| Page-table entry | `00000000` |
| EIP | `0010400C` |
| Memory | 33 MB total; 32 MB XMS; 0 MB EXT; 0 MB EMS |
| CPU state | `CR0=FFFFFFF1`, `CPL=0`, paging enabled |

### File activity and interpretation

The retained trace shows successful database/index opens and successful lock/unlock
operations under `M:\AMP`, including sales-order and inventory files. Temporary
files `TMP607A.DBF` through `TMP607D.DBF` and `TMP607B.IDX` were created successfully.
An `OPEN FAIL errno=2` for `44956287.TMP` was followed by a successful creation of
the same file. This sequence alone does not establish a fault in temporary-file handling.

The final recorded events are successful lock/unlock pairs on `SIS1\ICITM01.DBF`,
at positions 2147483533 and 2147483345. No failed lock appears in the retained trace.
The trace is a bounded history of 128 events, not a complete I/O history, and its
ticks are not wall-clock timestamps.

The screenshot and log agree: vDos stopped in the guest address-translation path
because the requested page-table entry was zero. The address is a guest linear
address; comparing it directly with configured physical/XMS memory does not prove
memory exhaustion. The log does not establish whether the missing mapping came
from the DOS application/runtime or an emulation defect, nor does it establish
database corruption or a network failure.

Relevant source: `LinToPhys2` and `WritePageFaultDiagnostics` in
`VDosApp/src/hardware/memory.cpp`; `DOS_WriteFileTrace` in
`VDosApp/src/dos/drives.cpp`.

### Follow-up

- Record the exact user action and whether the crash can be reproduced with the same order/items.
- Identify the deployed vDos build, DOS application/runtime version, and active configuration.
- Compare additional incidents for the same EIP, linear address, and preceding file events before choosing a fix.

## 2026-09-14: Paging investigation and boundary-access fix

Re-read the original desktop log and compared it with the memory and CPU code.
The log's page-table calculation is consistent: directory index 10, table index
371, byte offset 3. The directory entry locates the table at `00252000`, and
entry `002525CC` is zero. The existing failure path calls `E_Exit`; it does not
deliver a guest page-fault exception. The available log cannot establish whether
the guest expected to handle this fault or whether a pointer/mapping was corrupted.

Found and fixed a separate, reproducible defect in `Mem_Lodsw`, `Mem_Lodsd`,
`Mem_Stosw`, and `Mem_Stosd`: an access crossing a 4 KB linear page boundary
translated only the first page and then used contiguous physical bytes. With
noncontiguous mappings this reads unrelated data or writes into the wrong page.
Crossing accesses now translate each byte through the existing byte-access path.
Accesses contained within one page retain their existing fast path.

This is a confirmed emulation defect, **not yet a confirmed cause of the September
9 incident**. The recorded fault itself has page offset 3; earlier corruption
could be relevant, but the log does not establish that connection.

Diagnostic format 2 adds build time, segment bases, instruction bytes, stack
bytes, bytes around the fault, and neighboring page-table entries. Diagnostic
reads bypass the TLB and device handlers, check physical bounds, and mark missing
or unsupported mappings unavailable instead of raising another fault.

Validation: `tests/run-paging-memory.ps1` compiles the production access functions
with controlled page mappings. The pre-fix functions fail 22 assertions; the
fixed functions pass all 16,384 width/offset/paging combinations, missing-second-
page checks, and diagnostic-read safety checks. The sales-order workflow still
needs verification on the affected installation; no claim of incident resolution
is made from these isolated tests.

Release/Win32 build succeeded, producing `Release/vDos.exe`. Build warnings were
the size conversion in `dos_execute.cpp:123` and the FreeType `_sprintf` import
warning; neither is in the changed memory code.

## 2026-09-15: Recurrence with text-like page-table contents

Preserved the supplied cumulative log verbatim in
`docs/incidents/2026-09-15-vDos-pagefault.txt`. It contains September 9,
September 14 (14:55:48.094), and September 15 (07:17:20.739) incidents.
The two latest incidents report diagnostic format 2 and build Sep 14 2026
10:25:39. The earlier boundary-access fix has not resolved the observed crashes.

Both latest incidents stop at CS:EIP `0008:00008605`, instruction linear
`00932605`, with EDI `0008E550`, ESP `0009EA2C`, and ECX `00000800`.
Today's available instruction bytes begin `F3 A4` (REP MOVSB). Yesterday's
instruction bytes were unavailable, so the shared location is established,
not the bytes of yesterday's instruction.

Today's failing translation is linear `00101000`, CR3 `00109000`, PDE
`00103007`, PTE address `00103404`, PTE value `41445058` (present bit clear).
The adjacent dwords `45535646 41445058 05004554` contain the little-endian
ASCII sequence `FVSEXPDATE` followed by NUL and 05. This is strong evidence
that application-like data occupies memory currently referenced as a page
table. It does not establish which writer put it there or whether the directory
points to the wrong page. The stack cannot be read through the diagnostic
page-table walk either. Yesterday's fault also used the table at `00103000`,
but failed at linear `00103000` with a zero PTE.

The retained file trace shows successful opens and lock/unlock operations;
the temporary-file OPEN errno=2 is followed by successful creation. This trace
does not establish a network, lock, or database-file failure. Its final two
September 15 events are successful lock/unlock operations on ICITM01.DBF.
There is no process-start timestamp from which to calculate time to crash.

Source review: REP MOVSB can enter `Mem_rMovsb`, which already splits copies
at source and destination 4 KB boundaries under paging. `DoString` changes ECX
before copying and updates ESI/EDI after the copy returns; those registers in
a fatal log therefore cannot safely be interpreted as the exact failing byte
or the original copy length. The original September 9 fault has a different
EIP and is not proven to have this same cause.

Next diagnostic target: capture the active bulk-copy source, destination,
length, progress, and translated physical pages, and identify writes into the
active page-table page. A repair needs a reproducer or evidence identifying
the bad write/mapping; this incident alone does not justify a speculative
paging change. Exact action/order/item at failure remains unknown.

## 2026-09-15: Successful translation history (diagnostics 3)

Added a fixed 256-entry (6 KB) in-memory ring of successful page-table walks
in LinToPhys2. Each entry preserves the linear address, resulting mapping,
CR3, guest instruction address, PDE, PTE address, and PTE value. Fault logs
print these oldest first and safely reread each recorded physical PTE address,
marking changed values. There is no per-access disk output. TLB cache hits
are omitted, and the history survives cache invalidation with CR3 recorded
per entry. A successful walk means the existing present-bit checks passed;
it does not prove the mapping was semantically correct. CHANGED can also
reflect legitimate remapping or reuse; it is evidence to compare, not an
automatic corruption verdict. Older entries can be overwritten in the ring.

Validation: existing 16,384 access cases and diagnostic safety checks pass.
Added tests for bounded history, wraparound ordering, preservation of an
original present PTE after replacement with 41445058, changed-value output,
and absence of page-table mutation by the diagnostic reader. All pass.

## 2026-09-15: Timestamped exit reports (diagnostics 4)

Normal shell EXIT and accepted window-close events now save a diagnostic report
before SDL shutdown. Page faults use the same report writer with fault details.
Both are saved beside the executable with local date/time, milliseconds, and
process ID: vDos-exit-YYYYMMDD-HHMMSS-mmm-pPID.log or
vDos-pagefault-YYYYMMDD-HHMMSS-mmm-pPID.log. The old cumulative pagefault log
is no longer appended. Normal exit reports omit fault-only fields, preserve
successful-walk history, and include recent DOS/host file activity. An exit
write failure displays the attempted path. Forced process termination cannot
run the normal-exit hook. Shutdown can legitimately change/release mappings,
so CHANGED remains a comparison marker rather than proof of corruption.

Validation: paging/history tests pass; Release/Win32 build succeeds with the
existing dos_execute conversion and FreeType import warnings. An isolated
copy with autoexec.txt containing EXIT generated a timestamped diagnostics=4
normal-exit file and terminated successfully. Test artifacts are under
Debug/exit-diagnostics-smoke. The production AccountMate data was not used.

## 2026-09-15: Three screenshots of the exception dialog (two prior, undocumented incidents)

Recorded from three screenshots the user captured of the "vDos - Exception"
dialog; no accompanying log files were supplied for these three. Confirmed the
dialog's mechanism while reading the code: `E_Exit` formats its message and
`throw(buf)`s it (`VDosApp/src/misc/support.cpp:157`); `WinMain`'s
`catch (char * error)` (`VDosApp/src/gui/sdlmain.cpp:1257`) displays it in a
MessageBox titled "vDos - Exception" and the process then exits. The dialog
text is exactly the `E_Exit` message built in `LinToPhys2` — there is no
separate error path to account for.

### Incidents

| Date/time | Screen | Dialog text | Detail fields shown |
| --- | --- | --- | --- |
| 2026-09-02 14:06:58 | Create Sales Order, customer #0146, Ship Via WILL CALL, F.O.B. WC, P.O.# 1-0902-TRO, Actions menu on "Add Sales Order and Invoice Remark"; Non-taxable 813.60, Total 813.60 | `Page fault: table entry not present: 197b001` | none (short form) |
| 2026-09-04 09:16:54 | customer #0916, Actions menu on "Audit Entry"; items 06-4VPSET, 26-ML24, 26-ML48, 26-T12 | `Page fault: table entry not present: 2973001` | none (short form) |
| 2026-09-09 10:43:22 | Create Sales Order, customer #2500; items 06-CR4, SHIPPING; subtotal 226.38 | `Page fault: table entry not present: 2973003` | CR3 `00109000`, PDE `00252007`, PTE `00000000`, EIP `0010400C`, XMS 32 MB, log `C:\Program Files\vDos 2026\vDos-pagefault.log` |

The 2026-09-09 row matches the incident already recorded above (same CR3, PDE,
EIP) — this screenshot visually confirms that log, it is not a new incident.
The 2026-09-02 and 2026-09-04 dialogs are short-form, lacking the CR3/PDE/PTE/
EIP/XMS fields the 09-09 build already had, so both predate the build that
produced the 09-09 dialog (and predate diagnostic format 1). No log file is
available for either, and the exact build/version at those two dates is
unknown.

### Address analysis

Decoding each linear address as in the documented incidents (directory index
= addr>>22, table index = (addr>>12)&0x3ff):

- `02973001` (09-04): directory 10, table index 371 — the same page-table
  entry as the documented 09-09 fault `02973003` (directory 10, table index
  371); only the byte offset within the page differs (1 vs 3). The same
  linear page, `02973000`, faulted on two separate occasions five days apart.
- `0197b001` (09-02): directory 6, table index 891 — a different page-table
  entry from the other two, not previously documented.

This shows at least two distinct recurring trouble spots rather than one fixed
bad address, and strengthens the case that page `02973000` specifically is not
a one-off: it has now faulted twice. It does not establish what structure
normally maps to either address, what build ran on 09-02/09-04, or why the
entry was missing either time. All three incidents occurred during sales-order
entry actions for S.T.T. Southland Tile Tool Mfg., consistent with the working
hypothesis that this workflow triggers the fault, but the specific triggering
action is still not pinned down.

### Working hypothesis: demand paging, not corruption

`PAGING_SetDirBase` already carries the comment "Shoot me, Phar Lap is
frequently switching between 0 and the previous set value," indicating the
DOS extender under this application is a Phar Lap extender (286|DOS-Extender
or TNT). Those extenders are known to grow a process's mapped memory on
demand: the extender's own fault handler, hooked onto interrupt 14, commits a
new physical page and resumes the faulting instruction when the guest touches
an address beyond what is currently mapped. vDos does not deliver interrupt 14
into the guest at all — `LinToPhys2` treats a clear present bit as fatal and
throws through to `E_Exit` instead of invoking the guest's own handler. If
AccountMate's runtime relies on that demand-growth behavior (plausible for
order-entry workflows that allocate more workspace as line items are added),
every "table entry not present" here could be an entirely ordinary extender
page fault that vDos is failing to service, not memory corruption. This would
also fit why the 09-14 boundary-access fix in `Mem_Lodsw`/`Mem_Stosw` did not
stop the crashes, and would recast the 09-15 "text-like page-table contents"
finding as reading a stale/reused table page rather than evidence of a
stray writer. **This is a hypothesis, not yet confirmed** — it has not been
checked against AccountMate/Phar Lap documentation or tested by adding actual
fault delivery.

### Follow-up

- No new log files accompany these three screenshots; the newer timestamped
  `vDos-exit-*`/`vDos-pagefault-*` files, if any exist from 09-02 or 09-04,
  would let these be cross-checked with the same directory/table math.
- Determine what normally occupies linear page `02973000` during Create Sales
  Order entry, since it has now faulted twice.
- Evaluate the demand-paging hypothesis: check whether Phar Lap's documented
  fault-servicing behavior matches this pattern, and consider what it would
  take for vDos to deliver a real interrupt 14 (error code + CR2, guest IDT
  dispatch) to the extender's own handler instead of aborting, versus a
  narrower vDos-side fix (e.g., committing a fresh page on first touch).

## 2026-09-15: Guest #PF delivery implemented (diagnostics unchanged, unverified — not built)

Implemented the demand-paging fix from the hypothesis above: on a not-present
PDE/PTE, `LinToPhys2` (`VDosApp/src/hardware/memory.cpp`) now tries to raise a
real interrupt 14 into the guest before falling back to the existing
`E_Exit`. This reuses code already in the tree rather than adding new
infrastructure:

- `CPU_Interrupt`/`CPU_Exception` (`VDosApp/src/cpu/cpu.cpp`) already
  implement full real-mode and protected-mode interrupt/exception dispatch,
  including IDT-gate walking and privilege/stack-switch handling. This path
  is not dormant: `core_normal.cpp`'s `illegal_opcode` case already calls
  `CPU_Exception(6, 0)` (#UD) for any undecoded opcode, so pmode IDT dispatch
  is live, ordinary, already-exercised code, not something this change
  introduces untested.
- `CR2` (`paging.cr2`) is already wired to `MOV reg,CR2`
  (`CPU_GET_CRX`/`CPU_SET_CRX` in cpu.cpp) but was never populated by a real
  fault. It now is, immediately before delivery.
- `CPU_CHECK_COND` is compiled as `CPU_CHECK_IGNORE` (cpu.cpp:20) throughout
  this codebase's history — the extra descriptor/privilege validation it
  guards has always been disabled. That code is not touched or relied on
  here; delivery goes through the same `CPU_Interrupt` real/pmode branches
  #UD already uses.

New `DeliverPageFault(addr, write)` in memory.cpp: returns false (no state
changed, existing fatal path taken) unless `cpu.pmode` is set and the guest
has a nonzero-limit IDT installed, and unless fewer than two fault-delivery
attempts are already nested (a static depth counter, restored via an RAII
guard even when unwound through by an exception, so a fault that recurs
while being delivered — e.g. the guest's own stack page is also missing —
degrades to the original `E_Exit` instead of recursing without bound). On
success it sets `paging.cr2` and calls `CPU_Exception(EXCEPTION_PF, error)`
with a standard x86 error code (P=0, W/R from a new `write` parameter now
threaded through `LinToPhys`/`LinToPhys2` and all fourteen of their call
sites in memory.cpp, U/S from `cpu.cpl`).

`CPU_Exception` cannot itself unwind the guest instruction currently
executing — memory access happens many call frames below the CPU core's
fetch/decode loop (inside DOS/BIOS callback handlers, string-op helpers,
etc.), the same depth `E_Exit` already escapes from today via `throw(buf)`.
Delivery therefore throws a new empty marker, `GuestPageFault`
(`VDosApp/include/paging.h`), caught once in `RunPC()`'s inner loop
(`VDosApp/src/vDos.cpp`) around the `(*cpudecoder)()`/callback-handler calls;
the catch body is empty because `CPU_Interrupt` already retargeted CS:EIP
before the throw, so the loop simply decodes again from there.

### Companion fix: `DoString` committed ECX before the copy, not after

Making faults resumable exposed a real bug, independent of this feature: in
`VDosApp/src/cpu/core_normal/string.h`, every REP-prefixed load/store/move/
in/out (`type < R_SCASB`: OUTS, INS, MOVS, LODS, STOS) wrote the post-loop
value into `reg_ecx` *before* running the copy, while ESI/EDI were (and
still are) only written back *after* the loop/bulk copy finishes. Today this
is harmless because a fault during the copy always terminated vDos via
`E_Exit` before anything resumed. It stops being harmless the moment a fault
can be retried: a fault partway through, say, `Mem_rMovsb`, would leave ECX
already at its post-copy value while ESI/EDI were still at their pre-copy
value, so a retry would resume with an inconsistent count, understating how
much work remains and finishing early. For a REP MOVSB into a database
buffer, that is silent truncation, not a clean failure.

Fixed by deferring ECX's commit the same way ESI/EDI already defer theirs:
the cycle-budget bookkeeping that decides how much of the REP count to
attempt this pass is unchanged, but it now writes to local variables
(`commit_ecx`, `ecx_leftover`) instead of `reg_ecx` directly, and a single
line after the `switch` commits them to `reg_ecx` — reached only when the
switch completed without a fault. A fault now leaves ECX, ESI and EDI all at
their pre-instruction values together, so the whole instruction is safe to
fully re-execute from scratch once the guest's handler fixes the mapping.
(This also matches the 09-15 REP MOVSB recurrence above: that crash's
instruction class is exactly the one this bug could have corrupted on
retry, which is one more reason to trust the earlier "boundary-access fix
alone didn't stop it" finding rather than assume this alone fixes 09-09/
09-15 — it makes retrying *safe*, it does not by itself explain what caused
the original faults.) SCASx/CMPSx (`type >= R_SCASB`) already deferred their
own ECX writeback and needed no change.

### What was and was not done

Changed: `VDosApp/include/paging.h` (added `GuestPageFault`),
`VDosApp/src/hardware/memory.cpp` (`DeliverPageFault`, depth guard,
`LinToPhys`/`LinToPhys2` gain a `write` parameter, all 14 call sites
updated), `VDosApp/src/cpu/core_normal/string.h` (deferred ECX commit),
`VDosApp/src/vDos.cpp` (`try`/`catch (GuestPageFault&)` around the decode/
callback dispatch in `RunPC`). Diagnostics (`WritePageFaultDiagnostics`,
the translation-history ring) are unchanged and only still run on the
fatal-abort path, not on successful delivery, so a working fix should not
spam a log file for every ordinary demand-paged page-in.

`tests/paging-memory.cpp`'s `LinToPhys` stub was updated to accept and
record the new `write` parameter, with two new assertions that `Mem_Lodsw`/
`Mem_Lodsd` report reads and `Mem_Stosw`/`Mem_Stosd` report writes. That
covers the parameter plumbing for the functions the harness already
exercises; it does not and cannot cover `DeliverPageFault`, `CPU_Exception`,
the IDT dispatch, or the `string.h` change, since those need real CPU/IDT
state this lightweight harness doesn't model.

**Not done: this was not built or run.** `device_bash` (the shell on the
development machine) has been unavailable all session behind the
already-reported Windows-update mount issue, so neither
`tests/run-paging-memory.ps1` nor a Release build could be executed from
here. Everything above was written and reasoned through at the source
level and cross-checked against how `CPU_Interrupt`, `CR2`, and the #UD
path already behave elsewhere in this codebase, but none of it has
compiled, let alone run against AccountMate. Before this goes anywhere near
production:

- Run `tests/run-paging-memory.ps1` and confirm it still passes, including
  the two new read/write-classification assertions.
- Build Debug and Release and fix anything the compiler flags (the depth
  guard, the `GuestPageFault` catch, and the deferred-ECX rework are all new
  code paths that have not seen a compiler yet).
- Exercise the sales-order workflow under supervision. Three outcomes are
  possible, not just "fixed": (1) the crash stops, which supports the
  demand-paging hypothesis; (2) `vDos - Exception` still appears exactly as
  before, meaning `DeliverPageFault` declined (no pmode IDT, or the
  depth guard tripped) and the code fell back to today's behavior with no
  regression; (3) something new and worse happens — most plausibly the
  guest's own Phar Lap fault handler runs but can't resolve the fault either
  and the DOS extender reports its own error, or a hang if that handler
  loops. Any of (1)/(2) is consistent with this being a safe change; (3),
  especially a hang, would mean the IDT/gate assumptions above don't hold
  for this build and the feature should be reverted (the change is isolated
  to the five files listed above).
- Watch specifically for data problems after a recovered fault, not just
  whether the crash disappeared — the whole point of the `string.h` fix is
  to make sure a recovered REP MOVSB/STOS/etc. doesn't undercount, but that
  reasoning has not been exercised against the real extender's fault
  handler and retry timing.

### Build fixed; crash is not reproducible on the dev machine

The first Release/Win32 build attempt after this change failed with
`LNK1104: cannot open file 'Debug\vDos.exe'` — a locked output file from a
still-running `vDos.exe`/attached debugger session, not a compile error from
this change. Closing the stale process resolved it; the build now succeeds
and the user has it running under the Visual Studio debugger.

The user reports the crash has never reproduced on the development machine
at all — only on a separate Windows 7 32-bit machine (the one all the
screenshots and logs above came from). This has not been investigated as a
possible contributing factor (host OS/architecture affecting timing, or
some other difference between the two machines) and should not be assumed
irrelevant, but the immediate practical consequence is that neither this
session nor interactive debugging on the dev machine can force the fault
condition to actually exercise `DeliverPageFault`/`GuestPageFault` end to
end. `tests/run-paging-memory.ps1` has not yet been confirmed to run either.
Verification requires either reproducing on a non-production Windows 7
32-bit environment, or a supervised trial directly on the affected machine
with a rollback plan (keep the prior `vDos.exe` on hand, back up data
first).

## 2026-09-15: The demand-paging hypothesis is probably wrong, per vDos's own author — and a real crash risk in the fix it motivated

User chose to hold off deploying and dig further rather than trial the
09-15 build on the production machine. Two things came out of that.

### The extender likely never had a #PF handler at all

vDos's own SourceForge discussion forum has a thread titled "Page fault:
table entry not present" (the same message this project has been chasing).
The vDos author, Jos Schaars, explains it directly for a same-vendor case
(FoxProX under PharLap, not this AccountMate/Clipper installation, but the
same extender family): "A directory table entry is referring to a page
table entry that hasn't the present bit set. That should generate a 0x0e
exception, but PharLap/FoxProX doesn't install a handler for this." He
also states memory paging in protected mode was never finished in vDos —
this exact fatal message was added in 2014.07.18 specifically to detect
that unsupported case, not as a general safety net.

This weakens the 09-15 working hypothesis above (that AccountMate's extender
does legitimate on-demand page commits vDos simply fails to service). Two
things point the same direction: the extender family involved is documented,
by vDos's own author, as typically not handling interrupt 14 at all; and
separately, the incident logs show a small, fixed memory footprint (33 MB
total: 32 MB XMS, 0 MB EXT) rather than a large address space being grown
over time, which is more consistent with paging used only for flat 32-bit
address translation over a statically-sized, pre-committed pool than with
real demand-paged virtual memory. Under that reading, a not-present entry
during normal operation is unexpected on its own terms — i.e. more likely a
genuine bug (corruption, or a vDos-side translation defect) than routine
extender behavior — which is closer to the original 09-14/09-15 line of
investigation than to the demand-paging theory. **Not confirmed either** —
this AccountMate/Clipper installation's specific extender (likely reached
via Clipper's EXOSPACE/Phar Lap linkage, not confirmed) has not been shown
to behave exactly like the FoxProX case Schaars was describing.

### The fix's guard was too loose: a missing #PF handler could have caused a hard stack-overflow crash

`DeliverPageFault` (memory.cpp) only checked that the guest had *some*
protected-mode IDT installed (`cpu.idt.GetLimit() != 0`) before calling
`CPU_Exception(EXCEPTION_PF, ...)`. `CPU_Interrupt` (cpu.cpp), when it can't
find a descriptor for the vector it's asked to deliver, escalates by calling
`CPU_Exception(EXCEPTION_GP, ...)` on that same not-found path — a direct,
un-guarded recursive call into `CPU_Interrupt` again, outside
`LinToPhys2`/`DeliverPageFault` entirely, so the depth guard there never
sees it. If interrupt 14's IDT slot is missing (per Schaars, plausible or
likely here) **and** interrupt 13 (#GP)'s slot is also missing, that
recursion has no bound: `CPU_Interrupt(13)` → descriptor not found →
`CPU_Exception(13)` → `CPU_Interrupt(13)` → ... until the native call stack
overflows. That is a hard crash with no diagnostics at all — worse than
today's `E_Exit` dialog, and exactly the kind of regression this project
cannot afford to ship un-caught.

Fixed: `DeliverPageFault` now calls `cpu.idt.GetDescriptor(EXCEPTION_PF<<3,
gate)` itself and checks the gate's present bit *before* calling
`CPU_Exception`, so it only ever attempts delivery when interrupt 14
specifically has a real, present descriptor — guaranteeing `CPU_Interrupt`
takes one of its bounded paths (a normal gate dispatch, or its `default:
E_Exit(...)` for a garbage gate type) rather than the unbounded
not-found-escalates-to-GP recursion. If, as Schaars' account suggests, this
extender family has no interrupt 14 handler at all, `DeliverPageFault` will
now simply and safely return false every time, and behavior for this
specific fault stays exactly what it is today (`E_Exit`, diagnostics
written, dialog shown) — no worse, and the redirect-on-success behavior
stays available for the case where a handler does turn out to be present.
(Note #GP itself, and every other vector `CPU_Interrupt` might be asked to
deliver, still has the original unbounded not-found-escalates-to-GP
recursion as pre-existing behavior in cpu.cpp; that was true before this
change and is not something this fix touches or introduces — it's flagged
here only because chasing this exact failure mode is what surfaced it.)

### Net effect

Given the above, this fix should now be assumed *more* likely to be a safe
no-op for the original "table/directory entry not present" crash than a
working recovery path — worth keeping in place (it does no harm and helps
if a handler is ever present), but the search for what actually makes the
mapping go missing should continue on its own, independent of whether this
fix ever gets a supervised trial.

### Follow-up

- Revisit the 09-15 "text-like page-table contents" finding (`FVSEXPDATE`
  ASCII sitting where a PTE was expected) as the more likely line of
  investigation: something writing application data into memory the guest
  is using as a page table, rather than the guest legitimately never having
  mapped it.
- Try to confirm what extended-memory linkage this specific AccountMate/
  Clipper build actually uses (EXOSPACE, Phar Lap linker, or something
  else) rather than assuming it matches the FoxProX case Schaars described.
- If a non-production Windows 7 32-bit environment becomes available,
  confirming whether `cpu.idt.GetDescriptor(EXCEPTION_PF<<3, gate)` actually
  succeeds or fails for this application would directly settle whether the
  #PF-delivery path is live at all here, independent of whether the crash
  itself reproduces.

## 2026-09-15: Decision to deploy to the production machine for user testing

After the guard fix above, the user decided to deploy this build to the
Windows 7 32-bit production machine and have the end user there exercise it,
rather than continue digging from source first. This is a live production
accounting system, so record the state going in and what a returned result
means:

- Current build status: compiles and links (Debug, Win32) as of this
  session; the earlier `LNK1104` was a locked `vDos.exe` from a running
  process, not a compile error, and was resolved by closing it.
  `tests/run-paging-memory.ps1` has still not been confirmed to run.
- Nothing in this change has been exercised against a real occurrence of
  the fault. The working expectation, per the 09-15 entries above, is that
  `DeliverPageFault` most likely declines (no interrupt-14 handler in this
  extender family) and behavior is unchanged from before this session's
  changes — the `string.h` ECX-ordering fix and the `Mem_Lods*`/`Mem_Stos*`
  boundary fix (09-14) are the only parts with a plausible chance of
  changing observed behavior on their own, and neither directly explains
  the original fault.
- Three outcomes remain possible on a real trial, as noted earlier: no
  crash, the same `vDos - Exception` dialog as before (safe fallback, no
  regression), or something new (most concerning: a hang, if some code path
  not covered by the guard fix still recurses; less concerning: the
  extender's own #GP or generic handler runs and produces different,
  extender-owned output). A hang specifically should be treated as a bug in
  this session's change and reported back rather than worked around.
- Rollback: keep the prior production `vDos.exe` renamed and on hand
  (e.g. `vDos.exe.bak`) before replacing it, and back up the AccountMate
  data directory first. Restoring is: stop vDos, put the old exe back,
  restart.

## 2026-09-15: Debug instrumentation added ahead of the production trial

Before deploying to the Windows 7 machine, added diagnostics to
`memory.cpp` so the trial produces positive evidence either way, not just
"did it crash." All additions are in `WritePageFaultDiagnostics`, so they
appear both in a fatal fault log and in the log `MEM_WriteExitDiagnostics`
writes on ordinary shell exit/window close — a clean run now still leaves
a record of whether the new path ever engaged.

- **`pmode=`, `idtBase=`, `idtLimit=`, `pfGatePresent=`, `faultDeliveryDepth=`**
  line, computed the same way `DeliverPageFault` itself checks
  (`cpu.idt.GetDescriptor(EXCEPTION_PF<<3, gate) && gate.saved.seg.p`).
  This directly answers the open question from the entry above: whether
  this AccountMate/Clipper build's extender ever installs an interrupt-14
  handler at all. `pfGatePresent=0` confirms the "declines, falls through
  to the old abort" expectation; `pfGatePresent=1` means delivery was live
  and the recovered-fault trace below is the record of what happened with
  it.
- **`RecoveredFaultTrace`**: a 64-entry in-memory ring buffer
  (`RecordRecoveredFault`/`WriteRecoveredFaults`), recorded once per
  successful `DeliverPageFault` call (linear address, CR3, faulting
  instruction pointer, error code), written out under "Page faults
  redirected into the guest this run: N" — N uncapped even though only
  the most recent 64 are listed. This is the only way to learn the guard
  fired zero, one, or many times during a run that didn't end in a crash;
  without it a clean run is silent about whether the new code path was
  ever reached.
- No new disk I/O per fault — the ring buffer is memory-only and flushed
  only at the same points the existing trace files are already written,
  so this doesn't add I/O to the guest's own hot path.
- Diagnostics format bumped to `diagnostics=5` for this addition (5th
  field added to the exit/fault log across this project's history).

What to read from the next trial's `vDos-pagefault-*.log` or
`vDos-exit-*.log`:

- `pfGatePresent=0` and 0 recovered faults, with no crash: extender still
  doesn't hook #PF; the crash didn't reproduce, or something outside this
  change's scope changed. Says nothing about `DeliverPageFault` because it
  never had anything to attempt.
- `pfGatePresent=0` and the same `table/directory entry not present`
  abort as before: expected, unchanged, safe fallback — confirms the
  guard is declining exactly where predicted rather than silently
  misbehaving.
- `pfGatePresent=1` and one or more recovered-fault entries, no crash:
  the new delivery path fired and the guest handled it — direct evidence
  for the fix actually working, not just "no crash observed."
- `pfGatePresent=1` and a crash or hang after one or more recovered
  entries: delivery fired but the guest's own handler didn't resolve the
  underlying condition (or resolved it into a different fault) — the
  recovered-fault trace's linear/CR3/instruction fields become the lead
  for what to look at next, and a hang here specifically points at the
  guard-fix's bounded-recursion assumption being wrong for this build.

## 2026-09-15: Likely root cause found — `XMS_Defrag` relocates locked handles

User asked to work toward actually fixing the underlying bug, not just
containing it. Revisited the 09-15 "text-like page-table contents" follow-up
(the `FVSEXPDATE` ASCII sitting where a PTE was expected) with that goal, by
reading how vDos hands out the extended memory the page tables live in
(`VDosApp/src/ints/xms.cpp`) rather than only the paging code itself.

### The mechanism

`PAGING_Init()` (`VDosApp/src/cpu/paging.cpp`) is empty — vDos never builds
or reserves memory for the guest's page directory/tables itself. They are
built by the DOS extender (Phar Lap, per the 09-15 SourceForge finding) at
some physical address inside whatever extended memory vDos gives it, and
vDos's own paging code (`LinToPhys2`) only ever reads them, trusting
`PAGING_GetDirBase()`/`paging.cr3` (set by the guest via `MOV CR3`) to still
point at valid, unmoved data.

This installation's config carries `TotEXTMB=0`, `TotXMSMB=32`, `TotEMSMB=0`
(from the 09-09 log) — the entire 32 MB extended-memory pool is provisioned
as XMS, so it can only be obtained through `XMS_Handler`
(`VDosApp/src/ints/xms.cpp`), the same INT 2Fh-style API any DOS program
also uses for its own buffers. A protected-mode extender that gets its
workspace this way has to *lock* the handle holding its page tables
(`XMS_LOCK_EXTENDED_MEMORY_BLOCK`) once it starts running in protected mode,
because its CR3 and every page-table entry are physical addresses now baked
into live CPU/page-table state — nothing else could keep those addresses
valid if that memory were ever allowed to move.

`XMS_Defrag` — called on every `XMS_AllocateMemory`, `XMS_ResizeMemory`, and
free-space query, so routinely, not rarely — compacts all in-use handles by
`memmove()`-ing them toward the low or high end of the pool. Its own
pre-existing comment says exactly what it does and doesn't handle:

    // XMS_Defrag totally neglects memory blocks being blocked (unmovable)
    // And why shouldn't it, accessing these blocks directly isn't possible in real mode?
    // What with protected mode???
    // Don't move locked blocks and find make thing more complicated, largest free block, etc???

That is the author — Jos Schaars — flagging this exact scenario and leaving
it unresolved. Checked against the actual code: the `locked` field is real
(`XMS_LockMemory`/`XMS_UnlockMemory` maintain it, and `XMS_FreeMemory`/
`XMS_ResizeMemory` both refuse to act on a locked handle) but `XMS_Defrag`'s
two compaction loops never check it before calling `memmove()` and
overwriting `handlePtr->addr`. A locked handle is exactly as movable as any
other, as far as `XMS_Defrag` is concerned.

### Why this fits the evidence

If the extender's page-table handle gets silently relocated by ordinary XMS
traffic from something else entirely — e.g. AccountMate/Clipper allocating
or resizing one of the many temporary per-transaction buffers the retained
file-activity trace already shows (`TMP607A.DBF` and friends) — the guest's
CR3 and cached page-table pointers keep referring to the *old* physical
location. vDos has no way to tell the guest its memory moved (real extended
memory never does this), so the guest keeps using stale addresses. Nothing
stops that vacated physical range from later being handed to yet another
XMS client and filled with ordinary data. A subsequent access through the
stale page-table pointer then reads whatever is there now — which is
exactly the `FVSEXPDATE`-looking ASCII application data found at PTE address
`00103404` in the 09-15 recurrence, not a not-present entry the extender
"never mapped." This also explains why the fault is intermittent and
workflow-correlated (it depends on the timing and interleaving of XMS
activity from two independent memory managers — the extender's and
AccountMate's own — that vDos never coordinates), rather than a fixed,
reproducible address or condition.

**Not yet confirmed by a live trace** — this is a strong, mechanistically
complete explanation consistent with every piece of evidence gathered so
far (the 09-15 SourceForge finding, the ASCII-in-a-PTE finding, the
workflow correlation, the author's own comment), but it has not been
observed actually happening. `XMS_WriteDiagnostics` (below) was added
specifically to get that direct observation on the next run, crash or not,
before touching `XMS_Defrag`'s actual behavior.

### Diagnostics added (no behavior change)

`VDosApp/src/ints/xms.cpp`: `RecordXMSRelocation`, called from both
compaction loops in `XMS_Defrag`, whenever a handle being moved has
`locked != 0`. `XMS_WriteDiagnostics(FILE*)` (declared in
`VDosApp/include/mem.h`, next to `MEM_WriteExitDiagnostics`, since nothing
outside `src/ints` previously included `xms.h`) writes every currently
in-use XMS handle (address, size, lock count) plus this 32-entry relocation
ring buffer, and is now called from `WritePageFaultDiagnostics`
(`memory.cpp`) — so it appears in both a fatal-fault log and a normal-exit
log, same as the recovered-fault trace added earlier today. Diagnostics
format bumped to `diagnostics=6`. `XMS_Defrag`'s actual compaction logic is
untouched — only the recording calls were added — so this carries the same
risk profile as the earlier read-only diagnostics, not the risk profile of
a behavior change.

What the next log tells us:

- One or more `RELOCATED handle=... lockedCount>0` entries: direct
  confirmation this mechanism fires in this installation. Whether or not
  that particular run crashed, this alone would justify fixing
  `XMS_Defrag` to skip locked handles.
- Zero relocation entries, crash still happens: this mechanism isn't it (or
  didn't fire before this particular crash) — worth checking whether the
  extender ever locks a handle at all (the `XMS handles in use` listing's
  `locked=` column shows that directly) versus using some other means to
  pin its page tables that this project hasn't identified yet.
- Zero relocation entries, no crash: inconclusive either way, same as any
  other clean run.

### Proposed fix (not yet implemented)

Make `XMS_Defrag`'s two compaction loops skip any handle with
`locked != 0` — leave it at its current address, and treat its footprint as
a fixed gap the free-space/packing arithmetic works around instead of
compacting through. This is a small, contained change to one function, but
it changes real allocator behavior (not just adds logging) and cannot be
built or exercised from here any more than earlier changes this session
could. Given this affects live production data, the plan is: get one
confirmed relocation-of-a-locked-handle observation from a real run first
(via the diagnostics just added), then implement and ship the fix
separately, rather than changing allocator behavior on inference alone.

### Follow-up

- If `XMS handles in use` ever shows zero locked handles even while the
  extender is clearly running in protected mode (`pmode=1` from the 09-15
  diagnostics line), that would mean the extender pins its page tables some
  other way, and this theory needs revisiting.
- `EMS_FreeKBs()` (declared in `xms.h`, referenced by `ems.cpp` per the
  earlier file listing) was not examined — `TotEMSMB=0` in every incident
  seen so far makes EMS inactive for this installation, so it was left out
  of scope rather than assumed safe in general.

## 2026-09-16: Status check — no crash since yesterday's deploy

User reports vDos has not crashed on the production Windows 7 machine since
deploying the guard-fix build yesterday (09-15). Encouraging, but on its
own this is weak evidence either way — see the 09-15 "Decision to deploy"
and "Debug instrumentation" entries: a clean run has always been consistent
with several different explanations (the crash simply hasn't reproduced
yet; `DeliverPageFault` declined and nothing changed; or it fired and
worked). It does not by itself confirm or rule out the `XMS_Defrag`
relocation theory either, since the XMS diagnostics (`diagnostics=6`) were
finished after the 09-15 "Decision to deploy" recommendation — it is not
yet established whether the binary now running in production was built
before or after that instrumentation landed.

### Open question

Which `diagnostics=N` the running build reports (visible in the header
line of any `vDos-exit-*.log` or `vDos-pagefault-*.log`, or determinable by
rebuilding from current source and comparing) determines what, if
anything, can be read from a clean run so far:

- If the deployed build predates `diagnostics=5`/`6`, no exit log it
  produces will have the recovered-fault trace, the `pfGatePresent` line,
  or the XMS handle/relocation listing — "no crash" is all there is to go
  on, and confirming the `XMS_Defrag` theory still requires a rebuild and
  redeploy with current source.
- If it already includes `diagnostics=6`, the next normal exit (closing
  vDos, not just a crash) will already have written a `vDos-exit-*.log`
  with the full picture — XMS handles in use, whether any were locked, and
  whether any locked handle was ever relocated — regardless of whether a
  crash ever happens.

### Follow-up

- Confirm which build is actually deployed and, if it's not already
  `diagnostics=6`, whether to rebuild/redeploy so the next normal exit
  produces a log worth reading.
- Ask whether vDos has been closed/restarted at all since the 09-15
  deploy — `MEM_WriteExitDiagnostics` only writes a log on a normal
  exit (shell EXIT or window close), not while vDos keeps running — and if
  any `vDos-exit-*.log`/`vDos-pagefault-*.log` files already exist on the
  production machine, get a copy to read directly.

## 2026-10-01: 50 exit logs (09-23 to 09-29) — the guest #PF handler fired once, at the same page, and killed AccountMate

User reports about 10 days with no crashes after the 09-15 deploy, then many crashes; the end user says pressing Page Down triggers it. The user copied 50 `vDos-exit-*.log` files (09-23 to 09-29) into the repo root. No `vDos-pagefault-*.log` was supplied, and `docs/incidents/2026-09-28-vDos-pagefault.txt` is the old cumulative log (its newest entry is 09-15 12:38).

Findings:

- All 50 logs report `diagnostics=6 build=Sep 15 2026 13:55:34` and `pfGatePresent=1`. **The extender does install a #PF gate**, contrary to the FoxProX expectation. The application is FoxPro 2.6 extended (`AMP.EXE` + `FOXDX260.ESL`, .CDX/.FPT files), not Clipper.
- 49 logs show 0 redirected faults and an orderly AccountMate logout: tables closed in descending fd order, an ARPAS01 session-record lock, the APP is closed, then AMP.EXE exits. These are normal quits.
- `vDos-exit-20260928-104142-344-p15976.log` is the exception: `RECOVERED linear=02973000 cr3=00109000 instruction=00AA400C error=00` (supervisor read, not present). It is followed by every handle (fd 8 to 53, including the TMP files) closing in ascending order within one tick, which is DOS process-termination cleanup. AMP.EXE's own close is not in the retained tail. The guest's handler therefore terminated FoxPro rather than resolving the fault. This is the "delivery fired, guest died" outcome.
- **The same page faulted on 09-04 (02973001) and 09-09 (02973003, EIP 0010400C in CS=000C)**. The walk histories show instruction 00AA400C/00AA4005 routinely reading 0292xxxx to 0293xxxx, so this is very likely the same FoxPro routine every time.
- Directories 8, 9 and A (linear 0x02000000 to 0x02BFFFFF) map with a constant linear-minus-physical offset of 0x00873000 (thousands of walks). 02973000 - 0x873000 = **0x02100000 = 34603008 = exactly the end of emulated memory** (totalBytes). The faulting read is 0 to 3 bytes past the last byte of RAM. The PTE is legitimately empty because no memory exists there. This is not corruption, and it is not the XMS_Defrag mechanism (0 locked relocations in all logs; the XMS handle list is empty at exit, so the theory is neither confirmed nor ruled out for the 09-15 ASCII-PTE incident).
- Interpretation: FoxPro takes all available XMS, so its heap ends at the top of memory. Routine 00AA400C reads just past the final block, where nothing is mapped. Page Down (browse/scroll caching more records) plausibly fills the heap to the top. Growing data makes this more likely. Not confirmed: what the routine is, and whether real hardware would also fault.
- Before 09-15, this fault showed the vDos Exception dialog. Since 09-15, it is delivered to FoxPro's handler, which aborts AMP.EXE (what the user now sees is unknown: an error message, a return to DOS, or the window closing).

Open: logs from 09-30 and 10-01 (the "lots of crashes"), any `vDos-pagefault-*.log`, what the screen shows at the crash, and the Windows Application event log (vDos.exe faults would leave no vDos log). Candidate mitigations, all untested: (1) a CONFIG.FP `MEMLIMIT` so FoxPro's heap does not reach the top of XMS; (2) more XMS (likely just moves the top if FoxPro takes everything); (3) a vDos-side fix so that a not-present *read* just past the top of physical memory returns zeros instead of faulting.

### 2026-10-01 addendum: why the user sees nothing

User reports the crash shows no message and leaves no page-fault log. It always happens when adding remarks at the end of an order entry (a memo field, `.FPT`), or when using Page Down to move faster. This matches the 09-28 10:41:42 log.

The embedded `autoexec.txt` runs `CALL AMP.BAT` then `EXIT`. When FoxPro's own #PF handler kills AMP.EXE, control returns to the batch file, `EXIT` runs, and the window closes instantly. The only trace is an ordinary `vDos-exit-*.log` containing a `RECOVERED` line. Before the 09-15 build, the same fault produced the vDos Exception dialog. The user's "no message" crashes should therefore be the exit logs with a nonzero "redirected into the guest" count. That is confirmable by grepping the 09-30 and 10-01 logs.

`ILLPageHandler` already returns 0xFF for physical reads at or above `TotMemBytes`, the same as nonexistent memory on real hardware. That is the basis for proposed shim (3): for a *read* that hits an empty PTE, when the preceding PTE maps the last RAM page, return `TotMemBytes + offset` uncached, so reads come back as 0xFF and writes still fault.

### 2026-10-01 correction: FoxPro already has a fixed MEMLIMIT

`D:\AMP\CONFIG.FP` (dev copy, dated 2000) already contains `MEMLIMIT= 20,4096,4096`, so FoxPro's own pool is fixed at 4 MB. The earlier statement that "FoxPro takes all available XMS, so its heap ends at the top of memory" is **wrong**, and mitigations (1) and (2) above are unlikely to help. The faulting page is still the page right after the top of RAM in the extender's linear mapping, but what lives in that region (the extender, FoxPro outside its MEMLIMIT pool, or a buffer/cache) is unknown. The same CONFIG.FP runs fine under Win7 NTVDM, where FoxPro gets memory from NTVDM's DPMI host and vDos's XMS/Phar Lap paging path is not involved. `autoexec.txt` also sets `DOSX=-NOVM` and `FOXPROX=-NOVM`. Next step toward a real fix: identify the code at linear `00AA400C` (CS=000C, EIP 0010400C) in AMP.EXE/FOXDX260.ESL, now readable at `D:\AMP`.

## 2026-10-01: Disassembly of the crash site, and the TLB-flush fix

**Crash site.** FOXDX260.ESL is a 1.4 MB file: an MZ stub, then a Phar Lap `P3` flat image at file offset 0x2DED4. The image starts 0x200 bytes into the P3 header, is 0x10CB38 bytes long, and loads at segment offset 0x1000. CS 000C and DS 0014 both have base 0x009A0000. EIP 0010400C (linear 00AA400C) is the `rep movsd` inside FoxPro's runtime `memmove(dst=EAX, src=EDX, n=EBX)` (0x103FDC to 0x104018: forward copy with byte/dword alignment, and a backward path via 0x103FBC). The 09-09 registers fit a 2 KB copy (EBX=0x800, ECX=0x200) from DS:01EF5937 (linear 02895937) to DS:0012C49C. **Neither range comes near the fault address 02973003.** The three CS=0008:8605 incidents (the extender's own `rep movsb` transfer-buffer copy) show the same thing: faults at 00103000, 00101000 and 0010B000, about 0x72AB0 to 0x7CAB0 bytes away from a 2 KB copy involving 0008E550. The faulting address is not one the instruction should have been touching. That points at vDos's address translation rather than FoxPro.

**Root-cause candidate (a confirmed emulation bug).** `PAGING_SetDirBase` flushed the TLB only when CR3 changed to a new nonzero value (comment: "Phar Lap is frequently switching between 0 and the previous set value"). On a 386, every MOV CR3 flushes the TLB, and with no INVLPG, reloading CR3 is how an extender publishes page-table edits. INVLPG was also a no-op (`PAGING_ClearTLB` commented out). So after Phar Lap remaps pages, vDos keeps using stale linear-to-physical translations. Reads return another page's data, and writes land in whatever that physical page holds now. That fits:

- application text (`FVSEXPDATE`) found inside a page table,
- intermittent crashes that track memory churn (remarks/memo fields, Page Down),
- worsening as the data grows,
- Win7 NTVDM running fine (real TLB semantics).

Not proven: that this specific bug produced the 02973xxx faults. The exact path from a stale translation to that address is not reconstructed.

**Changes (uncommitted, not yet built):**

- `cpu/paging.cpp`: every CR3 write flushes the TLB (zero-value writes are still not stored). `PAGING_Enable` flushes when the paging state changes.
- `cpu/core_normal/prefix_0f.h` and `prefix_66_0f.h`: INVLPG flushes the TLB.
- `hardware/memory.cpp`: `clearTLB` resets only the slots filled since the last flush (`TLB_used` list, full memset on first use or when more than 1/4 full), because CR3 reloads are frequent. It also invalidates the core's 4-byte instruction-fetch cache.
- `cpu/core_normal.cpp`: new `CPU_InvalidateFetchCache()`, declared in `include/mem.h`.

**Verify:** build Debug and Release, run `tests/run-paging-memory.ps1`, and smoke test that AccountMate starts and runs (watch the speed). Then deploy with rollback (`vDos.exe.bak`). Success would be: no more `RECOVERED` lines or silent closes during remarks/Page Down.

### 2026-10-01: TLB-flush build verified and deployed

- **Paging tests pass:** `tests/run-paging-memory.ps1` passed (16384 access cases, 0 failures). The harness does not cover the TLB/CR3 change.
- **Release build is clean:** Release/Win32 rebuild succeeded with only the usual `dos_execute.cpp:123` and `_sprintf` warnings (build Oct 1 2026 10:39:07).
- **Dev-machine run is clean:** run with `XMEM = 32 XMS` added to `Release\config.txt` (the dev config had no XMEM line and defaulted to 4 MB). The log `vDos-exit-20261001-104621-921-p38144.log` shows xms=32MB, 0 redirected faults, 0 locked relocations, 0 CHANGED, and an orderly AMP exit. The dev machine never reproduced the crash, so this shows no regression, not a fix.
- **Deployed:** the user replaced `vDos.exe` only on the Win7 production machine, kept `vDos.exe.bak` for rollback, and briefed the end user.
- **Success criteria:** no silent window closes during remarks or Page Down, and no `RECOVERED` lines in the production exit logs.
