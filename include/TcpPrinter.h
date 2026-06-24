#ifndef vDOS_TCPPRINTER_H
#define vDOS_TCPPRINTER_H

#include <string>

bool TcpPrinter_Send(const std::string& host, int port, const std::string& data, std::string& detail);

#endif
