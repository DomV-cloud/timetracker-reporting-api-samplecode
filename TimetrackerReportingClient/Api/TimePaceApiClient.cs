using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;
using TimetrackerReportingClient.Api.Models;

namespace TimetrackerReportingClient.Api
{
    /// <summary>
    /// Client for the 7pace Timetracker REST API v3.x.
    ///
    /// Base URL format:
    ///   Azure DevOps Services: https://{your-org}.timehub.7pace.com/
    ///   Azure DevOps Server:   https://{server}/tfs/{collection}/{project}/_apis/timetracker/
    ///
    /// Authentication: Personal Access Token (PAT) or OAuth token from Azure DevOps.
    /// </summary>
    public class TimePaceApiClient
    {
        private const int PageSize = 500;
        private const string ApiVersion = "3.2";

        private readonly RestClient _client;

        public TimePaceApiClient(string baseUrl, string token)
        {
            _client = new RestClient(baseUrl.TrimEnd('/'));
            _client.AddDefaultHeader("Authorization", "Bearer " + token);
        }

        /// <summary>
        /// Downloads all work logs for the given month.
        /// Handles pagination automatically (API returns max 500 items per page).
        /// </summary>
        /// <param name="year">Year of the target month.</param>
        /// <param name="month">Month number (1–12).</param>
        /// <param name="allUsers">
        ///   When true, fetches logs for all users (requires Product/Budget/Administrator role).
        ///   When false, fetches only the authenticated user's logs.
        /// </param>
        public List<WorkLog> GetWorkLogsForMonth(int year, int month, bool allUsers = false)
        {
            var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = fromDate.AddMonths(1);

            var endpoint = allUsers ? "api/rest/workLogs/all" : "api/rest/workLogs";
            var allLogs = new List<WorkLog>();
            int skip = 0;

            Console.WriteLine($"Fetching work logs for {fromDate:MMMM yyyy}...");

            while (true)
            {
                var request = new RestRequest(endpoint, Method.GET);
                request.AddQueryParameter("$fromTimestamp", fromDate.ToString("o"));
                request.AddQueryParameter("$toTimestamp", toDate.ToString("o"));
                request.AddQueryParameter("$count", PageSize.ToString());
                request.AddQueryParameter("$skip", skip.ToString());
                request.AddQueryParameter("api-version", ApiVersion);

                var response = _client.Execute(request);

                if (!response.IsSuccessful)
                {
                    throw new Exception(
                        $"API request failed [{response.StatusCode}]: {response.Content}");
                }

                var result = JsonConvert.DeserializeObject<ApiResponse<List<WorkLog>>>(response.Content);

                if (result?.Error != null)
                {
                    throw new Exception(
                        $"API error [{result.Error.Code}]: {result.Error.Message}");
                }

                if (result?.Data == null || result.Data.Count == 0)
                    break;

                allLogs.AddRange(result.Data);
                Console.WriteLine($"  Retrieved {allLogs.Count} entries so far...");

                if (result.Data.Count < PageSize)
                    break;

                skip += PageSize;
            }

            Console.WriteLine($"  Total: {allLogs.Count} work log entries.");
            return allLogs;
        }
    }
}