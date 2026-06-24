#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdio.h>
#include <string.h>

#include "TcpPrinter.h"

static std::string TcpPrinter_FormatAddress(addrinfo* entry)
	{
	char hostText[NI_MAXHOST];
	if (getnameinfo(entry->ai_addr, (socklen_t)entry->ai_addrlen, hostText, sizeof(hostText), NULL, 0, NI_NUMERICHOST) != 0)
		strcpy_s(hostText, sizeof(hostText), "unknown-address");
	return hostText;
	}

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

	char portText[16];
	sprintf_s(portText, sizeof(portText), "%d", port);

	addrinfo hints;
	memset(&hints, 0, sizeof(hints));
	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	addrinfo* results = NULL;
	int gaiResult = getaddrinfo(host.c_str(), portText, &hints, &results);
	if (gaiResult != 0)
		{
		detail = TcpPrinter_FormatErrorCode("getaddrinfo", host, port, gaiResult);
		WSACleanup();
		return false;
		}

	bool sent = false;
	for (addrinfo* entry = results; entry; entry = entry->ai_next)
		{
		std::string resolvedAddress = TcpPrinter_FormatAddress(entry);
		SOCKET sock = socket(entry->ai_family, entry->ai_socktype, entry->ai_protocol);
		if (sock == INVALID_SOCKET)
			{
			detail = TcpPrinter_FormatSocketError("socket", host, port);
			continue;
			}

		if (connect(sock, entry->ai_addr, (int)entry->ai_addrlen) == SOCKET_ERROR)
			{
			detail = TcpPrinter_FormatSocketError("connect", host, port);
			closesocket(sock);
			continue;
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

		int shutdownResult = shutdown(sock, SD_SEND);
		int shutdownError = shutdownResult == SOCKET_ERROR ? WSAGetLastError() : 0;
		closesocket(sock);

		if (remaining == 0)
			{
			char message[384];
			sprintf_s(message, sizeof(message), "resolved=%s family=%d requestedBytes=%u sentBytes=%d sendCalls=%d shutdown=%s%s%d", resolvedAddress.c_str(), entry->ai_family, (unsigned int)data.size(), totalSent, sendCalls, shutdownResult == SOCKET_ERROR ? "error " : "ok", shutdownResult == SOCKET_ERROR ? "code=" : "", shutdownError);
			detail = message;
			sent = true;
			break;
			}
		}

	freeaddrinfo(results);
	WSACleanup();

	if (!sent && detail.empty())
		{
		char message[256];
		sprintf_s(message, sizeof(message), "Could not connect to TCP printer %s:%d", host.c_str(), port);
		detail = message;
		}
	return sent;
	}