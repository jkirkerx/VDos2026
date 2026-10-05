#include <stdlib.h>
#include <stdio.h>
#include "vDos.h"
#include "mem.h"
#include "inout.h"
#include "paging.h"
#include "cpu.h"
#include "bios.h"
#include "regs.h"
#include "dos_system.h"

static PageHandler * pageHandlers[64];												// Pagehandlers for first MB

unsigned int TotMemBytes;
unsigned int TotMemMB = 1;
unsigned int TotEXTMB = 0;
unsigned int TotXMSMB = 0;
unsigned int TotEMSMB = 0;
unsigned int EndConvMem = 0x9fff;

HostPt MemBase;

class RAMPageHandler : public PageHandler
	{
public:
	RAMPageHandler()
		{
		flags = PFLAG_READABLE|PFLAG_WRITEABLE;
		}
	Bit8u readb(PhysPt addr)
		{
		return *(MemBase+addr);
		}
	Bit16u readw(PhysPt addr)
		{
		return *(Bit16u*)(MemBase+addr);
		}
	Bit32u readd(PhysPt addr)
		{
		return *(Bit32u*)(MemBase+addr);
		}
	void writeb(PhysPt addr, Bit8u val)
		{
		*(MemBase+addr) = val;
		}
	void writew(PhysPt addr, Bit16u val)
		{
		*(Bit16u*)(MemBase+addr) = val;
		}
	void writed(PhysPt addr, Bit32u val)
		{
		*(Bit32u*)(MemBase+addr) = val;
		}
	HostPt GetHostPt(PhysPt addr)
		{
		return MemBase+addr;
		}
	};

class ROMPageHandler : public RAMPageHandler
	{
public:
	ROMPageHandler()
		{
		flags = PFLAG_READABLE;
		}
	void writeb(PhysPt addr, Bit8u val)
		{
		}
	void writew(PhysPt addr, Bit16u val)
		{
		}
	void writed(PhysPt addr, Bit32u val)
		{
		}
	};

class ILLPageHandler : public RAMPageHandler
	{
public:
	ILLPageHandler()
		{
		flags = 0;
		}
	Bit8u readb(PhysPt addr)
		{
		return 0xff;
		}
	Bit16u readw(PhysPt addr)
		{
		return 0xffff;
		}
	Bit32u readd(PhysPt addr)
		{
		return 0xffffffff;
		}
	void writeb(PhysPt addr, Bit8u val)
		{
		}
	void writew(PhysPt addr, Bit16u val)
		{
		}
	void writed(PhysPt addr, Bit32u val)
		{
		}
	};


static RAMPageHandler ram_page_handler;
static ROMPageHandler rom_page_handler;
static ILLPageHandler ill_page_handler;

#define TLB_SIZE 8192															// 8192 ebtries in TLB cache
static Bit32u TLB_phys[TLB_SIZE];
static Bit32u TLB_cache[TLB_SIZE];
// Slots filled since the last flush, so clearTLB() can reset just those
// instead of all 8192 entries. The guest reloads CR3 very often (Phar Lap does
// it on mode switches) and every reload must flush, see PAGING_SetDirBase.
static Bit32u TLB_used[TLB_SIZE];
static Bitu TLB_usedCount = 0;
static bool TLB_needFullClear = true;										// Initial contents aren't tracked

// Diagnostic reads must not recurse through LinToPhys or invoke device handlers.
static bool DiagnosticReadPhysicalDword(PhysPt addr, Bit32u& value)
	{
	if (TotMemBytes < 4 || addr > TotMemBytes-4 || (addr < 0xf0000 && addr+3 >= 0xa0000))
		return false;
	memcpy(&value, MemBase+addr, sizeof(value));
	return true;
	}

static bool DiagnosticReadByte(LinPt addr, Bit8u& value)
	{
	PhysPt physical = addr;
	if (PAGING_Enabled())
		{
		Bit32u directory, entry;
		Bit32u base = PAGING_GetDirBase() & ~0xfff;
		if (!DiagnosticReadPhysicalDword(base+((addr>>20)&0xffc), directory) || !(directory&1) || (directory&0x80))
			return false;
		if (!DiagnosticReadPhysicalDword((directory&~0xfff)+((addr>>10)&0xffc), entry) || !(entry&1))
			return false;
		physical = (entry&~0xfff)+(addr&0xfff);
		}
	if (physical >= TotMemBytes || (physical >= 0xa0000 && physical < 0xf0000))
		return false;
	value = MemBase[physical];
	return true;
	}

static void WriteDiagnosticBytes(FILE* file, const char* label, LinPt addr, unsigned int count)
	{
	fprintf(file, "%s linear=%08X (?? = unavailable):", label, addr);
	for (unsigned int i = 0; i < count; i++)
		{
		Bit8u value;
		if (DiagnosticReadByte(addr+i, value))
			fprintf(file, " %02X", value);
		else
			fprintf(file, " ??");
		}
	fprintf(file, "\n");
	}

