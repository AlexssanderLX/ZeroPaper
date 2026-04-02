using System.Drawing.Printing;

namespace ZeroPaper.PrintAgent;

internal static class PrintSlipRenderer
{
    public static Task PrintTestAsync(string printerName, string restaurantName, CancellationToken cancellationToken)
    {
        var testJob = new PrintOrderJob
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

        return PrintAsync(printerName, testJob, cancellationToken);
    }

    public static Task PrintAsync(string printerName, PrintOrderJob job, CancellationToken cancellationToken)
    {
        return PrintAsync(printerName, new[] { job }, cancellationToken);
    }

    public static Task PrintAsync(string printerName, IReadOnlyList<PrintOrderJob> jobs, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        ArgumentNullException.ThrowIfNull(jobs);

        if (jobs.Count == 0)
        {
            throw new InvalidOperationException("Nao existe pedido para imprimir.");
        }

        if (!PrinterCatalog.IsPhysicalPrinter(printerName))
        {
            throw new InvalidOperationException("Selecione uma impressora fisica da unidade. Impressoras PDF/XPS nao funcionam para a fila automatica.");
        }

        return Task.Run(() =>
        {
            using var document = new PrintDocument();
            document.PrinterSettings.PrinterName = printerName;

            if (!document.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException("A impressora selecionada nao esta disponivel.");
            }

            var primaryJob = jobs[0];
            var pageGroups = BuildPageGroups(jobs);
            var pageIndex = 0;

            if (string.Equals(primaryJob.PaperProfile, "A4", StringComparison.OrdinalIgnoreCase))
            {
                TryApplyA4Paper(document);
            }

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
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 9, FontStyle.Regular);
        using var tinyFont = new Font("Segoe UI", 8, FontStyle.Regular);
        using var brush = new SolidBrush(Color.Black);
        var graphics = args.Graphics ?? throw new InvalidOperationException("Superficie de impressao indisponivel.");

        var left = args.MarginBounds.Left;
        var right = args.MarginBounds.Right;
        var width = args.MarginBounds.Width;
        var y = args.MarginBounds.Top;

        graphics.DrawString(job.RestaurantName, titleFont, brush, new RectangleF(left, y, width, 28));
        y += 30;
        graphics.DrawString($"Pedido #{job.Number}  |  {job.TableName}", subtitleFont, brush, new RectangleF(left, y, width, 20));
        y += 22;
        graphics.DrawString($"Recebido em: {job.SubmittedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}", bodyFont, brush, new RectangleF(left, y, width, 18));
        y += 18;

        if (!string.IsNullOrWhiteSpace(job.CustomerName))
        {
            graphics.DrawString($"Cliente: {job.CustomerName}", bodyFont, brush, new RectangleF(left, y, width, 18));
            y += 18;
        }

        graphics.DrawLine(Pens.Black, left, y, right, y);
        y += 10;

        foreach (var item in job.Items)
        {
            var label = string.IsNullOrWhiteSpace(item.CategoryName)
                ? item.Name
                : $"{item.CategoryName}: {item.Name}";

            graphics.DrawString($"{item.Quantity:0.##}x {label}", subtitleFont, brush, new RectangleF(left, y, width - 90, 20));
            graphics.DrawString(item.TotalPrice.ToString("C"), subtitleFont, brush, new RectangleF(right - 90, y, 90, 20), new StringFormat
            {
                Alignment = StringAlignment.Far
            });
            y += 20;

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                graphics.DrawString($"Obs: {item.Notes}", tinyFont, brush, new RectangleF(left + 8, y, width - 8, 28));
                y += 24;
            }
        }

        if (!string.IsNullOrWhiteSpace(job.Notes))
        {
            graphics.DrawLine(Pens.Black, left, y, right, y);
            y += 10;
            graphics.DrawString($"Observacoes gerais: {job.Notes}", bodyFont, brush, new RectangleF(left, y, width, 40));
            y += 38;
        }

        graphics.DrawLine(Pens.Black, left, y, right, y);
        y += 10;
        graphics.DrawString($"Pagamento informado: {job.PaymentMethod}", bodyFont, brush, new RectangleF(left, y, width, 18));
        y += 18;
        graphics.DrawString($"Total: {job.TotalAmount:C}", subtitleFont, brush, new RectangleF(left, y, width, 20));
        y += 22;
        graphics.DrawString("ZeroPaper Impressao automatica", tinyFont, brush, new RectangleF(left, y, width, 16));
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
            if (paperSize.Kind == PaperKind.A4 || paperSize.PaperName.Contains("A4", StringComparison.OrdinalIgnoreCase))
            {
                document.DefaultPageSettings.PaperSize = paperSize;
                break;
            }
        }

        document.DefaultPageSettings.Landscape = false;
    }
}
