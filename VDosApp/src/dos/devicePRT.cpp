#include "devicePRT.h"
#include "TcpPrinter.h"
#include "process.h"
#include <Shellapi.h>
#include <Winspool.h>
#include <time.h>
#include "vDos.h"
#include "support.h"

void LPT_CheckTimeOuts(Bit32u mSecsCurr)
	{
	for (int dn = 0; dn < DOS_DEVICES; dn++)
		if (Devices[dn] && Devices[dn]->timeOutAt != 0)
			if (Devices[dn]->timeOutAt <= mSecsCurr)
				{
				Devices[dn]->timeOutAt = 0;
				Devices[dn]->Close();
				return;																// One device per run cycle
				}
	}

bool device_PRT::Read(Bit8u * data, Bit16u * size)
	{
	if (!strcmp("clipboard", destination.c_str()))
		if (OpenClipboard(NULL))
			{	
			if (HANDLE cbText = GetClipboardData(CF_UNICODETEXT))
				{
				Bit16u *p = (Bit16u *)GlobalLock(cbText);
				*size = Unicode2Ascii(p, data, *size);
				GlobalUnlock(cbText);
				}
			CloseClipboard();
			return true;
			}
	*size = 0;
	return true;
	}

bool device_PRT::Write(Bit8u * data, Bit16u * size)
	{
	Bit8u * datasrc = data;
	Bit8u * datadst = data;

	int numSpaces = 0;
	for (Bit16u idx = *size; idx; idx--)
		{
		if (*datasrc == 0x0c)
			ffWasLast = true;
		else if (!isspace(*datasrc))
			ffWasLast = false;
		if (*datasrc == ' ')														// Put spaces on hold
			numSpaces++;
		else
			{
			if (numSpaces && *datasrc != 0x0a && *datasrc != 0x0d)					// Spaces on hold and not end of line
				while (numSpaces--)
					*(datadst++) = ' ';
			numSpaces = 0;
			*(datadst++) = *datasrc;
			}
		datasrc++;
		}
	while (numSpaces--)
		*(datadst++) = ' ';
	if (Bit16u newsize = datadst - data)											// If data
		{
		if (rawdata.capacity() < 100000)											// Prevent repetive size allocations
			rawdata.reserve(100000);
		rawdata.append((char *)data, newsize);
		if (printTimeout)
			timeOutAt = GetTickCount()+LPT_LONGTIMEOUT;								// Long timeout so data is printed w/o Close()
		}
	return true;
	}

void device_PRT::Close()
	{
	rawdata.erase(rawdata.find_last_not_of(" \n\r\t")+1);							// Remove trailing white space
	if (!rawdata.size())															// Nothing captured/to do
		return;
	int len = rawdata.size();
	if (len > 2 && rawdata[len-3] == 0x0c && rawdata[len-2] == 27 && rawdata[len-1] == 64)	// <ESC>@ after last FF?
		{
		rawdata.erase(len-2, 2);
		ffWasLast = true;
		}
	if (!ffWasLast && timeOutAt && !fastCommit)										// For programs initializing the printer in a seperate module
		{
		timeOutAt = GetTickCount() + LPT_SHORTTIMEOUT;								// Short timeout if ff was not last
		return;
		}
	CommitData();
	}