// Keep successful page-table walks in memory; no disk I/O on the access path.
// TLB hits are not recorded: these entries describe actual table reads.
static const unsigned int TranslationTraceSize = 256;
struct TranslationTraceEntry
	{
	Bit32u linear, cr3, instruction, pdEntry, ptAddr, ptEntry;
	};
static TranslationTraceEntry translationTrace[TranslationTraceSize];
static unsigned int translationTraceNext = 0;
static unsigned int translationTraceCount = 0;

static void RecordSuccessfulTranslation(LinPt addr, Bit32u cr3, LinPt instruction,
	Bit32u pdEntry, Bit32u ptAddr, Bit32u ptEntry)
	{
	TranslationTraceEntry& entry = translationTrace[translationTraceNext];
	entry.linear = addr;
	entry.cr3 = cr3;
	entry.instruction = instruction;
	entry.pdEntry = pdEntry;
	entry.ptAddr = ptAddr;
	entry.ptEntry = ptEntry;
	translationTraceNext = (translationTraceNext+1)%TranslationTraceSize;
	if (translationTraceCount < TranslationTraceSize)
		translationTraceCount++;
	}

static void WriteSuccessfulTranslations(FILE* file)
	{
	fprintf(file, "Recent successful page-table walks (oldest first, %u retained; TLB hits omitted):\n", translationTraceCount);
	unsigned int first = (translationTraceNext+TranslationTraceSize-translationTraceCount)%TranslationTraceSize;
	for (unsigned int i = 0; i < translationTraceCount; i++)
		{
		const TranslationTraceEntry& entry = translationTrace[(first+i)%TranslationTraceSize];
		fprintf(file, "  OK linear=%08X physical=%08X cr3=%08X instruction=%08X pdEntry=%08X ptAddr=%08X ptEntry=%08X",
			entry.linear, (entry.ptEntry&~0xfff)+(entry.linear&0xfff), entry.cr3,
			entry.instruction, entry.pdEntry, entry.ptAddr, entry.ptEntry);
		Bit32u current;
		if (DiagnosticReadPhysicalDword(entry.ptAddr, current))
			fprintf(file, " ptEntryNow=%08X%s\n", current, current == entry.ptEntry ? "" : " CHANGED");
		else
			fprintf(file, " ptEntryNow=unavailable\n");
		}
	}

// Page faults that were redirected into the guest's own interrupt-14 handler
// instead of aborting vDos (see DeliverPageFault below). Kept in memory only
// - no per-fault disk I/O, since if this path fires at all it may fire
// routinely - and surfaced by WritePageFaultDiagnostics below, which already
// runs both on a later fatal fault and on ordinary shell EXIT/window close.
// That gives a direct, positive record of whether and how often recovery
// actually happened during a run, instead of inferring it only from the
// absence of a crash.
static const unsigned int RecoveredFaultTraceSize = 64;
struct RecoveredFaultEntry
	{
	Bit32u linear, cr3, instruction, error;
	};
static RecoveredFaultEntry recoveredFaultTrace[RecoveredFaultTraceSize];
static unsigned int recoveredFaultTraceNext = 0;
static unsigned int recoveredFaultTraceCount = 0;			// Total ever recorded this run, not clamped to the ring size

static void RecordRecoveredFault(LinPt addr, Bit32u cr3, LinPt instruction, Bit32u error)
	{
	RecoveredFaultEntry& entry = recoveredFaultTrace[recoveredFaultTraceNext];
	entry.linear = addr;
	entry.cr3 = cr3;
	entry.instruction = instruction;
	entry.error = error;
	recoveredFaultTraceNext = (recoveredFaultTraceNext+1)%RecoveredFaultTraceSize;
	recoveredFaultTraceCount++;
	}

static void WriteRecoveredFaults(FILE* file)
	{
	unsigned int shown = recoveredFaultTraceCount < RecoveredFaultTraceSize ? recoveredFaultTraceCount : RecoveredFaultTraceSize;
	fprintf(file, "Page faults redirected into the guest this run: %u (most recent %u shown, oldest first):\n", recoveredFaultTraceCount, shown);
	unsigned int first = (recoveredFaultTraceNext+RecoveredFaultTraceSize-shown)%RecoveredFaultTraceSize;
	for (unsigned int i = 0; i < shown; i++)
		{
		const RecoveredFaultEntry& entry = recoveredFaultTrace[(first+i)%RecoveredFaultTraceSize];
		fprintf(file, "  RECOVERED linear=%08X cr3=%08X instruction=%08X error=%02X\n",
			entry.linear, entry.cr3, entry.instruction, entry.error);
		}
	}

// Bounded guard against recursing back into fault delivery while a fault is
// already being delivered (e.g. the guest's own stack page also turns out not
// to be present while CPU_Interrupt pushes the return frame). Incremented and
// decremented by PageFaultDepthGuard's constructor/destructor, so it stays
// correct even when a nested attempt unwinds through here via GuestPageFault
// instead of returning normally. Declared here (ahead of DeliverPageFault,
// further below) so WritePageFaultDiagnostics can report its value too.
static int pageFaultDeliveryDepth = 0;

