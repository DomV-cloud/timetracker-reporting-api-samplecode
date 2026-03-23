using Newtonsoft.Json;

namespace TimetrackerReportingClient.Api.Models
{
    /// <summary>
    /// Standard 7pace API envelope: { "data": T } on success, { "error": ... } on failure.
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("error")]
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }
}
