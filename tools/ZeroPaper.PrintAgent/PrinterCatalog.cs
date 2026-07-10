using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;

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

    private static readonly string[] ThermalPrinterTokens =
    [
        "pos",
        "thermal",
        "termica",
        "receipt",
        "cupom",
        "80",
        "58",
        "tm-",
        "esc",
        "bematech",
        "elgin",
        "daruma",
        "sweda",
        "epson",
        "rongta",
        "xprinter",
        "xp-",
        "zjiang",
        "gprinter"
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
        return !VirtualPrinterTokens.Any(token => ContainsOrdinal(normalized, token));
    }

    public static bool LooksLikeThermalPrinter(string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return false;
        }

        var normalized = printerName.Trim().ToLowerInvariant();
        return ThermalPrinterTokens.Any(token => ContainsOrdinal(normalized, token));
    }

    private static bool ContainsOrdinal(string value, string token)
    {
        return value.IndexOf(token, StringComparison.Ordinal) >= 0;
    }
}
