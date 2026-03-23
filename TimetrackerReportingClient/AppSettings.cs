using System;
using System.IO;
using Newtonsoft.Json;

namespace TimetrackerReportingClient
{
    public class AppSettings
    {
        // ── 7pace ────────────────────────────────────────────────────────────────
        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        // ── Fakturoid ────────────────────────────────────────────────────────────
        [JsonProperty("fakturoidSlug")]
        public string FakturoidSlug { get; set; }

        [JsonProperty("fakturoidClientId")]
        public string FakturoidClientId { get; set; }

        [JsonProperty("fakturoidClientSecret")]
        public string FakturoidClientSecret { get; set; }

        /// <summary>
        /// Fakturoid subject ID of the customer to invoice.
        /// Find it in Fakturoid under Contacts — the ID is in the URL when you open a contact.
        /// </summary>
        [JsonProperty("fakturoidSubjectId")]
        public int FakturoidSubjectId { get; set; }

        /// <summary>
        /// Hourly rate in CZK (e.g. 500).
        /// </summary>
        [JsonProperty("hourlyRate")]
        public decimal HourlyRate { get; set; } = 500;

        /// <summary>
        /// Number of days until invoice is due (e.g. 14).
        /// </summary>
        [JsonProperty("dueDays")]
        public int DueDays { get; set; } = 14;

        /// <summary>
        /// Loads appsettings.json from the same directory as the executable.
        /// Returns an empty instance (not null) if the file does not exist.
        /// </summary>
        public static AppSettings Load()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(dir, "appsettings.json");

            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
    }
}
