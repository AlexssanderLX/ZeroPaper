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
