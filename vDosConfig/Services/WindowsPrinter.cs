using System.Runtime.InteropServices;

namespace vDosConfig.Services;

internal sealed class WindowsPrinter
{
    public required string Name { get; init; }
    public string? PortName { get; init; }
    public string? DriverName { get; init; }
    public bool IsDefault { get; init; }

    public override string ToString()
    {
        var suffix = string.IsNullOrWhiteSpace(PortName) ? "" : $" ({PortName})";
        return IsDefault ? $"{Name}{suffix} [Default]" : $"{Name}{suffix}";
    }
}

internal sealed class WindowsPrinterPort
{
    public required string Name { get; init; }
    public string? MonitorName { get; init; }
    public string? Description { get; init; }

    public override string ToString() => Name;
}

internal static class WindowsPrinterDiscovery
{
    private const int PrinterEnumLocal = 0x00000002;
    private const int PrinterEnumConnections = 0x00000004;
    private const int PrinterAttributesDefault = 0x00000004;

    public static IReadOnlyList<WindowsPrinter> GetInstalledPrinters()
    {
        var flags = PrinterEnumLocal | PrinterEnumConnections;

        _ = EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out var bytesNeeded, out _);
        if (bytesNeeded == 0)
            return Array.Empty<WindowsPrinter>();

        var buffer = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (!EnumPrinters(flags, null, 2, buffer, bytesNeeded, out _, out var returned))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to enumerate Windows printers.");

            var printers = new List<WindowsPrinter>((int)returned);
            var itemSize = Marshal.SizeOf<PrinterInfo2>();

            for (var i = 0; i < returned; i++)
            {
                var item = Marshal.PtrToStructure<PrinterInfo2>(buffer + (i * itemSize));
                var name = PtrToString(item.PrinterName);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                printers.Add(new WindowsPrinter
                {
                    Name = name,
                    PortName = PtrToString(item.PortName),
                    DriverName = PtrToString(item.DriverName),
                    IsDefault = (item.Attributes & PrinterAttributesDefault) != 0
                });
            }

            return printers
                .OrderByDescending(printer => printer.IsDefault)
                .ThenBy(printer => printer.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static IReadOnlyList<WindowsPrinterPort> GetInstalledPorts()
    {
        _ = EnumPorts(null, 2, IntPtr.Zero, 0, out var bytesNeeded, out _);
        if (bytesNeeded == 0)
            return Array.Empty<WindowsPrinterPort>();

        var buffer = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (!EnumPorts(null, 2, buffer, bytesNeeded, out _, out var returned))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to enumerate Windows printer ports.");

            var ports = new List<WindowsPrinterPort>((int)returned);
            var itemSize = Marshal.SizeOf<PortInfo2>();

            for (var i = 0; i < returned; i++)
            {
                var item = Marshal.PtrToStructure<PortInfo2>(buffer + (i * itemSize));
                var name = PtrToString(item.PortName);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                ports.Add(new WindowsPrinterPort
                {
                    Name = name,
                    MonitorName = PtrToString(item.MonitorName),
                    Description = PtrToString(item.Description)
                });
            }

            return ports
                .OrderBy(port => port.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? PtrToString(IntPtr value) =>
        value == IntPtr.Zero ? null : Marshal.PtrToStringUni(value);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumPrinters(
        int flags,
        string? name,
        int level,
        IntPtr printerEnum,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumPorts(
        string? name,
        int level,
        IntPtr ports,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo2
    {
        public IntPtr ServerName;
        public IntPtr PrinterName;
        public IntPtr ShareName;
        public IntPtr PortName;
        public IntPtr DriverName;
        public IntPtr Comment;
        public IntPtr Location;
        public IntPtr DevMode;
        public IntPtr SepFile;
        public IntPtr PrintProcessor;
        public IntPtr Datatype;
        public IntPtr Parameters;
        public IntPtr SecurityDescriptor;
        public int Attributes;
        public int Priority;
        public int DefaultPriority;
        public int StartTime;
        public int UntilTime;
        public int Status;
        public int Jobs;
        public int AveragePpm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PortInfo2
    {
        public IntPtr PortName;
        public IntPtr MonitorName;
        public IntPtr Description;
        public int PortType;
        public int Reserved;
    }
}