struct PageFaultDepthGuard
	{
	PageFaultDepthGuard()  { pageFaultDeliveryDepth++; }
	~PageFaultDepthGuard() { pageFaultDeliveryDepth--; }
	};

static bool WritePageFaultDiagnostics(const char* faultType, LinPt addr, Bit32u pdAddr, Bit32u pdEntry, Bit32u ptAddr, Bit32u ptEntry, char* filename, size_t filenameSize, bool normalExit = false)
	{
	SYSTEMTIME now;
	GetLocalTime(&now);
	char basename[128];
	sprintf_s(basename, "vDos-%s-%04u%02u%02u-%02u%02u%02u-%03u-p%lu.log",
		normalExit ? "exit" : "pagefault", now.wYear, now.wMonth, now.wDay,
		now.wHour, now.wMinute, now.wSecond, now.wMilliseconds, GetCurrentProcessId());
	strncpy_s(filename, filenameSize, _pgmptr ? _pgmptr : "vDos.exe", _TRUNCATE);
	char* separator = strrchr(filename, '\\');
	if (separator)
		separator[1] = 0;
	else
		filename[0] = 0;
	strcat_s(filename, filenameSize, basename);
	FILE* traceFile = fopen(filename, "a");
	if (!traceFile)
		return false;

	fprintf(traceFile, "\n==== vDos %s %04u-%02u-%02u %02u:%02u:%02u.%03u (local time) ====\n",
		normalExit ? "normal exit" : "page fault", now.wYear, now.wMonth, now.wDay, now.wHour, now.wMinute, now.wSecond, now.wMilliseconds);
	if (!normalExit)
	fprintf(traceFile, "type=%s linear=%08X cr3=%08X pdAddr=%08X pdEntry=%08X ptAddr=%08X ptEntry=%08X\n",
		faultType, addr, PAGING_GetDirBase(), pdAddr, pdEntry, ptAddr, ptEntry);
	fprintf(traceFile, "memory total=%uMB ext=%uMB xms=%uMB ems=%uMB totalBytes=%u\n",
		TotMemMB, TotEXTMB, TotXMSMB, TotEMSMB, TotMemBytes);
	fprintf(traceFile, "EAX=%08X EBX=%08X ECX=%08X EDX=%08X ESI=%08X EDI=%08X EBP=%08X ESP=%08X EIP=%08X FLAGS=%08X\n",
		reg_eax, reg_ebx, reg_ecx, reg_edx, reg_esi, reg_edi, reg_ebp, reg_esp, reg_eip, reg_flags);
	fprintf(traceFile, "CS=%04X DS=%04X ES=%04X SS=%04X FS=%04X GS=%04X CR0=%08X CPL=%u paging=%u\n",
		SegValue(cs), SegValue(ds), SegValue(es), SegValue(ss), SegValue(fs), SegValue(gs), cpu.cr0, cpu.cpl, PAGING_Enabled() ? 1 : 0);
	{
	// Explains why DeliverPageFault would or wouldn't have been able to
	// redirect this fault into the guest: pmode off, no IDT, or IDT[14]
	// specifically missing/not-present all show up here distinctly.
	Descriptor pfGate;
	bool pfGatePresent = cpu.idt.GetDescriptor(EXCEPTION_PF<<3, pfGate) && pfGate.saved.seg.p;
	fprintf(traceFile, "pmode=%u idtBase=%08X idtLimit=%08X pfGatePresent=%u faultDeliveryDepth=%d\n",
		cpu.pmode ? 1 : 0, cpu.idt.GetBase(), cpu.idt.GetLimit(), pfGatePresent ? 1 : 0, pageFaultDeliveryDepth);
	}
	fprintf(traceFile, "diagnostics=6 build=%s %s code32=%u stack32=%u cr3=%08X\n", __DATE__, __TIME__, cpu.code.big ? 1 : 0, cpu.stack.big ? 1 : 0, PAGING_GetDirBase());
	fprintf(traceFile, "segment bases CS=%08X DS=%08X ES=%08X SS=%08X FS=%08X GS=%08X\n",
		SegPhys(cs), SegPhys(ds), SegPhys(es), SegPhys(ss), SegPhys(fs), SegPhys(gs));
	WriteDiagnosticBytes(traceFile, "instruction", SegPhys(cs)+reg_eip, 32);
	WriteDiagnosticBytes(traceFile, "stack", SegPhys(ss)+(reg_esp&cpu.stack.mask), 128);
	if (!normalExit)
		WriteDiagnosticBytes(traceFile, "fault vicinity", addr >= 16 ? addr-16 : 0, 48);
	if (pdEntry&1)
		{
		fprintf(traceFile, "nearby PTEs (physical address:value):");
		unsigned int index = (addr>>12)&0x3ff;
		unsigned int first = index > 2 ? index-2 : 0;
		unsigned int last = index < 1021 ? index+2 : 1023;
		for (unsigned int i = first; i <= last; i++)
			{
			Bit32u location = (pdEntry&~0xfff)+i*4;
			Bit32u value;
			if (DiagnosticReadPhysicalDword(location, value))
				fprintf(traceFile, " %08X:%08X", location, value);
			else
				fprintf(traceFile, " %08X:unavailable", location);
			}
		fprintf(traceFile, "\n");
		}
	WriteSuccessfulTranslations(traceFile);
	WriteRecoveredFaults(traceFile);
	XMS_WriteDiagnostics(traceFile);
	DOS_WriteFileTrace(traceFile);
	bool written = fflush(traceFile) == 0 && !ferror(traceFile);
	if (fclose(traceFile) != 0)
		written = false;
	return written;
	}

