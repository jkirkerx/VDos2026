# Change notes

## 2026.10.05 (2026-10-05) - Protected-mode memory handling

This version brings cached address translation closer to Intel 386 paging
behavior, addressing a confirmed emulation defect found while investigating
intermittent FoxPro/AccountMate failures during order remarks and Page Down.

- Every CR3 write now flushes cached linear-to-physical translations, including
  reloads of the same value. Previously, a reload could leave an old mapping in
  use after a DOS extender edited its page tables, sending reads and writes to
  the wrong physical page.
- Changing the paging-enabled state also flushes translations. Flushes invalidate
  the CPU's instruction-fetch cache so it cannot keep bytes from an old mapping.
- INVLPG now flushes the translation cache in both operand-size paths. INVLPG
  was introduced after the 386; this improves compatibility with guests that use
  it. The implementation clears the whole cache rather than one page.
- Frequent flushes clear only populated cache slots, with a full-clear fallback
  when the cache is sufficiently occupied.
- Word and dword accesses crossing a 4 KB page boundary translate each page
  separately, including when adjacent linear pages map to nonadjacent RAM.
- Not-present page faults can be delivered to an installed protected-mode guest
  handler with CR2 and read/write/user error-code information. The existing fatal
  path remains when delivery is unavailable. REP operations defer ECX bookkeeping
  until the attempted copy completes, keeping it consistent with ESI/EDI if a
  fault interrupts execution.
- Timestamped exit/fault reports include bounded translation history, guest-fault
  delivery records, recent file activity, and XMS allocation/relocation details.
  A `RECOVERED` record means delivery was attempted; it does not prove the guest
  repaired the mapping or continued successfully.

These are targeted compatibility fixes, not a claim of complete 386 emulation.
The existing compatibility convention still ignores zero as a stored CR3 value,
although the write now flushes caches. Guest fault retry is not a complete model
of per-iteration hardware REP progress, and the memory tests do not cover CPU/IDT
fault delivery or the CR3/INVLPG paths.

The October 1 build passed the paging tests and a development-machine AccountMate
run and was deployed with rollback available. The production crash was never
reproduced on the development machine; resolution still needs confirmation from
production remarks/Page Down use and exit logs. See [project notes](PROJECT_NOTES.md)
for the investigation and historical validation.
