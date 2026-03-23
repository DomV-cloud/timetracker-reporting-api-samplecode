using System;
using Newtonsoft.Json;

namespace TimetrackerReportingClient.Api.Models
{
    public class WorkLog
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// When the work was performed (UTC).
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Duration of the work log in seconds.
        /// </summary>
        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("billable")]
        public bool Billable { get; set; }

        /// <summary>
        /// Azure DevOps work item ID.
        /// </summary>
        [JsonProperty("tfsId")]
        public int WorkItemId { get; set; }

        [JsonProperty("activityTypeId")]
        public Guid? ActivityTypeId { get; set; }

        [JsonProperty("userId")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Duration as decimal hours (Length / 3600).
        /// </summary>
        public double Hours => Length / 3600.0;
    }
}