void tryPCL2PDF(char * filename, bool postScript, bool openIt)
	{
	char pcl6Path[512];																// Try to start gswin32c/pcl6 from where vDos was started
	strcpy(strrchr(strcpy(pcl6Path+1, _pgmptr), '\\'), postScript ? "\\gswin32c.exe" : "\\pcl6.exe");
	if (_access(pcl6Path+1, 4))														// If not found/readable
		{
		MessageBox(sdlHwnd, "Could not find pcl6 or gswin32c to handle printjob", "vDos - Error", MB_OK|MB_ICONWARNING);
		return;
		}

	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = SW_HIDE;
	ZeroMemory(&pi, sizeof(pi));

	pcl6Path[0] ='"';																// Surround path with quotes to be sure
	strcat(pcl6Path, "\" -sDEVICE=pdfwrite -o ");
	strcat(pcl6Path, filename);
	pcl6Path[strlen(pcl6Path)-3] = 0;												// Replace .asc by .pdf
	strcat (pcl6Path, "pdf ");
	strcat(pcl6Path, filename);
	if (CreateProcess(NULL, pcl6Path, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi))	// Start pcl6/gswin32c.exe
		{
		WaitForSingleObject(pi.hProcess, INFINITE);									// Wait for pcl6/gswin32c to exit
		DWORD exitCode = -1;
		GetExitCodeProcess(pi.hProcess, &exitCode);
		CloseHandle(pi.hProcess);													// Close process and thread handles
		CloseHandle(pi.hThread);
		if (exitCode != 0)
			MessageBox(sdlHwnd, "pcl6 or gswin32c could not convert printjob to PDF", "vDos - Error", MB_OK|MB_ICONWARNING);
		else if (openIt)
			{
			strcpy(pcl6Path, filename);										
			pcl6Path[strlen(pcl6Path)-3] = 0;										// Replace .asc by .pdf
			strcat(pcl6Path, "pdf");
			if (!_access(pcl6Path, 4))												// If generated PDF file found/readable
				ShellExecute(NULL, "open", pcl6Path, NULL, NULL, SW_SHOWNORMAL);	// Open/show it
			}
		}
	return;
	}

static void LogTcpConfig(const char* filename, const char* portName, const char* host, int port)
	{
	FILE* fh = fopen(filename, "a");
	if (!fh)
		return;
	time_t now = time(NULL);
	struct tm localTime;
	localtime_s(&localTime, &now);
	char stamp[32];
	strftime(stamp, sizeof(stamp), "%Y-%m-%d %H:%M:%S", &localTime);
	fprintf(fh, "%s %s TCP %s:%d configured\n", stamp, portName, host, port);
	fclose(fh);
	}
static bool DumpTcpRaw(const char* filename, const std::string& data)
	{
	FILE* fh = fopen(filename, "wb");
	if (!fh)
		return false;
	fwrite(data.c_str(), data.size(), 1, fh);
	fclose(fh);
	return true;
	}
static bool ParseWindowsDeviceDestination(const std::string& destination, char* deviceName, size_t deviceNameSize)
	{
	if (deviceNameSize == 0)
		return false;
	deviceName[0] = 0;
	if (destination.empty())
		return false;

	const char* start = destination.c_str();
	while (*start == ' ' || *start == '\t')
		start++;

	const char* end = start + strlen(start);
	while (end > start && (end[-1] == ' ' || end[-1] == '\t'))
		end--;

	if (end <= start)
		return false;

	if (*start == '"')
		{
		start++;
		const char* quote = strchr(start, '"');
		if (!quote)
			return false;
		const char* afterQuote = quote + 1;
		while (*afterQuote == ' ' || *afterQuote == '\t')
			afterQuote++;
		if (*afterQuote != ':' || afterQuote[1] != 0)
			return false;
		end = quote;
		}
	else if (end[-1] != ':')
		return false;

	size_t len = end - start;
	if (len && start[len-1] == ':')
		len--;
	if (!len || len >= deviceNameSize)
		return false;

	memcpy(deviceName, start, len);
	deviceName[len] = 0;
	if (strchr(deviceName, '\\'))
		return false;

	if ((strnicmp(deviceName, "LPT", 3) && strnicmp(deviceName, "COM", 3)) || deviceName[3] < '1' || deviceName[3] > '9' || deviceName[4] != 0)
		return false;

	strcat_s(deviceName, deviceNameSize, ":");
	return true;
	}

