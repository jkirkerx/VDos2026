// Where vDos reads config.txt/autoexec.txt and writes its logs and print files.
//
// MSI installs (a Starter folder next to vDos.exe) and Microsoft Store installs: each Windows user's
// setup lives in %LOCALAPPDATA%\vDos 2026 - config.txt, autoexec.txt, logs and print files. On first
// start vDos copies the starter config.txt/autoexec.txt there and makes it the working folder.
//
// A copied vDos folder or a debug build (no Starter folder): unchanged - config files come from the
// working folder ("Start in"), logs and print files go next to vDos.exe.
//
// Microsoft Store (MSIX) installs: vDos.exe lives in a read-only WindowsApps folder and a Start
// menu tile cannot set "Start in", so vDos uses %LOCALAPPDATA%\vDos 2026\ for all of it
// (Windows keeps that folder private to the package; the configurator and Print Monitor in the
// same package see the same folder).
#ifndef VDOS_APPDATA_H
#define VDOS_APPDATA_H

#include <stddef.h>

// True when running from an MSIX package (GetCurrentPackageFullName is looked up at run time,
// so the same vDos.exe still runs on Windows 7).
bool vDos_IsPackaged();

// Folder for logs and print files, ending in '\\' (or "" for the working folder).
const char* vDos_OutputDir();

// vDos_OutputDir() + fileName.
void vDos_OutputPath(char* buffer, size_t size, const char* fileName);

// Store and MSI installs: create the data folder, seed config.txt/autoexec.txt from the starter
// copies (next to vDos.exe, or in its Starter folder) if missing, and make it the working folder.
// No-op for a copied folder or debug build.
void vDos_PrepareDataFolder();

// True when autoexec.txt in the working folder is missing or is still the installer's starter file
// (first line "rem vDos 2026 ... starter ..."): vDos then opens the configurator.
bool vDos_NeedsSetup();

#endif