void MEM_WriteExitDiagnostics()
	{
	static bool attempted = false;
	if (attempted || !MemBase || !TotMemBytes)
		return;
	attempted = true;
	char filename[MAX_PATH];
	if (!WritePageFaultDiagnostics("normal exit", 0, 0, 0, 0, 0, filename, sizeof(filename), true))
		MessageBoxA(NULL, filename, "vDos: could not write exit diagnostics", MB_OK|MB_ICONWARNING);
	}

void clearTLB(void)
	{
	if (TLB_needFullClear || TLB_usedCount >= TLB_SIZE/4)
		{
		memset(TLB_cache, 0xff, TLB_SIZE*4);
		TLB_needFullClear = false;
		}
	else
		for (Bitu i = 0; i < TLB_usedCount; i++)
			TLB_cache[TLB_used[i]] = 0xffffffff;
	TLB_usedCount = 0;
	CPU_InvalidateFetchCache();
	}

// Raise a real x86 #PF into the guest instead of aborting vDos: set CR2 to
// the faulting linear address and deliver interrupt 14 through the CPU's
// existing protected-mode interrupt-gate dispatch (CPU_Exception/
// CPU_Interrupt in cpu.cpp) - the same machinery already used, and already
// exercised, for #UD on an illegal opcode (core_normal.cpp's illegal_opcode
// case). The guest's own fault handler (installed by the DOS extender that
// owns paging here) then runs, is expected to fix up the mapping, and IRETs
// back to re-execute the faulting instruction.
//
// Returns true if delivery was set up; the caller must then throw
// GuestPageFault to unwind out of the faulting instruction so execution
// resumes at the handler CPU_Interrupt just switched CS:EIP to, instead of
// falling through to more code that assumes the translation succeeded.
// Returns false, with no state changed, when delivery isn't possible or safe
// right now (no guest IDT installed, or already unwinding a fault); the
// caller then keeps its existing fatal-abort behavior.
static bool DeliverPageFault(LinPt addr, bool write)
	{
	if (!cpu.pmode)													// Nothing protected-mode-shaped to deliver an IDT-gate interrupt into
		return false;
	if (pageFaultDeliveryDepth >= 2)								// Already unwinding a fault; don't recurse without bound
		return false;
	// Check the guest's IDT[14] slot ourselves before calling CPU_Exception.
	// CPU_Interrupt(), if it can't find a descriptor for the requested vector,
	// escalates to a #GP on that SAME vector-not-found path; if #GP's own IDT
	// slot is also missing, that recurses again with no bound between them,
	// which is a stack overflow, not a controlled failure. Confirmed from
	// vDos's own project history: the extender family this codebase targets
	// (PharLap-derived, per the vDos author's own diagnosis of this exact
	// "table/directory entry not present" message for PharLap/FoxProX) is
	// documented as NOT installing a real handler for interrupt 14 at all, so
	// this is the expected case here, not a rare edge condition - checking
	// first keeps that expected case a clean, safe "false" instead of risking
	// the recursion above.
	Descriptor gate;
	if (!cpu.idt.GetDescriptor(EXCEPTION_PF<<3, gate) || !gate.saved.seg.p)
		return false;
	PageFaultDepthGuard depthGuard;
	paging.cr2 = addr;
	Bitu error = (write ? 2 : 0) | (cpu.cpl == 3 ? 4 : 0);			// P=0 (not-present), W/R, U/S: standard x86 #PF error-code layout
	RecordRecoveredFault(addr, PAGING_GetDirBase(), SegPhys(cs)+reg_eip, error);
	CPU_Exception(EXCEPTION_PF, error);
	return true;
	}