static bool WriteRawToWindowsDevice(const char* deviceName, const std::string& data, std::string& detail)
	{
	HANDLE device = CreateFileA(deviceName, GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (device == INVALID_HANDLE_VALUE)
		{
		char message[256];
		sprintf_s(message, sizeof(message), "Could not open Windows device %s. Error %lu", deviceName, GetLastError());
		detail = message;
		return false;
		}

	const char* ptr = data.c_str();
	DWORD remaining = (DWORD)data.size();
	while (remaining)
		{
		DWORD written = 0;
		DWORD chunk = remaining > 32768 ? 32768 : remaining;
		if (!WriteFile(device, ptr, chunk, &written, NULL) || written == 0)
			{
			char message[256];
			sprintf_s(message, sizeof(message), "Could not write to Windows device %s. Error %lu", deviceName, GetLastError());
			detail = message;
			CloseHandle(device);
			return false;
			}
		ptr += written;
		remaining -= written;
		}

	CloseHandle(device);
	return true;
	}
static std::string FormatWindowsError(const char* action, DWORD errorCode)
	{
	char errorText[256];
	errorText[0] = 0;
	FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, errorCode, 0, errorText, sizeof(errorText), NULL);
	for (char* ch = errorText; *ch; ch++)
		if (*ch == '\r' || *ch == '\n')
			*ch = ' ';

	char message[512];
	sprintf_s(message, sizeof(message), "%s failed with Windows error %lu (%s)", action, errorCode, errorText[0] ? errorText : "no error text available");
	return message;
	}
static const char* SkipSpaces(const char* value)
	{
	while (*value == ' ' || *value == '\t')
		value++;
	return value;
	}
static bool ParseKeywordArgument(const std::string& destination, const char* keyword, std::string& value)
	{
	value.clear();
	const char* input = SkipSpaces(destination.c_str());
	size_t keywordLen = strlen(keyword);
	if (strnicmp(input, keyword, keywordLen) || (input[keywordLen] && input[keywordLen] != ' ' && input[keywordLen] != '\t'))
		return false;

	const char* start = SkipSpaces(input + keywordLen);
	if (!*start)
		return false;

	const char* end = NULL;
	if (*start == '"')
		{
		start++;
		end = strchr(start, '"');
		if (!end)
			return false;
		const char* tail = SkipSpaces(end + 1);
		if (*tail)
			return false;
		}
	else
		{
		end = start + strlen(start);
		while (end > start && (end[-1] == ' ' || end[-1] == '\t'))
			end--;
		}

	if (end <= start)
		return false;

	value.assign(start, end - start);
	return true;
	}
static bool ResolvePrinterNameForPort(const std::string& portName, std::string& printerName, std::string& detail)
	{
	printerName.clear();
	DWORD needed = 0;
	DWORD returned = 0;
	EnumPrintersA(PRINTER_ENUM_LOCAL|PRINTER_ENUM_CONNECTIONS, NULL, 2, NULL, 0, &needed, &returned);
	if (!needed)
		{
		detail = FormatWindowsError("EnumPrinters", GetLastError());
		return false;
		}

	std::vector<BYTE> buffer(needed);
	if (!EnumPrintersA(PRINTER_ENUM_LOCAL|PRINTER_ENUM_CONNECTIONS, NULL, 2, buffer.data(), needed, &needed, &returned))
		{
		detail = FormatWindowsError("EnumPrinters", GetLastError());
		return false;
		}

	PRINTER_INFO_2A* printers = (PRINTER_INFO_2A*)buffer.data();
	for (DWORD idx = 0; idx < returned; idx++)
		if (printers[idx].pPortName && !stricmp(printers[idx].pPortName, portName.c_str()) && printers[idx].pPrinterName)
			{
			printerName = printers[idx].pPrinterName;
			return true;
			}

	char message[256];
	sprintf_s(message, sizeof(message), "No Windows printer queue is using port %s", portName.c_str());
	detail = message;
	return false;
	}
static bool ParseSpoolPrinterDestination(const std::string& destination, std::string& printerName, std::string& detail)
	{
	detail.clear();
	if (ParseKeywordArgument(destination, "printer", printerName) || ParseKeywordArgument(destination, "queue", printerName))
		return true;

	std::string portName;
	if (ParseKeywordArgument(destination, "port", portName))
		return ResolvePrinterNameForPort(portName, printerName, detail);

	return false;
	}
