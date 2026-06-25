#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdio.h>
#include <string.h>

#include "TcpPrinter.h"

static void TcpPrinter_GetWindowsErrorText(int errorCode, char* buffer, size_t bufferSize)
	{
	if (!buffer || !bufferSize)
		return;
	buffer[0] = 0;
	FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, errorCode, 0, buffer, (DWORD)bufferSize, NULL);
	for (char* ch = buffer; *ch; ch++)
		if (*ch == '\r' || *ch == '\n')
			*ch = ' ';
	}

static std::string TcpPrinter_FormatSocketError(const char* action, const std::string& host, int port)
	{
	int errorCode = WSAGetLastError();
	char errorText[256];
	TcpPrinter_GetWindowsErrorText(errorCode, errorText, sizeof(errorText));
	char message[512];
	sprintf_s(message, sizeof(message), "%s to TCP printer %s:%d failed with Winsock error %d (%s)", action, host.c_str(), port, errorCode, errorText[0] ? errorText : "no error text available");
	return message;
	}

static std::string TcpPrinter_FormatErrorCode(const char* action, const std::string& host, int port, int errorCode)
	{
	char errorText[256];
	TcpPrinter_GetWindowsErrorText(errorCode, errorText, sizeof(errorText));
	char message[512];
	sprintf_s(message, sizeof(message), "%s for TCP printer %s:%d failed with error %d (%s)", action, host.c_str(), port, errorCode, errorText[0] ? errorText : "no error text available");
	return message;
	}

static bool TcpPrinter_ResolveAddress(const std::string& host, int port, sockaddr_storage& address, int& addressLength, std::string& detail)
	{
	char portText[16];
	sprintf_s(portText, sizeof(portText), "%d", port);

	addrinfo hints;
	memset(&hints, 0, sizeof(hints));
	hints.ai_family = AF_INET;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	addrinfo* results = NULL;
	int gaiResult = getaddrinfo(host.c_str(), portText, &hints, &results);
	if (gaiResult != 0)
		{
		detail = TcpPrinter_FormatErrorCode("getaddrinfo", host, port, gaiResult);
		return false;
		}

	memset(&address, 0, sizeof(address));
	memcpy(&address, results->ai_addr, results->ai_addrlen);
	addressLength = (int)results->ai_addrlen;
	freeaddrinfo(results);
	return true;
	}

bool TcpPrinter_Send(const std::string& host, int port, const std::string& data, std::string& detail)
	{
	detail.clear();
	if (host.empty())
		{
		detail = "TCP printer host is empty";
		return false;
		}
	if (port <= 0 || port > 65535)
		{
		detail = "TCP printer port is out of range";
		return false;
		}
	if (data.empty())
		{
		detail = "No print data to send";
		return true;
		}

	WSADATA wsaData;
	int startupResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (startupResult != 0)
		{
		detail = TcpPrinter_FormatErrorCode("WSAStartup", host, port, startupResult);
		return false;
		}

	sockaddr_storage address;
	int addressLength = 0;
	if (!TcpPrinter_ResolveAddress(host, port, address, addressLength, detail))
		{
		WSACleanup();
		return false;
		}

	SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (sock == INVALID_SOCKET)
		{
		detail = TcpPrinter_FormatSocketError("socket", host, port);
		WSACleanup();
		return false;
		}

	if (connect(sock, (sockaddr*)&address, addressLength) == SOCKET_ERROR)
		{
		detail = TcpPrinter_FormatSocketError("connect", host, port);
		closesocket(sock);
		WSACleanup();
		return false;
		}

	const char* current = data.c_str();
	int remaining = (int)data.size();
	int totalSent = 0;
	int sendCalls = 0;
	while (remaining > 0)
		{
		int chunk = send(sock, current, remaining, 0);
		if (chunk == SOCKET_ERROR)
			{
			detail = TcpPrinter_FormatSocketError("send", host, port);
			break;
			}
		if (chunk == 0)
			{
			char message[256];
			sprintf_s(message, sizeof(message), "send to TCP printer %s:%d returned 0 with %d bytes remaining", host.c_str(), port, remaining);
			detail = message;
			break;
			}
		current += chunk;
		remaining -= chunk;
		totalSent += chunk;
		sendCalls++;
		}

	closesocket(sock);
	WSACleanup();

	if (remaining != 0)
		return false;

	char message[256];
	sprintf_s(message, sizeof(message), "requestedBytes=%u sentBytes=%d sendCalls=%d", (unsigned int)data.size(), totalSent, sendCalls);
	detail = message;
	return true;
	}