PhysPt LinToPhys2(LinPt addr, bool write)
	{
	int TLB_idx = addr>>12;
	Bit32u pdAddr =(TLB_idx>>8)&0xffc;
	Bit32u pdEntry = *(Bit32u *)(MemBase+PAGING_GetDirBase()+pdAddr);
	if (pdEntry&1)
		{
		Bit32u ptAddr = (pdEntry&~0xfff)+((TLB_idx<<2)&0xffc);
		Bit32u ptEntry = *(Bit32u *)(MemBase+ptAddr);
		if (ptEntry&1)
			{
			RecordSuccessfulTranslation(addr, PAGING_GetDirBase(), SegPhys(cs)+reg_eip, pdEntry, ptAddr, ptEntry);
			if (TLB_cache[TLB_idx&(TLB_SIZE-1)] == 0xffffffff && TLB_usedCount < TLB_SIZE)
				TLB_used[TLB_usedCount++] = TLB_idx&(TLB_SIZE-1);
			TLB_cache[TLB_idx&(TLB_SIZE-1)] = TLB_idx;
			TLB_phys[TLB_idx&(TLB_SIZE-1)] = ptEntry&~0xfff;
			return TLB_phys[TLB_idx&(TLB_SIZE-1)]+(addr&0xfff);
			}
		if (DeliverPageFault(addr, write))
			throw GuestPageFault();
		char diagnosticsPath[MAX_PATH];
		bool diagnosticsWritten = WritePageFaultDiagnostics("table entry not present", addr, pdAddr, pdEntry, ptAddr, ptEntry, diagnosticsPath, sizeof(diagnosticsPath));
		E_Exit("Page fault: table entry not present: %x\n\nCR3: %08X\nPDE: %08X\nPTE: %08X\nEIP: %08X\nXMS: %u MB\n\nDebug log %s:\n%s",
			addr, PAGING_GetDirBase(), pdEntry, ptEntry, reg_eip, TotXMSMB, diagnosticsWritten ? "written" : "COULD NOT BE WRITTEN", diagnosticsPath);
		}
	if (DeliverPageFault(addr, write))
		throw GuestPageFault();
	char diagnosticsPath[MAX_PATH];
	bool diagnosticsWritten = WritePageFaultDiagnostics("directory entry not present", addr, pdAddr, pdEntry, 0, 0, diagnosticsPath, sizeof(diagnosticsPath));
	E_Exit("Page fault: directory entry not present: %x\n\nCR3: %08X\nPDE: %08X\nEIP: %08X\nXMS: %u MB\n\nDebug log %s:\n%s",
		addr, PAGING_GetDirBase(), pdEntry, reg_eip, TotXMSMB, diagnosticsWritten ? "written" : "COULD NOT BE WRITTEN", diagnosticsPath);
	}

__forceinline PhysPt LinToPhys(LinPt addr, bool write = false)
	{
	int TLB_idx = addr>>12;
	if (TLB_cache[TLB_idx&(TLB_SIZE-1)] == TLB_idx)
		return TLB_phys[TLB_idx&(TLB_SIZE-1)]+(addr&0xfff);
	return LinToPhys2(addr, write);
	}

__forceinline PageHandler * MEM_GetPageHandler(PhysPt addr)
	{
	if (addr < 0x100000)															// If in first MB
		return pageHandlers[addr/MEM_PAGESIZE];
	if (addr < TotMemBytes)															// If in extended (or XMS)
		return &ram_page_handler;
	return &ill_page_handler;
	}

void MEM_SetPageHandler(Bitu phys_page, Bitu pages, PageHandler * handler)
	{
	for (; pages > 0; pages--)
		pageHandlers[phys_page++] = handler;
	}

void MEM_ResetPageHandler(Bitu phys_page, Bitu pages)
	{
	for (; pages > 0; pages--)
		pageHandlers[phys_page++] = &ram_page_handler;
	}

void unhide_vDos()
	{
	if (winHidden && !winHide10th)													// Unhide window on access
		{
		hideWinTill = GetTickCount();
		winHide10th = 1;															// It would kept to be delayed
		}
	else
		idleCount++;
	}

Bit8u Mem_Lodsb(LinPt addr)
	{
	if (PAGING_Enabled())
		addr = LinToPhys(addr, false);
	if (addr < 0xa0000)																// If in lower memory, read direct (always readable)
		{
		if ((addr <= BIOS_KEYBOARD_BUFFER_TAIL && addr >= BIOS_KEYBOARD_FLAGS1))	// Access to keyboard info
			unhide_vDos();
		return *(MemBase+addr);
		}
	if ((addr >= 0xf0000 && addr < TotMemBytes))									// If in last 64KB or extended mem, read direct (always readable)
		return *(MemBase+addr);
	return MEM_GetPageHandler(addr)->readb(addr);
	}