static bool WriteRawToPrinterQueue(const char* printerName, const std::string& data, std::string& detail)
	{
	HANDLE printer = NULL;
	if (!OpenPrinterA((LPSTR)printerName, &printer, NULL))
		{
		detail = FormatWindowsError("OpenPrinter", GetLastError());
		return false;
		}

	DOC_INFO_1A docInfo;
	docInfo.pDocName = (LPSTR)"vDos print job";
	docInfo.pOutputFile = NULL;
	docInfo.pDatatype = (LPSTR)"RAW";

	DWORD jobId = StartDocPrinterA(printer, 1, (LPBYTE)&docInfo);
	if (!jobId)
		{
		detail = FormatWindowsError("StartDocPrinter", GetLastError());
		ClosePrinter(printer);
		return false;
		}

	bool success = true;
	if (!StartPagePrinter(printer))
		{
		detail = FormatWindowsError("StartPagePrinter", GetLastError());
		success = false;
		}

	const char* ptr = data.c_str();
	DWORD remaining = (DWORD)data.size();
	while (success && remaining)
		{
		DWORD written = 0;
		DWORD chunk = remaining > 32768 ? 32768 : remaining;
		if (!WritePrinter(printer, (LPVOID)ptr, chunk, &written) || written == 0)
			{
			detail = FormatWindowsError("WritePrinter", GetLastError());
			success = false;
			break;
			}
		ptr += written;
		remaining -= written;
		}

	if (!EndPagePrinter(printer) && success)
		{
		detail = FormatWindowsError("EndPagePrinter", GetLastError());
		success = false;
		}
	if (!EndDocPrinter(printer) && success)
		{
		detail = FormatWindowsError("EndDocPrinter", GetLastError());
		success = false;
		}
	ClosePrinter(printer);
	if (success)
		{
		char message[256];
		sprintf_s(message, sizeof(message), "requestedBytes=%u", (unsigned int)data.size());
		detail = message;
		}
	return success;
	}
static void BuildTcpRawDumpName(char* name, size_t nameSize, const char* portName)
	{
	time_t now = time(NULL);
	struct tm localTime;
	localtime_s(&localTime, &now);
	char stamp[32];
	strftime(stamp, sizeof(stamp), "%Y%m%d_%H%M%S", &localTime);
	sprintf_s(name, nameSize, "#%s_%s_%03u.tcp.raw", portName, stamp, (unsigned int)(GetTickCount()%1000));
	}
static void BuildTcpRawDumpPath(char* filename, size_t filenameSize, const char* baseRawPath, const char* portName)
	{
	strncpy(filename, baseRawPath, filenameSize-1);
	filename[filenameSize-1] = 0;
	char* lastSlash = strrchr(filename, '\\');
	char* nameStart = lastSlash ? lastSlash+1 : filename;
	BuildTcpRawDumpName(nameStart, filenameSize-(nameStart-filename), portName);
	}
static bool BuildTcpRawDumpTempPath(char* filename, size_t filenameSize, const char* portName)
	{
	strcpy_s(filename, filenameSize, "C:\\Temp");
	CreateDirectoryA(filename, NULL);
	strcat_s(filename, filenameSize, "\\vDosPrintDebug");
	if (!CreateDirectoryA(filename, NULL) && GetLastError() != ERROR_ALREADY_EXISTS)
		{
		DWORD len = GetTempPathA((DWORD)filenameSize, filename);
		if (!len || len >= filenameSize)
			return false;
		size_t used = strlen(filename);
		if (used && filename[used-1] != '\\')
			sprintf_s(filename+used, filenameSize-used, "\\");
		strcat_s(filename, filenameSize, "vDosPrintDebug");
		CreateDirectoryA(filename, NULL);
		}
	strcat_s(filename, filenameSize, "\\");
	size_t used = strlen(filename);
	BuildTcpRawDumpName(filename+used, filenameSize-used, portName);
	return true;
	}
static std::string LogToken(std::string value)
	{
	for (size_t i = 0; i < value.size(); i++)
		if (value[i] == ' ' || value[i] == '\t')
			value[i] = '_';
	return value;
	}
