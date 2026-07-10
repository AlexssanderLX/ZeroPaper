using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroPaper.PrintAgent;

public sealed class MainForm : Form
{
    private readonly TextBox _apiBaseUrlInput = new();
    private readonly TextBox _agentKeyInput = new();
    private readonly TextBox _agentNameInput = new();
    private readonly ComboBox _printerSelector = new();
    private readonly ComboBox _outputModeSelector = new();
    private readonly TextBox _previewFolderInput = new();
    private readonly Button _browsePreviewButton = new();
    private readonly NumericUpDown _pollIntervalInput = new();
    private readonly CheckBox _autoStartToggle = new();
    private readonly Label _statusValue = new();
    private readonly Panel _statusDot = new();
    private readonly TextBox _logBox = new();
    private readonly Button _saveButton = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _refreshPrintersButton = new();
    private readonly Button _testPrintButton = new();
    private readonly Label _printerHint = new();

    private readonly PrintAgentRuntime _runtime = new();
    private AgentConfig _config = AgentConfig.Load();

    public MainForm()
    {
        Text = "ZeroPaper Impressao";
        MinimumSize = new Size(940, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(34, 24, 20);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();
        BindEvents();
        LoadConfigIntoForm();
        RefreshPrinters();
        UpdateRuntimeButtons();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_runtime.IsRunning)
        {
            _runtime.StopAsync().GetAwaiter().GetResult();
        }

        base.OnFormClosing(e);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = Color.FromArgb(34, 24, 20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var headerCard = CreateCard();
        headerCard.Controls.Add(BuildHeaderPanel());

        var configCard = CreateCard();
        configCard.Controls.Add(BuildConfigPanel());

        var logCard = CreateCard();
        logCard.Controls.Add(BuildLogPanel());

        root.Controls.Add(headerCard, 0, 0);
        root.Controls.Add(configCard, 0, 1);
        root.Controls.Add(logCard, 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var eyebrow = new Label
        {
            AutoSize = true,
            Text = "AGENTE WINDOWS",
            ForeColor = Color.FromArgb(150, 113, 92),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        };

        var title = new Label
        {
            AutoSize = true,
            Text = "Impressao automatica da unidade",
            ForeColor = Color.FromArgb(40, 29, 23),
            Font = new Font("Georgia", 22F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var copy = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(620, 0),
            Text = "O app consulta o ZeroPaper, identifica pedido novo, imprime automaticamente e confirma a integridade da impressao no backend.",
            ForeColor = Color.FromArgb(95, 74, 62)
        };

        var statusBadge = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.FromArgb(244, 236, 228),
            Padding = new Padding(14, 10, 16, 10),
            Margin = new Padding(12, 0, 0, 0)
        };

        _statusDot.Size = new Size(12, 12);
        _statusDot.Margin = new Padding(0, 5, 9, 0);
        _statusDot.BackColor = Color.FromArgb(176, 142, 120);
        _statusDot.Paint += (sender, paintArgs) =>
        {
            if (sender is Panel dot)
            {
                paintArgs.Graphics.Clear(dot.Parent?.BackColor ?? Color.FromArgb(244, 236, 228));
                paintArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(dot.BackColor);
                paintArgs.Graphics.FillEllipse(brush, 0, 0, dot.Width - 1, dot.Height - 1);
            }
        };

        statusBadge.Controls.Add(_statusDot);
        statusBadge.Controls.Add(_statusValue);
        _statusValue.AutoSize = true;
        _statusValue.Text = "Agente parado";
        _statusValue.ForeColor = Color.FromArgb(61, 47, 38);
        _statusValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        panel.Controls.Add(eyebrow, 0, 0);
        panel.Controls.Add(statusBadge, 1, 0);
        panel.Controls.Add(title, 0, 1);
        panel.Controls.Add(copy, 0, 2);
        panel.SetColumnSpan(title, 2);
        panel.SetColumnSpan(copy, 2);

        return panel;
    }

    private Control BuildConfigPanel()
    {
        var column = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Codigo da unidade
        column.Controls.Add(CreateSectionTitle("1. Codigo da unidade"));
        column.Controls.Add(CreateField("Cole o codigo gerado no painel", _agentKeyInput));
        column.Controls.Add(CreateHint("No site: Impressao -> Gerar codigo -> Copiar. Depois cole aqui."));

        // 2. Impressora
        column.Controls.Add(CreateSectionTitle("2. Impressora"));
        var printerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };
        printerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        printerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _printerSelector.Dock = DockStyle.Top;
        _printerSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _printerSelector.Height = 38;
        ConfigureSecondaryButton(_refreshPrintersButton, "Atualizar");
        printerRow.Controls.Add(CreateField("Impressora da cozinha", _printerSelector), 0, 0);
        printerRow.Controls.Add(_refreshPrintersButton, 1, 0);
        column.Controls.Add(printerRow);

        _printerHint.AutoSize = true;
        _printerHint.MaximumSize = new Size(760, 0);
        _printerHint.ForeColor = Color.FromArgb(125, 83, 63);
        _printerHint.Margin = new Padding(2, -2, 0, 10);
        column.Controls.Add(_printerHint);

        _outputModeSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputModeSelector.Height = 38;
        _outputModeSelector.Items.Add("Impressora real");
        _outputModeSelector.Items.Add("Salvar previa em arquivo");
        _outputModeSelector.SelectedIndex = 0;
        column.Controls.Add(CreateField("Modo de saida", _outputModeSelector));

        var previewRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ConfigureSecondaryButton(_browsePreviewButton, "Escolher pasta");
        previewRow.Controls.Add(CreateField("Pasta da previa", _previewFolderInput), 0, 0);
        previewRow.Controls.Add(_browsePreviewButton, 1, 0);
        column.Controls.Add(previewRow);
        column.Controls.Add(CreateHint("No modo previa o agente nao usa impressora: salva o cupom como imagem PNG na pasta escolhida."));

        // Acoes principais
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        ConfigureHeroButton(_startButton, "Conectar agente");
        ConfigureSecondaryButton(_stopButton, "Parar");
        ConfigureSecondaryButton(_testPrintButton, "Testar impressao");
        actions.Controls.Add(_startButton);
        actions.Controls.Add(_stopButton);
        actions.Controls.Add(_testPrintButton);
        column.Controls.Add(actions);
        column.Controls.Add(CreateHint("Ao conectar, o agente salva tudo, fica online e imprime os pedidos novos sozinho. Pode minimizar esta janela."));

        // Avancado
        column.Controls.Add(CreateSectionTitle("Avancado"));
        var advRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        advRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        advRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        advRow.Controls.Add(CreateField("URL do ZeroPaper", _apiBaseUrlInput), 0, 0);
        advRow.Controls.Add(CreateField("Nome do agente", _agentNameInput), 1, 0);
        column.Controls.Add(advRow);

        _pollIntervalInput.Minimum = 1;
        _pollIntervalInput.Maximum = 10;
        _pollIntervalInput.Value = 1;
        _pollIntervalInput.Width = 120;
        column.Controls.Add(CreateField("Intervalo de consulta (segundos)", _pollIntervalInput));

        _autoStartToggle.Text = "Iniciar com o Windows";
        _autoStartToggle.AutoSize = true;
        _autoStartToggle.ForeColor = Color.FromArgb(63, 46, 37);
        _autoStartToggle.Margin = new Padding(4, 4, 0, 12);
        column.Controls.Add(_autoStartToggle);

        ConfigureSecondaryButton(_saveButton, "Salvar sem conectar");
        column.Controls.Add(_saveButton);

        return column;
    }

    private Control BuildLogPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true
        };

