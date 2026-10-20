#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "appdata.h"

typedef LONG (WINAPI *GetCurrentPackageFullNameFn)(UINT32* packageFullNameLength, PWSTR packageFullName);

static int packagedState = -1;													// -1 unknown, 0 no, 1 yes
static char outputDir[MAX_PATH+16];

bool vDos_IsPackaged()
	{
	if (packagedState < 0)
		{
		packagedState = 0;
		HMODULE kernel = GetModuleHandleA("kernel32.dll");						// Windows 8+ only, so look it up
		GetCurrentPackageFullNameFn fn = kernel ? (GetCurrentPackageFullNameFn)GetProcAddress(kernel, "GetCurrentPackageFullName") : NULL;
		if (fn)
			{
			UINT32 length = 0;
			if (fn(&length, NULL) == ERROR_INSUFFICIENT_BUFFER)					// APPMODEL_ERROR_NO_PACKAGE when not packaged
				packagedState = 1;
			}
		}
	return packagedState == 1;
	}

static void ExeDir(char* buffer, size_t size);

// MSI install: the installer puts the read-only starter files in <vDos.exe folder>\Starter\. Their presence
// means "installed copy": vDos then keeps each Windows user's setup in %LOCALAPPDATA%\vDos 2026, like the
// Store version. A debug build or a copied vDos folder has no Starter folder and works as it always did.
static int installedState = -1;
static bool IsInstalledCopy()
	{
	if (installedState < 0)
		{
		char starter[MAX_PATH+32];
		ExeDir(starter, sizeof(starter));
		strcat_s(starter, sizeof(starter), "Starter\\autoexec.txt");
		installedState = GetFileAttributesA(starter) != INVALID_FILE_ATTRIBUTES ? 1 : 0;
		}
	return installedState == 1;
	}

static void ExeDir(char* buffer, size_t size)
	{
	strncpy_s(buffer, size, _pgmptr ? _pgmptr : "", _TRUNCATE);
	char* lastSlash = strrchr(buffer, '\\');
	if (lastSlash)
		lastSlash[1] = 0;
	else
		buffer[0] = 0;															// Same as before: relative to the working folder
	}

const char* vDos_OutputDir()
	{
	if (outputDir[0])
		return outputDir;

	if (vDos_IsPackaged() || IsInstalledCopy())
		{
		char localAppData[MAX_PATH];
		DWORD length = GetEnvironmentVariableA("LOCALAPPDATA", localAppData, sizeof(localAppData));
		if (length > 0 && length < sizeof(localAppData))
			{
			_snprintf_s(outputDir, sizeof(outputDir), _TRUNCATE, "%s\\vDos 2026\\", localAppData);
			CreateDirectoryA(outputDir, NULL);									// Fails harmlessly if it exists
			return outputDir;
			}
		}

	ExeDir(outputDir, sizeof(outputDir));										// Copied folder or debug build: next to vDos.exe, as before
	return outputDir;
	}

void vDos_OutputPath(char* buffer, size_t size, const char* fileName)
	{
	_snprintf_s(buffer, size, _TRUNCATE, "%s%s", vDos_OutputDir(), fileName);
	}

static void SeedFromExeFolder(const char* fileName, const char* subFolder)
	{
	char target[MAX_PATH+32];
	vDos_OutputPath(target, sizeof(target), fileName);
	if (GetFileAttributesA(target) != INVALID_FILE_ATTRIBUTES)
		return;

	char source[MAX_PATH+32];
	ExeDir(source, sizeof(source));
	strcat_s(source, sizeof(source), subFolder);
	strcat_s(source, sizeof(source), fileName);
	CopyFileA(source, target, TRUE);											// Never overwrite the user's file
	}

void vDos_PrepareDataFolder()
	{
	const char* subFolder;
	if (vDos_IsPackaged())
		subFolder = "";															// Store package: starter files sit next to vDos.exe
	else if (IsInstalledCopy())
		subFolder = "Starter\\";													// MSI install: <install folder>\Starter
	else
		return;

	const char* dir = vDos_OutputDir();
	SeedFromExeFolder("config.txt", subFolder);
	SeedFromExeFolder("autoexec.txt", subFolder);
	SetCurrentDirectoryA(dir);
	}

bool vDos_NeedsSetup()
	{
	FILE* fh = NULL;
	if (fopen_s(&fh, "autoexec.txt", "r") != 0 || !fh)						// Read from the working folder, as the shell does
		return true;
	char line[256] = "";
	bool starter = false;
	while (fgets(line, sizeof(line), fh))
		{
		char* p = line;
		while (*p == ' ' || *p == '\t' || *p == '@')
			p++;
		if (*p == '\r' || *p == '\n' || *p == 0)
			continue;															// First non-blank line decides
		starter = _strnicmp(p, "rem vDos 2026", 13) == 0 && strstr(_strlwr(p), "starter") != NULL;
		break;
		}
	fclose(fh);
	return starter;
	}
