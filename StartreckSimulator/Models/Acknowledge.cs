using Newtonsoft.Json;

namespace StartreckSimulator.Models
{
    public class Acknowledge : RootObject
    {
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "requestId")]
        public string RequestId { get; set; }
    }
}