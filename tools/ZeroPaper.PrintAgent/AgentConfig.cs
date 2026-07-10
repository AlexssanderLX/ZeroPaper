using System;
using System.IO;
using System.Text.Json;

namespace ZeroPaper.PrintAgent;

internal sealed class AgentConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ApiBaseUrl { get; set; } = "https://zeropaperflow.com.br";
    public string AgentKey { get; set; } = string.Empty;
    public string AgentName { get; set; } = Environment.MachineName;
    public string PrinterName { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 1;
    public bool StartWithWindows { get; set; }

    // Modo de saida: "RealPrinter" (impressora fisica) ou "FilePreview" (salva previa em arquivo).
    public string OutputMode { get; set; } = "RealPrinter";

    // Pasta onde a previa em arquivo e salva. Vazio = pasta padrao em Documentos.
    public string PreviewFolder { get; set; } = string.Empty;

    public bool UseFilePreview =>
        string.Equals(OutputMode, "FilePreview", StringComparison.OrdinalIgnoreCase);

    public static string DefaultPreviewFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ZeroPaper", "previews");

    public string ResolvePreviewFolder() =>
        string.IsNullOrWhiteSpace(PreviewFolder) ? DefaultPreviewFolder : PreviewFolder;

    public static string ConfigDirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroPaper");

    public static string ConfigFilePath => Path.Combine(ConfigDirectoryPath, "print-agent.json");

    public static AgentConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new AgentConfig();
            }

            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<AgentConfig>(json, JsonOptions) ?? new AgentConfig();
        }
        catch
        {
            return new AgentConfig();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectoryPath);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }
}
