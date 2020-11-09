using Newtonsoft.Json;

namespace StartreckSimulator.Models
{
    public class RootObject
    {
        [JsonProperty(PropertyName = "messageType")]
        public MessageType MessageType { get; set; }
    }
}