static void LogPrintStatus(const char* filename, const char* portName, const char* backend, const char* target, size_t bytes, bool success, const std::string& detail, const char* rawFile)
	{
	FILE* fh = fopen(filename, "a");
	if (!fh)
		return;
	time_t now = time(NULL);
	struct tm localTime;
	localtime_s(&localTime, &now);
	char stamp[32];
	strftime(stamp, sizeof(stamp), "%Y-%m-%d %H:%M:%S", &localTime);
	fprintf(fh, "%s %s %s %s bytes=%u result=%s", stamp, portName, backend, target, (unsigned int)bytes, success ? "sent" : "failed");
	if (!detail.empty())
		fprintf(fh, " detail=%s", detail.c_str());
	if (rawFile && *rawFile)
		fprintf(fh, " raw=%s", rawFile);
	fprintf(fh, "\n");
	fclose(fh);
	}
static void LogTcpPrint(const char* filename, const char* portName, const char* host, int port, size_t bytes, bool success, const std::string& detail, const char* rawFile)
	{
	FILE* fh = fopen(filename, "a");
	if (!fh)
		return;
	time_t now = time(NULL);
	struct tm localTime;
	localtime_s(&localTime, &now);
	char stamp[32];
	strftime(stamp, sizeof(stamp), "%Y-%m-%d %H:%M:%S", &localTime);
	fprintf(fh, "%s %s TCP %s:%d bytes=%u result=%s", stamp, portName, host, port, (unsigned int)bytes, success ? "sent" : "failed");
	if (!detail.empty())
		fprintf(fh, " detail=%s", detail.c_str());
	if (rawFile && *rawFile)
		fprintf(fh, " raw=%s", rawFile);
	fprintf(fh, "\n");
	fclose(fh);
	}
