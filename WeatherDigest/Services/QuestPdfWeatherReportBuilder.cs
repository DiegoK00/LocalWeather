using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WeatherDigest.Models;

namespace WeatherDigest.Services;

public sealed class QuestPdfWeatherReportBuilder : IWeatherReportPdfBuilder
{
    private static readonly CultureInfo It = new("it-IT");

    public byte[] Build(WeatherDigestResult result)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Bollettino meteo – {result.LocationName}")
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text(
                        $"Generato il {result.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm} • " +
                        $"Media dei modelli: {string.Join(", ", result.Models)}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(12);
                    foreach (var day in result.Days)
                        col.Item().Element(c => RenderDay(c, day));
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Fonte dati: Open-Meteo (media multi-modello) • ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void RenderDay(IContainer container, DailySummary day)
    {
        container.Column(col =>
        {
            col.Item().Text(day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd d MMMM yyyy", It))
                .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

            col.Item().Text(
                $"Min {day.MinTemperatureC:0.#}°C  •  Max {day.MaxTemperatureC:0.#}°C  •  " +
                $"Precipitazioni totali {day.TotalPrecipitationMm:0.#} mm")
                .FontSize(10).SemiBold();

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Fascia (3h)");
                    HeaderCell(header, "Temp (°C)");
                    HeaderCell(header, "Pioggia (mm)");
                    HeaderCell(header, "N. modelli");
                });

                foreach (var h in day.Hours)
                {
                    BodyCell(table, h.Hour.ToString("HH:mm"));
                    BodyCell(table, h.TemperatureC.ToString("0.#", It));
                    BodyCell(table, h.PrecipitationMm.ToString("0.##", It));
                    BodyCell(table, h.SourceCount.ToString());
                }
            });
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text) =>
        header.Cell().Background(Colors.Blue.Darken3).Padding(4)
            .Text(text).FontColor(Colors.White).Bold().FontSize(9);

    private static void BodyCell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
            .Text(text).FontSize(9);
}
