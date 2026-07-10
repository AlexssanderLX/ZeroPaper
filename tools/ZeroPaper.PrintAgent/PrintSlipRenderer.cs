using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroPaper.PrintAgent;

internal static class PrintSlipRenderer
{
    private const int ThermalFallbackWidth = 315;
    private const int ThermalMinimumPaperHeight = 900;
    private const int ThermalMaximumPaperHeight = 9000;

    private static PrintOrderJob BuildTestJob(string restaurantName)
    {
        return new PrintOrderJob
        {
            OrderId = Guid.NewGuid(),
            Number = 999,
            PaperProfile = "Thermal80mm",
            OrdersPerPage = 1,
            RestaurantName = restaurantName,
            TableName = "Mesa teste",
            PaymentMethod = "Undefined",
            SubmittedAtUtc = DateTime.UtcNow,
            TotalAmount = 0,
            Notes = "Teste do agente local.",
            Items =
            [
                new PrintOrderItem
                {
                    CategoryName = "Teste",
                    Name = "Comanda automatica",
                    Quantity = 1,
                    UnitPrice = 0,
                    TotalPrice = 0
                }
            ]
        };
    }

    public static Task PrintTestAsync(string printerName, string restaurantName, CancellationToken cancellationToken)
    {
        return PrintAsync(printerName, BuildTestJob(restaurantName), cancellationToken);
    }

    public static string SavePreviewTestImage(string restaurantName, string folder)
    {
        return SavePreviewImage(BuildTestJob(restaurantName), folder);
    }

    public static Task PrintAsync(string printerName, PrintOrderJob job, CancellationToken cancellationToken)
    {
        return PrintAsync(printerName, new[] { job }, cancellationToken);
    }

    public static Task PrintAsync(string printerName, IReadOnlyList<PrintOrderJob> jobs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new ArgumentException("Informe a impressora da unidade.", nameof(printerName));
        }

        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (jobs.Count == 0)
        {
            throw new InvalidOperationException("Nao existe pedido para imprimir.");
        }

        if (!PrinterCatalog.IsPhysicalPrinter(printerName))
        {
            throw new InvalidOperationException("Selecione uma impressora fisica da unidade. Impressoras PDF/XPS nao funcionam para a fila automatica.");
        }

        var primaryJob = jobs[0];
        if (ShouldUseThermalLayout(printerName, primaryJob))
        {
            return Task.Run(() =>
            {
                foreach (var job in jobs)
                {
                    PrintThermalDocument(printerName, job);
                }
            }, cancellationToken);
        }

