// Compile the production memory-access functions with a controlled page map.
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <stdexcept>
#include <vector>
typedef uint8_t Bit8u;
typedef uint16_t Bit16u;
typedef uint32_t Bit32u;
typedef uint32_t LinPt;
typedef uint32_t PhysPt;
const unsigned MEM_PAGESIZE = 16384;
const unsigned BIOS_KEYBOARD_FLAGS1 = 0x417;
const unsigned BIOS_KEYBOARD_BUFFER_TAIL = 0x41c;
unsigned TotMemBytes = 0x200000;
std::vector<Bit8u> storage(TotMemBytes);
Bit8u* MemBase = storage.data();
bool enabled = true, secondPresent = true;
std::vector<bool> writeLog;					// Records the write flag passed on each LinToPhys call, most recent last
bool PAGING_Enabled() { return enabled; }
Bit32u PAGING_GetDirBase() { return 0x100000; }
PhysPt LinToPhys(LinPt addr, bool write = false)
{
    writeLog.push_back(write);
    if (addr >= 0x2000) throw std::runtime_error("outside test map");
    if (addr >= 0x1000 && !secondPresent) throw std::runtime_error("missing page");
    return (addr < 0x1000 ? 0x110000 : 0x150000) + (addr & 0xfff);
}
void unhide_vDos() {}
struct PageHandler {
    Bit8u readb(PhysPt a) { return storage.at(a); }
    Bit16u readw(PhysPt a) { Bit16u v; memcpy(&v, &storage.at(a), 2); return v; }
    Bit32u readd(PhysPt a) { Bit32u v; memcpy(&v, &storage.at(a), 4); return v; }
    void writeb(PhysPt a, Bit8u v) { storage.at(a) = v; }
    void writew(PhysPt a, Bit16u v) { memcpy(&storage.at(a), &v, 2); }
    void writed(PhysPt a, Bit32u v) { memcpy(&storage.at(a), &v, 4); }
} handler;
PageHandler* MEM_GetPageHandler(PhysPt) { return &handler; }
#include "memory-access.inc"
#include "memory-diagnostics.inc"

int failures = 0;
void check(bool ok, const char* message) {
    if (!ok && failures++ < 12) printf("FAIL: %s\n", message);
}
void physicalDword(PhysPt a, Bit32u v) { memcpy(MemBase+a, &v, 4); }
int main()
{
    for (int paging = 0; paging <= 1; paging++) {
        enabled = paging != 0;
        for (unsigned width = 2; width <= 4; width += 2) {
            for (unsigned offset = 0; offset < 4096; offset++) {
                memset(MemBase, 0xcc, TotMemBytes);
                const Bit32u expected = width == 2 ? 0xBBAA : 0xDDCCBBAA;
                for (unsigned i = 0; i < width; i++) {
                    PhysPt a = enabled ? LinToPhys(offset+i) : offset+i;
                    MemBase[a] = Bit8u(expected >> (i*8));
                }
                Bit32u actual = width == 2 ? Mem_Lodsw(offset) : Mem_Lodsd(offset);
                check(actual == expected, "read across separately mapped pages");
                if (enabled)
                    check(!writeLog.empty() && !writeLog.back(), "Lods* reports a read to LinToPhys");
                memset(MemBase, 0x55, TotMemBytes);
                if (width == 2) Mem_Stosw(offset, Bit16u(expected));
                else Mem_Stosd(offset, expected);
                if (enabled)
                    check(!writeLog.empty() && writeLog.back(), "Stos* reports a write to LinToPhys");
                for (unsigned i = 0; i < width; i++) {
                    PhysPt a = enabled ? LinToPhys(offset+i) : offset+i;
                    check(MemBase[a] == Bit8u(expected >> (i*8)), "write lands in mapped page");
                }
                if (enabled) {
                    for (unsigned i = 0; i < 4; i++)
                        check(MemBase[0x111000+i] == 0x55, "unrelated adjacent physical page untouched");
                }
            }
        }
    }
    enabled = true;
    secondPresent = false;
    for (unsigned operation = 0; operation < 4; operation++) {
        bool faulted = false;
        try {
            if (operation == 0) Mem_Lodsw(0xfff);
            if (operation == 1) Mem_Lodsd(0xffd);
            if (operation == 2) Mem_Stosw(0xfff, 0xabcd);
            if (operation == 3) Mem_Stosd(0xffd, 0xabcdef01);
        } catch (const std::runtime_error&) { faulted = true; }
        check(faulted, "missing second page detected");
    }
    memset(MemBase, 0, TotMemBytes);
    physicalDword(0x100000, 0x101007);
    physicalDword(0x101000, 0x110007);
    physicalDword(0x101004, 0x150007);
    MemBase[0x110fff] = 0xab;
    MemBase[0x150000] = 0xcd;
    Bit8u byte = 0;
    check(DiagnosticReadByte(0xfff, byte) && byte == 0xab, "diagnostic first page");
    check(DiagnosticReadByte(0x1000, byte) && byte == 0xcd, "diagnostic second page");
    physicalDword(0x101004, 0);
    check(!DiagnosticReadByte(0x1000, byte), "diagnostic missing mapping is safe");
    physicalDword(0x101004, 0xfffff007);
    check(!DiagnosticReadByte(0x1000, byte), "diagnostic out-of-range mapping is safe");
    physicalDword(0x100000, 0xfffff007);
    check(!DiagnosticReadByte(0, byte), "diagnostic out-of-range table is safe");
    Bit32u dword;
    check(!DiagnosticReadPhysicalDword(0xffffffff, dword), "diagnostic overflow is safe");
    check(!DiagnosticReadPhysicalDword(0xa0000, dword), "diagnostic avoids device memory");
    // Exercise the production trace through multiple wraps and a later damaged PTE.
    for (unsigned i = 0; i < TranslationTraceSize+17; i++)
        RecordSuccessfulTranslation(i, 0x100000, 0x932605, 0x101007, 0x101004, 0x150007);
    check(translationTraceCount == TranslationTraceSize, "translation history is bounded");
    physicalDword(0x101004, 0x41445058);
    FILE* trace = tmpfile();
    check(trace != nullptr, "trace output file available");
    if (trace) {
        WriteSuccessfulTranslations(trace);
        rewind(trace);
        char line[512];
        check(fgets(line, sizeof(line), trace) != nullptr, "trace header emitted");
        unsigned rows = 0;
        while (fgets(line, sizeof(line), trace)) {
            unsigned linear = 0;
            check(sscanf(line, "  OK linear=%x", &linear) == 1 && linear == rows+17,
                "wrapped history is oldest first");
            check(strstr(line, "ptEntry=00150007") != nullptr &&
                strstr(line, "ptEntryNow=41445058 CHANGED") != nullptr,
                "trace preserves good entry and reports later change");
            rows++;
        }
        check(rows == TranslationTraceSize, "all retained translations emitted");
        fclose(trace);
    }
    check(DiagnosticReadPhysicalDword(0x101004, dword) && dword == 0x41445058,
        "trace does not modify page tables");
    printf("%s: 16384 access cases, missing-page checks, diagnostic safety and translation-history checks (%d failures)\n",
        failures ? "FAILED" : "PASSED", failures);
    return failures ? 1 : 0;
}
