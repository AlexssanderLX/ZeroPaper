using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ZeroPaper.PrintAgent;

internal sealed class PrintAgentRuntime
{
    private static readonly TimeSpan A4BatchWindow = TimeSpan.FromMilliseconds(1500);

    private readonly PrintAgentApiClient _apiClient = new();
    private DateTime? _lastHeartbeatAtUtc;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public event Action<string>? LogReceived;
    public event Action<string>? StatusChanged;

    public void Start(AgentConfig config)
    {
        if (IsRunning)
        {
            return;
        }

        if (!config.UseFilePreview && !PrinterCatalog.IsPhysicalPrinter(config.PrinterName))
        {
            throw new InvalidOperationException("Selecione uma impressora fisica. Impressoras PDF/XPS exigem salvar arquivo e nao servem para impressao automatica.");
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(config, _cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            if (_loopTask is not null)
            {
                await _loopTask;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _loopTask = null;
            _lastHeartbeatAtUtc = null;
            StatusChanged?.Invoke("Agente parado.");
        }
    }

    public static void ApplyAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);

        if (key is null)
        {
            return;
        }

        const string valueName = "ZeroPaperPrintAgent";

        if (enabled)
        {
            key.SetValue(valueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    private async Task RunLoopAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        StatusChanged?.Invoke("Agente conectado. Aguardando pedidos.");
        LogReceived?.Invoke($"Agente iniciado para {config.ApiBaseUrl}.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_lastHeartbeatAtUtc.HasValue || _lastHeartbeatAtUtc.Value <= DateTime.UtcNow.AddSeconds(-10))
                {
                    await _apiClient.HeartbeatAsync(config, cancellationToken);
                    _lastHeartbeatAtUtc = DateTime.UtcNow;
                }

                var job = await _apiClient.ClaimNextAsync(config, cancellationToken);

                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, config.PollIntervalSeconds)), cancellationToken);
                    continue;
                }

                var jobsToPrint = await CollectBatchAsync(config, job, cancellationToken);
                var batchLabel = jobsToPrint.Count == 1
                    ? $"pedido #{jobsToPrint[0].Number}"
                    : $"{jobsToPrint.Count} pedidos";

                StatusChanged?.Invoke($"Imprimindo {batchLabel}...");
                LogReceived?.Invoke($"Fila recebida para impressao: {string.Join(", ", jobsToPrint.Select(item => $"#{item.Number}"))}.");

                try
                {
                    if (config.UseFilePreview)
                    {
                        var previewFolder = config.ResolvePreviewFolder();
                        var savedPaths = new List<string>();

                        foreach (var jobToSave in jobsToPrint)
                        {
                            savedPaths.Add(PrintSlipRenderer.SavePreviewImage(jobToSave, previewFolder));
                        }

                        await _apiClient.CompleteBatchAsync(config, jobsToPrint.Select(item => item.OrderId).ToList(), cancellationToken);
                        LogReceived?.Invoke($"Previa salva para {batchLabel}: {string.Join(", ", savedPaths)}");
                        StatusChanged?.Invoke("Previa salva em arquivo.");
                    }
                    else
                    {
                        await PrintSlipRenderer.PrintAsync(config.PrinterName, jobsToPrint, cancellationToken);
                        await _apiClient.CompleteBatchAsync(config, jobsToPrint.Select(item => item.OrderId).ToList(), cancellationToken);
                        LogReceived?.Invoke($"Impressao concluida para {batchLabel}.");
                        StatusChanged?.Invoke("Pedido enviado para a impressora.");
                    }
                }
                catch (Exception printError)
                {
                    await _apiClient.FailBatchAsync(config, jobsToPrint.Select(item => item.OrderId).ToList(), printError.Message, cancellationToken);
                    LogReceived?.Invoke($"Falha ao processar {batchLabel}: {printError.Message}");
                    StatusChanged?.Invoke(config.UseFilePreview ? "Falha ao salvar previa." : "Falha ao imprimir. Verifique a impressora.");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception error)
            {
                LogReceived?.Invoke($"Falha de comunicacao: {error.Message}");
                StatusChanged?.Invoke("Sem comunicacao com o ZeroPaper.");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, config.PollIntervalSeconds)), cancellationToken);
            }
        }
    }

    private async Task<List<PrintOrderJob>> CollectBatchAsync(AgentConfig config, PrintOrderJob firstJob, CancellationToken cancellationToken)
    {
        var jobs = new List<PrintOrderJob> { firstJob };

        if (!string.Equals(firstJob.PaperProfile, "A4", StringComparison.OrdinalIgnoreCase) || firstJob.OrdersPerPage <= 1)
        {
            return jobs;
        }

        var deadline = DateTime.UtcNow.Add(A4BatchWindow);
        while (jobs.Count < firstJob.OrdersPerPage && DateTime.UtcNow < deadline)
        {
            var nextJob = await _apiClient.ClaimNextAsync(config, cancellationToken);
            if (nextJob is null)
            {
                await Task.Delay(180, cancellationToken);
                continue;
            }

            jobs.Add(nextJob);
        }

        return jobs;
    }
}
