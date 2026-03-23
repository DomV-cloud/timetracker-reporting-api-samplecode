using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Newtonsoft.Json;
using TimetrackerReportingClient.Api;
using TimetrackerReportingClient.Api.Models;
using TimetrackerReportingClient.Fakturoid;
using TimetrackerReportingClient.Fakturoid.Models;

namespace TimetrackerReportingClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLineOptions cmd = null;
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(x => cmd = x)
                .WithNotParsed(_ =>
                {
                    Console.WriteLine("See --help for usage.");
                    Environment.Exit(1);
                });

            // Load appsettings.json; CLI args override if provided
            var settings = AppSettings.Load();
            var baseUrl = !string.IsNullOrEmpty(cmd.BaseUrl) ? cmd.BaseUrl : settings.BaseUrl;
            var token   = !string.IsNullOrEmpty(cmd.Token)   ? cmd.Token   : settings.Token;

            if (string.IsNullOrEmpty(baseUrl))
            {
                Console.WriteLine("Error: Base URL is required. Set it in appsettings.json or pass it as the first argument.");
                Environment.Exit(1);
            }
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Error: Token is required. Set it in appsettings.json or pass it with -t.");
                Environment.Exit(1);
            }

            var (year, month) = cmd.Month > 0 && cmd.Year > 0
                ? (cmd.Year, cmd.Month)
                : AskForMonth();

            var firstDay = new DateTime(year, month, 1);
            var lastDay  = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            Console.WriteLine($"Reporting period: {firstDay:dd.MM.yyyy} – {lastDay:dd.MM.yyyy}");

            var client = new TimePaceApiClient(baseUrl, token);

            List<WorkLog> logs;
            try
            {
                logs = client.GetWorkLogsForMonth(year, month, cmd.AllUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching work logs: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            if (logs.Count == 0)
            {
                Console.WriteLine("No work logs found for the selected period.");
                return;
            }

            PrintSummary(logs, year, month);

            if (!string.IsNullOrEmpty(cmd.Format))
                Export(cmd.Format, logs, year, month);

            Console.WriteLine();
            Console.Write("Create invoice in Fakturoid? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
                CreateFakturoidInvoice(logs, year, month, settings);

            Console.ReadLine();
        }

        private static void PrintSummary(List<WorkLog> logs, int year, int month)
        {
            Console.WriteLine();
            Console.WriteLine($"=== Work Log Summary — {new DateTime(year, month, 1):MMMM yyyy} ===");
            Console.WriteLine();

            // Group by date, sum hours per day
            var byDay = logs
                .GroupBy(l => l.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalHours = g.Sum(l => l.Hours),
                    Entries = g.Count()
                })
                .ToList();

            Console.WriteLine($"{"Date",-14} {"Entries",8} {"Hours",10}");
            Console.WriteLine(new string('-', 36));

            foreach (var day in byDay)
            {
                Console.WriteLine($"{day.Date:yyyy-MM-dd,-14} {day.Entries,8} {day.TotalHours,10:F2}");
            }

            Console.WriteLine(new string('-', 36));

            double totalHours = logs.Sum(l => l.Hours);
            Console.WriteLine($"{"TOTAL",-14} {logs.Count,8} {totalHours,10:F2}");
            Console.WriteLine();
        }

        private static void Export(string format, List<WorkLog> logs, int year, int month)
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fileName = $"worklogs_{year}_{month:D2}";

            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var path = Path.Combine(dir, fileName + ".json");
                File.WriteAllText(path, JsonConvert.SerializeObject(logs, Formatting.Indented));
                Console.WriteLine($"Exported to {path}");
            }
            else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var path = Path.Combine(dir, fileName + ".csv");
                var lines = new List<string> { "Date,WorkItemId,Hours,Comment,Billable" };
                lines.AddRange(logs.Select(l =>
                    $"{l.Timestamp:yyyy-MM-dd},{l.WorkItemId},{l.Hours:F4},{EscapeCsv(l.Comment)},{l.Billable}"));
                File.WriteAllLines(path, lines);
                Console.WriteLine($"Exported to {path}");
            }
            else
            {
                Console.WriteLine($"Unknown export format '{format}'. Use 'json' or 'csv'.");
            }
        }

        private static void CreateFakturoidInvoice(List<WorkLog> logs, int year, int month, AppSettings settings)
        {
            if (string.IsNullOrEmpty(settings.FakturoidSlug) ||
                string.IsNullOrEmpty(settings.FakturoidClientId) ||
                string.IsNullOrEmpty(settings.FakturoidClientSecret))
            {
                Console.WriteLine("Error: Fakturoid credentials missing in appsettings.json.");
                return;
            }
            if (settings.FakturoidSubjectId == 0)
            {
                Console.WriteLine("Error: fakturoidSubjectId is not set in appsettings.json.");
                return;
            }

            // Issue date = last day of the reported month
            var issuedOn = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var dueOn    = issuedOn.AddDays(settings.DueDays);

            // Group work logs by work item, sum hours per item
            var lines = logs
                .GroupBy(l => l.WorkItemId)
                .Select(g => new InvoiceLine
                {
                    Name      = $"#{g.Key}",
                    Quantity  = Math.Round(g.Sum(l => l.Hours), 2).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    Unit      = "hod",
                    UnitPrice = settings.HourlyRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    VatRate   = 0
                })
                .OrderByDescending(l => double.Parse(l.Quantity, System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            var invoice = new InvoiceRequest
            {
                SubjectId     = settings.FakturoidSubjectId,
                IssuedOn      = issuedOn.ToString("yyyy-MM-dd"),
                DueOn         = dueOn.ToString("yyyy-MM-dd"),
                PaymentMethod = "bank",
                Lines         = lines
            };

            Console.WriteLine($"\nCreating invoice in Fakturoid...");
            Console.WriteLine($"  Issue date : {issuedOn:dd.MM.yyyy}");
            Console.WriteLine($"  Due date   : {dueOn:dd.MM.yyyy}");
            Console.WriteLine($"  Lines      : {lines.Count}");
            Console.WriteLine($"  Total      : {lines.Sum(l => double.Parse(l.Quantity, System.Globalization.CultureInfo.InvariantCulture) * (double)settings.HourlyRate):N2} Kč");

            try
            {
                var client = new FakturoidClient(
                    settings.FakturoidSlug,
                    settings.FakturoidClientId,
                    settings.FakturoidClientSecret);

                var location = client.CreateInvoice(invoice);
                Console.WriteLine($"  Invoice created: {location}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static (int Year, int Month) AskForMonth()
        {
            while (true)
            {
                Console.Write("Which month do you need? (e.g. April): ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                // Try parsing as a month name (English)
                if (DateTime.TryParseExact(input, "MMMM",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var parsed))
                {
                    return (DateTime.Today.Year, parsed.Month);
                }

                // Also accept short names: Jan, Feb, Mar...
                if (DateTime.TryParseExact(input, "MMM",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out parsed))
                {
                    return (DateTime.Today.Year, parsed.Month);
                }

                // Also accept a number: 4 or 04
                if (int.TryParse(input, out int monthNumber) && monthNumber >= 1 && monthNumber <= 12)
                    return (DateTime.Today.Year, monthNumber);

                Console.WriteLine($"  Could not parse '{input}' as a month. Try: April, Apr, or 4.");
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