void device_PRT::CommitData()
	{
	timeOutAt = 0;
	DPexitcode = -1;
	char tcpRawDump[MAX_PATH_LEN] = "";
	if (printDebug)
		{
		if (!BuildTcpRawDumpTempPath(tcpRawDump, sizeof(tcpRawDump), GetName()) || !DumpTcpRaw(tcpRawDump, rawdata))
			tcpRawDump[0] = 0;
		}
	if (tcpConfigured)
		{
		std::string detail;
		bool sent = TcpPrinter_Send(tcpHost, tcpPort, rawdata, detail);
		LogTcpPrint(tcpLog, GetName(), tcpHost.c_str(), tcpPort, rawdata.size(), sent, detail, tcpRawDump);
		if (!sent)
			MessageBox(sdlHwnd, detail.c_str(), "vDos - TCP Printer Error", MB_OK|MB_ICONWARNING);
		rawdata.clear();
		return;
		}
	std::string printerName;
	std::string detail;
	if (ParseSpoolPrinterDestination(destination, printerName, detail))
		{
		bool sent = WriteRawToPrinterQueue(printerName.c_str(), rawdata, detail);
		LogPrintStatus(tcpLog, GetName(), "PRINTER", LogToken(printerName).c_str(), rawdata.size(), sent, detail, tcpRawDump);
		if (!sent)
			MessageBox(sdlHwnd, detail.c_str(), "vDos - Printer Queue Error", MB_OK|MB_ICONWARNING);
		rawdata.clear();
		return;
		}
	else if (!detail.empty())
		{
		MessageBox(sdlHwnd, detail.c_str(), "vDos - Printer Queue Error", MB_OK|MB_ICONWARNING);
		rawdata.clear();
		return;
		}
	char windowsDevice[16];
	if (ParseWindowsDeviceDestination(destination, windowsDevice, sizeof(windowsDevice)))
		{
		detail.clear();
		if (!WriteRawToWindowsDevice(windowsDevice, rawdata, detail))
			MessageBox(sdlHwnd, detail.c_str(), "vDos - Printer Port Error", MB_OK|MB_ICONWARNING);
		rawdata.clear();
		return;
		}

	if (DPhandle != -1)																// DOSprinter previously used
		GetExitCodeProcess((HANDLE)DPhandle, &DPexitcode);

	FILE* fh = fopen(tmpAscii, DPexitcode == STILL_ACTIVE ? "ab" : "wb");			// Append or write to ASCII file
	if (fh)
		{
		fwrite(rawdata.c_str(), rawdata.size(), 1, fh);
		fclose(fh);
		fh = fopen(tmpUnicode, DPexitcode == STILL_ACTIVE ? "a+b" : "w+b");			// The same for Unicode file (it's eventually read)
		if (fh)
			{
			if ( DPexitcode != STILL_ACTIVE)
				fprintf(fh, "\xff\xfe");											// It's a Unicode text file
			for (Bit32u i = 0; i < rawdata.size(); i++)
				{
				Bit16u textChar =  (Bit8u)rawdata[i];
				switch (textChar)
					{
				case 9:																// Tab
				case 12:															// Formfeed
					fwrite(&textChar, 1, 2, fh);
					break;
				case 10:															// Linefeed (combination)
				case 13:
					fwrite("\x0d\x00\x0a\x00", 1, 4, fh);
					if (i < rawdata.size() -1 && textChar == 23-rawdata[i+1])
						i++;
					break;
				default:
					if (textChar >= 32)												// Forget about further control characters?
						fwrite(cpMap+textChar, 1, 2, fh);
					break;
					}
				}
			}
		}
	if (!fh)
		{
		rawdata.clear();
		MessageBox(NULL, "Could not save printerdata", "vDos - Warning", MB_OK|MB_ICONSTOP);
		return;
		}
	if (!stricmp(destination.c_str(), "clipboard"))									// Copy to clipboard, Unicode file handle is still open
		{
		if (OpenClipboard(NULL))
			{
			if (EmptyClipboard())
				{
				int bytes = ftell(fh);
				HGLOBAL hCbData = GlobalAlloc(NULL, bytes);
				Bit8u* pChData = (Bit8u*)GlobalLock(hCbData);
				if (pChData)
					{
					fseek(fh, 2, SEEK_SET);											// Skip Unicode signature
					fread(pChData, 1, bytes-2, fh);
					pChData[bytes-2] = 0;
					pChData[bytes-1] = 0;
					SetClipboardData(CF_UNICODETEXT, hCbData);
					GlobalUnlock(hCbData);
					}
				}
			CloseClipboard();
			}
		fclose(fh);
		rawdata.clear();
		return;
		}

	fclose(fh);																		// No longer needed
	if (useDP)
		{
		if (nothingSet)																// DP was assumed, nothing set
			{
			if (!rawdata.find("\x1b%-12345X@") || !rawdata.find("\x1b\x45"))		// It's PCL (rawdata isn't empty at this point, so test is ok)
				{																	// Postscript can be embedded (some WP drivers)
				tryPCL2PDF(tmpAscii, rawdata.find("\n%!PS") < min(rawdata.length(), 60), true);	// A line should start with the signature in the first 70s characters or so
				rawdata.clear();
				return;
				}
			if (rawdata.find("%!PS") == 0)											// It's Postscript
				{
				tryPCL2PDF(tmpAscii, true, true);
				rawdata.clear();
				return;
				}
			}
		if (DPexitcode != STILL_ACTIVE)												// If DOSprinter isn't still running
			{
			char dpPath[256];														// Try to start it from where vDos was started
			strcpy(strrchr(strcpy(dpPath, _pgmptr), '\\'), "\\DOSPrinter.exe");
			DPhandle = _spawnl(P_NOWAIT, dpPath, "DOSPrinter.exe", destination.c_str(), NULL);
			if (DPhandle == -1)
				MessageBox(sdlHwnd, "Could not start DOSPrinter to handle printjob", "vDos - Error", MB_OK|MB_ICONWARNING);
			}
		}
	else if (stricmp(destination.c_str(), "dummy"))									// Windows command or program assumed
		{
		if (rawdata.find("\x1b%-12345X@") == 0)										// It's PCL (rawdata isn't empty at this point, so test is ok)
			tryPCL2PDF(tmpAscii, rawdata.find("\n%!PS") < min(rawdata.length(), 60), false);	// a line should start with the signature in the first 70s characters or so
		else if (rawdata.find("%!PS") == 0)											// It's Postscript
			tryPCL2PDF(tmpAscii, true, false);
		if (destination[0] == '@')													// If the commandline starts with '@' assume program to be started hidden							
			{
			STARTUPINFO si;
			PROCESS_INFORMATION pi;

			ZeroMemory(&si, sizeof(si));
			si.cb = sizeof(si);
			si.dwFlags = STARTF_USESHOWWINDOW;
			si.wShowWindow = SW_HIDE;
			ZeroMemory(&pi, sizeof(pi));

			if (!ExpandEnvironmentStrings(destination.c_str()+1, (char *)dos_copybuf, 4096))	// Replace %% Windows variables
				strcpy((char *)dos_copybuf, destination.c_str()+1);							// Should always work, just to be too sure
			if (CreateProcess(NULL, (char *)dos_copybuf, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi))	// Start program
				{
				CloseHandle(pi.hProcess);											// Close process and thread handles
				CloseHandle(pi.hThread);
				}
			}
		else
			system(destination.c_str());											// Let Windows decide what is meant				
		}
	rawdata.clear();																// Fall thru
	}