Bit16u Mem_Lodsw(LinPt addr)
	{
	// Adjacent linear pages need not be adjacent in physical memory.
	if (PAGING_Enabled() && (addr&0xfff) == 0xfff)
		{
		Bit16u value = Mem_Lodsb(addr);
		return value | (Bit16u(Mem_Lodsb(addr+1)) << 8);
		}
	if (PAGING_Enabled())
		addr = LinToPhys(addr, false);
	if (addr < 0x9ffff)																// If in lower mem, read direct (always readable)
		{
		if (addr <= BIOS_KEYBOARD_BUFFER_TAIL && addr >= BIOS_KEYBOARD_FLAGS1)		// Access to keyboard info
			unhide_vDos();
		return *(Bit16u *)(MemBase+addr);
		}
	if ((addr >= 0xf0000 && addr < TotMemBytes-1))									// If in last 64 KB ir extended mem, read direct (always readable)
		return *(Bit16u *)(MemBase+addr);
	if ((addr&(MEM_PAGESIZE-1)) != (MEM_PAGESIZE-1))
		return MEM_GetPageHandler(addr)->readw(addr);
	Bit16u ret = 0;
	for (Bit32u i = addr+1; i >= addr; i--)
		{
		ret <<= 8;
		ret |= MEM_GetPageHandler(i)->readb(i);
		}
	return ret;
	}

Bit32u Mem_Lodsd(LinPt addr)
	{
	if (PAGING_Enabled() && (addr&0xfff) > 0xffc)
		{
		Bit32u value = 0;
		for (unsigned int i = 0; i < 4; i++)
			value |= Bit32u(Mem_Lodsb(addr+i)) << (i*8);
		return value;
		}
	if (PAGING_Enabled())
		addr = LinToPhys(addr, false);
	if (addr < 0x9fffd || (addr >= 0xf0000 && addr < TotMemBytes-3))				// In lower, last 64 KB, or extended mem, read direct (always readable)
		return *(Bit32u *)(MemBase+addr);
	if ((addr&(MEM_PAGESIZE-1)) < (MEM_PAGESIZE-3))
		return MEM_GetPageHandler(addr)->readd(addr);
	Bit32u ret = 0;
	for (Bit32u i = addr+3; i >= addr; i--)
		{
		ret <<= 8;
		ret |= MEM_GetPageHandler(i)->readb(i);
		}
	return ret;
	}

void Mem_Stosb(LinPt addr, Bit8u val)
	{
	if (PAGING_Enabled())
		addr = LinToPhys(addr, true);
	if (addr < 0xa0000 || (addr > 0xfffff && addr < TotMemBytes))					// If in lower or extended mem, write direct (always writeable)
		{
		*(MemBase+addr) = val;
		return;
		}
	MEM_GetPageHandler(addr)->writeb(addr, val);
	}

void Mem_Stosw(LinPt addr, Bit16u val)
	{
	if (PAGING_Enabled() && (addr&0xfff) == 0xfff)
		{
		Mem_Stosb(addr, Bit8u(val));
		Mem_Stosb(addr+1, Bit8u(val >> 8));
		return;
		}
	if (PAGING_Enabled())
		addr = LinToPhys(addr, true);
	if (addr < 0x9ffff || (addr > 0xfffff && addr < TotMemBytes-1))					// If in lower or extended mem, write direct (always writeable)
		{
		*(Bit16u *)(MemBase+addr) = val;
		return;
		}
	if ((addr&(MEM_PAGESIZE-1)) != (MEM_PAGESIZE-1))
		MEM_GetPageHandler(addr)->writew(addr, val);
	else
		{
		MEM_GetPageHandler(addr)->writeb(addr, val&0xff);
		MEM_GetPageHandler(addr+1)->writeb(addr+1, val>>8);
		}
	}

void Mem_Stosd(LinPt addr, Bit32u val)
	{
	if (PAGING_Enabled() && (addr&0xfff) > 0xffc)
		{
		for (unsigned int i = 0; i < 4; i++)
			Mem_Stosb(addr+i, Bit8u(val >> (i*8)));
		return;
		}
	if (PAGING_Enabled())
		addr = LinToPhys(addr, true);
	if (addr < 0x9fffd || (addr > 0xfffff && addr < TotMemBytes-3))					// If in lower or extended mem, write direct (always writeable)
		{
		*(Bit32u *)(MemBase+addr) = val;
		return;
		}
	if ((addr&(MEM_PAGESIZE-1)) < (MEM_PAGESIZE-3))
		MEM_GetPageHandler(addr)->writed(addr, val);
	else
		for (int i = 0; i < 4; i++)
			{
			MEM_GetPageHandler(addr)->writeb(addr, val&0xff);
			val >>= 8;
			addr++;
			}
	}

void Mem_Movsb(LinPt dest, LinPt src)
	{
	if (PAGING_Enabled())
		{
		src = LinToPhys(src, false);
		dest = LinToPhys(dest, true);
		}
	MEM_GetPageHandler(dest)->writeb(dest, MEM_GetPageHandler(src)->readb(src));
	}