        return Task.Run(() =>
        {
            using var document = new PrintDocument();
            document.PrinterSettings.PrinterName = printerName;

            if (!document.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException("A impressora selecionada nao esta disponivel.");
            }

            var pageGroups = BuildPageGroups(jobs);
            var pageIndex = 0;

            TryApplyA4Paper(document);

            document.DocumentName = jobs.Count == 1
                ? $"ZeroPaper Pedido {primaryJob.Number}"
                : $"ZeroPaper Lote {jobs.Count} pedidos";
            document.PrintController = new StandardPrintController();
            document.PrintPage += (_, args) =>
            {
                Render(args, pageGroups[pageIndex], primaryJob.PaperProfile, primaryJob.OrdersPerPage);
                pageIndex += 1;
                args.HasMorePages = pageIndex < pageGroups.Count;
            };
            document.Print();
        }, cancellationToken);
    }

    private static bool ShouldUseThermalLayout(string printerName, PrintOrderJob job)
    {
        if (PrinterCatalog.LooksLikeThermalPrinter(printerName))
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(printerName) &&
               string.Equals(job.PaperProfile, "Thermal80mm", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintThermalDocument(string printerName, PrintOrderJob job)
    {
        using var document = new PrintDocument();
        document.PrinterSettings.PrinterName = printerName;

        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException("A impressora selecionada nao esta disponivel.");
        }

        ApplyThermalPaper(document, EstimateThermalPaperHeight(job));

        document.DocumentName = $"ZeroPaper Pedido {job.Number}";
        document.PrintController = new StandardPrintController();
        document.PrintPage += (_, args) =>
        {
            RenderThermal(args, job);
            args.HasMorePages = false;
        };
        document.Print();
    }

    private static List<List<PrintOrderJob>> BuildPageGroups(IReadOnlyList<PrintOrderJob> jobs)
    {
        var ordersPerPage = jobs[0].OrdersPerPage <= 1 ? 1 : jobs[0].OrdersPerPage;
        var groups = new List<List<PrintOrderJob>>();

        for (var index = 0; index < jobs.Count; index += ordersPerPage)
        {
            groups.Add(jobs.Skip(index).Take(ordersPerPage).ToList());
        }

        return groups;
    }

    private static void Render(PrintPageEventArgs args, IReadOnlyList<PrintOrderJob> jobs, string paperProfile, int ordersPerPage)
    {
        if (string.Equals(paperProfile, "A4", StringComparison.OrdinalIgnoreCase))
        {
            RenderA4(args, jobs, ordersPerPage);
            return;
        }

        RenderThermal(args, jobs[0]);
    }

    private static void RenderThermal(PrintPageEventArgs args, PrintOrderJob job)
    {
        var graphics = args.Graphics ?? throw new InvalidOperationException("Superficie de impressao indisponivel.");

        graphics.PageUnit = GraphicsUnit.Display;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        RenderThermalCore(graphics, ResolveThermalLayout(args), job);
    }

    // Gera uma previa do cupom (mesmo layout termico) em PNG, sem depender de impressora fisica.
    public static string SavePreviewImage(PrintOrderJob job, string folder)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = AgentConfig.DefaultPreviewFolder;
        }

        const int scale = 2;
        var logicalWidth = ThermalFallbackWidth;
        var logicalHeight = EstimateThermalPaperHeight(job);

        using (var bitmap = new Bitmap(logicalWidth * scale, logicalHeight * scale))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.ScaleTransform(scale, scale);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                var safetyMargin = Clamp(logicalWidth * 0.08F, 10F, 20F);
                var layout = new ThermalLayout(
                    safetyMargin,
                    8F,
                    Math.Max(150F, logicalWidth - safetyMargin * 2F),
                    logicalHeight - 8F);

                RenderThermalCore(graphics, layout, job);
            }

            Directory.CreateDirectory(folder);
            var fileName = $"zeropaper-preview-{DateTime.Now:yyyy-MM-dd-HHmmss}-{job.OrderId}.png";
            var path = Path.Combine(folder, fileName);
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }
    }

    private static void RenderThermalCore(Graphics graphics, ThermalLayout layout, PrintOrderJob job)
    {
        using var brush = new SolidBrush(Color.Black);
        var fontScale = Clamp(layout.Width / 260F, 0.78F, 1F);

        using var titleFont = new Font("Segoe UI", 10.4F * fontScale, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 8.6F * fontScale, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 8.0F * fontScale, FontStyle.Regular);
        using var tinyFont = new Font("Segoe UI", 7.2F * fontScale, FontStyle.Regular);

        var left = layout.Left;
        var width = layout.Width;
        var right = layout.Right;
        var y = layout.Top;

        DrawCentered(graphics, job.RestaurantName, titleFont, brush, left, width, ref y);
        DrawCentered(graphics, $"Pedido #{job.Number}", subtitleFont, brush, left, width, ref y);
        DrawCentered(graphics, job.TableName, bodyFont, brush, left, width, ref y);
        DrawCentered(graphics, job.SubmittedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")), tinyFont, brush, left, width, ref y);
        y += 4;
        DrawRule(graphics, left, right, ref y);

        if (HasDeliveryData(job))
        {
            DrawSectionTitle(graphics, "ENTREGA", subtitleFont, brush, left, width, ref y);
            DrawField(graphics, "Cliente", job.CustomerName, bodyFont, brush, left, width, ref y);
            DrawField(graphics, "Telefone", job.DeliveryPhone, bodyFont, brush, left, width, ref y);
            DrawField(graphics, "Endereco", BuildDeliveryAddress(job), bodyFont, brush, left, width, ref y);
            DrawField(graphics, "CEP", job.DeliveryPostalCode, bodyFont, brush, left, width, ref y);

            if (job.DeliveryFreightAmount > 0)
            {
                var freight = FormatMoney(job.DeliveryFreightAmount);
                if (job.DeliveryDistanceKm.HasValue)
                {
                    freight += $" ({job.DeliveryDistanceKm.Value:0.##} km)";
                }

                DrawField(graphics, "Frete", freight, bodyFont, brush, left, width, ref y);
            }

            DrawRule(graphics, left, right, ref y);
        }
        else if (!string.IsNullOrWhiteSpace(job.CustomerName))
        {
            DrawField(graphics, "Cliente", job.CustomerName, bodyFont, brush, left, width, ref y);
            DrawRule(graphics, left, right, ref y);
        }

        DrawSectionTitle(graphics, "ITENS", subtitleFont, brush, left, width, ref y);

        foreach (var item in job.Items)
        {
            var label = string.IsNullOrWhiteSpace(item.CategoryName)
                ? item.Name
                : $"{item.CategoryName}: {item.Name}";

            DrawWrapped(graphics, $"{FormatQuantity(item.Quantity)}x {label}", subtitleFont, brush, left, width, ref y);
            DrawField(graphics, "Valor item", FormatMoney(item.TotalPrice), bodyFont, brush, left, width, ref y);

            foreach (var additional in item.Additionals)
            {
                DrawWrapped(graphics, FormatAdditionalLine(item, additional), bodyFont, brush, left + 8, width - 8, ref y);
                DrawField(graphics, "Valor", FormatAdditionalPrice(item, additional), tinyFont, brush, left + 8, width - 8, ref y);
            }

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                DrawField(graphics, "Obs item", item.Notes, tinyFont, brush, left + 8, width - 8, ref y);
            }

            DrawRule(graphics, left, right, ref y);
        }

        if (!string.IsNullOrWhiteSpace(job.Notes))
        {
            DrawSectionTitle(graphics, "OBSERVACOES", subtitleFont, brush, left, width, ref y);
            DrawWrapped(graphics, job.Notes, bodyFont, brush, left, width, ref y);
            DrawRule(graphics, left, right, ref y);
        }

        DrawField(graphics, "Pagamento", FormatPaymentMethod(job.PaymentMethod), bodyFont, brush, left, width, ref y);
        y += 2;
        DrawCentered(graphics, $"TOTAL {FormatMoney(job.TotalAmount)}", subtitleFont, brush, left, width, ref y);
        y += 6;
        DrawCentered(graphics, "ZeroPaper", tinyFont, brush, left, width, ref y);
    }

    private static void ApplyThermalPaper(PrintDocument document, int height)
    {
        var width = ResolveThermalPaperWidth(document.PrinterSettings, document.DefaultPageSettings.PaperSize);
        var targetHeight = Clamp(height, ThermalMinimumPaperHeight, ThermalMaximumPaperHeight);

        document.DefaultPageSettings.PaperSize = new PaperSize("ZeroPaper automatico", width, targetHeight);
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.OriginAtMargins = false;
        document.DefaultPageSettings.Landscape = false;
    }

    private static int ResolveThermalPaperWidth(PrinterSettings printerSettings, PaperSize defaultPaperSize)
    {
        if (LooksLikeRollPaper(defaultPaperSize))
        {
            return defaultPaperSize.Width;
        }

        var candidates = printerSettings.PaperSizes
            .Cast<PaperSize>()
            .Where(LooksLikeRollPaper)
            .OrderBy(size => Math.Abs(size.Width - ThermalFallbackWidth))
            .ThenByDescending(size => size.Height)
            .ToList();

        return candidates.Count > 0 ? candidates[0].Width : ThermalFallbackWidth;
    }

    private static bool LooksLikeRollPaper(PaperSize? paperSize)
    {
        if (paperSize is null)
        {
            return false;
        }

        return paperSize.Width is >= 180 and <= 380;
    }

    private static int EstimateThermalPaperHeight(PrintOrderJob job)
    {
        var lines = 12;

        if (HasDeliveryData(job))
        {
            lines += 10;
        }

        foreach (var item in job.Items)
        {
            lines += 5;
            lines += item.Additionals.Count * 3;

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                lines += Math.Max(2, item.Notes.Length / 28);
            }
        }

        if (!string.IsNullOrWhiteSpace(job.Notes))
        {
            lines += Math.Max(3, job.Notes.Length / 28);
        }

        return Clamp(220 + lines * 24, ThermalMinimumPaperHeight, ThermalMaximumPaperHeight);
    }

    private static ThermalLayout ResolveThermalLayout(PrintPageEventArgs args)
    {
        var printableArea = args.PageSettings.PrintableArea;
        var pageBounds = args.PageBounds;

        var sourceLeft = printableArea.Width > 0 ? printableArea.Left : pageBounds.Left;
        var sourceTop = printableArea.Height > 0 ? printableArea.Top : pageBounds.Top;
        var sourceWidth = printableArea.Width > 0 ? printableArea.Width : pageBounds.Width;
        var sourceHeight = printableArea.Height > 0 ? printableArea.Height : pageBounds.Height;

        if (sourceWidth <= 0)
        {
            sourceWidth = ThermalFallbackWidth;
        }

        if (sourceHeight <= 0)
        {
            sourceHeight = ThermalMinimumPaperHeight;
        }

        var cappedWidth = Math.Min(sourceWidth, ThermalFallbackWidth);
        var safetyMargin = Clamp(cappedWidth * 0.08F, 10F, 20F);
        var left = sourceLeft + safetyMargin;
        var top = sourceTop + 8F;
        var width = Math.Max(150F, cappedWidth - safetyMargin * 2F);
        var bottom = sourceTop + sourceHeight - 8F;

        return new ThermalLayout(left, top, width, bottom);
    }

    private readonly struct ThermalLayout
    {
        public ThermalLayout(float left, float top, float width, float bottom)
        {
            Left = left;
            Top = top;
            Width = width;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Top { get; }
        public float Width { get; }
        public float Bottom { get; }
        public float Right => Left + Width;
    }

    private static void DrawCentered(Graphics graphics, string? text, Font font, Brush brush, float left, float width, ref float y)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        if (normalized.Length == 0)
        {
            return;
        }

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.Word
        };

        var height = Math.Max(font.GetHeight(graphics) + 4, graphics.MeasureString(normalized, font, (int)width, format).Height + 3);
        graphics.DrawString(normalized, font, brush, new RectangleF(left, y, width, height), format);
        y += height + 1;
    }

    private static void DrawSectionTitle(Graphics graphics, string title, Font font, Brush brush, float left, float width, ref float y)
    {
        DrawWrapped(graphics, title, font, brush, left, width, ref y);
    }

    private static void DrawField(Graphics graphics, string label, string? value, Font font, Brush brush, float left, float width, ref float y)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        DrawWrapped(graphics, $"{label}: {value.Trim()}", font, brush, left, width, ref y);
    }

    private static void DrawWrapped(Graphics graphics, string? text, Font font, Brush brush, float left, float width, ref float y)
    {
        var normalized = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        if (normalized.Length == 0)
        {
            return;
        }

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.Word
        };

        var measured = graphics.MeasureString(normalized, font, (int)Math.Floor(width), format);
        var height = Math.Max(font.GetHeight(graphics) + 4, measured.Height + 3);
        graphics.DrawString(normalized, font, brush, new RectangleF(left, y, width, height), format);
        y += height + 2;
    }

    private static void DrawRule(Graphics graphics, float left, float right, ref float y)
    {
        using var pen = new Pen(Color.Black, 0.8F);
        y += 4;
        graphics.DrawLine(pen, left, y, right, y);
        y += 7;
    }

    private static string FormatMoney(decimal value)
    {
        return $"R$ {value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}";
    }

    private static string FormatQuantity(decimal value)
    {
        return value % 1 == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatAdditionalLine(PrintOrderItem item, PrintOrderAdditional additional)
    {
        var quantityPrefix = item.Quantity > 1 ? $"{FormatQuantity(item.Quantity)}x " : string.Empty;

        return $"+ {quantityPrefix}{FormatAdditionalLabel(additional)}";
    }

    private static string FormatAdditionalLabel(PrintOrderAdditional additional)
    {
        var groupName = additional.GroupName?.Trim() ?? string.Empty;
        var optionName = additional.OptionName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(groupName) ||
            string.Equals(groupName, optionName, StringComparison.OrdinalIgnoreCase))
        {
            return optionName;
        }

        return $"{groupName}: {optionName}";
    }

    private static string FormatAdditionalPrice(PrintOrderItem item, PrintOrderAdditional additional)
    {
        var price = FormatMoney(additional.UnitPrice);

        return item.Quantity > 1 ? $"{price} cada" : price;
    }

    private static string FormatPaymentMethod(string? value)
    {
        return value switch
        {
            "Pix" => "Pix",
            "Credit" => "Credito",
            "Debit" => "Debito",
            "Cash" => "Dinheiro",
            "Undefined" => "A definir",
            _ => string.IsNullOrWhiteSpace(value) ? "A definir" : value
        };
    }

    private static bool HasDeliveryData(PrintOrderJob job)
    {
        return !string.IsNullOrWhiteSpace(job.DeliveryPhone) ||
               !string.IsNullOrWhiteSpace(job.DeliveryAddress) ||
               !string.IsNullOrWhiteSpace(job.DeliveryNumber) ||
               !string.IsNullOrWhiteSpace(job.DeliveryComplement) ||
               !string.IsNullOrWhiteSpace(job.DeliveryPostalCode) ||
               job.DeliveryFreightAmount > 0;
    }

    private static string? BuildDeliveryAddress(PrintOrderJob job)
    {
        var address = $"{job.DeliveryAddress?.Trim() ?? string.Empty}, {job.DeliveryNumber?.Trim() ?? string.Empty}"
            .Trim()
            .TrimEnd(',')
            .Trim();

        if (!string.IsNullOrWhiteSpace(job.DeliveryComplement))
        {
            address = string.IsNullOrWhiteSpace(address)
                ? job.DeliveryComplement.Trim()
                : $"{address} ({job.DeliveryComplement.Trim()})";
        }

        return address;
    }

    private static void RenderA4(PrintPageEventArgs args, IReadOnlyList<PrintOrderJob> jobs, int ordersPerPage)
    {
        var graphics = args.Graphics ?? throw new InvalidOperationException("Superficie de impressao indisponivel.");
        using var borderPen = new Pen(Color.Black, 1F);

        var bounds = args.MarginBounds;
        var slots = ordersPerPage switch
        {
            4 => BuildFourUpSlots(bounds),
            2 => BuildTwoUpSlots(bounds),
            _ => new[] { bounds }
        };

        for (var index = 0; index < jobs.Count && index < slots.Length; index += 1)
        {
            var slot = Rectangle.Inflate(slots[index], -6, -6);
            graphics.DrawRectangle(borderPen, slot);
            RenderJobBlock(graphics, jobs[index], slot);
        }
    }

    private static Rectangle[] BuildTwoUpSlots(Rectangle bounds)
    {
        var height = bounds.Height / 2;
        return
        [
            new Rectangle(bounds.Left, bounds.Top, bounds.Width, height),
            new Rectangle(bounds.Left, bounds.Top + height, bounds.Width, bounds.Height - height)
        ];
    }

    private static Rectangle[] BuildFourUpSlots(Rectangle bounds)
    {
        var width = bounds.Width / 2;
        var height = bounds.Height / 2;
        return
        [
            new Rectangle(bounds.Left, bounds.Top, width, height),
            new Rectangle(bounds.Left + width, bounds.Top, bounds.Width - width, height),
            new Rectangle(bounds.Left, bounds.Top + height, width, bounds.Height - height),
            new Rectangle(bounds.Left + width, bounds.Top + height, bounds.Width - width, bounds.Height - height)
        ];
    }

    private static void RenderJobBlock(Graphics graphics, PrintOrderJob job, Rectangle bounds)
    {
        using var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        using var tinyFont = new Font("Segoe UI", 7.5F, FontStyle.Regular);
        using var brush = new SolidBrush(Color.Black);

        var left = bounds.Left + 12;
        var right = bounds.Right - 12;
        var width = bounds.Width - 24;
        var y = bounds.Top + 12;

        graphics.DrawString(job.RestaurantName, titleFont, brush, new RectangleF(left, y, width, 24));
        y += 24;
        graphics.DrawString($"Pedido #{job.Number} | {job.TableName}", subtitleFont, brush, new RectangleF(left, y, width, 18));
        y += 18;
        graphics.DrawString($"Recebido em: {job.SubmittedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}", bodyFont, brush, new RectangleF(left, y, width, 16));
        y += 18;

        foreach (var item in job.Items)
        {
            var label = string.IsNullOrWhiteSpace(item.CategoryName)
                ? item.Name
                : $"{item.CategoryName}: {item.Name}";

            graphics.DrawString($"{item.Quantity:0.##}x {label}", bodyFont, brush, new RectangleF(left, y, width - 80, 18));
            graphics.DrawString(item.TotalPrice.ToString("C"), bodyFont, brush, new RectangleF(right - 80, y, 80, 18), new StringFormat
            {
                Alignment = StringAlignment.Far
            });
            y += 16;

            foreach (var additional in item.Additionals)
            {
                graphics.DrawString(FormatAdditionalLine(item, additional), tinyFont, brush, new RectangleF(left + 8, y, width - 80, 16));
                graphics.DrawString(FormatAdditionalPrice(item, additional), tinyFont, brush, new RectangleF(right - 80, y, 80, 16), new StringFormat
                {
                    Alignment = StringAlignment.Far
                });
                y += 14;
            }

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                graphics.DrawString($"Obs: {item.Notes}", tinyFont, brush, new RectangleF(left + 8, y, width - 8, 24));
                y += 20;
            }
        }

        if (!string.IsNullOrWhiteSpace(job.Notes))
        {
            y += 4;
            graphics.DrawString($"Observacoes: {job.Notes}", tinyFont, brush, new RectangleF(left, y, width, 32));
            y += 26;
        }

        graphics.DrawString($"Pagamento: {job.PaymentMethod}", tinyFont, brush, new RectangleF(left, bounds.Bottom - 36, width, 14));
        graphics.DrawString($"Total: {job.TotalAmount:C}", subtitleFont, brush, new RectangleF(left, bounds.Bottom - 22, width, 18));
    }

    private static void TryApplyA4Paper(PrintDocument document)
    {
        foreach (PaperSize paperSize in document.PrinterSettings.PaperSizes)
        {
            if (paperSize.Kind == PaperKind.A4 || ContainsOrdinalIgnoreCase(paperSize.PaperName, "A4"))
            {
                document.DefaultPageSettings.PaperSize = paperSize;
                break;
            }
        }

        document.DefaultPageSettings.Landscape = false;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static bool ContainsOrdinalIgnoreCase(string? value, string token)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
