using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Documents;

public class DailyCashReportDocument : IDocument
{
    private readonly DailyCashReportData _data;

    public DailyCashReportDocument(DailyCashReportData data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(text => text.FontSize(9).FontColor("#2F241E"));

            page.Header().Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("Relatorio diario do caixa").FontSize(20).SemiBold().FontColor("#241A14");
                column.Item().Text(_data.RestaurantName).FontSize(13).SemiBold().FontColor("#6B4D39");
                column.Item().Text($"Data de referencia: {_data.ReportDateLabel}");
                column.Item().Text($"Gerado em: {_data.GeneratedAtLabel}").FontColor("#705E53");
            });

            page.Content().PaddingTop(12).Column(column =>
            {
                column.Spacing(14);
                ComposeMetricGrid(column.Item());
                ComposeSimpleTable(
                    column.Item(),
                    "Formas de pagamento registradas hoje",
                    ["Forma", "Quantidade", "Total"],
                    _data.PaymentSummaries.Select(item => new[] { item.Label, item.CountLabel, item.TotalLabel }).ToList(),
                    "Nenhum pagamento registrado hoje.");
                ComposeSimpleTable(
                    column.Item(),
                    "Pedidos a cobrar hoje",
                    ["Pedido", "Mesa", "Situacao", "Pagamento", "Total", "Horario", "Detalhes"],
                    _data.PendingOrders.Select(item => new[]
                    {
                        item.OrderLabel,
                        item.TableLabel,
                        item.StatusLabel,
                        item.PaymentLabel,
                        item.TotalLabel,
                        item.TimeLabel,
                        BuildOrderDetails(item)
                    }).ToList(),
                    "Nenhum pedido pendente registrado hoje.");
                ComposeSimpleTable(
                    column.Item(),
                    "Pedidos pagos hoje",
                    ["Pedido", "Mesa", "Situacao", "Pagamento", "Total", "Horario", "Detalhes"],
                    _data.PaidOrders.Select(item => new[]
                    {
                        item.OrderLabel,
                        item.TableLabel,
                        item.StatusLabel,
                        item.PaymentLabel,
                        item.TotalLabel,
                        item.TimeLabel,
                        BuildOrderDetails(item)
                    }).ToList(),
                    "Nenhum pedido pago registrado hoje.");
                ComposeSimpleTable(
                    column.Item(),
                    "Divergencias de forma de pagamento",
                    ["Pedido", "Mesa", "Solicitado", "Aplicado", "Total", "Contexto"],
                    _data.PaymentDifferences.Select(item => new[]
                    {
                        item.OrderLabel,
                        item.TableLabel,
                        item.RequestedPaymentLabel,
                        item.AppliedPaymentLabel,
                        item.TotalLabel,
                        item.ContextLabel
                    }).ToList(),
                    "Nenhuma divergencia de forma de pagamento.");
                ComposeSimpleTable(
                    column.Item(),
                    "Pedidos apagados hoje",
                    ["Pedido", "Mesa", "Pagamento", "Total", "Apagado em", "Motivo", "Itens"],
                    _data.DeletedOrders.Select(item => new[]
                    {
                        item.OrderLabel,
                        item.TableLabel,
                        item.PaymentLabel,
                        item.TotalLabel,
                        item.DeletedAtLabel,
                        item.ReasonLabel,
                        item.ItemsLabel
                    }).ToList(),
                    "Nenhum pedido apagado no fluxo de hoje.");
            });

            page.Footer().AlignCenter().DefaultTextStyle(text => text.FontSize(8).FontColor("#7B6A5E")).Text(text =>
            {
                text.Span("ZeroPaper | pagina ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeMetricGrid(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("Resumo do dia").FontSize(12).SemiBold().FontColor("#241A14");
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                foreach (var metric in _data.Metrics)
                {
                    table.Cell()
                        .Padding(10)
                        .Background("#F6F0E9")
                        .Border(1)
                        .BorderColor("#E0D1C2")
                        .Column(inner =>
                        {
                            inner.Spacing(4);
                            inner.Item().Text(metric.Label).FontSize(8).SemiBold().FontColor("#7B5E4C");
                            inner.Item().Text(metric.Value).FontSize(14).SemiBold().FontColor("#241A14");

                            if (!string.IsNullOrWhiteSpace(metric.Detail))
                            {
                                inner.Item().Text(metric.Detail).FontSize(8).FontColor("#6C5A4F");
                            }
                        });
                }
            });
        });
    }

    private void ComposeSimpleTable(
        IContainer container,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]> rows,
        string emptyCopy)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text(title).FontSize(12).SemiBold().FontColor("#241A14");

            if (rows.Count == 0)
            {
                column.Item().Padding(10).Background("#F9F5F1").Border(1).BorderColor("#E4D8CE").Text(emptyCopy).FontColor("#6C5A4F");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var title in headers)
                    {
                        header.Cell().Element(HeaderCell).Text(title).SemiBold().FontColor("#3A2B22");
                    }
                });

                foreach (var row in rows)
                {
                    foreach (var value in row)
                    {
                        table.Cell().Element(BodyCell).Text(value ?? string.Empty).FontColor("#3A2B22");
                    }
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background("#EFE3D8")
            .Border(1)
            .BorderColor("#D7C3B3")
            .PaddingVertical(6)
            .PaddingHorizontal(5);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#E5D7CA")
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }

    private static string BuildOrderDetails(DailyCashReportOrderRow item)
    {
        if (string.IsNullOrWhiteSpace(item.NotesLabel))
        {
            return item.ItemsLabel;
        }

        return $"{item.ItemsLabel}\nObs: {item.NotesLabel}";
    }
}