void Mem_rMovsb(LinPt dest, LinPt src, Bitu bCount)
	{
	Bit16u maxMove = PAGING_Enabled() ? 4096 : MEM_PAGESIZE;						// If paging, use 4096 bytes page size (strictly not needed?)
	while (bCount)																	// Set in chunks of MEM_PAGESIZE for mem mapping to take effect
		{
		PhysPt physSrc = PAGING_Enabled() ? LinToPhys(src, false) : src;
		PhysPt physDest = PAGING_Enabled() ? LinToPhys(dest, true) : dest;
		Bit16u srcOff = ((Bit16u)physSrc)&(maxMove-1);
		Bit16u destOff = ((Bit16u)physDest)&(maxMove-1);
		Bit16u bTodo = maxMove - max(srcOff, destOff);
		if (bTodo > bCount)
			bTodo = bCount;
		src += bTodo;
		dest += bTodo;
		bCount -= bTodo;
		PageHandler * phSrc = MEM_GetPageHandler(physSrc);
		PageHandler * phDest = MEM_GetPageHandler(physDest);

		if (phDest->flags&PFLAG_WRITEABLE && phSrc->flags&PFLAG_READABLE)
			{
			Bit8u *hSrc = phSrc->GetHostPt(physSrc);
			Bit8u *hDest = phDest->GetHostPt(physDest);
			if ((hSrc <= hDest && hSrc+bTodo > hDest) || (hDest <= hSrc && hDest+bTodo > hSrc))
				while (bTodo--)														// If source and destination overlap, do it "by hand"
					*(hDest++) = *(hSrc++);											// memcpy() messes things up in another way than rep movsb does!
			else
				memcpy(hDest, hSrc, bTodo);
			}
		else																		// Not writeable, or use (VGA)handler
			while (bTodo--)
				phDest->writeb(physDest++, phSrc->readb(physSrc++));
		}
	}

Bitu Mem_StrLen(LinPt addr)
	{
	for (Bitu len = 0; len < 65536; len++)
		if (!Mem_Lodsb(addr+len))
			return len;
	return 0;																		// This shouldn't happen
	}

void Mem_CopyTo(LinPt dest, void const * const src, Bitu bCount)
	{
	Bit8u const * srcAddr = (Bit8u const *)src;
	Bit16u maxMove = PAGING_Enabled() ? 4096 : MEM_PAGESIZE;						// If paging, use 4096 bytes page size (strictly not needed?)
	while (bCount)
		{
		PhysPt physDest = PAGING_Enabled() ? LinToPhys(dest, true) : dest;
		Bit16u bTodo = maxMove-(((Bit16u)physDest)&(maxMove-1));
		if (bTodo > bCount)
			bTodo = bCount;
		bCount -= bTodo;
		dest += bTodo;
		PageHandler * ph = MEM_GetPageHandler(physDest);
		if (ph->flags&PFLAG_WRITEABLE)
			{
			memcpy(ph->GetHostPt(physDest), srcAddr, bTodo);
			srcAddr += bTodo;
			}
		else
			while (bTodo--)
				ph->writeb(physDest++, *srcAddr++);
		}
	}

void Mem_CopyFrom(LinPt src, void * dest, Bitu bCount)
	{
	Bit8u * destAddr = (Bit8u *)dest;
	Bit16u maxMove = PAGING_Enabled() ? 4096 : MEM_PAGESIZE;						// If paging, use 4096 bytes page size (strictly not needed?)
	while (bCount)
		{
		PhysPt physSrc = PAGING_Enabled() ? LinToPhys(src, false) : src;
		Bit16u bTodo = maxMove - (((Bit16u)physSrc)&(maxMove-1));
		if (bTodo > bCount)
			bTodo = bCount;
		bCount -= bTodo;
		src += bTodo;
		PageHandler * ph = MEM_GetPageHandler(physSrc);
		if (ph->flags & PFLAG_READABLE)
			{
			memcpy(destAddr, ph->GetHostPt(physSrc), bTodo);
			destAddr += bTodo;
			}
		else
			while (bTodo--)
				*destAddr++ = ph->readb(physSrc++);
		}
	}

void Mem_StrnCopyFrom(char * data, LinPt pt, Bitu bCount)
	{
	while (bCount--)
		{
		Bit8u c = Mem_Lodsb(pt++);
		if (!c)
			break;
		*data++ = c;
		}
	*data = 0;
	}

