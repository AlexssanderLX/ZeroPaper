using System.Drawing.Printing;

namespace ZeroPaper.PrintAgent;

internal static class PrinterCatalog
{
    private static readonly string[] VirtualPrinterTokens =
    [
        "pdf",
        "xps",
        "onenote",
        "fax"
    ];

    public static IReadOnlyList<string> GetPhysicalPrinters()
    {
        var printers = new List<string>();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (IsPhysicalPrinter(printer))
            {
                printers.Add(printer);
            }
        }

        return printers;
    }

    public static bool IsPhysicalPrinter(string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return false;
        }

        var normalized = printerName.Trim().ToLowerInvariant();
        return !VirtualPrinterTokens.Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }
}