        var title = new Label
        {
            AutoSize = true,
            Text = "Log da fila de impressao",
            Font = new Font("Georgia", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 29, 23),
            Margin = new Padding(0, 0, 0, 10)
        };

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Height = 240;
        _logBox.BackColor = Color.FromArgb(252, 249, 245);
        _logBox.ForeColor = Color.FromArgb(57, 41, 33);
        _logBox.BorderStyle = BorderStyle.FixedSingle;

        panel.Controls.Add(title);
        panel.Controls.Add(_logBox);
        return panel;
    }

    private Control CreateField(string label, Control input)
    {
        var wrapper = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var labelControl = new Label
        {
            AutoSize = true,
            Text = label,
            ForeColor = Color.FromArgb(63, 46, 37),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        };

        if (input is TextBox textBox)
        {
            textBox.Dock = DockStyle.Top;
            textBox.Height = 38;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        wrapper.Controls.Add(labelControl);
        wrapper.Controls.Add(input);
        return wrapper;
    }

    private static Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.FromArgb(248, 241, 234),
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 16)
        };
    }

    private static void ConfigurePrimaryButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Padding = new Padding(16, 10, 16, 10);
        button.BackColor = Color.FromArgb(34, 24, 20);
        button.ForeColor = Color.FromArgb(255, 248, 242);
        button.Margin = new Padding(0, 0, 10, 10);
    }

    private static void ConfigureSecondaryButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Padding = new Padding(14, 10, 14, 10);
        button.BackColor = Color.FromArgb(235, 226, 218);
        button.ForeColor = Color.FromArgb(63, 46, 37);
        button.Margin = new Padding(0, 0, 10, 10);
    }

    private static void ConfigureHeroButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Padding = new Padding(24, 12, 24, 12);
        button.BackColor = Color.FromArgb(63, 158, 106);
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        button.Margin = new Padding(0, 0, 10, 10);
    }

    private static Control CreateSectionTitle(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            ForeColor = Color.FromArgb(40, 29, 23),
            Font = new Font("Georgia", 13F, FontStyle.Bold),
            Margin = new Padding(0, 16, 0, 8)
        };
    }

    private static Control CreateHint(string text)
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Text = text,
            ForeColor = Color.FromArgb(95, 74, 62),
            Margin = new Padding(2, -2, 0, 10)
        };
    }

    private void BindEvents()
    {
        _refreshPrintersButton.Click += (_, _) => RefreshPrinters();
        _browsePreviewButton.Click += (_, _) => BrowsePreviewFolder();
        _outputModeSelector.SelectedIndexChanged += (_, _) => UpdateOutputModeUi();
        _saveButton.Click += async (_, _) => await SaveConfigAsync(startAfterSave: false);
        _startButton.Click += async (_, _) => await SaveConfigAsync(startAfterSave: true);
        _stopButton.Click += async (_, _) => await StopRuntimeAsync();
        _testPrintButton.Click += async (_, _) => await TestPrintAsync();

        _runtime.StatusChanged += UpdateStatus;
        _runtime.LogReceived += AppendLog;
    }

    private void LoadConfigIntoForm()
    {
        _apiBaseUrlInput.Text = _config.ApiBaseUrl;
        _agentKeyInput.Text = _config.AgentKey;
        _agentNameInput.Text = string.IsNullOrWhiteSpace(_config.AgentName) ? Environment.MachineName : _config.AgentName;
        _pollIntervalInput.Value = Math.Max(_pollIntervalInput.Minimum, Math.Min(_pollIntervalInput.Maximum, _config.PollIntervalSeconds));
        _autoStartToggle.Checked = _config.StartWithWindows;
        _outputModeSelector.SelectedIndex = _config.UseFilePreview ? 1 : 0;
        _previewFolderInput.Text = string.IsNullOrWhiteSpace(_config.PreviewFolder)
            ? AgentConfig.DefaultPreviewFolder
            : _config.PreviewFolder;
        UpdateOutputModeUi();
    }

    private void UpdateOutputModeUi()
    {
        var usePreview = _outputModeSelector.SelectedIndex == 1;
        _previewFolderInput.Enabled = usePreview;
        _browsePreviewButton.Enabled = usePreview;
        _printerSelector.Enabled = !usePreview;
    }

    private void BrowsePreviewFolder()
    {
        using var dialog = new FolderBrowserDialog();

        if (!string.IsNullOrWhiteSpace(_previewFolderInput.Text))
        {
            dialog.SelectedPath = _previewFolderInput.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _previewFolderInput.Text = dialog.SelectedPath;
        }
    }

    private void RefreshPrinters()
    {
        var previousSelection = _printerSelector.SelectedItem?.ToString() ?? _config.PrinterName;
        var availablePrinters = PrinterCatalog.GetPhysicalPrinters();

        _printerSelector.Items.Clear();
        foreach (var printer in availablePrinters)
        {
            _printerSelector.Items.Add(printer);
        }

        if (!string.IsNullOrWhiteSpace(previousSelection) && _printerSelector.Items.Contains(previousSelection))
        {
            _printerSelector.SelectedItem = previousSelection;
        }
        else if (_printerSelector.Items.Count > 0)
        {
            _printerSelector.SelectedIndex = 0;
        }

        _printerHint.Text = availablePrinters.Count == 0
            ? "Nenhuma impressora fisica foi encontrada. Conecte uma impressora termica ou de rede. Impressoras PDF/XPS nao servem para a fila automatica."
            : "Use uma impressora fisica da unidade. Impressoras PDF/XPS pedem para salvar arquivo e nao servem para pedido automatico.";
    }

    private AgentConfig ReadConfigFromForm()
    {
        return new AgentConfig
        {
            ApiBaseUrl = _apiBaseUrlInput.Text.Trim(),
            AgentKey = _agentKeyInput.Text.Trim(),
            AgentName = _agentNameInput.Text.Trim(),
            PrinterName = _printerSelector.SelectedItem?.ToString() ?? string.Empty,
            PollIntervalSeconds = (int)_pollIntervalInput.Value,
            StartWithWindows = _autoStartToggle.Checked,
            OutputMode = _outputModeSelector.SelectedIndex == 1 ? "FilePreview" : "RealPrinter",
            PreviewFolder = _previewFolderInput.Text.Trim()
        };
    }

    private async Task SaveConfigAsync(bool startAfterSave)
    {
        try
        {
            var nextConfig = ReadConfigFromForm();
            ValidateConfig(nextConfig);
            nextConfig.Save();
            PrintAgentRuntime.ApplyAutoStart(nextConfig.StartWithWindows);
            _config = nextConfig;
            AppendLog("Configuracao salva.");

            if (startAfterSave)
            {
                if (_runtime.IsRunning)
                {
                    await _runtime.StopAsync();
                }

                _runtime.Start(_config);
            }

            UpdateRuntimeButtons();
            UpdateStatus(startAfterSave ? "Agente iniciado." : "Configuracao salva.");
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "ZeroPaper Impressao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task StopRuntimeAsync()
    {
        await _runtime.StopAsync();
        UpdateRuntimeButtons();
    }

    private async Task TestPrintAsync()
    {
        try
        {
            var config = ReadConfigFromForm();
            ValidateConfig(config, requireAgentKey: false);

            if (config.UseFilePreview)
            {
                var savedPath = PrintSlipRenderer.SavePreviewTestImage("ZeroPaper", config.ResolvePreviewFolder());
                AppendLog($"Previa de teste salva: {savedPath}");
                UpdateStatus("Previa de teste salva em arquivo.");
            }
            else
            {
                await PrintSlipRenderer.PrintTestAsync(config.PrinterName, "ZeroPaper", CancellationToken.None);
                AppendLog("Teste de impressao enviado.");
                UpdateStatus("Teste enviado para a impressora.");
            }
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "ZeroPaper Impressao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateRuntimeButtons()
    {
        _startButton.Enabled = !_runtime.IsRunning;
        _stopButton.Enabled = _runtime.IsRunning;
    }

    private static void ValidateConfig(AgentConfig config, bool requireAgentKey = true)
    {
        if (string.IsNullOrWhiteSpace(config.ApiBaseUrl))
        {
            throw new InvalidOperationException("Informe a URL do ZeroPaper.");
        }

        if (requireAgentKey && string.IsNullOrWhiteSpace(config.AgentKey))
        {
            throw new InvalidOperationException("Cole a chave do agente gerada na tela de Impressao.");
        }

        if (string.IsNullOrWhiteSpace(config.AgentName))
        {
            throw new InvalidOperationException("Informe um nome para o agente.");
        }

        if (!config.UseFilePreview && string.IsNullOrWhiteSpace(config.PrinterName))
        {
            throw new InvalidOperationException("Selecione a impressora da unidade ou troque para o modo Salvar previa em arquivo.");
        }
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void UpdateStatus(string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatus(status));
            return;
        }

        _statusValue.Text = status;

        var isError =
            status.IndexOf("Falha", StringComparison.OrdinalIgnoreCase) >= 0 ||
            status.IndexOf("Sem comunic", StringComparison.OrdinalIgnoreCase) >= 0;
        _statusDot.BackColor = _runtime.IsRunning
            ? Color.FromArgb(63, 158, 106)
            : isError
                ? Color.FromArgb(200, 86, 74)
                : Color.FromArgb(176, 142, 120);
        _statusDot.Invalidate();

        UpdateRuntimeButtons();
    }
}