void Mem_rStos4b(LinPt addr, Bit32u val32, Bitu bCount)								// Used by rStosw and rStosd (bytes are not the same)
	{
	Bit16u maxMove = PAGING_Enabled() ? 4096 : MEM_PAGESIZE;						// If paging, use 4096 bytes page size (strictly not needed?)
	while (bCount)																	// Set in chunks of MEM_PAGESIZE for mem mapping to take effect
		{
		PhysPt physAddr = PAGING_Enabled() ? LinToPhys(addr, true) : addr;
		Bit16u bTodo = maxMove - (((Bit16u)physAddr)&(maxMove-1));
		if (bTodo > bCount)
			bTodo = bCount;
		PageHandler *ph = MEM_GetPageHandler(physAddr);
		addr += bTodo;
		if (ph->flags&PFLAG_WRITEABLE)
			{
			HostPt hPtr = ph->GetHostPt(physAddr);
			while ((Bit32u)(hPtr)&3)												// Align start address to 32 bit
				{
				*(hPtr++) = val32&0xff;
				val32 = _rotr(val32, 8);
				bTodo--;
				bCount--;
				}
			bCount -= bTodo&~3;
			for (bTodo >>= 2; bTodo; bTodo--)										// Set remaing with 32 bit value
				{
				*(Bit32u *)hPtr = val32;
				hPtr += 4;
				}
			if (bCount < 4)															// Eventually last remaining bytes
				{
				while (bCount--)
					{
					*hPtr++ = val32&0xff;
					val32 >>= 8;
					}
				return;
				}
			}
		else																		// Not writeable, or use (VGA)handler
			{
			while ((Bit32u)(physAddr) & 3)											// Align start address to 32 bit
				{
				ph->writeb(physAddr++, val32&0xff);
				val32 = _rotr(val32, 8);
				bTodo--;
				bCount--;
				}
			bCount -= bTodo&~3;
			for (bTodo >>= 2; bTodo; bTodo--)										// Set remaing with 32 bit value
				{
				ph->writed(physAddr, val32);
				physAddr += 4;
				}
			if (bCount < 4)															// Eventually last remaining bytes
				{
				while (bCount--)
					{
					ph->writeb(physAddr++, val32&0xff);
					val32 >>= 8;
					}
				return;
				}
			}
		}
	}

void Mem_rStosb(LinPt addr, Bit8u val, Bitu count)
	{
	Bit16u maxMove = PAGING_Enabled() ? 4096 : MEM_PAGESIZE;						// If paging, use 4096 bytes page size (strictly not needed?)
	while (count)																	// Set in chunks of MEM_PAGESIZE for mem mapping to take effect
		{
		PhysPt physAddr = PAGING_Enabled() ? LinToPhys(addr, true) : addr;
		Bit16u bTodo = maxMove - (((Bit16u)physAddr)&(maxMove-1));
		if (bTodo > count)
			bTodo = count;
		count -= bTodo;
		addr += bTodo;
		PageHandler *ph = MEM_GetPageHandler(physAddr);
		if (ph->flags&PFLAG_WRITEABLE)
			memset(ph->GetHostPt(physAddr), val, bTodo);							// memset() is optimized for 32 bit
		else																		// Not writeable, or use (VGA)handler
			while (bTodo--)
				ph->writeb(physAddr++, val);
		}
	}

static void write_p92(Bitu port, Bitu val, Bitu iolen)
	{
	}

static Bitu read_p92(Bitu port, Bitu iolen)
	{
	return 0;
	}

static IO_ReadHandleObject ReadHandler;
static IO_WriteHandleObject WriteHandler;

void MEM_Init()
	{
	char *xmem = ConfGetString("xmem");
	if (*xmem)
		{
		int testVal;
		char testStr1[512];
		char testStr2[512];
		bool error = true;
		if (*xmem == '+')															// 704K option
			{
			EndConvMem += 0x1000;
			xmem++;
			}
		if (sscanf(xmem, "%d%s%s", &testVal, testStr1, testStr2) == 2)
			{
			if (testVal > 0 && testVal < 64)
				{
				error = false;
				if (!stricmp("EXT", testStr1))
					TotEXTMB = testVal;
				else if (!stricmp("XMS", testStr1))
					TotXMSMB = testVal;
				else if (!stricmp("EMS", testStr1))
					TotEMSMB = testVal;
				else
					error = true;
				if (!error)
					TotMemMB = TotEXTMB+TotXMSMB+1;
				}
			}
		if (error)
			ConfAddError("Invalid XMEM= parameters\n", xmem);
		}
	else
		{
		TotXMSMB = 4;																// Default 4MB XMS
		TotMemMB = TotXMSMB+1;
		}

	MemBase = (Bit8u *)_aligned_malloc((TotMemMB+TotEMSMB)*1024*1024, MEM_PAGESIZE);// Setup the physical memory
	if (!MemBase)
		E_Exit("Can't allocate main memory of %d MB", TotMemMB+TotEMSMB);
	TotMemBytes = TotMemMB*1024*1024;
	memset((void*)MemBase, 0, TotMemBytes);											// Clear the memory
		
	for (Bitu i = 0; i < 64; i++)													// Setup handlers for first MB
		pageHandlers[i] = &ram_page_handler;
	for (Bitu i = 0xc0000/MEM_PAGESIZE; i < 0xc4000/MEM_PAGESIZE; i++)				// Setup rom at 0xc0000-0xc3fff
		pageHandlers[i] = &rom_page_handler;
	for (Bitu i = 0xf0000/MEM_PAGESIZE; i < 0x100000/MEM_PAGESIZE; i++)				// Setup rom at 0xf0000-0xfffff
		pageHandlers[i] = &rom_page_handler;
	WriteHandler.Install(0x92, write_p92);											// (Dummy) A20 Line - PS/2 system control port A
	ReadHandler.Install(0x92, read_p92);
	}
