using Newtonsoft.Json;

namespace StartreckSimulator.Models
{
    public class Response : RootObject
    {
        [JsonProperty(PropertyName = "sensorId")]
        public string SensorId { get; set; }

        [JsonProperty(PropertyName = "missionId")]
        public int MissionId { get; set; }

        [JsonProperty(PropertyName = "classifications")]
        public Classification[] Classifications { get; set; }

        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }

        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}