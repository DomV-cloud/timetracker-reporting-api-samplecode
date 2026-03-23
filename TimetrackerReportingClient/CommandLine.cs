using CommandLine;

namespace TimetrackerReportingClient
{
    public class CommandLineOptions
    {
        [Value(0, Required = false, HelpText = "Base URL for the 7pace Timetracker API. Overrides appsettings.json.\n" +
            "Azure DevOps Services: https://{your-org}.timehub.7pace.com/\n" +
            "Azure DevOps Server:   https://{server}/tfs/{collection}/{project}/_apis/timetracker/")]
        public string BaseUrl { get; set; }

        [Option('t', "token", Required = false, HelpText = "Bearer token for 7pace API authentication. Overrides appsettings.json.")]
        public string Token { get; set; }

        [Option('m', "month", Default = 0, HelpText = "Month to report on (1-12). Defaults to previous month.")]
        public int Month { get; set; }

        [Option('y', "year", Default = 0, HelpText = "Year to report on. Defaults to current year (or previous year if month is December).")]
        public int Year { get; set; }

        [Option('a', "all-users", Default = false, HelpText = "Fetch work logs for all users (requires Product/Budget/Administrator role).")]
        public bool AllUsers { get; set; }

        [Option('x', "export", Default = null, HelpText = "Export format: json or csv. Omit to skip export.")]
        public string Format { get; set; }

    }
}