bool device_PRT::ParseTcpDestination(const char* cmd)
	{
	tcpHost.clear();
	if (!cmd || !*cmd)
		return false;

	char buffer[512];
	strncpy(buffer, cmd, sizeof(buffer)-1);
	buffer[sizeof(buffer)-1] = 0;

	char* context = NULL;
	char* mode = strtok_s(buffer, " 	", &context);
	if (!mode || (stricmp(mode, "tcp") && stricmp(mode, "jetdirect") && stricmp(mode, "rawtcp")))
		return false;

	char* host = strtok_s(NULL, " 	", &context);
	if (!host || !*host)
		return false;

	char* portText = strtok_s(NULL, " 	", &context);
	char* colon = strrchr(host, ':' );
	if (colon)
		{
		*colon = 0;
		if (!portText)
			portText = colon+1;
		}

	int parsedPort = portText && *portText ? atoi(portText) : 9100;
	if (!*host || parsedPort <= 0 || parsedPort > 65535)
		return false;

	tcpConfigured = true;
	tcpHost = host;
	tcpPort = parsedPort;
	return true;
	}

Bit16u device_PRT::GetInformation(void)
	{
//	return 0x80A0;
	return 0x80E0;																	// dBase IV checks for not ready
	}

static char* PD_select[] = {"/SEL", "/PDF", "/RTF"};
static char DP_lCode[] = "  ";

