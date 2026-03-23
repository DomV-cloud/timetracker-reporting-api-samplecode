using System;
using System.IO;
using Newtonsoft.Json;

namespace TimetrackerReportingClient
{
    public class AppSettings
    {
        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

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
