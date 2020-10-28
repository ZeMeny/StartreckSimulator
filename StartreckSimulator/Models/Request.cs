using Newtonsoft.Json;

namespace StartreckSimulator.Models
{
    public class Request
    {
        [JsonProperty(PropertyName = "command")]
        public string Command { get; set; }

        [JsonProperty(PropertyName = "sensorId")]
        public string SensorId { get; set; }

        [JsonProperty(PropertyName = "missionId")]
        public int MissionId { get; set; }

        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }
    }
}