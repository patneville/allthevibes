using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RMoney
{
    public static class Exporter
    {
        public static void ExportCsv(string path, UserState state)
        {
            Flow.ComputePlan(state);

            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("Done");
            csv.WriteField("Priority");
            csv.WriteField("Action");
            csv.WriteField("Reasons");
            csv.NextRecord();

            foreach (var item in state.Plan)
            {
                csv.WriteField(item.Done ? "Yes" : "No");
                csv.WriteField(item.Priority);
                csv.WriteField(item.Text);
                csv.WriteField(string.Join(" | ", item.Reasons));
                csv.NextRecord();
            }
        }

        public static void ExportPdf(string path, UserState state)
        {
            Flow.ComputePlan(state);

            // QuestPDF requirement (Community license)
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(36);

                    // ---------- Header ----------
                    page.Header().Column(h =>
                    {
                        h.Spacing(4);

                        h.Item()
                            .Text("r-money — Action Plan")
                            .FontSize(20)
                            .SemiBold()
                            .FontColor(Colors.Teal.Medium);

                        h.Item()
                            .Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    // ---------- Content ----------
                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Spacing(6);

                        // Notes block (optional but helps spacing/context)
                        if (state.Notes.Any())
                        {
                            col.Item().Text("Key notes").FontSize(12).SemiBold();
                            col.Item().PaddingLeft(6).Column(notesCol =>
                            {
                                notesCol.Spacing(2);
                                foreach (var n in state.Notes)
                                {
                                    notesCol.Item()
                                        .Text("• " + n)
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken2);
                                }
                            });

                            col.Item().PaddingTop(8);
                        }

                        // Checklist block
                        col.Item().Text("Checklist").FontSize(12).SemiBold();

                        col.Item().PaddingTop(4).Column(list =>
                        {
                            list.Spacing(8);

                            int i = 1;
                            foreach (var item in state.Plan)
                            {
                                list.Item().Column(card =>
                                {
                                    card.Spacing(3);

                                    var box = item.Done ? "☑" : "☐";

                                    card.Item()
                                        .Text($"{box} {i}. {item.Text}")
                                        .FontSize(10)
                                        .SemiBold();

                                    if (item.Reasons.Any())
                                    {
                                        card.Item().PaddingLeft(18).Column(reasonCol =>
                                        {
                                            reasonCol.Spacing(2);

                                            foreach (var r in item.Reasons)
                                            {
                                                reasonCol.Item()
                                                    .Text("• " + r)
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken2);
                                            }
                                        });
                                    }
                                });

                                i++;
                            }
                        });
                    });

                    // ---------- Footer ----------
                    // IMPORTANT for QuestPDF 2025.x:
                    // The Text(Action<...>) overload returns void, so you must NOT chain after it.
                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken2))
                        .PaddingTop(8)
                        .Text(t =>
                        {
                            t.Span("r-money • Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                });
            });

            doc.GeneratePdf(path);
        }
    }
}