device_PRT::device_PRT(const char *pname, const char* cmd)
	{
// pname: LPT1-LPT9, COM1-COM9
// cmd:
//  1.  Not set or empty						: DOSPrinter is assumed with "/SEL /LINES /CPIA /LEFT0.50 /TOP0.50 /Lngxx" switches.
//												  If the data is recognized being PCL or Postscript, pcl6/gswin32c is started.
//	2.	/SEL, /PDF or /RTF...					: DOSPrinter is called with these switches (and /Lngxx if not included).
//  3.	clipboard								: Data is put on the clipboard.
//	4.	dummy									: Data is discarded, output in #LPT1-9/#COM1-9 is in ASCII.
//	5.	<Windows command/program> [options]		: Fallthru, cCommand/program [options] is started.
	SetName(pname);

	strncpy(tmpAscii, _pgmptr, sizeof(tmpAscii)-1);
	tmpAscii[sizeof(tmpAscii)-1] = 0;
	if (char* lastSlash = strrchr(tmpAscii, '\\'))
		sprintf_s(lastSlash+1, sizeof(tmpAscii)-(lastSlash+1-tmpAscii), "#%s.asc", pname);
	else
		sprintf_s(tmpAscii, sizeof(tmpAscii), "#%s.asc", pname);
	strncpy(tmpUnicode, _pgmptr, sizeof(tmpUnicode)-1);
	tmpUnicode[sizeof(tmpUnicode)-1] = 0;
	if (char* lastSlash = strrchr(tmpUnicode, '\\'))
		sprintf_s(lastSlash+1, sizeof(tmpUnicode)-(lastSlash+1-tmpUnicode), "#%s.txt", pname);
	else
		sprintf_s(tmpUnicode, sizeof(tmpUnicode), "#%s.txt", pname);
		strncpy(tcpLog, _pgmptr, sizeof(tcpLog)-1);
	tcpLog[sizeof(tcpLog)-1] = 0;
	if (char* lastSlash = strrchr(tcpLog, '\\'))
		sprintf_s(lastSlash+1, sizeof(tcpLog)-(lastSlash+1-tcpLog), "#%s.tcp.log", pname);
	else
		sprintf_s(tcpLog, sizeof(tcpLog), "#%s.tcp.log", pname);
	strncpy(tcpRaw, _pgmptr, sizeof(tcpRaw)-1);
	tcpRaw[sizeof(tcpRaw)-1] = 0;
	if (char* lastSlash = strrchr(tcpRaw, '\\'))
		sprintf_s(lastSlash+1, sizeof(tcpRaw)-(lastSlash+1-tcpRaw), "#%s.tcp.raw", pname);
	else
		sprintf_s(tcpRaw, sizeof(tcpRaw), "#%s.tcp.raw", pname);
	DPhandle = -1;
	tcpConfigured = false;
	tcpPort = 0;
	ParseTcpDestination(cmd);
	if (tcpConfigured)
		{
		DeleteFile(tmpAscii);
		DeleteFile(tmpUnicode);
		DeleteFile(tcpRaw);
		if (printDebug)
			LogTcpConfig(tcpLog, pname, tcpHost.c_str(), tcpPort);
		}

	if (wpVersion && pname[3] == '9' || !stricmp("clip", cmd))						// LPT9/COM9 in combination with WP or "clip"
		{
		destination = "clipboard";
		fastCommit = true;
		}
	else
		{
		destination = cmd;
		fastCommit = false;
		}

	nothingSet = false;
	if (destination.empty())														// Not defined or invalid setup, use DOSPrinter with standard switches
		{
		destination = "/SEL /LINES /CPIA /LEFT0.50 /TOP0.50";
		useDP = true;
		nothingSet = true;
		}
	else 
		{
		useDP = false;																// Test if set for using DOSPrinter with switches
		for (int i = 0; i < 3; i++)
			if (!strnicmp(destination.c_str(), PD_select[i], 4))
				useDP = true;
		}
	if (useDP)
		{
		char *upperDest = new char[destination.size() + 1];
		for (unsigned int i = 0; i < destination.size(); i++)
			upperDest[i] = toupper(destination[i]);
		upperDest[destination.size()] = '\0';
		if (!strstr(upperDest, "/LNG"))												// If language not set in switches
			{
			if (DP_lCode[0] == ' ')
				{
				int langID = GetSystemDefaultLangID()&0x1ff;						// Determine UI language for DOSPrinter
				int suppID[] = {0x16, 0x0a, 0x0c, 0x1a, 0x1b, 0x24, 0x0e, 0x10, 0x03, 0x13, 0x07, 0x00};
				char *suppLN[] = {"PT", "ES", "FR", "HR", "SI", "SI", "HU", "IT", "CA", "NL", "DE"};
				DP_lCode[0] = 'x';
				for (int i = 0; suppID[i] != 0; i++)								// LCIDToLocaleName not supported in Win XP
					if (langID == suppID[i])										// So we do it "by hand"
						strcpy(DP_lCode, suppLN[i]);
				}
			if (DP_lCode[0] != 'x')
				{
				destination += " /lng";
				destination += DP_lCode;
				}
			}
		destination += " ";
		destination += tmpAscii;
		delete upperDest;
		}
	}

device_PRT::~device_PRT()
	{
	}
